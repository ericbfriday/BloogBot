//! Hotspot — defines a grinding area with waypoints, vendors, and level range.
//!
//! Port of BloogBot/Hotspot.cs. A hotspot is the bot's operational area.
//! The bot patrols between waypoints looking for targets, and can navigate
//! to nearby vendors for repairs/selling when needed.

const std = @import("std");
const game = @import("game_objects.zig");

pub const Hotspot = struct {
    id: u32,
    zone: []const u8,
    faction: []const u8,
    min_level: i32,
    waypoints: []const game.Position,
    safe_for_grinding: bool,
};
