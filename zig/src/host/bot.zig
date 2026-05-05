//! Bot state machine — stack-based state pattern ported from BloogBot/AI/Bot.cs.
//!
//! The bot uses a stack of states. Each tick, the top state's `update()` is called.
//! States can push new states (sub-states) or pop themselves (done).
//! The main loop in Bot ticks at ~50ms, same as the C# original.
//!
//! State types:
//!   Grind      — find target or wander hotspot waypoints
//!   MoveToTarget — navigate to target, pull when in range
//!   Combat     — execute rotation until target dead
//!   Loot       — move to corpse, loot items
//!   Rest       — eat/drink until health/mana restored

const std = @import("std");
const win = @import("winapi");
const log = @import("log");
const game = @import("game_objects.zig");
const addr = @import("memory_addresses.zig");
const StubClient = @import("stub_client.zig");
const obj_mgr = @import("object_manager.zig");
const nav = @import("navigation.zig");
const hotspot_mod = @import("hotspot.zig");

// ---------------------------------------------------------------------------
// State types — tagged union for the state stack
// ---------------------------------------------------------------------------

pub const BotState = union(enum) {
    grind: GrindState,
    move_to_target: MoveToTargetState,
    combat: CombatState,
    loot: LootState,
    rest: RestState,

    pub fn update(self: *BotState, ctx: *BotContext) void {
        switch (self.*) {
            inline else => |*s| s.update(ctx),
        }
    }

    pub fn stateName(self: BotState) []const u8 {
        return @tagName(self);
    }
};

// ---------------------------------------------------------------------------
// Bot context — shared state across all bot states
// ---------------------------------------------------------------------------

pub const BotContext = struct {
    client: *StubClient,
    allocator: std.mem.Allocator,
    /// Stack of states. Top of stack is the active state.
    state_stack: std.array_list.AlignedManaged(BotState, null),
    /// Current hotspot (grinding area).
    hotspot: *const hotspot_mod.Hotspot,
    /// Tick counter for timing.
    tick: u32,
    /// Timestamps (ms since start) for stuck detection.
    state_start_ms: u32,
    /// Whether the bot is running.
    running: bool,

    pub fn pushState(self: *BotContext, state: BotState) void {
        self.state_stack.append(state) catch {};
        self.state_start_ms = self.tick;
        log.info("[bot] -> {s}", .{@tagName(state)});
    }

    pub fn popState(self: *BotContext) void {
        if (self.state_stack.pop()) |popped| {
            log.info("[bot] <- {s}", .{@tagName(popped)});
        }
    }

    pub fn currentState(self: *BotContext) ?*BotState {
        if (self.state_stack.items.len == 0) return null;
        return &self.state_stack.items[self.state_stack.items.len - 1];
    }

    pub fn popToBase(self: *BotContext) void {
        while (self.state_stack.items.len > 1) {
            _ = self.state_stack.pop();
        }
    }
};

// ---------------------------------------------------------------------------
// GrindState — find target or wander to random waypoint
// Port of BloogBot/AI/SharedStates/GrindState.cs
// ---------------------------------------------------------------------------

pub const GrindState = struct {
    pub fn update(self: *GrindState, ctx: *BotContext) void {
        _ = self;

        // Look for a target.
        const target_guid = findTarget() catch {
            // No target found — wander to a random waypoint.
            wanderToWaypoint(ctx);
            return;
        };

        if (target_guid != 0) {
            // Found a target — push MoveToTarget state.
            ctx.pushState(.{ .move_to_target = .{
                .target_guid = target_guid,
                .start_tick = ctx.tick,
            } });
        } else {
            wanderToWaypoint(ctx);
        }
    }

    fn findTarget() !u64 {
        const units = obj_mgr.allUnits();
        var best_guid: u64 = 0;
        var best_dist: f32 = 999999.0;

        // Find closest hostile, alive, non-tapped unit.
        for (units) |unit| {
            if (unit.health <= 0) continue;
            if (unit.is_in_combat and unit.target_guid != 0) continue; // tapped by other
            // TODO: check faction for hostility
            // For now, just pick the closest alive unit.
            const dist = distApprox(unit.position, .{ .x = 0, .y = 0, .z = 0 }); // TODO: use player position
            if (dist < best_dist) {
                best_dist = dist;
                best_guid = unit.guid;
            }
        }
        return best_guid;
    }

    fn wanderToWaypoint(ctx: *BotContext) void {
        const waypoints = ctx.hotspot.waypoints;
        if (waypoints.len == 0) return;
        const idx = ctx.tick % @as(u32, @intCast(waypoints.len));
        ctx.pushState(.{ .move_to_target = .{
            .target_guid = 0, // 0 = move to waypoint
            .waypoint = waypoints[idx],
            .start_tick = ctx.tick,
        } });
    }
};

