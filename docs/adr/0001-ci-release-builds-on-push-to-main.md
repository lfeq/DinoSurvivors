# 1. CI release builds on push to `main`

Date: 2026-06-07

## Status

Accepted (design only — workflow not yet implemented)

## Context

We want every push to `main` to produce downloadable, working builds of
DinoSurvivors for two targets:

- **Windows** — `win-x64`
- **macOS Apple Silicon** — `osx-arm64`

The game is a **MonoGame DesktopGL** project on **.NET 9**. DesktopGL uses
OpenGL on both desktop platforms, so the compiled content pipeline output
(`.xnb`) is platform-identical — a build for one desktop RID can be produced
from any host OS. The repo is hosted at `github.com/lfeq/DinoSurvivors`, and
PC is the MVP target (other platforms are deferred per CONTEXT.md).

Two throwaway local build scripts existed (`scripts/build-windows.sh`,
`scripts/build-macos.sh`) that did single-file, self-contained `dotnet publish`
and zipped the result. They were written for manual testing only. The macOS
script relies on macOS-only tooling (`ditto`, `xattr`, `chmod`).

A subtlety that drove several decisions: `PublishSingleFile` does **not** embed
the MonoGame `Content/` folder into the executable. `Content.mgcb` is currently
empty (no assets), so today the publish output is just the single binary — but
as soon as any asset is added, a `Content/` folder will appear alongside the
exe and must ship with it.

## Decision

Add a GitHub Actions workflow (`.github/workflows/release.yml`) that runs on
`push` to `main` (with a concurrency group cancelling superseded runs) and is
structured as four jobs:

1. **`test`** (ubuntu) — runs `dotnet test`. Gates everything; a failing test
   blocks the release.
2. **`build-macos`** (`macos-14`, needs `test`) — `dotnet publish -r osx-arm64`
   self-contained single-file, `chmod +x`, zip the **entire publish directory**.
3. **`build-windows`** (`windows-latest`, needs `test`) —
   `dotnet publish -r win-x64` self-contained single-file, PowerShell
   `Compress-Archive` of the **entire publish directory**.
4. **`release`** (ubuntu, needs both builds) — downloads both artifacts and
   creates a GitHub Release tagged `v0.0.${{ github.run_number }}` (title
   includes the short commit SHA) with both zips attached and notes containing
   per-platform run instructions and Gatekeeper/SmartScreen workarounds. Uses
   `permissions: contents: write`.

Specific choices:

- **Delivery via GitHub Releases**, not workflow artifacts — clean, permanent,
  no-login download links suitable for sharing.
- **Per-push permanent releases** tagged `v0.0.<run_number>` — full build
  history; major/minor can be bumped by hand later.
- **Matrix of native runners** (`macos-14` + `windows-latest`), one RID each —
  each platform builds itself; avoids cross-shell zip hacks.
- **Test gate** as a separate upstream job — tests run once on ubuntu; broken
  builds are never published.
- **Zip the whole publish directory**, not just the binary — future-proofs for
  when `Content/*.xnb` assets are added.
- **A dedicated `release` job** assembles the single Release from both
  artifacts — avoids two build jobs racing to create the same tag.
- **Unsigned builds**, with the Gatekeeper/SmartScreen workaround documented in
  the release notes (no Apple Developer cert; acceptable for MVP/friends).
- **Delete the two `scripts/build-*.sh`** test scripts; the workflow becomes the
  single source of truth for release builds.

## Consequences

- Every merge to `main` consumes CI minutes on macOS and Windows runners
  (macOS minutes are billed at a higher multiplier on GitHub-hosted runners).
- The Releases list grows by one entry per push to `main`; pruning may be needed
  over time.
- macOS users must clear quarantine
  (`xattr -dr com.apple.quarantine DinoSurvivors`) or right-click → Open on
  first launch; Windows users click "More info → Run anyway" on SmartScreen.
  If a frictionless launch is later required, a follow-up ADR should cover
  Apple notarization + Windows code signing (paid, adds secrets and steps).
- Because the workflow triggers on branch pushes (not tag pushes), creating the
  release tag does not retrigger the workflow.
- Local one-off builds are no longer scripted; contributors run
  `dotnet publish` manually or rely on CI.
