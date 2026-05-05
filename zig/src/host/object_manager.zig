//! Object manager — polls the stub for visible objects and builds enriched structs.
//!
//! Port of BloogBot/Game/ObjectManager.cs:216-318.
//! The host periodically calls enumerateObjects via the stub client,
//! then reads additional fields for each object to build WoWUnit/WoWPlayer structs.

const std = @import("std");
const log = @import("log");
const rpc = @import("rpc_types");
const game = @import("game_objects.zig");
const addr = @import("memory_addresses.zig");
const StubClient = @import("stub_client.zig");

// ---------------------------------------------------------------------------
// Object list state
// ---------------------------------------------------------------------------

const ArrayListManaged = std.array_list.AlignedManaged;

var objects: ArrayListManaged(game.WoWObject, null) = undefined;
var units: ArrayListManaged(game.WoWUnit, null) = undefined;
var players: ArrayListManaged(game.WoWPlayer, null) = undefined;

var initialized = false;

pub fn init(allocator: std.mem.Allocator) void {
    if (initialized) return;
    objects = ArrayListManaged(game.WoWObject, null).init(allocator);
    units = ArrayListManaged(game.WoWUnit, null).init(allocator);
    players = ArrayListManaged(game.WoWPlayer, null).init(allocator);
    initialized = true;
}

pub fn deinit() void {
    if (!initialized) return;
    objects.deinit();
    units.deinit();
    players.deinit();
    initialized = false;
}

// ---------------------------------------------------------------------------
// Public accessors
// ---------------------------------------------------------------------------

pub fn allObjects() []const game.WoWObject {
    return objects.items;
}

pub fn allUnits() []const game.WoWUnit {
    return units.items;
}

pub fn allPlayers() []const game.WoWPlayer {
    return players.items;
}

// ---------------------------------------------------------------------------
// Enumeration
// ---------------------------------------------------------------------------

/// Poll the stub for visible objects and rebuild the local lists.
/// Must be called periodically (e.g., every 500ms) from the host main loop.
pub fn poll(client: *StubClient, allocator: std.mem.Allocator) !void {
    // Clear previous data.
    objects.clearRetainingCapacity();
    units.clearRetainingCapacity();
    players.clearRetainingCapacity();

    // Ask the stub to enumerate objects via the game's EnumerateVisibleObjects.
    const resp = client.enumerateObjects(
        allocator,
        addr.EnumerateVisibleObjectsFunPtr,
        addr.GetObjectPtrFunPtr,
    ) catch |err| {
        log.warn("[obj-mgr] enumerate failed: {s}", .{@errorName(err)});
        return err;
    };
    defer allocator.free(resp);

    // Parse response: [count: u32][ObjectInfo...]
    if (resp.len < 4) return;
    const count = std.mem.readInt(u32, resp[0..4], .little);

    const obj_size = @sizeOf(rpc.ObjectInfo);
    const expected_len = 4 + count * obj_size;
    if (resp.len < expected_len) {
        log.warn("[obj-mgr] truncated response: got {d}, expected {d}", .{ resp.len, expected_len });
        return;
    }

    const obj_infos: [*]const rpc.ObjectInfo = @ptrCast(@alignCast(resp[4..].ptr));

    for (obj_infos[0..count]) |info| {
        const obj = game.WoWObject{
            .guid = info.guid,
            .object_type = @enumFromInt(info.object_type),
            .base_ptr = info.descriptor_ptr,
        };
        objects.append(obj) catch continue;

        // Enrich specific object types.
        switch (obj.object_type) {
            .unit => {
                if (enrichUnit(client, allocator, obj)) |unit| {
                    units.append(unit) catch {};
                }
            },
            .player => {
                if (enrichPlayer(client, allocator, obj, false)) |player| {
                    players.append(player) catch {};
                }
            },
            else => {},
        }
    }
}

// ---------------------------------------------------------------------------
// Enrichment — read additional fields for specific object types
// ---------------------------------------------------------------------------

fn readDescU32(client: *StubClient, allocator: std.mem.Allocator, base_ptr: usize, desc_offset: usize) ?u32 {
    // descriptor_ptr = *(base_ptr + WoWObject_DescriptorOffset)
    const desc_ptr = client.readU32(allocator, base_ptr + addr.WoWObject_DescriptorOffset) catch return null;
    if (desc_ptr == 0) return null;
    return client.readU32(allocator, desc_ptr + desc_offset) catch return null;
}

