# LTLM UI Toolkit

[![Unity 2020.3+](https://img.shields.io/badge/Unity-2020.3%2B-black.svg?logo=unity)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)

Pre-built UI components for the **LTLM License & Token Management SDK**. Drop-in UI for license activation, login flows, and customer portal interfaces.

## Installation

### Unity Package Manager (Recommended)

Add to your `manifest.json`:

```json
{
  "dependencies": {
    "com.ljomatech.ltlm.uitoolkit": "https://github.com/LjomaTech/ltlm-unity-sdk-ui-toolkit.git"
  }
}
```

This will automatically install dependencies:
- [LTLM SDK](https://github.com/LjomaTech/ltlm-unity-sdk.git)
- [LjomaTech Forms](https://github.com/LjomaTech/unity-forms.git)

Or via Package Manager UI: **Window → Package Manager → + → Add package from git URL**

## Features

- **License Activation UI** - Complete activation flow with key input
- **OTP Login** - Email-based one-time password authentication
- **License Dashboard** - Display license status, expiry, capabilities
- **Seat Management** - View and release concurrent seats
- **Token Display** - Show token balance and consumption
- **Offline Activation** - Air-gapped license support

## Quick Start

1. Import the package via Package Manager
2. Add LTLM-UI prefab from `Prefabs/` to your scene
3. Configure your project keys in the LTLM Manager
4. The UI automatically connects to LTLM services

## Prefabs

| Prefab | Description |
|--------|-------------|
| `LTLM-UI` | Complete LTLM-UI Presistance Canvas with all functionality. fully modular for edits. |

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
