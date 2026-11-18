# Asset bundle workflow

See [PrefabSettings.md](PrefabSettings.md) for the current component map, override precedence and preservation-safe build command. Wand, projectile, bracer, elemental and Mage Table defaults have been migrated to inspectable authored profiles.

## Prefab authoring policy

Inspecting a prefab in Unity should reveal its complete authored configuration: item stats, equipment category and slot, capabilities, progression, visual settings and behavior settings. Store defaults as serialized components or explicitly referenced profile assets. Native inheritance must identify its source prefab and expose the effective authored values rather than hiding important settings in registration code.

Runtime adjustments remain supported. Precedence is authored prefab defaults, then explicit runtime or configuration overrides. Configuration defaults must originate from the prefab; document any values derived from current game state. Builders preserve authored edits and seed only new settings or assets. Apply this policy to new and changed features; older code-only settings have not all been migrated yet.

Elemental test profiles are documented in [WandElements.md](WandElements.md). The production bundle also includes the five `HexWandElement<Element>` defaults prefabs. Use the dedicated elemental preparation command to rebuild them without regenerating approved equipment visuals.

Work only in HexModsUnityProject. Never edit ValheimRIP.

## Meadows table extensions

`Crafting/Extensions/HexMageRuneStone.prefab` is a faceted standing stone on a wooden base with a constant blue Algiz-inspired rune. `HexMageReagentStand.prefab` is a short wooden stand holding resin lumps, feathers and bone fragments, with the same blue rune on its front. Both use existing Mage Table material assets; new blue, amber and bone materials are stored beside the prefabs.

Use **HexBattleMage > Prepare Meadows Table Extensions and Build Bundle** (`UnityEditor/MageTableExtensionAssetBuilder.cs`, mirrored to `Assets/Editor`). Existing authored prefabs, geometry, materials and recipes are preserved. The optional one-shot request is `Temp/HexMageExtensions.request`; results go to `mage-extensions-result.txt` in the shared workspace. Rebuild the DLL to embed the resulting bundle.

Each prefab contains native Piece, ZNetView, WearNTear and StationExtension components, colliders and a MageRecipeProfile with a referenced recipe TextAsset. StationExtension exposes the Mage Table reference, 5-metre radius, non-stacking behavior and a Jotunn mock of the native extension connection effect. Runtime registration uses these authored settings and the Mage Table profile's Hammer category. Transparent sprite icons are baked into the Piece components before registration; Jotunn rejects pieces without icons. The builder seeds a missing icon without overwriting an existing authored icon.

Provisional test recipes: Rune Stone = 10 Wood, 10 Stone, 5 Resin; Reagent Stand = 15 Wood, 8 Resin, 5 Feathers, 5 Bone Fragments. Both require a nearby Mage Table and appear under Battle Mage on the Hammer. No custom research unlocks or forced extension order are implemented yet. With both nearby the table reaches level 3; either alone makes level 2. The installed Valheim extension discovery code was exercised for distinct extensions, duplicates, range and removal. Full construction, destruction/refunds, world reload and multiplayer require in-game testing.

## Meadows trinket 01

The selected stone pendant concept is implemented at `Assets/_CustomItems/HexBattleMage/Trinkets/Meadows/Trinket_Meadows_01/HexBattleMage_Trinket_Meadows_01.prefab`. Its six separate mesh assets form a faceted river stone, crossed leather bindings, leather cord, dark engraving border and constant blue Algiz-inspired rune. The stone is about 9 cm tall; the complete pendant and loop are about 22 cm tall. Material colors, emission, texture references and child transforms are authored on the prefab and referenced assets. There is no additional light source.

`UnityEditor/MeadowsTrinket01AssetBuilder.cs`, mirrored to `Assets/Editor`, provides **HexBattleMage > Prepare Meadows Trinket 01 Visual and Build Bundle**. It seeds geometry/materials only when the prefab is absent. Normal rebuilds preserve authored mesh, material, texture and prefab files; byte-identical preservation was checked. The builder validates dependencies, bundles the visual and writes `trinket-meadows-01-preview.png` and `trinket-meadows-01-visual-result.txt` to the shared workspace. The preview uses Standard material equivalents because production materials use Jotunn's native Piece shader mock.