// ---------------------------------------------------------------------------
// MoveToTargetState — navigate to target, pull when in range
// Port of FrostMageBot/MoveToTargetState.cs
// ---------------------------------------------------------------------------

pub const MoveToTargetState = struct {
    target_guid: u64,
    waypoint: ?game.Position = null,
    start_tick: u32,

    pub fn update(self: *MoveToTargetState, ctx: *BotContext) void {
        // If this is a waypoint patrol (no target), check for targets first.
        if (self.target_guid == 0) {
            // If we found a target while patrolling, switch to fighting it.
            const found = GrindState.findTarget() catch 0;
            if (found != 0) {
                ctx.popState();
                ctx.pushState(.{ .move_to_target = .{
                    .target_guid = found,
                    .start_tick = ctx.tick,
                } });
                return;
            }

            // Check if we arrived at waypoint.
            if (self.waypoint) |wp| {
                // TODO: check player distance to waypoint
                _ = wp;
                // For now, pop after a few ticks.
                if (ctx.tick - self.start_tick > 20) {
                    ctx.popState();
                    return;
                }
            }
            return;
        }

        // Timeout — 30 seconds = ~600 ticks at 50ms
        if (ctx.tick - self.start_tick > 600) {
            log.warn("[bot] move-to-target timed out, blacklisting target", .{});
            ctx.popState();
            return;
        }

        // TODO: get player position, check distance to target
        // TODO: if in range, pull spell then push CombatState
        // For now, just transition to combat.
        ctx.popState();
        ctx.pushState(.{ .combat = .{
            .target_guid = self.target_guid,
            .start_tick = ctx.tick,
        } });
    }
};

// ---------------------------------------------------------------------------
// CombatState — execute rotation until target dead
// Port of FrostMageBot/CombatState.cs
// ---------------------------------------------------------------------------

pub const CombatState = struct {
    target_guid: u64,
    start_tick: u32,
    frost_nova_backpedaling: bool = false,
    frost_nova_start_tick: u32 = 0,
    unstucking: bool = false,

    pub fn update(self: *CombatState, ctx: *BotContext) void {
        // Frost Nova backpedal handling.
        if (self.frost_nova_backpedaling) {
            if (ctx.tick - self.frost_nova_start_tick > 50) { // 2.5s
                self.frost_nova_backpedaling = false;
            }
            return; // don't cast during backpedal
        }

        // Check if target is dead.
        const target = findUnitByGuid(ctx, self.target_guid);
        if (target == null or target.?.health <= 0) {
            ctx.popState();
            ctx.pushState(.{ .loot = .{
                .target_guid = self.target_guid,
                .start_tick = ctx.tick,
            } });
            return;
        }

        // Stuck detection: 30 seconds without damage dealt.
        if (ctx.tick - self.start_tick > 600 and target.?.health >= 99) {
            log.warn("[bot] stuck in combat, blacklisting", .{});
            ctx.popState();
            return;
        }

        // Execute Frost Mage rotation.
        self.executeRotation(ctx, target.?);
    }

    fn executeRotation(self: *CombatState, ctx: *BotContext, target: game.WoWUnit) void {
        _ = self; // Will be used for frost nova backpedal state

        // TODO: Implement full rotation via stub RPC calls:
        // 1. TryCastSpell("Evocation") — if mana < 8% and hp > 50%
        // 2. TryCastSpell("Summon Water Elemental")
        // 3. TryCastSpell("Icy Veins") — if multiple aggressors
        // 4. TryCastSpell("Counterspell") — if target is casting
        // 5. TryCastSpell("Ice Barrier") — if under pressure
        // 6. TryCastSpell("Frost Nova") — if target close, with backpedal callback
        // 7. TryCastSpell("Deep Freeze") — if target frozen
        // 8. TryCastSpell("Ice Lance") — if target frozen or Fingers of Frost
        // 9. TryCastSpell("Fire Blast") — if target not frozen
        // 10. TryCastSpell("Frostfire Bolt") — if Brain Freeze buff
        // 11. TryCastSpell("Frostbolt") — nuke
        //
        // Each cast goes through the stub's call2/call3 fastcall shims
        // which invoke WoW's CastSpellById function pointer.
        //
        // For now this is a stub that logs the rotation attempt.
        if (ctx.tick % 20 == 0) { // every ~1s
            log.info("[bot] combat tick: target hp={d} guid={x}", .{ target.health, target.guid });
        }
    }
};

