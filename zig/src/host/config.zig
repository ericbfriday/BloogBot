//! Configuration loading — port of BloogBot/BotSettings.cs.
//!
//! Reads `botSettings.json` using std.json and populates a typed struct.
//! Unknown fields are ignored for forward-compatibility.

const std = @import("std");
const log = @import("log");
const win = @import("winapi");

// ---------------------------------------------------------------------------
// BotSettings — mirrors BotSettings.cs field-for-field
// ---------------------------------------------------------------------------

pub const BotSettings = struct {
    // Database
    database_type: []const u8 = "sqlite",
    database_path: []const u8 = "",

    // Discord integration (disabled by default)
    discord_bot_enabled: bool = false,
    discord_bot_token: []const u8 = "",
    discord_guild_id: []const u8 = "",
    discord_role_id: []const u8 = "",
    discord_channel_id: []const u8 = "",

    // Consumables
    food: []const u8 = "Conjured Sweet Roll",
    drink: []const u8 = "Conjured Sparkling Water",

    // Targeting
    targeting_included_names: []const u8 = "",
    targeting_excluded_names: []const u8 = "Silithid|Kodo|Centipaar|Darkmist|Ooze",
    level_range_min: i32 = 2,
    level_range_max: i32 = 2,

    // Creature type filters
    creature_type_beast: bool = true,
    creature_type_dragonkin: bool = false,
    creature_type_demon: bool = true,
    creature_type_elemental: bool = true,
    creature_type_humanoid: bool = true,
    creature_type_undead: bool = false,
    creature_type_giant: bool = false,

    // Unit reaction filters
    unit_reaction_hostile: bool = true,
    unit_reaction_unfriendly: bool = true,
    unit_reaction_neutral: bool = true,

    // Loot settings
    loot_poor: bool = true,
    loot_common: bool = true,
    loot_uncommon: bool = true,
    loot_excluded_names: []const u8 = "Shredder|Clam|Journal",

    // Vendor settings
    sell_poor: bool = true,
    sell_common: bool = true,
    sell_uncommon: bool = false,
    sell_excluded_names: []const u8 = "Healing Potion|Felcloth",

    // Hotspot / path selection
    grinding_hotspot_id: ?i32 = null,
    current_travel_path_id: ?i32 = null,
    current_gather_route_id: ?i32 = null,
    current_bot_name: []const u8 = "Frost Mage",

    // Killswitches
    use_teleport_killswitch: bool = false,
    use_stuck_in_position_killswitch: bool = true,
    use_stuck_in_state_killswitch: bool = true,
    use_player_targeting_killswitch: bool = false,
    use_player_proximity_killswitch: bool = false,

    // Powerlevel
    powerlevel_player_name: []const u8 = "",

    // Timers (ms)
    targeting_warning_timer: i32 = 7500,
    targeting_stop_timer: i32 = 15000,
    proximity_warning_timer: i32 = 10000,
    proximity_stop_timer: i32 = 20000,

    // Misc
    use_verbose_logging: bool = false,
    permanently_blacklist_unreachable_targets: bool = false,
    username: []const u8 = "",
    password: []const u8 = "",

    // Runtime-only (not in JSON)
    web_port: u16 = 8080,
    pipe_name: []const u8 = "\\\\.\\pipe\\bloogbot",
};

/// Holds the parsed JSON + the settings struct. Call `deinit()` to free.
pub const LoadedConfig = struct {
    parsed: std.json.Parsed(BotSettings),
    settings: BotSettings,

    pub fn deinit(self: *@This()) void {
        self.parsed.deinit();
    }
};

// ---------------------------------------------------------------------------
// Loading
// ---------------------------------------------------------------------------

/// Load botSettings.json from the given path using Win32 ReadFile.
pub fn loadFromFile(allocator: std.mem.Allocator, path: [:0]const u8) !LoadedConfig {
    const hFile = win.CreateFileA(
        path.ptr,
        win.GENERIC_READ,
        win.FILE_SHARE_READ,
        null,
        win.OPEN_EXISTING,
        0,
        null,
    );
    if (hFile == win.INVALID_HANDLE_VALUE) {
        log.warn("[config] failed to open {s} — using defaults", .{path});
        return loadDefaults(allocator);
    }
    defer _ = win.CloseHandle(hFile);

    var buf = std.array_list.AlignedManaged(u8, null).init(allocator);
    errdefer buf.deinit();

    var tmp: [4096]u8 = undefined;
    while (true) {
        var bytes_read: win.DWORD = 0;
        const ok = win.ReadFile(hFile, &tmp, @intCast(tmp.len), &bytes_read, null);
        if (ok == win.FALSE or bytes_read == 0) break;
        buf.appendSlice(tmp[0..bytes_read]) catch {
            return loadDefaults(allocator);
        };
    }

    if (buf.items.len == 0) return loadDefaults(allocator);

    return loadFromSlice(allocator, buf.items);
}

/// Parse a JSON string into BotSettings.
pub fn loadFromSlice(allocator: std.mem.Allocator, json_text: []const u8) !LoadedConfig {
    const parsed = std.json.parseFromSlice(
        BotSettings,
        allocator,
        json_text,
        .{ .ignore_unknown_fields = true },
    ) catch |err| {
        log.warn("[config] failed to parse JSON: {s} — using defaults", .{@errorName(err)});
        return loadDefaults(allocator);
    };

    return .{
        .parsed = parsed,
        .settings = parsed.value,
    };
}

fn loadDefaults(allocator: std.mem.Allocator) !LoadedConfig {
    const defaults_json = "{}";
    const parsed = try std.json.parseFromSlice(
        BotSettings,
        allocator,
        defaults_json,
        .{ .ignore_unknown_fields = true },
    );
    return .{
        .parsed = parsed,
        .settings = parsed.value,
    };
}

// ---------------------------------------------------------------------------
// Serialization (for /api/settings endpoint)
// ---------------------------------------------------------------------------

/// Serialize settings to a JSON string. Caller must free.
pub fn toJson(allocator: std.mem.Allocator, settings: *const BotSettings) ![]const u8 {
    return std.json.Stringify.valueAlloc(allocator, settings, .{ .whitespace = .indent_2 });
}
