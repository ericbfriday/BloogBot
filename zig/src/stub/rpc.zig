//! RPC message framing for stub↔host communication.
//!
//! Wire format:
//!   Request:  [Opcode: u8][payload_len: u32][payload: [payload_len]u8]
//!   Response: [Response: u8][payload_len: u32][payload: [payload_len]u8]
//!
//! This module re-exports the shared types from rpc_types and provides
//! helpers for constructing and parsing messages on the wire.

pub const rpc_types = @import("rpc_types");

// Re-export everything from rpc_types.
pub const Opcode = rpc_types.Opcode;
pub const Response = rpc_types.Response;
pub const HEADER_SIZE = rpc_types.HEADER_SIZE;
pub const ReadRequest = rpc_types.ReadRequest;
pub const ReadBytesRequest = rpc_types.ReadBytesRequest;
pub const WriteBytesRequest = rpc_types.WriteBytesRequest;
pub const EnumerateObjectsResponse = rpc_types.EnumerateObjectsResponse;
pub const ObjectInfo = rpc_types.ObjectInfo;

// Fastcall shim request types.
pub const Fastcall2Request = rpc_types.Fastcall2Request;
pub const Fastcall3Request = rpc_types.Fastcall3Request;
pub const Fastcall4Request = rpc_types.Fastcall4Request;
pub const Fastcall6Request = rpc_types.Fastcall6Request;
pub const FastcallSellItemRequest = rpc_types.FastcallSellItemRequest;
pub const FastcallBuyItemRequest = rpc_types.FastcallBuyItemRequest;
pub const FastcallGetObjPtrRequest = rpc_types.FastcallGetObjPtrRequest;

// Object enumeration.
pub const EnumerateObjectsRequest = rpc_types.EnumerateObjectsRequest;

// Run-on-main-thread.
pub const RunOnMainThreadRequest = rpc_types.RunOnMainThreadRequest;

// Detour management.
pub const InstallDetourRequest = rpc_types.InstallDetourRequest;
pub const RemoveDetourRequest = rpc_types.RemoveDetourRequest;
pub const DetourInfo = rpc_types.DetourInfo;

// Header parsing/building.
pub const parseOpcode = rpc_types.parseOpcode;
pub const parsePayloadLen = rpc_types.parsePayloadLen;
pub const buildHeader = rpc_types.buildHeader;