Shared trinket authoring is documented in [Trinkets.md](Trinkets.md). The visual includes HexBattleMageTrinketProfile, MageEitrProfile, MageEquipmentDefinition (Trinket/Trinket) and MageRecipeProfile. **HexBattleMage > Prepare Meadows Trinket 01 Gameplay and Build Bundle** (`UnityEditor/MeadowsTrinket01GameplayBuilder.cs`) seeds missing components and a transparent sprite icon while preserving existing authored values and geometry. Recipe JSON is refreshed from the authored resource array. Both data scripts are mirrored into the Unity runtime assembly; the editor builders are mirrored to Assets/Editor. Rebuild the DLL after the bundle.

Runtime registration clones the authored Amber base for native world-drop/network behavior, projects the authored item stats, then assigns the native Trinket category without an adrenaline equip effect. It installs a world model/collider and a separate `attach_Neck` hierarchy through the native armor-attachment path. The worn visual reads the current neck anchor and character axes so tilted rig bones do not turn the rune inward. WornPosition is measured in character axes; WornRotation, WornScale and AttachmentBone remain inspectable. Native equipment synchronization handles peer visibility. Body/armor clearance and full in-game appearance remain untested.

The production hexbattlemage bundle contains HexArcaneWorkbench, HexBattleMage_Wand_Meadows_01, HexMagicBolt, HexMagicBracerVisual, MagicBracerIcon, the alternate Apprentice style materials, and their dependencies. Test prefabs remain in Unity but have no production bundle assignment. The wand uses its authored progression identity at runtime; old development identities have no aliases.

Use HexBattleMage > Prepare Authored Defaults and Build Bundle in Unity for normal rebuilds. The older Prepare Mage Table and Build Bundle command delegates to that preserving workflow when the table exists. Initial table material/model generation is retained for first-time authoring. The one-shot request is Temp/HexBattleMagePrepare.request; the result is written to the shared workspace's mage-table-prepare-result.txt.

HexBattleMage > Clean Shader Dependencies and Build Bundle runs the shader policy separately. HexBattleMageBundlePolicy removes test bundle assignments and obsolete projectile particles, uses the current projectile reference on the wand, and replaces native prefab references with Jotunn mocks. Validation rejects copied native asset folders and imported shader assets in the dependency graph. Keep the scripts under UnityEditor synchronized with Assets/Editor in Unity.

Materials use JVLmock_Custom/Piece and their authored textures. Jotunn replaces mock shaders and prefab references at runtime. See https://valheim-modding.github.io/Jotunn/tutorials/asset-mocking.html. The worn DarkOak_Worn_BaseColor texture is used for the tabletop; the original texture is retained. AlchemyDetails is preserved on subsequent builds. Six opposite support rune meshes complement the original FBX runes.

The floating Mage Table graphics and wand projectile flight and impact graphics are created by C# using Sprites/Default. Runtime-only graphics changes do not require a bundle rebuild. Unity prefab, material, texture, and model changes do.

## Magic bracer assets

`UnityEditor/MagicShieldAssetBuilder.cs` is mirrored to `Assets/Editor/MagicShieldAssetBuilder.cs` in HexModsUnityProject. Use **HexBattleMage > Prepare Magic Bracer and Build Bundle**. For an existing bracer this preserves its authored components, meshes and materials, migrates missing settings, validates dependencies, builds the Windows bundle and copies it to the mod repository. It does not regenerate the approved wand or table assets.

The bundle includes `HexMagicBracerVisual` and `MagicBracerIcon`. The visual prefab and its generated meshes/materials live under `Assets/_CustomItems/HexBattleMage/Shields/Apprentice`. The hierarchy contains `BracerModel` and an initially inactive `MagicField`, with separate boundary, translucent veil, and rotating rune ring meshes. Existing mesh and material GUIDs are retained when rebuilding. Bracer materials use the Piece shader mock; the field receives the native `Sprites/Default` shader at runtime. No copied native shield assets enter the bundle.

