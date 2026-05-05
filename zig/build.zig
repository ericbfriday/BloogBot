const std = @import("std");

// The WoW clients BloogBot targets (1.12.1 / 2.4.3 / 3.3.5) are 32-bit, so
// every artifact that talks to them must also be 32-bit. We default to
// `x86-windows-gnu` so a developer on Linux or macOS can `zig build` with no
// extra flags. Override with `-Dtarget=...` if needed.
//
// We avoid `-windows-msvc` because it requires the Microsoft C runtime
// headers and libraries to be present at compile time. The MinGW ABI works
// across all WoW versions of interest and ships with the Zig compiler.
const default_target_query = std.Target.Query{
    .cpu_arch = .x86,
    .os_tag = .windows,
    .abi = .gnu,
};

pub fn build(b: *std.Build) void {
    const target = b.standardTargetOptions(.{
        .default_target = default_target_query,
    });
    const optimize = b.standardOptimizeOption(.{});

    // Shared Win32 declarations live in src/common/winapi.zig and are
    // exposed as the `winapi` module to every artifact in the workspace.
    const winapi_mod = b.createModule(.{
        .root_source_file = b.path("src/common/winapi.zig"),
        .target = target,
        .optimize = optimize,
    });

    // -----------------------------------------------------------------
    // bloog-launcher: small EXE that starts wow.exe suspended, injects
    // bloog-stub.dll via CreateRemoteThread(LoadLibraryW), then resumes.
    // Replaces the existing C# Bootstrapper project.
    // -----------------------------------------------------------------
    const launcher_mod = b.createModule(.{
        .root_source_file = b.path("src/launcher/main.zig"),
        .target = target,
        .optimize = optimize,
    });
    launcher_mod.addImport("winapi", winapi_mod);

    const launcher = b.addExecutable(.{
        .name = "bloog-launcher",
        .root_module = launcher_mod,
    });
    // Console subsystem so we get a stdout/stderr window for diagnostics.
    launcher.subsystem = .Console;

    b.installArtifact(launcher);

    // `zig build run-launcher -- <wow.exe> <stub.dll>` — only meaningful on
    // a Windows host; cross-compiled binaries can't be executed here.
    const run_launcher = b.addRunArtifact(launcher);
    run_launcher.step.dependOn(b.getInstallStep());
    if (b.args) |args| run_launcher.addArgs(args);
    const run_launcher_step = b.step(
        "run-launcher",
        "Run bloog-launcher (Windows host only)",
    );
    run_launcher_step.dependOn(&run_launcher.step);
}
