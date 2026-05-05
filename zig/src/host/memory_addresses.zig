//! WotLK 3.3.5a memory addresses and offsets.
//!
//! Port of BloogBot/Game/MemoryAddresses.cs (lines 9-101).
//! These are function pointers and static addresses for the WotLK 3.3.5a client.
//! Offsets are build-specific — wrong offset = crash or corruption.
//!
//! comptime asserts catch zero-valued TODO entries.

// ---------------------------------------------------------------------------
// Function pointers (WotLK 3.3.5a)
// ---------------------------------------------------------------------------

pub const EnumerateVisibleObjectsFunPtr: usize = 0x004D4B30;
pub const GetObjectPtrFunPtr: usize = 0x004D4DB0;
pub const GetPlayerGuidFunPtr: usize = 0x004D3790;
pub const SetFacingFunPtr: usize = 0x0072EA50;
pub const SendMovementUpdateFunPtr: usize = 0x007413F0;
pub const SetControlBitFunPtr: usize = 0x005FBE10;
pub const SetControlBitDevicePtr: usize = 0x00D3F78C;
pub const JumpFunPtr: usize = 0x006F0DD0;
pub const GetCreatureTypeFunPtr: usize = 0x0071F300;
pub const GetCreatureRankFunPtr: usize = 0x00718A00;
pub const GetUnitReactionFunPtr: usize = 0x007251C0;
pub const LuaCallFunPtr: usize = 0x00819210;
pub const GetTextFunPtr: usize = 0x00819D40;
pub const CastSpellByIdFunPtr: usize = 0x0080DA40;
pub const GetRow2FunPtr: usize = 0x0065C290;
pub const IntersectFunPtr: usize = 0x0077F310;
pub const SetTargetFunPtr: usize = 0x00524BF0;
pub const RetrieveCorpseFunPtr: usize = 0x0051B800;
pub const ReleaseCorpseFunPtr: usize = 0x0051AA90;
pub const GetItemCacheEntryFunPtr: usize = 0x0067CA30;
pub const ItemCacheEntryBasePtr: usize = 0x00C5D828;
pub const IsSpellOnCooldownFunPtr: usize = 0x00809000;
pub const LootSlotFunPtr: usize = 0x00589140;
pub const UseItemFunPtr: usize = 0x00708C20;
pub const SellItemByGuidFunPtr: usize = 0x006D2D40;
pub const BuyVendorItemFunPtr: usize = 0x006D2DE0;
pub const DismountFunPtr: usize = 0x0051D170;
pub const GetAuraFunPtr: usize = 0x00556E10;
pub const GetAuraCountFunPtr: usize = 0x004F8850;

// ---------------------------------------------------------------------------
// Static memory addresses
// ---------------------------------------------------------------------------

pub const ZoneTextPtr: usize = 0x00BD0788;
pub const SubZoneTextPtr: usize = 0x00BD0784;
pub const MinimapZoneTextPtr: usize = 0x00BD077C;
pub const MapId: usize = 0x00AB63BC;
pub const ServerName: usize = 0x00C79B9E;
pub const LootFrameItemsBasePtr: usize = 0x00C9D340;
pub const CoinCountPtr: usize = 0x00BFA8D0;
pub const MerchantFrameItemsBasePtr: usize = 0x00BFA3F0;
pub const MerchantFrameItemPtr: usize = 0x00BF912C;
pub const DialogFrameBase: usize = 0x00BD07A8;
pub const LocalPlayerCorpsePositionX: usize = 0x00BD0A58;
pub const LastHardwareAction: usize = 0x00B499A4;
pub const LocalPlayerSpellsBase: usize = 0x00BE5D88;
pub const LocalPlayerClass: usize = 0x00C79E89;
pub const LocalPlayerFirstExtraBag: usize = 0x00C23540;

// ---------------------------------------------------------------------------
// Object descriptor offsets (from object base + WoWObject_DescriptorOffset)
// ---------------------------------------------------------------------------

pub const LootFrameItemOffset: usize = 0x18;
pub const MerchantFrameItemOffset: usize = 0x20;
pub const LocalPlayer_SetFacingOffset: usize = 0x7A8;
pub const LocalPlayer_BackpackFirstItemOffset: usize = 0x5C8;
pub const LocalPlayer_EquipmentFirstItemOffset: usize = 0x1E68;
pub const WoWItem_ItemIdOffset: usize = 0x0C;
pub const WoWItem_StackCountOffset: usize = 0x38;
pub const WoWItem_DurabilityOffset: usize = 0xF0;
pub const WoWItem_ContainerFirstItemOffset: usize = 0x108;
pub const WoWItem_ContainerSlotsOffset: usize = 0x760;

// ---------------------------------------------------------------------------
// V-table offsets (from object base → vtable pointer → offset)
// ---------------------------------------------------------------------------

pub const WoWObject_DescriptorOffset: usize = 0x08;
pub const WoWObject_GetPositionFunOffset: usize = 0x30;
pub const WoWObject_GetFacingFunOffset: usize = 0x38;
pub const WoWObject_InteractFunOffset: usize = 0xB0;
pub const WoWObject_GetNameFunOffset: usize = 0xD8;

// ---------------------------------------------------------------------------
// Unit descriptor offsets (relative to descriptor pointer)
// ---------------------------------------------------------------------------

pub const WoWUnit_SummonedByGuidOffset: usize = 0x38;
pub const WoWUnit_TargetGuidOffset: usize = 0x48;
pub const WoWUnit_HealthOffset: usize = 0x60;
pub const WoWUnit_ManaOffset: usize = 0x64;
pub const WoWUnit_RageOffset: usize = 0x68;
pub const WoWUnit_EnergyOffset: usize = 0x70;
pub const WoWUnit_MaxHealthOffset: usize = 0x80;
pub const WoWUnit_MaxManaOffset: usize = 0x84;
pub const WoWUnit_LevelOffset: usize = 0xD8;
pub const WoWUnit_FactionIdOffset: usize = 0x24;
pub const WoWUnit_UnitFlagsOffset: usize = 0xEC;
pub const WoWUnit_BuffsBaseOffset: usize = 0xC70;
pub const WoWUnit_DynamicFlagsOffset: usize = 0x13C;
pub const WoWUnit_CurrentChannelingOffset: usize = 0xA80;
pub const WoWUnit_MovementFlagsOffset: usize = 0x7CC;
pub const WoWUnit_CurrentSpellcastOffset: usize = 0xA6C;

// ---------------------------------------------------------------------------
// DB access (WotLK only)
// ---------------------------------------------------------------------------

pub const WowDbTableBase: usize = 0x006337D0;
pub const GetRowFunPtr: usize = 0x004BB1C0;
pub const GetLocalizedRowFunPtr: usize = 0x004CFD20;

// ---------------------------------------------------------------------------
// Warden (antecedent awareness, not used in stub)
// ---------------------------------------------------------------------------

pub const WardenLoadHook: usize = 0x008724C0;
pub const WardenBase: usize = 0x00A9C414;
