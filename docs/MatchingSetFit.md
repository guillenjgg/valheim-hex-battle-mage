# Matching-set fit

On 2026-11-18 the user changed the fitting policy: fit BattleMage armor together around the player bodies and our own prefabs. Accommodating unrelated high-tier armor is no longer a goal for the starter set. Earlier Ashlands clearance revisions remain history, not the current silhouette target.

Both body fits were adjusted together:

- Tunic sleeves were fitted to the native body surfaces with modest cloth ease, reducing the oversized upper arms.
- The reusable hood was narrowed by 13% and reduced in depth by 17%, with body/tunic clearance checks during fitting. Both Meadows variants inherit the changed shared meshes.
- The cloak's upper wrap was fitted to the actual tunic in its authored relaxed pose. The source Blender file was backed up before editing. Lower drape below the fit transition remains unchanged. The clasp follows the new wrap position. The fitted source was exported to FBX and reimported into Unity.

The cloak keeps its original vertex order, UVs, weights and paint association. FBX import changed split/order details, so imported positions were remapped to the original UV/position correspondence before copying them into the existing mesh assets. Cloth paint positions and native initialization records were refreshed. Existing materials, styles, stats and recipes remain unchanged. Hood/tunic drop meshes and icons were refreshed to match their fitted visuals.

Authored cloak collision defaults now disable `FitEquippedChestArmor`. ArmorPadding is 0.012 m, MaximumArmorExpansion is 0.025 m (inactive while fitting is disabled), and ClothThickness is 0.012 m. The previous dynamic armor expansion and 0.035 m cloth thickness were pushing the cloak outward. Native body protection, the fourteen authored body shapes, world protection, anchors, damping, wind, backstop and travel settings are retained. These settings are baked into all cloak visual prefabs and mirrored in the profile's new-component defaults.

Static reference-pose checks and a combined preview were inspected for both bodies. Registration tests passed. The actual standalone player restored the authored body shapes with inline arrays removed; unrelated unreadable chest geometry no longer expands the volumes, anchors remain fixed, upper cloth remains movable and native damping conversion still passes. These checks do not establish zero clipping during every animation: validate standing, turning, blocking and running in game with the matching set.

Authoring work files are in the shared workspace: `fit-own-set.py`, `own-set-fit-report.json`, `extension-authoring/Assets/Editor/ReimportOwnSet.cs`, and the source Blender file under BattleMage/cloaks. The operation is an explicit fit migration, not part of ordinary rebuilding. Normal asset preparation preserves these authored meshes. The preview and fitting measurements are saved under docs/Validation.

## Utility-belt waist clearance (2026-11-19)

The tunic waist now follows each native body surface with 6 mm of radial cloth ease through the belt band, blending back into the existing chest and skirt. Only torso vertices between 1.10 m and 1.41 m change; sleeves, UVs, topology and skin weights remain unchanged. The body-specific migration starts from saved pre-fit meshes, so repeated application does not accumulate offsets. Native utility attachments and other armor are unchanged. A static male/female reference render with the native BeltStrength mesh shows the front belt outside the tunic; movement remains an in-game check. It is not a universal guarantee for all utility meshes or animations.

The brown and black cloak styles now reference the existing patched cloth/deer-hide texture instead of an empty albedo. White keeps its accepted heathered texture. Cloth materials retain zero gloss/metallic/rain sheen, and normal intensity is reduced to 0.04. Inventory icons were rebaked. The authored source is StarterGearFinish in UnityEditor; normal bundle preparation preserves these edits.

## Connected tunic and utility layering (2026-11-19)

This supersedes the earlier torso-tube waist fit. The male/female tunics now use continuous native-body-derived shoulder, torso and sleeve topology, retaining native skin influences and the existing reference bind poses. The shoulder roots are no longer separate open tubes. The upper back follows the native body surface; the loose lower skirt retains starter-tunic coverage. Geometry validation finds exactly four openings (neck, two cuffs, hem), no nonmanifold edges, finite positions and normalized 53-bone influences. Static front/rear renders include the native utility belt. Live moving fit still requires confirmation.

The prefab's HexBattleMageArmorProfile.UtilityVisualPadding is 0.025 m on this chest piece and zero by default elsewhere. After native equipment updates, a worn skinned utility receives an instance-only radial mesh offset while this armor is equipped. Native source meshes, UVs, skin weights and bind poses remain intact; removal restores the original mesh. Repeated updates reuse the instance without cumulative expansion. Unreadable utility meshes are left intact. Drop models and collision shapes are unaffected.

ConnectedTunicAssetBuilder imports the inspectable ConnectedTunic_Male/Female JSON references, updates the world model and rebakes icons. Copy the references from UnityEditor/References into Assets/Editor/UtilityClearanceReference when setting up an authoring project. The superseded StarterGearFinish tube fitter was removed to prevent accidental reapplication to the new topology. Normal bundle builders preserve the authored meshes.

## Open buttoned neckline and narrow cloak wrap (2026-11-19)

Both tunics now have a V-shaped front opening ending at 1.475 m. Four small wooden buttons and stitched holes are real skinned meshes; upper two buttons sit beside the open edges, lower two close the front. Button weights are sampled from the tunic surface. Buttons and thread use inspectable matte accent materials and remain unchanged by clothing styles, including dropped-item styles. Male/female worn meshes and the static drop model carry the detail; icons were rebaked. ConnectedTunic references preserve the neckline cut; TunicFastenerAssetPreparation authors the fasteners on explicit import.

The cloak's continuous upper front wrap was narrowed to 38 percent of its previous span in the central front region, blended into the shoulders. The lower drape below 1.38 m is unchanged in the Blender source. The source was backed up under cloaks/Backups before editing and both FBXs exported again. ReimportNarrowWrap maps the revised positions back to existing UV/topology/skin-weight order and refreshes cloth initialization records, preserving authored motion/collision settings. The clasp follows the revised wrap. Combined male/female reference previews show all four buttons exposed below it. Standalone cloak-data loading, registration, button skinning, accent-style preservation and drop-physics checks passed; final animated appearance is an in-game check.

The user accepted the buttoned tunic and narrower cloak wrap in game on 2026-11-19. Preserve this matching-set visual baseline during progression and UI work.
