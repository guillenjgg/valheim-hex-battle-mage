# Meadows cloak

## Accepted baseline for future back attachments

User accepted the current cloak in game on 2026-11-17. Treat this as the working baseline; do not retune its motion or regenerate its approved geometry without a requested change. Earlier tuning experiments below are history, superseded by this baseline.

Latest supplied game log: no error/exception entries found; 14 custom body shapes and 105 world probes are configured; successful cloth teams report 126 total colliders and backstop enabled. Each successful build is preceded by a success=False/team=0 callback; this sequence was present in the accepted session and should not alone be reported as a persistent build failure. The exact reason for that preliminary callback has not been established.

Reusable foundation:

- Export a single simulation surface with outward normals; render both sides through the material. Do not simulate both Solidify layers.
- Use the native attach_skin path with the installed player's exact 53-bone order and bindposes. Author/check male and female fits separately.
- Keep all fit, motion and collision defaults inspectable on the prefab or referenced assets. Current accepted motion: animation pose ratio 0.1, damping 0.1, wind influence 1, maximum travel 0.18 m; backstop radius 0.75 m, distance 0; cloth thickness 0.035 m.
- Keep managed, baked body-definition data with primitive DTOs. Test recovery in a standalone player with inline arrays cleared; an Editor-only serialization test is insufficient.
- Custom collision setup must never abort native attachment. Restore original collider lists and allow native setup to continue if custom protection fails. Keep extra collider lists private to the attachment.
- Derive surface contacts from penetration queries; do not call ClosestPoint on terrain/nonconvex mesh colliders. Follow moving support transforms and disable removed supports.
- Apply these lessons to future back attachments as appropriate; rigid items do not automatically need cloth simulation. Body/armor clearance and animation still require in-game validation per new asset. This baseline is accepted, not a guarantee of zero clipping in every situation.

`HexBattleMage_Cloak_Meadows_01` is registered as **Wayfarer Cloak** in Valheim's native cape (Shoulder) slot. Craft at a level-1 Mage Table after discovering 6 Leather Scraps and 4 Deer Hide. These are provisional visual-test costs. Console test: `spawn HexBattleMage_Cloak_Meadows_01 1`.

The gameplay defaults are on `HexBattleMageCloakProfile`, `MageEquipmentDefinition`, `MageEitrProfile` and `MageRecipeProfile` at `Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01/HexBattleMage_Cloak_Meadows_01.prefab` in HexModsUnityProject. Player-facing names and descriptions use localization tokens. The first cloak has weight 4, armor 2, durability 400, maximum quality 1 and no movement penalty. Its root MageEitrProfile grants +5 maximum Eitr while equipped, with no additional regeneration multiplier. The existing gear Eitr system permits native regeneration without Eitr food. Equip/unequip immediately updates the cap without refilling it. Other bonuses remain undecided. Runtime registration clones the profile's native `CapeDeerHide` baseline for networking, equipment and durability behavior, then applies the explicit authored item stats. The base's native equip timing, damage durability drain and drop physics remain inherited.

Both visual variants contain a direct `attach_skin` child, cloth/clasp renderers and 53 native-ordered bones. The authoring rig shows a relaxed fitting pose. A narrowly scoped transpiler selects the appropriate body-fit prefab after the native AttachArmor item lookup; ordinary equipment passes through unchanged. Native AttachArmor still performs bone replacement, cloth initialization, equipment cleanup and LOD integration. Selection reads the receiving VisEquipment's current body-model index, including remote character visuals. It does not use the local player's body type globally. Both fits are retained under an inactive BodyFits child in the registered item so Jotunn can resolve their material mocks. The world drop uses separate baked static meshes without cloth simulation.

The neckline is lowered below the chin. Cloth has no Head or Jaw weights. Both relaxed body fits passed triangle-intersection checks. This does not establish clearance for every beard, armor combination or game animation. Native MagicaCloth initialization and selection are serialized. The armor-aware revision anchors the neck and clasp while allowing the shoulder fabric and lower drape to respond to collision; its in-game appearance is awaiting validation. Native attachment supplies the character's cloth colliders and wind shelter. Native reference body geometry stays exclusively in the editor-only preview folder.

