# LTLM UI Toolkit

[![Unity 2020.3+](https://img.shields.io/badge/Unity-2020.3%2B-black.svg?logo=unity)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)

Pre-built UI components for the **LTLM License & Token Management SDK**. Drop-in UI for license activation, login flows, and customer portal interfaces.

> **Note:** This repository is provided as source code reference. Import the Unity package file or copy the source directly into your project.

## Installation

### Option 1: Unity Package File

Download the latest `.unitypackage` from [Releases](https://github.com/LjomaTech/ltlm-unity-sdk-ui-toolkit/releases) and import via **Assets → Import Package → Custom Package**.

### Option 2: Copy Source

Clone this repository and copy the contents into your Unity project's `Assets` folder.

### Required Dependencies

You must install these dependencies separately:

1. **LTLM SDK** - Add to `manifest.json`:
   ```json
   "com.ljomatech.ltlm": "https://github.com/LjomaTech/ltlm-unity-sdk.git"
   ```

2. **LjomaTech Unity Forms** - Add to `manifest.json`:
   ```json
   "com.ljomatech.forms": "https://github.com/LjomaTech/unity-forms.git"
   ```

3. **TextMeshPro** - Install via Package Manager (built-in)

## Quick Start

1. Import the package or copy source files
2. Install required dependencies (see above)
3. Add LTLM-UI prefab from `Prefabs/` to your scene
4. Configure your project keys in the LTLM Manager
5. The UI automatically connects to LTLM services

## Prefabs

| Prefab | Description |
|--------|-------------|
| `LTLM-UI` | Complete LTLM-UI Persistence Canvas with all functionality. Fully modular for edits. |

## Customization

All UI uses Unity's UI system with TextMeshPro. Customize by:
- Modifying prefab styles in the Inspector
- Overriding controller scripts
- Replacing sprite assets in Resources

## Dependencies

- [LTLM SDK](https://github.com/LjomaTech/ltlm-unity-sdk.git) - Core licensing SDK
- [LjomaTech Unity Forms](https://github.com/LjomaTech/unity-forms.git) - Form management
- TextMeshPro 3.0+

## Documentation

See the [LTLM SDK Documentation](https://github.com/LjomaTech/ltlm-unity-sdk) for complete integration guides.

## Support

- 📧 Email: support@ljomatech.com
- 🌐 Dashboard: [ltlm.ljomatech.com](https://ltlm.ljomatech.com)

## License

[MIT License](LICENSE.md) © 2025 LjomaTech