// ---------------------------------------------------------------------------
// LootState — move to corpse and loot
// Port of BloogBot/AI/SharedStates/LootState.cs
// ---------------------------------------------------------------------------

pub const LootState = struct {
    target_guid: u64,
    start_tick: u32,

    pub fn update(self: *LootState, ctx: *BotContext) void {
        // Timeout — 10 seconds = ~200 ticks.
        if (ctx.tick - self.start_tick > 200) {
            ctx.popState();
            return;
        }

        // TODO: Move to corpse position, interact, loot items.
        // For now, just pop after a short delay.
        if (ctx.tick - self.start_tick > 10) {
            ctx.popState();
            // Check if we should rest.
            const players = obj_mgr.allPlayers();
            if (players.len > 0) {
                // TODO: check player health/mana thresholds
                // For now, push rest state.
                ctx.pushState(.{ .rest = .{
                    .start_tick = ctx.tick,
                } });
            }
        }
    }
};

// ---------------------------------------------------------------------------
// RestState — eat/drink until health/mana restored
// Port of FrostMageBot/RestState.cs
// ---------------------------------------------------------------------------

pub const RestState = struct {
    start_tick: u32,

    pub fn update(self: *RestState, ctx: *BotContext) void {

        // Check if in combat — stop resting.
        const players = obj_mgr.allPlayers();
        if (players.len > 0 and players[0].is_in_combat) {
            ctx.popState();
            return;
        }

        // TODO: Check player health/mana. If both OK, pop.
        // For now, pop after ~3 seconds (60 ticks).
        if (ctx.tick - self.start_tick > 60) {
            ctx.popState();
            return;
        }
    }
};

// ---------------------------------------------------------------------------
// Helper functions
// ---------------------------------------------------------------------------

fn findUnitByGuid(ctx: *BotContext, guid: u64) ?game.WoWUnit {
    _ = ctx;
    const units = obj_mgr.allUnits();
    for (units) |unit| {
        if (unit.guid == guid) return unit;
    }
    return null;
}

fn distApprox(a: game.Position, b: game.Position) f32 {
    const dx = a.x - b.x;
    const dy = a.y - b.y;
    const dz = a.z - b.z;
    return @sqrt(dx * dx + dy * dy + dz * dz);
}

// ---------------------------------------------------------------------------
// Bot main loop — drives the state machine
// ---------------------------------------------------------------------------

pub fn run(ctx: *BotContext) void {
    log.info("[bot] starting bot loop (Ctrl+C to exit)", .{});

    // Push initial state.
    ctx.pushState(.{ .grind = .{} });

    while (ctx.running) {
        ctx.tick += 1;

        const current = ctx.currentState() orelse {
            log.warn("[bot] state stack empty, stopping", .{});
            ctx.running = false;
            break;
        };

        current.update(ctx);

        // Poll objects every tick (same as C# ObjectManager).
        obj_mgr.poll(ctx.client, ctx.allocator) catch {};

        // 50ms tick rate (same as C#).
        win.Sleep(50);
    }

    log.info("[bot] bot loop ended", .{});
}