Open `Assets/Editor/HexBattleMage/CloakReference/Cloak_Meadows_01_Preview.unity` to inspect both fits. **HexBattleMage > Cloak > Import Meadows Cloak 01 (No Bundle Build)** preserves existing visual assets. **Prepare Meadows Cloak 01 Gameplay and Build Bundle** seeds missing gameplay settings, icon and static drop model, refreshes recipe JSON, assigns the root to `hexbattlemage`, validates dependencies, builds and copies the bundle into the mod repository. Existing profile, icon, material, geometry and recipe values are preserved. Rebuild the DLL to embed the updated bundle. No separate cloak bundle is needed.

Validation: imported rest meshes retain their authored positions within 0.002 metres; all 53 bone indices match; Head/Jaw changes move the cloak by zero; three authored spine/arm poses produce bounded skinning. The isolated compiled-mod test exercises actual Jotunn registration, recipe creation, authored icon, native cape category, both body-fit selections, vanilla pass-through and application of the native AttachArmor transpiler. Native-base/manager fixtures isolate startup and deferred mock resolution. Full game movement, cloth, armor/beard clearance, death/drop, save/reload and peer visibility require in-game testing.

## Cloak collision protection

Each body-fit prefab's attach_skin child now carries HexBattleMageCloakCollisionProfile. Its authored body capsules, cloth thickness/friction, surface padding, solid layers, probe interval/grid and query capacity can be inspected and tuned in Unity. Normal gameplay preparation preserves these values. The installed game's MagicaClothV2 assembly is a non-copying build reference; no separate mod is required.

A prefix on native VisEquipment.SetupCloth configures protection only on attachments carrying that profile, before native cloth initialization. Fourteen extra body capsules follow current bone positions with world-metre dimensions and independent male/female radii. Edge collision and 3.5 cm cloth thickness are authored defaults. Collider lists are copied before adding protection; vanilla capes and the character's shared list are not edited.

For visible cloaks, a reused baked mesh supplies current cloth bounds every 0.05 seconds. A bounded 5 x 7 x 3 grid samples nearby non-trigger solid geometry using Unity penetration queries, producing finite Magica sphere supports with a 4 cm surface margin. Opposite faces pointing away from the hips are rejected to avoid inflating thin walls. Support positions follow their source transform between scans, including moving/rotating boats; removed/disabled supports are disabled. The query sphere is parked independently below the world so it never joins the player's Rigidbody. All owned helpers are removed with the attachment.

This is bounded collision protection, not a mathematical guarantee of zero intersections. Finite surface sampling, capped nearby-collider buffers, fast movement between probes, fixed/skinned vertices, bulky armor, and spaces too narrow for both body and cloth can still produce clipping or visible displacement. Full in-game animation and solver performance require testing. The implementation does not hide the cloak or modify gameplay collision shapes to conceal intersections.

The compiled-mod collision test passes floor/wall/sloped-surface/inside-solid/nonconvex-mesh/terrain contacts, opposite-face rejection, both cold-loaded profiles, scaled rigs, private collider lists, finite pools, moving support tracking, support removal, own-character exclusion, and application of the actual installed SetupCloth hook with unrelated capes unchanged. Native cloth simulation is not claimed verified by those fixture tests.

## Inward-folding correction

The first in-game collision build produced repeated Physics.ClosestPoint warnings on terrain/nonconvex mesh colliders. Surface ranking now uses the penetration-derived distance instead of that unsupported API. Existing geometry fixtures had tested the surface calculation but had not covered this ranking call.

The exported simulation mesh now uses the original single cloth surface (5,289 imported male vertices), rather than the Solidify shell's two opposing layers (10,836 vertices). The cloth material has Cull = Off and TwoSidedNormals = 1 so both faces remain visible. The Blender source retains its modeling modifier; the physics FBX export excludes Solidify. Geometry, skin weights, paint and existing collision-profile values were retained during the Unity mesh refresh. The preservation-aware migration is recorded in UnityEditor/CloakSingleSurfaceMigration.cs.

HexBattleMageCloakCollisionProfile now exposes UseBackstop, BackstopRadius (0.75 m), BackstopDistance (0) and MaximumTravel (0.18 m). These are also applied to the native MagicaCloth motion constraint in the prefab. Backstops oppose inward movement relative to the animated rest-surface normal; the maximum travel limits large flips. Body colliders alone do not enforce which side of the body a cape must occupy. Full in-game animation remains necessary to assess the final appearance; the screenshot does not establish one exclusive cause of clipping.

CloakMotionValidation invokes the installed MagicaCloth MotionConstraint solver directly with live NativeArrays. It verifies inward-crossing correction, permitted outward flutter and a large displacement limited to 0.18 m. Collision/bundle validation confirms both fits retain the backstop/travel settings and two-sided material. Runtime logs now report protection configuration and cloth build completion, including team/collider counts.

