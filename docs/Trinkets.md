# Trinkets

The mod supports multiple trinket definitions, with one equipped at a time in Valheim's native trinket slot. Only Stone of Reprisal currently ships. Extra definitions used during validation are test fixtures, not new gameplay items.

Player-facing profile values are localization tokens; see [Localization.md](Localization.md) for translation keys and live tooltip formatting.

## Code organization

| Location | Responsibility |
| --- | --- |
| `Core/Trinkets/HexBattleMageTrinketProfile.cs` | Inspectable item, attachment, tooltip and supported reward settings. |
| `Core/Trinkets/TrinketSettings.cs` | Separate configuration entries and reward identity for each definition. |
| `Core/Managers/TrinketManager.cs` | Discover, validate and register bundled definitions; resolve the actual native equipped slot. |
| `Core/Trinkets/TrinketEffects.cs` | Equipped passives, successful-parry rewards, tooltips and cleanup. |
| `Core/Trinkets/TrinketRewardStatus.cs` | Temporary reward state, including whether the latest parry was against a boss. |
| `Patches/TrinketPatches.cs` | Native equipment, Eitr, parry, projectile and tooltip hook entry points. |
| `Core/Visuals/TrinketWornVisual.cs` | Shared worn placement relative to the live attachment and character axes. |

`HexBattleMageTrinketProfile` is the single shared data component for every trinket. There is no stone-specific subclass or Reset method. The first authored asset and registered item are both named `HexBattleMage_Trinket_Meadows_01`; its player-facing name remains Stone of Reprisal.

## Adding another definition

1. Author its prefab under `Assets/_CustomItems/HexBattleMage/Trinkets/<Tier>/<TierSlot>`, using a mod-prefixed progression name such as `HexBattleMage_Trinket_Meadows_02`. Material and behavior belong in the prefab values and visuals, not its identity.
2. Add `HexBattleMageTrinketProfile`, `MageEitrProfile`, `MageEquipmentDefinition` and `MageRecipeProfile`. Set category and slot to Trinket.
3. Supply a unique item prefab identity, display text, icon, native base, item stats, attachment settings and recipe. Assign the prefab to `hexbattlemage`.
4. Choose `RewardTrigger = None` for passive-only equipment or `SuccessfulParry` for the existing configurable wand-damage reward. Set its duration, normal/boss bonuses and display text when applicable.
5. Run **Prepare Authored Defaults and Build Bundle**, then rebuild the DLL. `TrinketAssetPreparation` refreshes recipe JSON from the resource array, preserving existing referenced recipe paths and authored settings. Recipe TextAssets are explicitly assigned to the bundle so name-based recovery works when a Unity reference is unavailable. Runtime discovery finds all bundled prefab roots with a `HexBattleMageTrinketProfile`; no registration list needs editing.

New kinds of combat reward require an explicit effect implementation and trigger hook. They should share the existing registry, equipment and passive handling rather than duplicate the manager. The enum exposes only implemented behavior.

## Independent settings and naming

Each definition owns its configuration entries and reward hash. Blank config sections derive as `Equipment / Trinkets / <ItemPrefabName>`. Blank reward names derive as `<ItemPrefabName>ParryEmpowerment`. Duplicate item identities, config sections or reward names are rejected before replacing the registry. Passive-only definitions do not bind parry settings, create combat reward statuses or show combat/boss tooltip sections.

The first prefab lives at `Trinkets/Meadows/Trinket_Meadows_01/HexBattleMage_Trinket_Meadows_01.prefab`. Leave ConfigSection and RewardStatusName blank to derive independent identities from its progression-based item name. Its defaults remain +25 Eitr, +15% regeneration and a 2-second +25% wand reward, increased to +35% after a boss parry. This development rename intentionally has no aliases or migrations for old saved items or config sections.

Only the actual equipped trinket's settings apply. Replacing or unequipping it clears registered trinket reward statuses. Replacement preserves existing current Eitr up to the new maximum; moving back to a larger pool does not refill it. Death also clears rewards. Native equipment serialization and replication own slot state and peer visibility.

## Verification

The isolated native-method harness passes 65 checks, including production-bundle discovery, the renamed Meadows profile, three distinct definitions, native equip/replace/unequip and saved-equipment restoration, independent passives/rewards, passive-only behavior, Eitr clamping without refill, boss parries, refresh/expiry and wand-only shot scaling. Network transport, visual rig setup, damage text, analytics and stagger physics are isolated. Full in-game startup, visual fit and multiplayer should be retested after this refactor.

`Tests/Trinkets/TrinketRegistryValidation.cs` provides a checked-in registry regression test for per-definition defaults, overrides, unique reward identities, passive-only reward configuration, duplicate safeguards and avoiding partial registry replacement. Tooltip localization is checked separately under Tests/Localization. See each directory's README for the isolated runner requirements.

Registration resolves the authored native base with PrefabManager.GetPrefab, then uses AssetManager.ClonePrefab under Jotunn's inactive prefab container. It deliberately avoids CreateClonedPrefab: that helper rejects an item name whenever its broad prefab lookup finds the bundled visual with the same name. AddItem/AddPrefab still perform their normal registered-item duplicate checks. PrefabContainer is an internal Jotunn property accessed with a Harmony method delegate; its getter and clone behavior were verified against local 2.30.0 and installed 2.30.2 assemblies. The registration regression test covers this loaded-visual collision independently of combat/equipment checks.


2026-11-20 update: Stone of Reprisal passive defaults are +10% Eitr regeneration and +10% stamina regeneration, without flat Eitr. Gear supplies flat pools. Existing parry damage rewards remain unchanged. Rune Block authored version 5 replaces the below-origin capsule with a fitted box (bottom at ground origin); rebuild previously placed blocks to correct their saved height.
