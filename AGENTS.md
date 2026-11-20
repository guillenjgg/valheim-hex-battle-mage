# Valheim development

- Prefabs are the source of truth for authored defaults. Serialize item values, equipment roles, progression settings, visual settings and behavior settings on the prefab or explicitly referenced, inspectable profile assets so inspecting the prefab in Unity reveals its complete intended configuration.
- Runtime code and configuration may override authored values, but must start from those prefab defaults. Keep override precedence explicit and document runtime-derived values. Do not silently replace authored settings with hardcoded initialization values.
- New balance and progression changes must have a clear named tuning entry point under `HexBattleMage/Core/Tuning/`, in addition to inspectable prefab defaults. Do not leave values editable only in an asset builder or C# component initializer. Update `docs/PrefabTuning.md` with the edit location and verify overrides survive recipe hydration and runtime registration.
- Asset builders must preserve existing authored values on rebuild and seed defaults only for new settings or assets. For new or changed features, move their defaults into inspectable prefab components or referenced profiles. Existing code-only settings remain migration work; do not claim all existing prefabs are already complete.

- Validate game behavior, member visibility, and Harmony hook signatures against the installed Valheim assemblies before implementing changes. The runtime assembly is `G:\SteamLibrary\steamapps\common\Valheim\Valheim_Data\Managed\assembly_valheim.dll`.
- Publicized build references do not change runtime member visibility. Use public APIs or Harmony access mechanisms for private members.
- Consult the Valheim Modding Wiki when applicable, including its animation replacement guide, and Jotunn documentation for Jotunn APIs.
- The local Jotunn reference is the compiled DLL at `C:\Users\Jonathan\Documents\valheim_mods\Jotunn-2.30.0\plugins\Jotunn.dll`. Inspect its compiled code when validating Jotunn behavior.

## Back attachment baseline

Fit new BattleMage armor as matching sets around the male/female player bodies and this mod's own authored garment prefabs. The user explicitly superseded cross-fitting to vanilla/high-tier armor on 2026-11-18. Do not inflate starter gear to accommodate unrelated armor. Preserve native skin mapping, body/world collision and working motion behavior while applying the matching-set fit settings documented in docs/MatchingSetFit.md.

Before changing or creating cloak/back attachments, read `docs/Cloaks.md`, especially **Accepted baseline for future back attachments**. The user accepted the current in-game cloak on 2026-11-17. Preserve that baseline unless the requested work requires a change. Reuse its single-surface authoring, native skin/bone mapping, prefab-authored defaults, managed baked-data loading, standalone-player validation and non-blocking collision fallback as appropriate. Do not repeat the superseded motion tuning or let optional protection abort native equipment attachment.

## Attunement authority

Wand attunements belong to the character. The Rune Block authors the definition catalog and active limit; AttunementTuning supplies code overrides. Learned and active state uses Player.m_customData. Do not restore per-wand elemental selections or let spare/replacement wands bypass the character limit. All learning, activation and replacement occur at the Rune Block. Item quality/durability/style remain native item state. Current limit is one; later limits must enforce the same cap across all wands.

## Gear presentation

- List gear perks near the top of tooltips, alongside armor/resistance, before durability, repair level, weight and crafted-by details.
- New armor should expose cosmetic choices through the shared prefab-authored MageArmorStyleProfile. Verify native variant propagation for each equipment slot when introducing it.
- BattleMage texture imports use maximum size 256, Read/Write enabled, Normal Quality compression, Crunch enabled and quality 100. Keep the scoped import policy and bundle validation synchronized with Unity.
- Appearance catalogs accumulate per equipment category: later tiers retain earlier looks and add options. Keep saved variant ordering stable; cosmetic unlock tier is separate from item upgrade quality.
