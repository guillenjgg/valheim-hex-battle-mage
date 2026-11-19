# Reusable cloth hood

## Current approved mesh

The latest approved version is `RecessedFaceHood14`: crown follows the forehead/head, both face contours remain close, and the entire opening extends forward 7 cm so the face sits inside the hood. This supersedes FaceFitHood08 and all pointed-crown review candidates below. Production meshes are `HexBattleMage_Hood_Cloth_RecessedFace_Male/Female.asset` in the shared hood folder. Exact vertex/triangle/UV/weight comparisons against the approved review assets passed.

These are real Unity mesh assets referenced by SkinnedMeshRenderer components on shared male/female base prefabs. Meadows visuals remain prefab variants. Changes were authored through Unity editor scripts; the previews are renders of those mesh assets. The current hood is not authored in the cloak's Blender file.

The default fabric is light brown. The prefab's MageArmorStyleProfile records four looks, in stable variant order: light brown, charcoal, grey, brown. Materials and transparent 256px icons live in the shared Styles folder. They are copied from authored review assets only when absent, then preserved on rebuild. The icon uses the authored mesh directly, close framing, readable fabric shading and a shaded interior; it does not render a character head into the icon. Drop model geometry matches the approved hood.

The existing Mage Table Style dialog handles selection. ArmorManager now permits multiple helmet appearances, sets native icons/variant count, and configures the shared style visual on both fits and the drop. HelmetStylePatches publishes the native item variant into an owner-written `HexBattleMage_HelmetVariant` ZDO field, refreshes the native attachment when that value changes, and passes it to AttachItem. It affects registered BattleMage helmets only. Native fields and hook signatures were checked against installed Valheim; Jotunn CustomItem.GetHashCode was verified to return the native stable item hash. No new dependency was added.

Tests cover all four colors on cloned male/female and drop visuals, actual native ZDO storage/reads, non-owner write prevention, native attachment variant and refresh, unrelated helmets passing through, named baked-data recovery, and existing cloak styling. Native networking/world bootstrap is a fixture; live multiplayer, animated fit and native in-game lighting still require testing.

On 2026-11-18 the user approved the `FaceFitHood08` review: a fresh arched cloth surface, separate male/female face contours, closer cheek/jaw depth, retained brow overhang, and a crown lowered approximately 3 cm from the preceding candidate. This supersedes the earlier ring/loft shapes described historically below.

The approved meshes are `HexBattleMage_Hood_Cloth_FaceFit_Male.asset` and `HexBattleMage_Hood_Cloth_FaceFit_Female.asset` under the shared hood folder. The shared base prefabs reference them; Meadows appearances remain Unity prefab variants and inherit the same geometry. Later tiers can reuse these bases and add cosmetic material/detail overrides. Gameplay identity, stats and recipe are unchanged. The drop model and icon were updated for this shape.

**HexBattleMage > Armor > Apply Approved Face-Fitted Hood and Build Bundle** applies the approved shared assets. Existing approved mesh files are preserved. Initial creation requires the staged `Assets/Editor/HoodReview/FaceFitHood08` snapshots; ordinary rebuilds and subsequent applications use the saved production meshes and do not require review files. Do not run the rejected review generators over production assets.

Validation compared each production variant to its approved preview: vertices, triangles, UVs and skin weights match exactly, coordinates are finite, and both appearances retain prefab-variant inheritance. Compiled-mod/Jotunn registration and native binding checks were also run. Final native lighting and movement remain in-game checks. Approved reference images are `Validation/ApprovedHoodMale.png` and `Validation/ApprovedHoodFemale.png`.

The user approved the plain linen hood concept on 2026-11-18: rounded crown, a thin fabric edge, visible face and loose folds tapering onto the neck. Fenris styling is reserved for a future appearance.

Shared meshes, worn linen material and male/female base prefabs live under `Assets/_CustomItems/HexBattleMage/Armor/Shared/Hoods/PlainClothHood`. The Meadows `HexBattleMage_Helmet_Meadows_01_Draped_Male/Female` prefabs are Unity prefab variants of these bases. The existing item definition references those variants, a new static drop and a new transparent icon. The runtime identity, recipe, armor and Eitr values are unchanged. Native item cloning supplies equipment behavior only; the worn geometry is custom.

The crown is closed with a triangle fan. Broad side/neck folds and a 3 mm folded face edge are baked into the mesh. Native-ordered 53-bone bindposes and normalized head/neck/chest weights provide animation attachment. There is no runtime cloth simulation. Material emission, metallic and gloss are zero. The reused fabric and new icon retain the project's readable, 256-max, compressed/Crunch import policy.

Create later tier appearances as variants of the shared male/female bases. Retain the shared mesh and override materials or add appropriately skinned details under the reserved `CosmeticDetails` group. Static objects placed under that group do not automatically follow a character bone. Gameplay values stay on the tier's equipment definition. Cosmetic unlock rules and multiple selectable helmet variants are still separate implementation work; this adds one appearance, not a new progression system.

`DrapedHoodAssetBuilder.Build` provides **HexBattleMage > Armor > Apply Draped Cloth Hood and Build Bundle**. It seeds missing shared assets and item variants without regenerating existing mesh/material assets, then explicitly assigns the Meadows appearance. The normal authored-defaults build preserves the selected body references. Existing chest, pants and cloak geometry is unchanged. `MeadowsClothingVisualBuilder` now builds its fitting preview from the definition's actual current visual references.

Validation: actual Jotunn registration with isolated native bootstrap fixtures, body selection, attachment transpiler installation, native bone binding count, normalized weights, authored recipes/stats and texture policy passed. Front, angled and side views of the actual mesh were inspected against fitting references. Final appearance, head turns, beard/hair, cloak overlap and multiplayer still need in-game testing.

The approved image is a concept target, not an in-game render. The actual mesh preview is in `Validation/DrapedHoodPreview.png`.
