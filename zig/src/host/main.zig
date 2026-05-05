//! bloog-host — out-of-process bot host.
//!
//! Connects to bloog-stub via named pipe, enumerates game objects,
//! and runs bot logic. This is the main entry point for the host process.

const std = @import("std");
const builtin = @import("builtin");
const log = @import("log");
const win = @import("winapi");
const stub_client = @import("stub_client.zig");
const obj_mgr = @import("object_manager.zig");
const game = @import("game_objects.zig");

comptime {
    if (builtin.os.tag != .windows) {
        @compileError("bloog-host only supports Windows targets (x86-windows-gnu).");
    }
}

pub fn main(init: std.process.Init) !void {
    const allocator = init.arena.allocator();

    log.info("[bloog-host] starting...", .{});

    // Initialize object manager.
    obj_mgr.init(allocator);

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

    // Main loop: enumerate objects periodically.
    log.info("[bloog-host] entering main loop (Ctrl+C to exit)", .{});

    var iteration: u32 = 0;
    while (true) {
        iteration += 1;

        // Poll for objects.
        obj_mgr.poll(&client, allocator) catch |err| {
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

        // Sleep 500ms between polls (same as C# ObjectManager.StartEnumeration).
        win.Sleep(500);
    }
}
