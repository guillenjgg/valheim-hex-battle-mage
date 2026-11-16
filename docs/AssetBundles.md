# Asset bundle workflow

Elemental test profiles are documented in [WandElements.md](WandElements.md). The production bundle also includes the five `HexWandElement<Element>` defaults prefabs. Use the dedicated elemental preparation command to rebuild them without regenerating approved equipment visuals.

Work only in HexModsUnityProject. Never edit ValheimRIP.

The production hexbattlemage bundle contains HexArcaneWorkbench, HexBattleMageWand, HexMagicBolt, HexMagicBracerVisual, MagicBracerIcon, the alternate Apprentice style materials, and their dependencies. Test prefabs remain in Unity but have no production bundle assignment. The wand keeps its existing runtime prefab identity for save compatibility.

Use HexBattleMage > Prepare Mage Table and Build Bundle in Unity. MageTableAssetBuilder prepares table materials, alchemy details and opposite support runes, applies the bundle policy, validates dependencies, builds for Windows, verifies the prefabs, and copies the bundle into HexBattleMage/Assets/AssetBundles/hexbattlemage in the mod repository. The one-shot request is Temp/HexBattleMagePrepare.request; the result is written to the shared workspace's mage-table-prepare-result.txt.

HexBattleMage > Clean Shader Dependencies and Build Bundle runs the shader policy separately. HexBattleMageBundlePolicy removes test bundle assignments and obsolete projectile particles, uses the current projectile reference on the wand, and replaces native prefab references with Jotunn mocks. Validation rejects copied native asset folders and imported shader assets in the dependency graph. Keep the scripts under UnityEditor synchronized with Assets/Editor in Unity.

Materials use JVLmock_Custom/Piece and their authored textures. Jotunn replaces mock shaders and prefab references at runtime. See https://valheim-modding.github.io/Jotunn/tutorials/asset-mocking.html. The worn DarkOak_Worn_BaseColor texture is used for the tabletop; the original texture is retained. AlchemyDetails is preserved on subsequent builds. Six opposite support rune meshes complement the original FBX runes.

The floating Mage Table graphics and wand projectile flight and impact graphics are created by C# using Sprites/Default. Runtime-only graphics changes do not require a bundle rebuild. Unity prefab, material, texture, and model changes do.

## Magic bracer assets

`UnityEditor/MagicShieldAssetBuilder.cs` is mirrored to `Assets/Editor/MagicShieldAssetBuilder.cs` in HexModsUnityProject. Use **HexBattleMage > Prepare Magic Bracer and Build Bundle**. This prepares only the shield assets, validates production dependencies, builds the Windows bundle, verifies the existing production prefabs and new bracer assets, and copies the bundle to the mod repository. It does not regenerate the approved wand or table assets.

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
