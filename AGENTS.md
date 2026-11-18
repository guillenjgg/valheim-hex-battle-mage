# Valheim development

- Prefabs are the source of truth for authored defaults. Serialize item values, equipment roles, progression settings, visual settings and behavior settings on the prefab or explicitly referenced, inspectable profile assets so inspecting the prefab in Unity reveals its complete intended configuration.
- Runtime code and configuration may override authored values, but must start from those prefab defaults. Keep override precedence explicit and document runtime-derived values. Do not silently replace authored settings with hardcoded initialization values.
- Asset builders must preserve existing authored values on rebuild and seed defaults only for new settings or assets. For new or changed features, move their defaults into inspectable prefab components or referenced profiles. Existing code-only settings remain migration work; do not claim all existing prefabs are already complete.

- Validate game behavior, member visibility, and Harmony hook signatures against the installed Valheim assemblies before implementing changes. The runtime assembly is `G:\SteamLibrary\steamapps\common\Valheim\Valheim_Data\Managed\assembly_valheim.dll`.
- Publicized build references do not change runtime member visibility. Use public APIs or Harmony access mechanisms for private members.
- Consult the Valheim Modding Wiki when applicable, including its animation replacement guide, and Jotunn documentation for Jotunn APIs.
- The local Jotunn reference is the compiled DLL at `C:\Users\Jonathan\Documents\valheim_mods\Jotunn-2.30.0\plugins\Jotunn.dll`. Inspect its compiled code when validating Jotunn behavior.

## Back attachment baseline

Before changing or creating cloak/back attachments, read `docs/Cloaks.md`, especially **Accepted baseline for future back attachments**. The user accepted the current in-game cloak on 2026-11-17. Preserve that baseline unless the requested work requires a change. Reuse its single-surface authoring, native skin/bone mapping, prefab-authored defaults, managed baked-data loading, standalone-player validation and non-blocking collision fallback as appropriate. Do not repeat the superseded motion tuning or let optional protection abort native equipment attachment.
