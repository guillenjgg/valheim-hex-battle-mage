# Heathered white cloak and armor clearance

Generated with the built-in image generation tool. Saved texture: `C:/Users/Jonathan/Documents/valheim_mods/Unity/HexModsUnityProject/Assets/_CustomItems/HexBattleMage/Cloaks/Meadows/Cloak_Meadows_01/Styles/White_Heathered_BaseColor.png`. The White.mat color is (0.8, 0.8, 0.78); its existing style index 1 and localized name remain unchanged. White.png was rerendered from this material. Texture imports retain maximum 256, Read/Write enabled, Normal Quality compression, Crunch quality 100.

## Final prompt

A square seamless tileable flat game albedo texture for a rough beginner Nordic vagabond cloak: heathered pale grey wool and coarse linen, visibly mixed with many dark charcoal-grey fibres and irregular subtle worn darker threads. Overall medium-light ash grey / dirty oatmeal, definitely not bright white or cream. Muted matte fabric with fine scuffs and a few faint dark rubbed areas. Restrained Valheim-compatible low-poly hand-painted material, readable weave at 256-pixel import size, not photorealistic hair or fur. Uniform material only, full canvas edge-to-edge, no garment silhouette, no folds, no lighting or baked directional shadows, no borders, no seams, no symbols or text. Mostly pale grey with enough charcoal interwoven that it will not appear stark white in sun. Horizontal and vertical edges tile cleanly. Opaque RGB texture.

## Style and attachment references

Reviewed the [Valheim Modding Wiki Item Variants guide](https://github.com/Valheim-Modding/Wiki/wiki/Item-Variants), [Attaching Weapons and Armor](https://github.com/Valheim-Modding/Wiki/wiki/Attaching-Weapons-and-Armor), and [Jötunn Item Variants tutorial](https://valheim-modding.github.io/Jotunn/tutorials/item-variants.html). The native variant index/icon picker and IEquipmentVisual.Setup path are retained, with the existing attach_skin and 53-bone skin mapping. Jötunn documents that StyleTex uses its shader/atlas replacement path; this cloak keeps the working material-based styles so its matte/two-sided cloth settings are preserved. No StyleTex shader conversion or new dependency was introduced.

## Candidate clearance revision

Previously chest armor was baked for fitting only when equipment changed. Visible cloaks now resample posed chest geometry every authored ArmorFitInterval (0.1s). The largest sampled envelope is retained until armor changes, so radius changes do not repeatedly shrink/expand the cloak. Bone segments and armor transforms are cached for the fitting pass. Native capsule parameters now update when radius changes even if total length does not change. Equipment removal resets the envelope.

Male/female profiles bake ArmorPadding=0.04m, MaximumArmorExpansion=0.14m, NeckAnchorRadius=0.16m and AnchorLayoutVersion=2. Reducing the fixed neckline radius from 0.21m frees outer shoulder vertices to respond to collisions: measured neck pins went from 740 to 310 for male and 812 to 417 for female, while clasp anchors remain. Mesh topology, bones, body capsules, cloth damping/wind/backstop/travel and world collision pools are unchanged. Fixed inner anchors still follow the skinned pose; this revision cannot guarantee zero intersections in every animation/armor combination.

Standalone checks passed on both fits with unreadable Ashlands chest geometry, pose-envelope retention, explicit native radius updates, equipment removal reset, body-data recovery and the new authored anchor/clearance values. Existing body/world geometry and non-blocking collision fallback tests also passed. Actual walking/blocking/turning and visual clearance remain in-game checks. Bundle and DLL were rebuilt and installed in Dev with embedded-bundle/hash verification.

