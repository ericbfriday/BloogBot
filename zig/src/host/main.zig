//! bloog-host — out-of-process bot host.
//!
//! Connects to bloog-stub via named pipe, enumerates game objects,
//! and runs bot logic. This is the main entry point for the host process.
//!
//! Modes:
//!   --poll     Just enumerate objects and print counts (default).
//!   --bot      Run the full bot state machine (grind/combat/loot/rest).

const std = @import("std");
const builtin = @import("builtin");
const log = @import("log");
const win = @import("winapi");
const stub_client = @import("stub_client.zig");
const obj_mgr = @import("object_manager.zig");
const game = @import("game_objects.zig");
const bot_mod = @import("bot.zig");
const nav = @import("navigation.zig");
const hotspot_mod = @import("hotspot.zig");

comptime {
    if (builtin.os.tag != .windows) {
        @compileError("bloog-host only supports Windows targets (x86-windows-gnu).");
    }
}

// Default hotspot for testing — WotLK Elwynn Forest area.
const default_hotspot = hotspot_mod.Hotspot{
    .id = 0,
    .zone = "Elwynn Forest",
    .faction = "Alliance",
    .min_level = 1,
    .waypoints = &[_]game.Position{
        .{ .x = -9466.0, .y = -9.0, .z = 49.0 },
        .{ .x = -9430.0, .y = 65.0, .z = 56.0 },
        .{ .x = -9380.0, .y = 20.0, .z = 60.0 },
    },
    .safe_for_grinding = true,
};

pub fn main(init: std.process.Init) !void {
    const allocator = init.arena.allocator();

    log.info("[bloog-host] starting...", .{});

    // Parse mode from args.
    var mode: enum { poll, bot } = .poll;
    {
        const args = try init.minimal.args.toSlice(allocator);
        for (args) |arg| {
            if (std.mem.eql(u8, arg, "--bot")) {
                mode = .bot;
            }
        }
    }

    // Initialize object manager.
    obj_mgr.init(allocator);

    // Try to initialize navigation (non-fatal if Navigation.dll missing).
    nav.init();

    // Connect to stub.
    var client = stub_client.connect() catch |err| {
        log.err("[bloog-host] failed to connect to stub: {s}", .{@errorName(err)});
        log.err("[bloog-host] make sure bloog-stub.dll is injected into the target process", .{});
        return err;
    };
    defer client.disconnect();

    // Ping the stub to verify the connection.
    client.ping(allocator) catch |err| {
        log.err("[bloog-host] ping failed: {s}", .{@errorName(err)});
        return err;
    };
    log.info("[bloog-host] ping OK — stub is alive", .{});

    switch (mode) {
        .poll => runPollMode(allocator, &client),
        .bot => runBotMode(allocator, &client),
    }
}

fn runPollMode(allocator: std.mem.Allocator, client: *stub_client) void {
    log.info("[bloog-host] running in poll mode (Ctrl+C to exit)", .{});

    var iteration: u32 = 0;
    while (true) {
        iteration += 1;

        obj_mgr.poll(client, allocator) catch |err| {
            log.warn("[bloog-host] poll #{d} failed: {s}", .{ iteration, @errorName(err) });
            win.Sleep(1000);
            continue;
        };

        const all_objects = obj_mgr.allObjects();
        const all_units = obj_mgr.allUnits();
        const all_players = obj_mgr.allPlayers();

        if (iteration % 10 == 0) {
            log.info("[bloog-host] objects={d} units={d} players={d}", .{
                all_objects.len,
                all_units.len,
                all_players.len,
            });
        }

        win.Sleep(500);
    }
}

fn runBotMode(allocator: std.mem.Allocator, client: *stub_client) void {
    log.info("[bloog-host] running in bot mode", .{});

    const state_stack = std.array_list.AlignedManaged(bot_mod.BotState, null).init(allocator);

    var ctx = bot_mod.BotContext{
        .client = client,
        .allocator = allocator,
        .state_stack = state_stack,
        .hotspot = &default_hotspot,
        .tick = 0,
        .state_start_ms = 0,
        .running = true,
    };

    bot_mod.run(&ctx);
}
