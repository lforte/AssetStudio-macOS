# AssetStudio for macOS

**None of this repo, the tool, nor the repo owner is affiliated with, or sponsored or authorized by, Unity Technologies or its affiliates.**

This is a macOS port of [Perfare/AssetStudio](https://github.com/Perfare/AssetStudio), a tool for exploring, extracting and exporting Unity assets and AssetBundles. The original project is Windows-only (WinForms). This fork adds a native macOS application built with **.NET MAUI / Mac Catalyst**, on top of the same parsing/export engine from the original repo.

All credit for the core asset-parsing engine, the TypeTree/SerializedFile format handling, and the texture/FBX export pipeline goes to [Perfare](https://github.com/Perfare) and the original contributors. This fork's job was porting that engine to run natively on macOS and building a new UI for it — the parsing logic itself (`AssetStudio/`, `AssetStudioUtility/`) is unchanged except for a couple of upstream bug fixes described below.

## What's different in this fork

- **`AssetStudio.Maui`** — a new native macOS app (Mac Catalyst), replacing the Windows WinForms GUI for Mac users.
- **`AssetStudioCore`** — a new UI-agnostic port of the original `AssetStudioGUI/Studio.cs` facade, with all WinForms types (`TreeNode`, `ListViewItem`, `MessageBox`, `Properties.Settings`, etc.) replaced by plain, platform-neutral equivalents so the same engine can drive any UI.
- Native decoder/exporter libraries (`Texture2DDecoderNative`, `AssetStudioFBXNative`) gained `CMakeLists.txt` files to build as macOS universal (arm64 + x86_64) dylibs — the original repo only ships Visual Studio project files for these.
- Two real parsing bugs found and fixed while building this port (see [Notable fixes](#notable-fixes-upstreamed-from-this-port)).
- macOS-specific app features beyond the original Windows GUI:
  - Sortable asset list columns (click "Name"/"Type" to sort, click again to reverse).
  - Texture preview zoom via scroll wheel/trackpad or +/- buttons, with proper clipping and scrollbars when zoomed past the preview area.
  - An interactive 3D mesh viewer (rotate/zoom with the mouse) for `Mesh` assets, built with [three.js](https://threejs.org/), in addition to the existing OBJ export.
  - `Mesh` → FBX export (not just OBJ).
  - A generic 3D placeholder preview for `Shader` assets — see [limitations](#shader-export-and-preview-limitations) below for why it's a placeholder and not the real shader.

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

- **macOS** with **Xcode** and the command line tools installed (`xcode-select --install`).
- **[.NET 10 SDK](https://dotnet.microsoft.com/download)**.
- The **.NET MAUI workload**:
  ```
  dotnet workload install maui
  ```
- **[CMake](https://cmake.org/) 3.16+** to build the native libraries (`brew install cmake`, or download from cmake.org if you don't use Homebrew).
- **[Autodesk FBX SDK 2020.3.9 for macOS](https://aps.autodesk.com/developer/overview/fbx-sdk)** — only required if you want FBX export/preview to work. It's a free download but requires registering an Autodesk account; it cannot be bundled in this repo. The app and every other feature (texture/audio/mesh-OBJ export, the 3D mesh/shader viewer, etc.) work fine without it — without the FBX SDK, just skip building `AssetStudioFBXNative` and FBX export will simply be unavailable at runtime instead of failing to build.
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

It can also be run on demand from the Actions tab (`workflow_dispatch`) without creating a release, to sanity-check that the build still works. Note that CI-built releases don't include FBX export, since the proprietary FBX SDK isn't available on GitHub-hosted runners — see the comments at the top of that workflow file for details.

## Usage

- **Open File(s)** loads one or more Unity asset/AssetBundle files. The status bar shows how many assets were found.
- The **asset list** on the left can be filtered with the search box, and sorted by clicking the "Name" or "Type" column header (click again to reverse the order).
- Selecting an asset shows a **preview** on the right:
  - **Texture2D / Sprite** — the decoded image. Zoom with the scroll wheel/trackpad or the +/- buttons (bottom-right); the preview area clips and scrolls when zoomed past its bounds.
  - **Mesh** — an interactive 3D viewer (drag to rotate, scroll to zoom).
  - **Shader** — the decompiled shader text, with a "Show 3D Preview" button that swaps in a generic shaded sphere (see limitation below).
  - Everything else — a generic dump of the asset's fields (from its TypeTree, when present).
- **Export Selected** / **Export All** write the converted assets (PNG/TGA textures, OBJ+FBX meshes, WAV/MP3 audio, JSON MonoBehaviours, etc.) to a folder you choose.

## Shader export and preview limitations

Unity strips shader **source** out of built players and AssetBundles — only compiled GPU bytecode (DXBC/SPIR-V/Metal, depending on target platform) survives. What gets exported as `.shader` is a **disassembly of that bytecode** formatted to look like ShaderLab/HLSL, for human inspection — it is not valid, recompilable shader source. That's also why it won't re-import cleanly into a Unity project (you'll see a pink/error material): there's no way to recover the original source from compiled bytecode without a project-scale decompiler, which is out of scope here.

For the same reason, the 3D "Show 3D Preview" option for shaders does **not** run the actual shader — it renders a generic sphere whose color/metalness/roughness are derived from the shader's own declared default property values (or a deterministic color derived from its name, if no useful default exists), just so different shaders are visually distinguishable while browsing.

## Notable fixes (upstreamed from this port)

While building this port and testing against real-world AssetBundles, two parsing bugs in the original engine surfaced and were fixed (these aren't macOS-specific and apply equally to the original Windows app):

- **`Texture2D` (Unity 2022.2+):** a missing `m_MipmapLimitGroupName` field read caused all subsequent texture data to be misaligned, corrupting decoded textures.
- **`Shader`/`SerializedProgram` (Unity 2022.1+):** two missing fields (`m_PlayerSubPrograms`, `m_ParameterBlobIndices`, added by Unity's player-data-separation feature) caused shader parsing to read garbage data, manifesting as 20-60 second parse times per shader object.

## Acknowledgments

- [Perfare/AssetStudio](https://github.com/Perfare/AssetStudio) — the original project this is ported from. This fork would not exist without it.
- [Ishotihadus/mikunyan](https://github.com/Ishotihadus/mikunyan), [BinomialLLC/crunch](https://github.com/BinomialLLC/crunch), [Unity-Technologies/crunch](https://github.com/Unity-Technologies/crunch/tree/unity) — texture decoder references used by `Texture2DDecoderNative` (inherited from upstream).
- [three.js](https://threejs.org/) (MIT) and its `OrbitControls` addon — power the interactive 3D viewer.
- [Autodesk FBX SDK](https://aps.autodesk.com/developer/overview/fbx-sdk) — used by `AssetStudioFBXNative` for FBX export; proprietary, not redistributed in this repo.

## License

MIT — see [LICENSE](LICENSE). Original copyright retained from Radu and Perfare; macOS port additions are licensed the same way.
