# Wand elemental testing

The wand now directly references all five profiles through its WandProfile component. Tip/impact colors, spread/stack/stun rules and the native burning visual source are also authored on those profiles. See [PrefabSettings.md](PrefabSettings.md) for the complete component map and preserving rebuild workflow.

Use the BepInEx Configuration Manager under HexBattleMage:

- `Development / Elements / ActiveElement` selects Fire, Frost, Spirit, Lightning or Poison for all test wands.
- `Development / Elements / Enabled` enables the custom system. Disable it to return to the previous native wand element behavior and expire custom statuses.
- Each `Development / Elements / <Element>` section contains live balance overrides. The existing config-file watcher also reloads edits made outside the game every 0.5 seconds.

Initial damage changes apply to the next shot. Active statuses read duration, tick damage, tick interval, slow strengths, spread radius/check interval, stack cap and boss DoT multiplier from the current configuration. The selector only changes future shots; an existing Fire mark remains Fire after switching to another element.

For isolated spread tests, set `Development / Projectile / AreaOfEffectRadius` to `0`; the existing default impact AoE can otherwise infect several enemies immediately.

## Starting values

| Element | Initial elemental damage | DoT damage per tick | Duration | Other effects |
|---|---:|---:|---:|---|
| Fire | 30 | 6 | 5 s | One tick per second; no slow |
| Frost | 20 | 0 | 4 s | 30% movement and attack animation slow; refreshes without stacking |
| Spirit | 10 | 8 | 8 s | Automatic 8 m spread; refreshes without stacking |
| Lightning | 25 | 0 | 1 s | Stops movement/attack animation, stops the current attack and blocks new attacks |
| Poison | 5 | 3 per stack | 10 s | Automatic 8 m spread; refreshes and stacks up to 4; boss DoT multiplier 0.25 |

Fire duration/tick interval, Frost duration/slow strength, both spread radii and Poison's four-stack cap follow the agreed design. Raw damage values, Spirit/Poison durations, Lightning stun duration, the 0.5 second spread-check interval and Poison's boss multiplier are provisional testing defaults.

The existing positive blunt/pierce damage remains. Initial elemental damage retains a configurable increment of 6 per additional item quality. Custom DoT damage is independently configured per tick and does not currently scale with quality, skill or projectile range. Initial projectile damage still uses the existing skill/attack/range calculations.

## Spread and combat rules

Spirit and Poison can infect further enemies from each newly infected host. Each source can affect a particular neighbour once until a fresh damaging wand hit resets that source's spread list. Receiving a spread refresh does not reset its spread list, preventing an endless immediate reinfection loop. Distinct infected sources can add Poison stacks up to the cap. Tick timers continue through refreshes so repeated hits do not postpone DoT damage.

Spread uses three-dimensional distance without a wall/line-of-sight test. It excludes players and tamed creatures and requires hostility toward the original attacker. Spread goes through native `Character.Damage` status transport with zero initial damage and the original attacker identity. If that attacker is no longer available, existing DoTs continue but stop spreading.

Native elemental resistances and immunities still matter. Custom ticks use their elemental damage channel and apply current resistances. Tagged wand hits bypass the native secondary elemental statuses, avoiding duplicate burns, poison or frost slow. Fire, Spirit and Poison now deliver their configured initial damage immediately after native resistance/armor calculations, instead of routing that initial amount into vanilla DoTs. Frost and Lightning retain native initial-hit damage. Untagged attacks keep their vanilla handlers.

Lightning controls and Frost animation speeds are maintained on the target's owner. An owner-written animation-speed multiplier is shared through ZDO data for other peers. All players need the new DLL; use matching elemental configurations for co-op tests. Server-enforced configuration synchronization is not implemented.

## Prefab authoring

Five data-only prefabs live in HexModsUnityProject under `Assets/_CustomItems/HexBattleMage/Elements/HexWandElement<Element>.prefab`. They contain `HexBattleMage.Core.Elements.WandElementProfile`. Its mirrored source and the `HexBattleMage` assembly definition are under the Runtime subfolder, matching the runtime DLL's assembly/type identity. Do not place the full mod DLL in this Unity project.

`UnityEditor/WandElementAssetBuilder.cs` is mirrored into `Assets/Editor`. Use **HexBattleMage > Prepare Wand Elements and Build Bundle**, or the one-shot `Temp/HexWandElements.request`. The builder creates the baseline profile defaults, preserves prefab GUIDs, validates dependencies, checks the five profiles and existing production prefabs, builds and copies the bundle. It does not rebuild the approved wand/bracer models or cosmetic assets. Existing authored profile values and GUIDs are retained on subsequent runs; baseline numbers are only assigned when a profile is first created. Use configuration overrides for balance testing.

New defaults apply when a config entry is first created. Existing saved configuration values take precedence. Editing config does not require rebuilding the asset bundle. Changes to authored prefabs or profile serialization do require a bundle rebuild.

Mage Table element selection, tier unlocks, later-tier appearances and progression costs remain unimplemented. `ActiveElement` is a development testing shortcut.

## Verification

Compiled against the installed unmodified Valheim assembly and compiled Jotunn 2.30.0. Unity checks load the actual bundled profile prefabs, patch the actual five native elemental damage calls, and exercise native SEMan cloning, refresh and removal with synthetic characters. Checks cover Fire tick counts and live tuning; Frost movement/attack slow and animation restoration; Lightning attack/movement suppression; Spirit chain spread without stacks; Poison refresh, four-stack cap and finite feedback; boss multiplier; immunity; vanilla burn isolation; active element selection; physical baseline preservation; and disabling/restoring the old element behavior.

Character world/health and network transport were substituted in these checks. Full in-game combat, multiplayer, ownership transfer and interactions with the user's other mods remain unverified. Test the five elements individually before building progression on top.