fn readDescU64(client: *StubClient, allocator: std.mem.Allocator, base_ptr: usize, desc_offset: usize) ?u64 {
    const desc_ptr = client.readU32(allocator, base_ptr + addr.WoWObject_DescriptorOffset) catch return null;
    if (desc_ptr == 0) return null;
    return client.readU64(allocator, desc_ptr + desc_offset) catch return null;
}

fn enrichUnit(client: *StubClient, allocator: std.mem.Allocator, obj: game.WoWObject) ?game.WoWUnit {
    const desc_ptr = client.readU32(allocator, obj.base_ptr + addr.WoWObject_DescriptorOffset) catch return null;
    if (desc_ptr == 0) return null;

    const health = client.readU32(allocator, desc_ptr + addr.WoWUnit_HealthOffset) catch 0;
    const max_health = client.readU32(allocator, desc_ptr + addr.WoWUnit_MaxHealthOffset) catch 0;
    const mana = client.readU32(allocator, desc_ptr + addr.WoWUnit_ManaOffset) catch 0;
    const max_mana = client.readU32(allocator, desc_ptr + addr.WoWUnit_MaxManaOffset) catch 0;
    const rage = client.readU32(allocator, desc_ptr + addr.WoWUnit_RageOffset) catch 0;
    const energy = client.readU32(allocator, desc_ptr + addr.WoWUnit_EnergyOffset) catch 0;
    const level = client.readU32(allocator, desc_ptr + addr.WoWUnit_LevelOffset) catch 0;
    const faction_id = client.readU32(allocator, desc_ptr + addr.WoWUnit_FactionIdOffset) catch 0;
    const target_guid = client.readU64(allocator, desc_ptr + addr.WoWUnit_TargetGuidOffset) catch 0;
    const summoned_by = client.readU64(allocator, desc_ptr + addr.WoWUnit_SummonedByGuidOffset) catch 0;
    const unit_flags = client.readU32(allocator, desc_ptr + addr.WoWUnit_UnitFlagsOffset) catch 0;
    const spellcast = client.readU32(allocator, obj.base_ptr + addr.WoWUnit_CurrentSpellcastOffset) catch 0;
    const channeling = client.readU32(allocator, obj.base_ptr + addr.WoWUnit_CurrentChannelingOffset) catch 0;

    return game.WoWUnit{
        .guid = obj.guid,
        .base_ptr = obj.base_ptr,
        .descriptor_ptr = desc_ptr,
        .health = @intCast(health),
        .max_health = @intCast(max_health),
        .mana = @intCast(mana),
        .max_mana = @intCast(max_mana),
        .rage = @intCast(rage),
        .energy = @intCast(energy),
        .level = @intCast(level),
        .faction_id = @intCast(faction_id),
        .target_guid = target_guid,
        .summoned_by_guid = summoned_by,
        .position = .{ .x = 0, .y = 0, .z = 0 }, // TODO: read via vtable
        .facing = 0,
        .is_in_combat = (unit_flags & 0x0008) != 0,
        .is_casting = spellcast > 0,
        .is_channeling = channeling > 0,
        .name = std.mem.zeroes([64]u8),
        .name_len = 0,
    };
}

fn enrichPlayer(client: *StubClient, allocator: std.mem.Allocator, obj: game.WoWObject, is_local: bool) ?game.WoWPlayer {
    const unit = enrichUnit(client, allocator, obj) orelse return null;
    return game.WoWPlayer{
        .guid = unit.guid,
        .base_ptr = unit.base_ptr,
        .descriptor_ptr = unit.descriptor_ptr,
        .health = unit.health,
        .max_health = unit.max_health,
        .mana = unit.mana,
        .max_mana = unit.max_mana,
        .rage = unit.rage,
        .energy = unit.energy,
        .level = unit.level,
        .faction_id = unit.faction_id,
        .target_guid = unit.target_guid,
        .summoned_by_guid = unit.summoned_by_guid,
        .position = unit.position,
        .facing = unit.facing,
        .is_in_combat = unit.is_in_combat,
        .is_casting = unit.is_casting,
        .is_channeling = unit.is_channeling,
        .name = unit.name,
        .name_len = unit.name_len,
        .is_local_player = is_local,
    };
}
