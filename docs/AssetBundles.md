# Asset bundle workflow

Work only in HexModsUnityProject. Never edit ValheimRIP.

The production hexbattlemage bundle contains HexArcaneWorkbench, HexBattleMageWand, HexMagicBolt, the alternate Apprentice style materials, and their dependencies. Test prefabs remain in Unity but have no production bundle assignment. The wand keeps its existing runtime prefab identity for save compatibility.

Use HexBattleMage > Prepare Mage Table and Build Bundle in Unity. MageTableAssetBuilder prepares table materials, alchemy details and opposite support runes, applies the bundle policy, validates dependencies, builds for Windows, verifies the prefabs, and copies the bundle into HexBattleMage/Assets/AssetBundles/hexbattlemage in the mod repository. The one-shot request is Temp/HexBattleMagePrepare.request; the result is written to the shared workspace's mage-table-prepare-result.txt.

HexBattleMage > Clean Shader Dependencies and Build Bundle runs the shader policy separately. HexBattleMageBundlePolicy removes test bundle assignments and obsolete projectile particles, uses the current projectile reference on the wand, and replaces native prefab references with Jotunn mocks. Validation rejects copied native asset folders and imported shader assets in the dependency graph. Keep the scripts under UnityEditor synchronized with Assets/Editor in Unity.

Materials use JVLmock_Custom/Piece and their authored textures. Jotunn replaces mock shaders and prefab references at runtime. See https://valheim-modding.github.io/Jotunn/tutorials/asset-mocking.html. The worn DarkOak_Worn_BaseColor texture is used for the tabletop; the original texture is retained. AlchemyDetails is preserved on subsequent builds. Six opposite support rune meshes complement the original FBX runes.

The floating Mage Table graphics and wand projectile flight and impact graphics are created by C# using Sprites/Default. Runtime-only graphics changes do not require a bundle rebuild. Unity prefab, material, texture, and model changes do.

## Wand impacts

Runtime registration clears both native hit-effect lists so the fire-staff explosion and its smoke are never spawned. The projectile-owner callback broadcasts the impact point, actual AoE radius, and elemental flags to co-op peers. Surface probes move visuals just outside the struck collider when possible. Each peer renders a brief expanding spark burst with the original impact thump, without a circle border, smoke, filled sphere, colliders, or impact light. Spark reach is capped at 0.5 metres and stays within the shot's AoE; radius zero uses a 0.12 metre contact flash. Visuals do not change damage or its OverlapSphere radius.

Development / Projectile provides ImpactVisualDuration, ImpactBurstOpacity (default 0.85), and ImpactLineWidth. ImpactBurstOpacity replaces the old faint ImpactOutlineOpacity setting. All co-op players need the updated DLL because the custom impact RPC includes element flags. Multiplayer appearance still requires in-game verification.

Positive elemental damage channels select spark colors: orange fire, icy blue frost, violet spirit, blue-white lightning, and green poison. Mixed-element shots display all applicable colors; physical-only shots use BoltColor. These visuals appear on accepted surface impacts without applying character statuses to terrain, trees, or objects.

ImpactSoundVolume controls local impact sound volume from 0 (muted) to 1 (existing loudness), with a default of 1. It hot reloads for the next impact, including other players' wand impacts heard locally. It does not change the game's mixer settings. The sound object outlives the brief visual so playback is not cut short.
