# Repository Instructions

## Build and Test

This repository is a legacy Visual Studio/.NET Framework solution with native VC++ projects. It is not an SDK-style `dotnet build` repository.

Build from the repository root with Visual Studio 2022 MSBuild using x86. Do not rely on `msbuild` being on `PATH`; use the full executable path. In PowerShell, quote `/p:"Configuration=Debug;Platform=x86"` because the semicolon will otherwise break the command. Use `/m:1` because several projects write to the shared `Bot\` output folder and parallel builds can cause file-lock copy warnings.

Restore:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\BloogBot.sln /t:Restore /p:"RestorePackagesConfig=true;Configuration=Debug;Platform=x86" /nologo /m:1
```

Build:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\BloogBot.sln /t:Build /p:"Configuration=Debug;Platform=x86" /nologo /m:1
```

Test:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" .\Bot\BloogBotTests.dll
```

Do not report restore-only or managed-test-only success as full solution success if native VC++ projects have not been checked.

## Death Knight Work

Before coding Death Knight support, read the source-of-truth research in `Docs\DeathKnightWotLKSupport\`:

- `README.md`
- `codebase-findings.md`
- `wotlk-death-knight-research.md`
- `ffi-and-client-integration.md`
- `sources.md`

Default DK implementation path:

- Death Knight support is WotLK-only. Guard DK behavior with `ClientVersion.WotLK` and `Class.DeathKnight`.
- Start with `BloodDeathKnightBot` for open-world grinding and leveling.
- Add Frost and Unholy only after shared rune/runic-power helpers are implemented and verified.
- Treat all gameplay guides as WotLK Classic baselines until behavior is verified against the original WotLK 3.3.5.12340 client.

Record new DK findings, live-client observations, probe results, and external sources under `Docs\DeathKnightWotLKSupport\`. Keep that directory current when implementation discovers new facts.
