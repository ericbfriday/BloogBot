const std = @import("std");
const win = @import("winapi");

pub fn main(init: std.process.Init) !void {
    _ = init;
    std.debug.print("[test-target] sleeping for 30 seconds...\n", .{});
    win.Sleep(30_000);
    std.debug.print("[test-target] done\n", .{});
}