The runtime item `HexMagicBracer` clones the installed `ShieldWood`, then uses the Hands item category and its own equipment-slot hooks. A `model` child supplies the world-drop mesh. `MagicBracerVisualController` independently instantiates the complete visual under the left-hand anchor while the bracer is worn. There are no hand-held or holstered shield attachment instances. The world drop has a fitted collider; the worn visual and field have no colliders.

The inventory icon is rendered through an RGBA preview target and copied into an RGBA32 texture before PNG export. The static-preview export path discarded alpha and must not be used for this icon. Source PNG and bundled texture alpha were checked separately.

Forearm fitting, body-specific cuff scaling and the 1.5x field display scale are runtime settings. The original field geometry remains 0.96 metres across in the asset; its nominal runtime diameter is 1.44 metres. The independent wearable slot also reuses this asset unchanged. Runtime changes require a DLL rebuild, not a bundle rebuild.

Build results and bracer/field previews are written to the shared workspace as `magic-shield-result.txt`, `magic-bracer-preview.png`, and `magic-shield-preview.png`. In-game positioning and multiplayer validation remain required.
## Wand impacts

Runtime registration clears both native hit-effect lists so the fire-staff explosion and its smoke are never spawned. The projectile-owner callback broadcasts the impact point, actual AoE radius, and elemental flags to co-op peers. Surface probes move visuals just outside the struck collider when possible. Each peer renders a brief expanding spark burst with the original impact thump, without a circle border, smoke, filled sphere, colliders, or impact light. Spark reach is capped at 0.5 metres and stays within the shot's AoE; radius zero uses a 0.12 metre contact flash. Visuals do not change damage or its OverlapSphere radius.

Development / Projectile provides ImpactVisualDuration, ImpactBurstOpacity (default 0.85), and ImpactLineWidth. ImpactBurstOpacity replaces the old faint ImpactOutlineOpacity setting. All co-op players need the updated DLL because the custom impact RPC includes element flags. Multiplayer appearance still requires in-game verification.

Positive elemental damage channels select spark colors: orange fire, icy blue frost, violet spirit, blue-white lightning, and green poison. Mixed-element shots display all applicable colors; physical-only shots use BoltColor. These visuals appear on accepted surface impacts without applying character statuses to terrain, trees, or objects.

ImpactSoundVolume controls local impact sound volume from 0 (muted) to 1 (existing loudness), with a default of 1. It hot reloads for the next impact, including other players' wand impacts heard locally. It does not change the game's mixer settings. The sound object outlives the brief visual so playback is not cut short.

## Bracer protection engraving

The cuff carries an upright Algiz-inspired engraving on its negative local Z face, opposite the original hip-facing engraving. Its mesh follows the tapered cuff surface. The forearm fit and body-specific scales remain unchanged. A runtime property override keeps its emission at twice the authored level whenever the bracer is visible, independent of blocking. The field geometry and animation are unchanged. The bracer preparation command also regenerates its transparent inventory icon from the engraved side.

## Bracer shield stats

MagicBracerProfile on HexMagicBracerVisual stores the editable ShieldWood baseline, block power and per-quality increment, block force and increment, parry bonus, block stamina modifier, durability and increment, and maximum quality. Registration clones the named vanilla shield, applies these authored numbers to the registered HexMagicBracer ItemDrop, and retains native blocking effects and durability behavior. The bracer builder preserves existing profile values. The profile script is mirrored into the Unity HexBattleMage runtime assembly alongside elemental profiles. Changing authored profile values requires rebuilding the bundle and DLL.

## Equipment definitions

MageEquipmentDefinition is a separate data component on HexMagicBracerVisual. Category = Bracer and Slot = Bracer identify wearable bracers; SupportsBlocking enables the shield role. Registration copies the definition onto the registered ItemDrop root so saved inventory items resolve it through their drop prefab. Equipment checks use these fields instead of an exact prefab name. Names and existing saved equipment flags remain unchanged. An item without this definition is not treated as a custom bracer. Disabling SupportsBlocking removes its shield stance, blocker and shield tooltip while retaining its wearable slot and visible cuff. MagicBracerProfile remains the separate source of shield numbers. The asset builder preserves both components' authored values. Both scripts must be mirrored into the Unity HexBattleMage runtime assembly.