## Animation following and body-definition loading

The follow tuning is authored on each body-fit collision profile: AnimationPoseRatio = 1, MotionDamping = 0.35 and WindInfluence = 0.25. The normal preserving preparation command projects these into native cloth settings, and runtime starts from those authored values. This replaces the old code-only animationPoseRatio = 0.1 for existing registered cloaks without changing their mesh or item stats.

The next in-game log showed successful native cloth builds and no previous ClosestPoint spam, but only 112 colliders (105 world supports plus seven native colliders). This suggests the extra body definition array was unavailable in that game load. Body capsule defaults are now also baked into inspectable Collision_Male.json / Collision_Female.json assets referenced by the profile. Inline values remain editable; if the inline array is empty at runtime, GetBodyCapsules restores the authored definitions from that asset rather than silently omitting body protection. The test deliberately empties the inline array and checks exact bone/radius recovery and all 14 generated body shapes for both fits. Startup now logs the generated body/probe counts. Final game animation/follow appearance still needs user validation.

## Attachment regression repair

The subsequent game log contained InvalidOperationException from GetBodyCapsules in the SetupCloth prefix. This prevented native attachment from completing; it was a mod regression, not evidence of a detached source mesh. The loader now uses managed DataContractJsonSerializer with primitive DTO fields rather than Unity JsonUtility. Both body definitions are freshly baked with this serializer. Latest follow tuning was explicitly rolled back to AnimationPoseRatio 0.1, MotionDamping 0.1 and WindInfluence 1; normal preparation preserves those authored values.

The native cloth prefix now prevalidates body data and catches protection-setup failures, restores the original collider lists, disables any partial controller and allows native SetupCloth to run. A protection error must not abort cape attachment. The negative fixture verifies invalid body data does not escape this hook. CloakLoadPlayer additionally runs outside the Editor as a standalone Unity executable using the actual compiled mod and production bundle: both body profiles recover all 14 authored capsules when inline arrays are removed. Its report passed. This closes the prior test gap where only the Editor runtime had exercised data recovery. Full in-game appearance remains a user check after restarting.

## Upper armor clearance revision (awaiting in-game fit check)

The user reported upper-back/shoulder armor showing through the cloak after accepting the cloth behavior. That log still showed 14 body shapes, 105 world probes and successful cloth teams with 126 colliders, with no error/exception entries. The pinned upper fabric cannot be pushed clear of armor by the dynamic cloth colliders.

Both Blender body-fit meshes received a smoothly tapered upper-back offset up to 0.04 m and lateral shoulder offset up to 0.018 m. The neck opening is protected from expansion; the lower drape below Blender Z 1.32 m has zero displacement. The clasp receives matching lateral displacement. Object custom properties record ArmorClearanceRevision = 1 and the authored offsets; a pre-edit backup is retained beside the Blender source. Single-surface FBXs were re-exported and reimported, with existing Unity profiles, motion tuning, skin mapping and cloth paint retained. No gameplay stats were changed.

Both relaxed fitting bodies have zero static triangle intersections after this change. Unity validation confirms all 53 bone indices, rest-mesh agreement, zero Head/Jaw influence and bounded skinning in three authored poses. The specific chest armor from the screenshot has not been identified, so clearance against that armor and all animation poses remains an in-game check. This is a mesh-fit revision, not a claim of universal armor collision.

## Armor-aware revision, superseding the fixed padding experiment

The user identified ArmorMageChest_Ashlands and clarified that torso/back collision should not require per-outfit setup. A relaxed-pose comparison against its extracted chest mesh still showed intersections after the 4 cm padding experiment. That experiment was discarded: the pre-edit Blender source was restored and the approved single-surface geometry re-exported/reimported. The fixed upper-wrap mask was the relevant limitation: fixed particles cannot respond to collision.

Current revised paint fixes neck and clasp vertices, using authored NeckAnchorHeight 1.59 m, NeckAnchorRadius 0.21 m and ClaspAnchorRadius 0.045 m. Other shoulder/upper-back vertices are movable. AnchorLayoutVersion prevents normal preparation from repainting existing authored selections repeatedly. Motion tuning, gameplay stats and the approved mesh silhouette are retained.

