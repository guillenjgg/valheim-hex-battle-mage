# Inspecting authored prefab settings

The current wand, projectile, bracer, Mage Table and five elemental profiles now store their authored defaults in Unity. Open the prefabs in `HexModsUnityProject`, under `Assets/_CustomItems/HexBattleMage`.

| Prefab | Inspect these components |
|---|---|
| `Wands/HexBattleMageWand/HexBattleMageWand.prefab` | `ItemDrop` for native item values; `WandProfile` for combat/config defaults, light, constant rune color, holster placement, style materials, projectile reference and all five elemental profile references; `MageRecipeProfile` for crafting/upgrade costs; `MageEquipmentDefinition` for category, slot and progression metadata |
| `Projectiles/HexMagicBolt.prefab` | `Projectile` for native flight/lifetime rules; `WandProjectileProfile` for collision/AoE defaults, flight rendering, impact geometry, sound source, native-effect suppression and surface placement |
| `Shields/Apprentice/HexMagicBracerVisual.prefab` | `MagicBracerProfile` for shield values, durability, weight, drop collider, male/female fit, constant rune glow and field animation; `MageRecipeProfile` for costs; `MageEquipmentDefinition` for wearable/blocking roles; child meshes/materials for cuff and field geometry |
| `Elements/HexWandElement<Element>.prefab` | `WandElementProfile` for initial/tick damage, quality scaling, duration, slows, spreading/stacking/stun rules, target exclusions, tip/impact colors and native status visual source |
| `Crafting/HexArcaneWorkbench.prefab` | `CraftingStation`/`Piece` for native station values; `MageTableProfile` for names/category, halos, crystals, sparks and lighting; `MageRecipeProfile` for building costs |

## Defaults and overrides

1. The prefab/profile is the authored baseline. Component field initializers only seed newly added settings; serialized Unity edits are retained.
2. BepInEx config entries use these authored defaults. Existing saved config values remain explicit overrides, including when they equal an older default. Reset an entry to its new default or remove that entry to test an edited baseline. Existing config keys and legacy-section migration remain supported.
3. Runtime code computes current state: equipped/usable flags, body-mesh fit selection, skill/resistance calculations, status stacks/timers, animation state and networking. These are not fixed prefab values.

`WandProfile` is authoritative for the settings that also appear on native `ItemDrop.Attack`, the physical damage channels and the tip light's range/intensity/color. Preparation projects those defaults onto the native components so they agree in the Inspector. Other native item/projectile settings remain directly authored on their existing components. The native light component retains its authored light type, shadows and render mode.

`MagicBracerProfile.BaseShieldPrefab` explicitly identifies the installed native shield used at registration. Profile fields override its shield stats and wearable settings. Remaining native shield rules and effect references are inherited from that installed prefab, rather than copied native assets. The registered item receives the current native values. `InheritNativeBlockEffects` controls only the inherited block-effect list. The Fire profile's `NativeVisualStatus` similarly identifies the installed status supplying visual effects; it does not borrow its damage calculations.

Category/slot, catalog biome and the Apprentice tier label are inspectable metadata. Later tier unlocks and Mage Table element attunement are still unimplemented. Stable save identities, Harmony hooks, RPC/ZDO keys, shader property names, UI layout and numerical implementation tolerances remain code contracts, not equipment tuning values.

## Build without resetting edits

Use **HexBattleMage > Prepare Authored Defaults and Build Bundle**. It fills missing profile components, preserves existing references/settings, updates native projections, validates dependencies, builds the Windows bundle and copies it to the mod repository. Rebuild the DLL afterward to embed that bundle.

The older wand-rune, elemental, bracer and Mage Table build commands now preserve existing authored assets. They do not regenerate approved geometry or overwrite material edits during normal rebuilds. First-time geometry generation remains available when an asset is absent; changing a mesh or rune layout is a separate intentional authoring operation.

Scripts are mirrored from the repository into Unity: pure data components compile in the `HexBattleMage` runtime assembly, while `UnityEditor` tools belong in `Assets/Editor`. Do not import the compiled mod DLL alongside those data scripts. Never edit `ValheimRIP`.

## Verification

The preservation check is available as **HexBattleMage > Validate Authored Defaults Preservation**. It checks repeated preparation for byte-stable prefab output and deliberately edits a light value and recipe cost to confirm legacy preparation retains them, then restores the originals and rebuilds.

Isolated Unity checks load the actual production bundle and verify profile references, default/config precedence, tip colors versus constant runes, unchanged environmental lighting, shield tooltip values, equip/blocking behavior, male/female fit and all five elemental effects. Network/world transport and the equipment-visual interface are substituted where game bootstrap is unavailable. In-game save reload, combat/UI integration and multiplayer remain required.

Recipe preparation writes the complete authored recipe to a referenced TextAsset under Settings/Recipes. This replaces the failed inline JSON recovery used briefly during the migration. Runtime registration reads this baked baseline once before applying runtime overrides. If the component reference is unavailable during startup, the data is loaded directly from the bundle by its stable recipe asset name, including the legacy wand-name mapping. No ingredient values are hardcoded as a fallback. Wand registration now waits for Jotunn OnVanillaPrefabsAvailable, matching bracer/table registration, and validates the generated recipe before reading it. Standalone player checks force empty inline data and a missing asset reference, then verify exact ingredient and station recovery for all three recipes. Platform login was disabled only in a test copy of the game assembly; installed Valheim assemblies were not modified.

Recipe JSON is encoded/decoded with the standard .NET data-contract serializer rather than Unity JsonUtility. This uses the System.Runtime.Serialization assembly already shipped with Valheim; no external package or additional deployed file is needed. Decode failures include the recipe asset name, byte count and validation details.

## Stone trinket

`Trinkets/ApprenticeStone/HexStoneParryTrinketVisual.prefab` exposes StoneTrinketProfile (identity, icon, native base, item stats, reward values, worn placement and drop collider), MageEitrProfile (flat maximum Eitr and regeneration fraction), MageEquipmentDefinition (native trinket category/slot) and MageRecipeProfile (station, level and ingredients). The initial defaults are +25 Eitr, +15% regeneration, +25% wand projectile damage and a 5-second reward. These are test balance values.

`Equipment / Stone Trinket` config entries MaximumEitr, RegenerationModifier, ParryWandDamageBonus and ParryDuration default from those authored profiles. Saved configuration overrides prefab values. Existing configuration hot reload applies to the next stat evaluation, shot or parry; an already active status retains the duration it received at activation. Native stat evaluation refreshes the Eitr cap periodically; equip/unequip applies it immediately without filling current Eitr. The reusable MageEitrProfile can supply Eitr on future clothing. Native trinket-slot state remains the authority for this item, independent of the bracer/belt slots.

Registration copies simple profile fields directly, avoiding Unity JSON hydration. The compiled mod's isolated integration checks exercise native equip/replace/unequip, no-food Eitr cap and regeneration modifier, the actual native boss-parry branch, failed/held blocks, timed refresh/expiry, death, wand-only projectile scaling and worn transform orientation. Network status transport, visual rig setup, damage text, analytics and stagger physics are isolated in the test scene. Full in-game behavior remains to be confirmed.
