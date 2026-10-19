# TASK-078: Pick Runtime candidates from the export version

Status: Completed (2026-10-20).

## Objective

Inspecting a binary skeleton without a Runtime override must not hang.
`artifacts/official-v38/spineboy/spineboy-pro.skel` (Spine 3.8.55 binary) never
finished: automatic detection tries every Runtime adapter in registration
order, and the vendored 2.1.25 binary parser loops on 3.8 data (15 s timeout,
27 MiB working set) before the 3.8.95 adapter is reached. Found while
validating the TASK-068 browser, where it kept the background busy and left
later rows unfilled.

## Context

- `AssetService.InspectWithRuntime` tries candidates in order and returns the
  first adapter whose `Inspect` succeeds.
- Every adapter parses the whole file and only then compares the export
  version (`Expected a X export`), so an adapter for another version can never
  accept a file; trying it only costs time, or hangs.
- Forcing each Runtime on the file (CLI `inspect --runtime`): 2.1.08 rejects
  binaries; 2.1.25 times out; 3.1.07–3.7.94 and 4.x fail within 0.4 s;
  3.8.95 succeeds in 0.4 s.
- `scripts/test-official-v38.ps1` always passes `--runtime 3.8.95`, so it never
  exercised automatic detection on the binary.

## Scope

Branch `fix/v3-skel-runtime-detection` from `master`.

## Allowed Paths

- `src/SpineViewerWPF.Application/**` (version probe, candidate selection)
- `tests/SpineViewerWPF.Application.Smoke/**`
- `scripts/test-official-v38.ps1` (automatic-detection inspect)
- `docs/ai/04-runtime-matrix.md`, `docs/ai/12-application-use-cases.md`,
  `docs/ai/00-context-index.md`, this task

## Forbidden Paths

- `runtimes/**` (vendored parsers stay unchanged), Core contracts, WPF, CLI behavior

## Required Behavior

- Without a Runtime override, read the export version before parsing:
  JSON from the `"spine"` value in the first 4 KB; binary from the header,
  either a 3.x/2.x layout (hash string, version string) or a 4.x layout
  (8-byte hash, version string). Only a version shaped like `N.N[.N][suffix]`
  counts.
- When a version is found and adapters with the same major.minor exist, try
  only those, in registration order. Otherwise keep the previous behavior of
  trying every adapter.
- A Runtime override, results for supported files, and error messages from the
  matching adapters are unchanged.

## Validation

```powershell
dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v38.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v41.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v42.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v43.ps1 -Offline
git diff --check
```

Smoke: the probe reads every committed JSON fixture's version and synthetic 3.x
and 4.x binary headers, and rejects unrecognizable data; automatic detection on
a synthetic 3.8 binary (valid header, damaged body) fails quickly with the 3.8
adapter's error instead of reaching other parsers. Official v38: `inspect`
without `--runtime` on the binary finishes and reports 3.8.

## Completion Report

- `SkeletonVersionProbe` (Application, internal) reads the export version from
  the first 4 KB of JSON or the first 256 bytes of a binary; IO errors and
  unrecognizable data return null.
- `AssetService.InspectWithRuntime` narrows automatic candidates to the
  export's major.minor line when such adapters exist. Every adapter's accepted
  version prefix lies within its own major.minor line, so no previously
  accepted file changes adapter.
- `RuntimeDetectionSmoke` covers all committed JSON fixtures, synthetic 2.x/3.x
  and 4.x headers, garbage, and a damaged 3.8 binary whose automatic error
  equals the forced 3.8.95 error.
- `scripts/test-official-v38.ps1` adds an automatic binary inspect.

Validation (2026-10-20): Release build passed with 0 warnings; Application
smoke passed; `test-v3.ps1`, official v38/v41, `test-v42.ps1`, and
`test-v43.ps1` (all `-Offline`) passed. The official 3.8 binary now inspects
automatically in about 0.35 s as 3.8.55; 4.1 binaries from the official and
upstream example caches still select 4.1.

Known limitation: if a JSON file's first `"spine"` string value in the first
4 KB is not the skeleton version, the wrong line could be chosen; Spine
exports write `skeleton.spine` first, so this is not expected in practice.
