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