CloakCollisionController reads the installed VisEquipment.m_chestItemInstances through a verified Harmony field accessor. It bakes the equipped chest's posed skinned geometry once on attachment and when that native chest-instance list changes, then expands nearby body capsule radii by the measured geometry plus authored ArmorPadding (0.025 m), bounded by MaximumArmorExpansion (0.12 m). There are no armor-name special cases. It does not read CPU skin channels from native meshes: the Ashlands source is not readable in game, but native BakeMesh can produce a readable fitting mesh. Removing armor restores baseline body radii. These are fitted capsule approximations, not exact triangle-to-cloth collision; pose-dependent or unusually bulky gear can still need testing/tuning.

The standalone player test loads an isolated copy of the actual non-readable Ashlands chest mesh, applies native player bone transforms, verifies that equipped geometry expands protection radii for both fits, verifies restoration on removal, and verifies fixed neck anchors plus movable shoulder/back fabric. This validates setup/data behavior, not a complete collision-solver animation. The native reference mesh remains exclusively in disposable test/editor-only folders and is not a production bundle dependency. User in-game fit validation remains pending.

## Regional upper-wrap stability (awaiting in-game motion check)

The user reported successful clearance improvement but continuous movement of the chest strap/shoulder pieces. The supplied log had successful builds and no error/exception entries. Freeing upper cloth for armor collision had exposed that region to uniform full wind and low damping.

Motion settings now use a single shared CloakMotionSettings.Apply projection in authoring and runtime. Values remain authored on HexBattleMageCloakCollisionProfile: upper-wrap damping 0.8, lower-drape MotionDamping 0.1, upper restoration 0.9, lower restoration 0.2, upper-region depth split 0.35, wind influence 0.15, wind depth weight 1 and turbulence 0.2. Damping/restoration curves hold their upper value through the split and soften toward the hem. The installed solver scales authored damping by 0.2; standalone checks verify resulting curve values 0.16 at anchors and 0.02 at the hem. Wind is baked into native cloth settings before PlayerClothWindShelter.Awake caches it. Upper cloth remains collision-responsive rather than reverting to fixed particles inside chest armor.

No mesh, item stats or attachment-slot changes were made in this tuning pass. The standalone player test still verifies armor-aware volume fitting/removal and anchor layout, and now evaluates the actual native-converted damping curve and baked depth-weighted wind for both fits. Full visible upper-wrap stability remains an in-game check.

## Installed DLL cloth-class audit

A read-only audit enumerated all 156 DLL files in the installed Valheim directory: all 141 managed assemblies were read successfully, and the remaining 15 were native/non-managed binaries without managed class metadata. Class searches included cloth/backstop and alternate fabric/cape/garment/armor-collision names. No installed DLL or game setting was modified. Full type/member metadata reports are in the shared workspace as installed-cloth-class-audit.json and installed-cloth-dll-inventory.json.

Relevant installed classes:

- assembly_valheim.dll: VisEquipment.SetupCloth supplies character collision components, replaces transforms and builds Magica cloth. PlayerClothWindShelter caches authored wind strength in Awake and sets it to zero while the player is sheltered.
- assembly_valheim.dll: ClothQualityManager.RefreshClothQuality maps cloth-quality indices 0/1/2/3 to 30/45/60/90 Hz and maximum simulation steps per frame 1/2/3/3. This is a global simulation-quality controller, not an armor collision generator. Graphics presets may affect the effective setting.
- MagicaClothV2.dll: ColliderCollisionConstraint supports None/Point/Edge modes and explicitly supplied ColliderComponent lists. MagicaSphereCollider, MagicaCapsuleCollider and MagicaPlaneCollider are native supported shapes.
- MagicaClothV2.dll: SelfCollisionConstraint supports FullMesh self-collision and linked cloth-to-cloth collision via syncMode/syncPartner. This does not automatically treat an ordinary skinned chest mesh as collision geometry. The inspected Ashlands chest reference contains a skinned mesh but no Magica cloth serialization block; no automatic partner hookup was found in the installed game assembly.
- MagicaClothV2.dll: MotionConstraint provides the native backstop and motion-distance controls already used by the cloak.
- UnityEngine.ClothModule.dll also exposes the legacy UnityEngine.Cloth API and its collider pairs. Some Valheim compatibility/reset/wind/armor-stand code still references it, while the installed VisEquipment cape setup uses MagicaCloth. These are distinct engines; their collision components are not interchangeable.

This audit found relevant built-in controls but no separate managed Valheim class that automatically prevents all cape/armor/world intersections. Native binary internals were not decompiled, and the introduction date of these classes was not established against an older game version. No additional physics behavior was enabled as part of this audit.
