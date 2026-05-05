//! WoW object types and game data structures.
//!
//! Port of BloogBot/Game/Objects/*.cs and BloogBot/Game/Enums/ObjectType.cs.
//! These are the types the host uses to represent game objects queried from
//! the stub via the RPC protocol.

// ---------------------------------------------------------------------------
// Object type enum (matches ObjectType.cs)
// ---------------------------------------------------------------------------

pub const ObjectType = enum(u8) {
    item = 1,
    container = 2,
    unit = 3,
    player = 4,
    game_object = 5,
    dynamic_object = 6,
    corpse = 7,
    _,
};

// ---------------------------------------------------------------------------
// 3D position
// ---------------------------------------------------------------------------

pub const Position = extern struct {
    x: f32,
    y: f32,
    z: f32,
};

pub fn formatPos(pos: Position) [48]u8 {
    var buf: [48]u8 = undefined;
    const slice = std.fmt.bufPrint(&buf, "({d:.1}, {d:.1}, {d:.1})", .{
        pos.x, pos.y, pos.z,
    }) catch "|pos truncated|";
    buf[slice.len] = 0;
    return buf;
}

// ---------------------------------------------------------------------------
// Flat WoW object — represents one enumerated object from the stub.
// The host receives an array of these from the enumerate_objects RPC.
// ---------------------------------------------------------------------------

pub const WoWObject = extern struct {
    guid: u64,
    object_type: ObjectType,
    base_ptr: usize,
};

// ---------------------------------------------------------------------------
// Enriched unit data — populated by the host reading additional fields
// through the stub's memory read RPCs.
// ---------------------------------------------------------------------------

pub const WoWUnit = struct {
    guid: u64,
    base_ptr: usize,
    descriptor_ptr: usize,
    health: i32,
    max_health: i32,
    mana: i32,
    max_mana: i32,
    rage: i32,
    energy: i32,
    level: i32,
    faction_id: i32,
    target_guid: u64,
    summoned_by_guid: u64,
    position: Position,
    facing: f32,
    is_in_combat: bool,
    is_casting: bool,
    is_channeling: bool,
    name: [64]u8,
    name_len: usize,
};

pub fn unitName(unit: *const WoWUnit) []const u8 {
    return unit.name[0..unit.name_len];
}

// ---------------------------------------------------------------------------
// Enriched player data — extends WoWUnit with player-specific fields.
// ---------------------------------------------------------------------------

pub const WoWPlayer = struct {
    guid: u64,
    base_ptr: usize,
    descriptor_ptr: usize,
    health: i32,
    max_health: i32,
    mana: i32,
    max_mana: i32,
    rage: i32,
    energy: i32,
    level: i32,
    faction_id: i32,
    target_guid: u64,
    summoned_by_guid: u64,
    position: Position,
    facing: f32,
    is_in_combat: bool,
    is_casting: bool,
    is_channeling: bool,
    name: [64]u8,
    name_len: usize,
    /// True if this is the local player (our character).
    is_local_player: bool,
};

pub fn playerName(player: *const WoWPlayer) []const u8 {
    return player.name[0..player.name_len];
}

// ---------------------------------------------------------------------------
// Enriched game object data
// ---------------------------------------------------------------------------

pub const WoWGameObject = struct {
    guid: u64,
    base_ptr: usize,
    position: Position,
    name: [64]u8,
    name_len: usize,
};

pub fn gameObjectName(obj: *const WoWGameObject) []const u8 {
    return obj.name[0..obj.name_len];
}

const std = @import("std");
