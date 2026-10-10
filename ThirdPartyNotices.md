# Third-Party Notices

SpeakBack (Uprise Studios / J M Games) uses the third-party items below. Licence policy (D-018): anything shipped must be MIT, BSD, Apache-2.0, Zlib, OFL, CC0 or CC-BY. Every new item is added here in the same change that adds it.

## Shipped in the player build

| Item | Version / source | Licence | Where |
|---|---|---|---|
| Baloo 2 (variable, wght 400–800) | google/fonts `ofl/baloo2/Baloo2[wght].ttf`, downloaded 2026-10-10, SHA-256 `d47a6852…abb025c2` | SIL Open Font Licence 1.1. © 2019 The Baloo 2 Project Authors (github.com/EkType/Baloo2) | `Assets/_SpeakBack/Art/UI/Fonts/Baloo2-Variable.ttf`, licence text alongside (`Baloo2-OFL.txt`) |
| Nunito (variable, wght 200–1000) | google/fonts `ofl/nunito/Nunito[wght].ttf`, downloaded 2026-10-10, SHA-256 `bb55a5ca…a1e85207` | SIL Open Font Licence 1.1. © 2014 The Nunito Project Authors (github.com/googlefonts/nunito) | `Assets/_SpeakBack/Art/UI/Fonts/Nunito-Variable.ttf`, licence text alongside (`Nunito-OFL.txt`) |
| Liberation Sans (TMP default font) | TMP Essential Resources from `com.unity.ugui` 2.0.0 | SIL Open Font Licence 1.1. © 2010 Google Corporation; © 2012 Red Hat, Inc. | `Assets/TextMesh Pro/Fonts/` (licence text alongside) |
| TextMesh Pro essentials (shaders, settings, style sheet) | `com.unity.ugui` 2.0.0 | Unity Companion Licence (Unity package content used with the Unity engine) | `Assets/TextMesh Pro/` |
| Unity engine and packages | See `docs/dependency-decisions.md` | Unity Terms of Service / Unity Companion Licence | `Packages/` |

The EmojiOne sample sprite sheet that ships inside TMP Essential Resources was **removed**, because its licence terms aren't confirmed against D-018. It would otherwise have shipped from `Resources/`.

## Not shipped

| Item | Use | Licence |
|---|---|---|
| NUnit 3.14.0, NUnit3TestAdapter 4.6.0, Microsoft.NET.Test.Sdk 17.11.1 | `Tools/DotnetTests` (NuGet) | MIT |
