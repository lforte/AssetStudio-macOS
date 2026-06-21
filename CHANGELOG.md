# Changelog

All notable changes to this macOS port are documented here. Dates are when each
GitHub Release was published. See the [README](README.md) for current features,
requirements, and known limitations.

## [1.4.0] - 2026-06-21

### Confirmed
- `AudioClip` playback preview (Play/Stop) and `AudioClip` export verified end-to-end
  against real `AudioClip`-bearing AssetBundles — previously shipped in v1.3.0 but
  untested against actual audio data.
- Two downloads are published for this release: a CI build (no FBX export / no
  `AudioClip` playback, since the proprietary FBX SDK and FMOD Engine aren't available
  on GitHub-hosted runners) and a locally-built "Universal" build with both included.

## [1.3.0] - 2026-06-20

### Added
- **Scene Hierarchy** tab — a `GameObject` tree view (parent/child nesting from each
  object's `Transform`), mirroring the original Windows GUI.
- **Asset Classes** tab — lists TypeTree class definitions found in the loaded file(s);
  selecting one shows its field layout in the Dump tab.
- **Asset List**: `Container` / `PathID` / `Size` columns, visible grid lines, and
  resizable columns (drag the column border).
- Preview pane split into **Visual Preview** and **Dump** tabs, so a field dump is
  available for every asset type, including ones with a visual preview.
- `AudioClip` playback preview (Play/Stop) via the FMOD Engine and native `AVAudioPlayer`.
- App icon now uses the `as.ico` artwork instead of the default .NET icon.

### Fixed
- Two FMOD wrapper bugs, macOS-only (the bundled Windows `fmod.dll` path is untouched):
  `FMOD.Factory.System_Create` was missing the `headerversion` argument FMOD 2.x
  requires, and `FMOD.VERSION.number` was hardcoded to the old FMOD 1.07.16 value.
- CI release workflow: the last two release builds (v1.1.0, v1.2.0) had failed against
  an outdated MacCatalyst SDK on `macos-latest`; fixed by moving to the `macos-26`
  runner image (Xcode 26.5).
- A crash on every Scene Hierarchy row click, caused by a custom tap-gesture recognizer
  conflicting with `CollectionView`'s own gesture handling on Mac Catalyst; replaced
  with the standard `SelectionChanged` pattern.
- A black rendering artifact next to indented Scene Hierarchy sub-items, caused by a
  `BoxView` used purely as a spacer; replaced with a bound `ColumnDefinition.Width`.

## [1.2.0] - 2026-06-19

### Added
- A "Wireframe" toggle button for the 3D mesh viewer and the `Shader` 3D placeholder
  preview.

### Removed
- The on-screen debug overlay (`#debug` div / `debugLog()` / `window.onerror`) left over
  from chasing an earlier WebView data-transfer bug in the 3D viewer.

## [1.1.0] - 2026-06-19

### Added
- Automated GitHub Actions release workflow (`.github/workflows/release.yml`), building
  the app and publishing it as a GitHub Release on `v*` tags.

### Fixed
- Release-configuration builds, which need the Xcode 26.4+ MacCatalyst SDK.

## [1.0.0] - 2026-06-18

### Added
- Initial macOS port: `AssetStudio.Maui` (the new native Mac Catalyst UI) and
  `AssetStudioCore` (a UI-agnostic port of the original `AssetStudioGUI/Studio.cs`
  facade), on top of the unchanged `AssetStudio`/`AssetStudioUtility` parsing engine.
- CMake builds for `Texture2DDecoderNative` and `AssetStudioFBXNative` as macOS
  universal (arm64 + x86_64) dylibs.
- Texture/image preview with zoom, an interactive 3D mesh viewer (three.js) with FBX/OBJ
  export, and a generic 3D placeholder preview for `Shader` assets.
