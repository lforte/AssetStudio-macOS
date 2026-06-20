# AssetStudio for macOS

**None of this repo, the tool, nor the repo owner is affiliated with, or sponsored or authorized by, Unity Technologies or its affiliates.**

This is a macOS port of [Perfare/AssetStudio](https://github.com/Perfare/AssetStudio), a tool for exploring, extracting and exporting Unity assets and AssetBundles. The original project is Windows-only (WinForms). This fork adds a native macOS application built with **.NET MAUI / Mac Catalyst**, on top of the same parsing/export engine from the original repo.

All credit for the core asset-parsing engine, the TypeTree/SerializedFile format handling, and the texture/FBX export pipeline goes to [Perfare](https://github.com/Perfare) and the original contributors. This fork's job was porting that engine to run natively on macOS and building a new UI for it — the parsing logic itself (`AssetStudio/`, `AssetStudioUtility/`) is unchanged except for a couple of upstream bug fixes described below.

## What's different in this fork

- **`AssetStudio.Maui`** — a new native macOS app (Mac Catalyst), replacing the Windows WinForms GUI for Mac users.
- **`AssetStudioCore`** — a new UI-agnostic port of the original `AssetStudioGUI/Studio.cs` facade, with all WinForms types (`TreeNode`, `ListViewItem`, `MessageBox`, `Properties.Settings`, etc.) replaced by plain, platform-neutral equivalents so the same engine can drive any UI.
- Native decoder/exporter libraries (`Texture2DDecoderNative`, `AssetStudioFBXNative`) gained `CMakeLists.txt` files to build as macOS universal (arm64 + x86_64) dylibs — the original repo only ships Visual Studio project files for these.
- Real parsing/interop bugs found and fixed while building this port (see [Notable fixes](#notable-fixes-upstreamed-from-this-port)).
- The left panel has three tabs mirroring the original Windows GUI's layout:
  - **Asset List** — the flat, filterable, sortable list, with **Name / Container / Type / PathID / Size** columns. Columns are resizable (drag the column border) and have visible grid lines, matching the original `ListView`.
  - **Scene Hierarchy** — a `GameObject` tree view (parent/child nesting from each object's `Transform`), expand/collapse per node.
  - **Asset Classes** — the list of TypeTree class definitions found in the loaded file(s) (Name + ID); selecting one shows its full field layout in the Dump tab.
- The right (preview) panel has two tabs:
  - **Visual Preview** — the image / interactive 3D viewer / audio player / generic placeholder, depending on the selected asset's type.
  - **Dump** — the asset's TypeTree field dump as text, available for every asset type (including ones with a visual preview, e.g. a `Mesh`'s raw fields alongside its 3D view).
- macOS-specific app features beyond the original Windows GUI:
  - Texture preview zoom via scroll wheel/trackpad or +/- buttons, with proper clipping and scrollbars when zoomed past the preview area.
  - An interactive 3D mesh viewer (rotate/zoom with the mouse) for `Mesh` assets, built with [three.js](https://threejs.org/), in addition to the existing OBJ export, with a wireframe toggle.
  - `Mesh` → FBX export (not just OBJ).
  - A generic 3D placeholder preview for `Shader` assets — see [limitations](#shader-export-and-preview-limitations) below for why it's a placeholder and not the real shader.
  - `AudioClip` playback preview (Play/Stop) via the FMOD Engine and native `AVAudioPlayer` — see [Requirements](#requirements) for the separate FMOD download this needs.

## Project structure

| Project | Purpose | Platform |
|---|---|---|
| `AssetStudio` | Core parsing engine (SerializedFile/TypeTree, asset classes) | net8.0, cross-platform |
| `AssetStudioUtility` | Export helpers (texture/FBX/audio conversion) | net8.0, cross-platform |
| `AssetStudio.PInvoke` | Cross-platform native library loader (`dlopen`/`LoadLibrary`) | net8.0, cross-platform |
| `AssetStudioFBXWrapper` | P/Invoke bindings to `AssetStudioFBXNative` | net8.0, cross-platform |
| `Texture2DDecoderWrapper` | P/Invoke bindings to `Texture2DDecoderNative` | net8.0, cross-platform |
| `Texture2DDecoderNative` | Native texture decoders (ASTC/ETC/PVRTC/crunch/...) | C++, now builds via CMake for macOS |
| `AssetStudioFBXNative` | Native FBX export, links the Autodesk FBX SDK | C++, now builds via CMake for macOS |
| `AssetStudioCore` | **New.** UI-agnostic facade used by the macOS app | net8.0 |
| `AssetStudio.Maui` | **New.** The macOS app | net10.0-maccatalyst |
| `AssetStudioGUI` | Original Windows WinForms app | net8.0-windows, untouched |

## Requirements

To build this from source on macOS you'll need:

- **macOS** with **Xcode 26.4+** and the command line tools installed (`xcode-select --install`). The .NET 10 MacCatalyst SDK's linker step requires the Xcode 26.4 MacCatalyst SDK or newer for Release-configuration builds; Debug builds are more lenient about the exact version. After installing/upgrading Xcode, run `xcodebuild -runFirstLaunch` once to finish setting up its command-line components.
- **[.NET 10 SDK](https://dotnet.microsoft.com/download)**.
- The **.NET MAUI workload**:
  ```
  dotnet workload install maui
  ```
- **[CMake](https://cmake.org/) 3.16+** to build the native libraries (`brew install cmake`, or download from cmake.org if you don't use Homebrew).
- **[Autodesk FBX SDK 2020.3.9 for macOS](https://aps.autodesk.com/developer/overview/fbx-sdk)** — only required if you want FBX export/preview to work. It's a free download but requires registering an Autodesk account; it cannot be bundled in this repo. The app and every other feature (texture/audio/mesh-OBJ export, the 3D mesh/shader viewer, etc.) work fine without it — without the FBX SDK, just skip building `AssetStudioFBXNative` and FBX export will simply be unavailable at runtime instead of failing to build.
- **[FMOD Engine for macOS](https://www.fmod.com/download)** — only required for the `AudioClip` playback preview (Play/Stop button). Same situation as the FBX SDK: it's a free download requiring registration, proprietary, and not bundled in this repo. Without it, every other feature works fine — `AudioClip` assets just show the generic placeholder preview instead of a Play button (export still works independently of this). `AssetStudio.Maui.csproj` looks for it at `/Applications/FMOD Engine/api/core/lib/libfmod.dylib` (the default install location for the "FMOD Engine" — not "FMOD Studio" — macOS package) and copies it into the app bundle automatically if present.
- **three.js** (bundled) — the 3D viewer ships a local copy of three.js r128 and its classic `OrbitControls.js` under `AssetStudio.Maui/Resources/Raw/threejs/` so the app works without a network connection. No separate install needed; see [Acknowledgments](#acknowledgments) for license/credit.

## Building

1. **Clone the repo:**
   ```
   git clone https://github.com/lforte/AssetStudio-macOS.git
   cd AssetStudio-macOS
   ```

2. **Build the native libraries** as universal (arm64 + x86_64) dylibs:
   ```
   cmake -B Texture2DDecoderNative/build -S Texture2DDecoderNative \
         -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES="arm64;x86_64"
   cmake --build Texture2DDecoderNative/build --config Release
   ```
   If you installed the FBX SDK (see [Requirements](#requirements)), also build:
   ```
   cmake -B AssetStudioFBXNative/build -S AssetStudioFBXNative \
         -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES="arm64;x86_64"
   cmake --build AssetStudioFBXNative/build --config Release
   ```
   By default `AssetStudioFBXNative/CMakeLists.txt` looks for the SDK at `/Applications/Autodesk/FBX SDK/2020.3.9`. If yours is elsewhere, pass `-DFBXSDK_ROOT=/path/to/sdk` to the first `cmake` command above.

   `AssetStudio.Maui.csproj` automatically copies whichever of these dylibs it finds into the app bundle at build time — you don't need to copy them manually.

3. **Build and run the app:**
   ```
   cd AssetStudio.Maui
   dotnet build -t:Run -f net10.0-maccatalyst
   ```
   or open the solution in an IDE that supports .NET MAUI (Visual Studio 2022+, VS Code with the MAUI extension, JetBrains Rider) and run the `AssetStudio.Maui` target.

## Releases

[`.github/workflows/release.yml`](.github/workflows/release.yml) builds the app and publishes it as a GitHub Release automatically whenever a `v*` tag is pushed:

```
git tag -a v1.0.1 -m "v1.0.1"
git push origin v1.0.1
```

It can also be run on demand from the Actions tab (`workflow_dispatch`) without creating a release, to sanity-check that the build still works. Note that CI-built releases don't include FBX export or `AudioClip` playback preview, since the proprietary FBX SDK and FMOD Engine aren't available on GitHub-hosted runners — see the comments at the top of that workflow file for details. The workflow runs on the `macos-26` runner image (Xcode 26.5), which the .NET 10 MacCatalyst Release build needs — see [Requirements](#requirements).

## Usage

- **Open File(s)** loads one or more Unity asset/AssetBundle files. The status bar shows how many assets were found.
- The left panel has three tabs:
  - **Asset List** — filter with the search box; sort by clicking any column header (Name/Container/Type/PathID/Size), click again to reverse. Drag a column's right edge to resize it.
  - **Scene Hierarchy** — click a row to expand/collapse its children (▶/▼) and, for `GameObject` rows, show that object's field dump in the Dump tab.
  - **Asset Classes** — click an entry to show its full TypeTree field layout in the Dump tab.
- The right panel has two tabs, independent of which left-panel tab is active:
  - **Visual Preview**:
    - **Texture2D / Sprite** — the decoded image. Zoom with the scroll wheel/trackpad or the +/- buttons (bottom-right); the preview area clips and scrolls when zoomed past its bounds.
    - **Mesh** — an interactive 3D viewer (drag to rotate, scroll to zoom), with a Wireframe toggle.
    - **Shader** — a generic shaded-sphere 3D placeholder (see limitation below), also with a Wireframe toggle.
    - **AudioClip** — a Play/Stop button (requires the FMOD Engine — see [Requirements](#requirements)).
    - Everything else — the generic placeholder image.
  - **Dump** — the selected asset's field dump as text (from its TypeTree, when present), for any asset type.
- **Export Selected** / **Export All** write the converted assets (PNG/TGA textures, OBJ+FBX meshes, WAV/MP3 audio, JSON MonoBehaviours, etc.) to a folder you choose.

## Shader export and preview limitations

Unity strips shader **source** out of built players and AssetBundles — only compiled GPU bytecode (DXBC/SPIR-V/Metal, depending on target platform) survives. What gets exported as `.shader` is a **disassembly of that bytecode** formatted to look like ShaderLab/HLSL, for human inspection — it is not valid, recompilable shader source. That's also why it won't re-import cleanly into a Unity project (you'll see a pink/error material): there's no way to recover the original source from compiled bytecode without a project-scale decompiler, which is out of scope here.

For the same reason, the 3D "Show 3D Preview" option for shaders does **not** run the actual shader — it renders a generic sphere whose color/metalness/roughness are derived from the shader's own declared default property values (or a deterministic color derived from its name, if no useful default exists), just so different shaders are visually distinguishable while browsing.

## Notable fixes (upstreamed from this port)

While building this port and testing against real-world AssetBundles, two parsing bugs in the original engine surfaced and were fixed (these aren't macOS-specific and apply equally to the original Windows app):

- **`Texture2D` (Unity 2022.2+):** a missing `m_MipmapLimitGroupName` field read caused all subsequent texture data to be misaligned, corrupting decoded textures.
- **`Shader`/`SerializedProgram` (Unity 2022.1+):** two missing fields (`m_PlayerSubPrograms`, `m_ParameterBlobIndices`, added by Unity's player-data-separation feature) caused shader parsing to read garbage data, manifesting as 20-60 second parse times per shader object.

Two more bugs surfaced specifically while wiring up `AudioClip` playback against a current (2.03.14) FMOD Engine download — these are fixed only for the macOS path, since the bundled Windows `fmod.dll` is the much older 1.07.16 and the original Windows call is left untouched to avoid any risk of regressing it:

- **`FMOD.Factory.System_Create` was missing the `headerversion` argument** that `FMOD_System_Create` expects — it was calling a 1-argument overload against a native function that takes 2, which FMOD 2.x rejects outright with `ERR_HEADER_MISMATCH`.
- **`FMOD.VERSION.number` was hardcoded to `0x00010716`** (FMOD 1.07.16); the macOS path now sets it to `0x00020314` to match the FMOD Engine version Apple/ARM64 users currently download from fmod.com.

## Roadmap

Planned/possible improvements for future versions, roughly in priority order:

**Build & distribution**
- ~~Upgrade to Xcode 26.4 so Release-configuration builds work~~ — done as of Xcode 26.5; Release builds work locally now (run `xcodebuild -runFirstLaunch` after upgrading Xcode if you hit an `actool`/`ibtoold` plugin-load error).
- ~~Get the CI release workflow building again~~ — done; `macos-latest`'s newest Xcode (26.3, macOS SDK 26.2) was too old for the same 26.4+ requirement above, so [`release.yml`](.github/workflows/release.yml) now runs on the `macos-26` image and selects its Xcode 26.5 directly. Verified end-to-end with the [v1.3.0 release](https://github.com/lforte/AssetStudio-macOS/releases/tag/v1.3.0).
- Code-sign with a Developer ID and notarize releases, so macOS Gatekeeper doesn't block first launch.
- Build `AssetStudioFBXNative` on a self-hosted CI runner with the FBX SDK installed, so automated releases include FBX export (currently only local builds do — see [`release.yml`](.github/workflows/release.yml)).

**Feature parity with the original Windows GUI**
- ~~A Scene Hierarchy / GameObject tree view~~ — done; see the Scene Hierarchy tab, plus the new Asset Classes tab and Container/PathID/Size columns on the Asset List tab.
- ~~`AudioClip` playback preview~~ — done via the FMOD Engine + `AVAudioPlayer`, pending a real-world test against a file that actually contains `AudioClip` assets (the bundle used during development had none).
- A search box for the Scene Hierarchy tab (the original GUI's `treeSearch`) — the Mac app's tree has no search yet, only the Asset List tab does.
- An assembly-directory picker for MonoBehaviour export, including the Il2CppDumper dummy-DLL workflow.
- An export-options panel (scale factor, FBX version, eulerFilter, etc.) — currently uses `ExportSettings` defaults with no UI to change them.

**Polish**
- ~~Remove the on-screen debug overlay in the 3D viewer~~ — done; the diagnostic `#debug` div/`debugLog()`/`window.onerror` leftover from chasing a WebView data-transfer bug has been removed from `MeshViewerHtml.cs`.
- ~~Add a wireframe-toggle button in the UI~~ — done; a "Wireframe" button appears top-right of the 3D viewer for both `Mesh` previews and the `Shader` 3D placeholder preview.
- Revisit 3D viewer performance for very large meshes — it currently rebuilds and reloads the whole WebView page per selection.
- The Asset List's Name column isn't resizable (only Container/Type/PathID/Size are) — it currently absorbs all remaining width by design, but a drag handle would match the original GUI more closely.
- Replace the deprecated `AVAudioPlayer(NSData, string, out NSError)` constructor used for audio playback with the recommended non-deprecated equivalent.

**Robustness**
- Audit other asset classes for the same kind of version-specific missing-field bugs found in `Texture2D` and `Shader` (see [Notable fixes](#notable-fixes-upstreamed-from-this-port)), especially for Unity versions newer than 2022.1 (the original project's documented support ceiling).

## Acknowledgments

- [Perfare/AssetStudio](https://github.com/Perfare/AssetStudio) — the original project this is ported from. This fork would not exist without it.
- [Ishotihadus/mikunyan](https://github.com/Ishotihadus/mikunyan), [BinomialLLC/crunch](https://github.com/BinomialLLC/crunch), [Unity-Technologies/crunch](https://github.com/Unity-Technologies/crunch/tree/unity) — texture decoder references used by `Texture2DDecoderNative` (inherited from upstream).
- [three.js](https://threejs.org/) (MIT) and its `OrbitControls` addon — power the interactive 3D viewer.
- [Autodesk FBX SDK](https://aps.autodesk.com/developer/overview/fbx-sdk) — used by `AssetStudioFBXNative` for FBX export; proprietary, not redistributed in this repo.
- [FMOD Engine](https://www.fmod.com/) by Firelight Technologies — used for `AudioClip` decoding/playback preview; proprietary, not redistributed in this repo.

## License

MIT — see [LICENSE](LICENSE). Original copyright retained from Radu and Perfare; macOS port additions are licensed the same way.
