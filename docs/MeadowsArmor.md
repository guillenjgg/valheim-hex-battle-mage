# Meadows clothing prototype

Current gear is fitted together according to [MatchingSetFit.md](MatchingSetFit.md), rather than enlarged for unrelated armor. Both body fits, the shared hood meshes and the cloak source were adjusted as a set.

The initial hood shape is superseded by the approved [reusable draped cloth hood](PlainClothHood.md). Its shared male/female base prefabs support future tier cosmetics through Unity prefab variants.

| Item | Identity | Jotunn clone baseline | Appearance |
| --- | --- | --- | --- |
| Wayfarer Hood | HexBattleMage_Helmet_Meadows_01 | HelmetFenring | Custom plain open-faced cloth hood |
| Wayfarer Tunic | HexBattleMage_Chest_Meadows_01 | ArmorLeatherChest | Custom grey-white long-sleeve tunic |
| Wayfarer Trousers | HexBattleMage_Legs_Meadows_01 | ArmorLeatherLegs | Native brown leather |

Unity definitions live under `Assets/_CustomItems/HexBattleMage/Armor/Meadows/{Helmet,Chest,Legs}_Meadows_01`. Their names end with `_Definition` so loaded authoring assets do not collide with Jotunn's runtime clone identity check. `ArmorManager` calls `PrefabManager.Instance.CreateClonedPrefab` after vanilla prefabs are available. Equipment roles, networking and hood hair/beard rules come from the installed item. Hood and chest visuals are replaced with authored custom meshes. Pants retain the native visuals. No game or ValheimRIP assets are modified.

Each definition contains HexBattleMageArmorProfile, MageEquipmentDefinition, MageEitrProfile, MageRecipeProfile and MageArmorStyleProfile. Item identity is localized. Stats, native source and visual overrides are inspectable on the definition; native hierarchy/behavior remain runtime-derived from BaseItemPrefab. Recipe/style records are baked and explicitly bundled. Runtime tuning must start from those authored fields.

Provisional test defaults: 2 armor, +5 maximum Eitr, 400 durability, one quality, no movement bonus. Each costs 6 Deer Hide and 2 Resin at Mage Table level 1. Final bonuses, upgrade curves and recipe balance remain design work. Fenris set effects, resistance and movement bonuses are cleared on the hood clone. Eitr uses the existing regeneration-without-food path, and equipping does not refill it. Perks use the existing top-of-tooltip layout.

The first chest recolor was superseded because the native leather mesh still showed a brown belt/waist and bare arms. Custom male/female tunics now use the approved grey-white cloak fabric, wrist-length sleeves and a plain hip-length hem. The new hood keeps a pointed hood silhouette but has an open face and no Fenris eye mesh, emission or trim. Both garments are matte. Pants retain native brown leather. The existing cloak is unchanged. Textures remain maximum 256, Read/Write enabled, Normal Quality compression, Crunch enabled and quality 100.

The armor profile exposes MaleVisual, FemaleVisual, WorldModel, ReplaceNativeVisuals and ClearBodyArmorMaterial. Runtime removes the cloned item's old visual children, copies the authored fits under an inactive container for Jotunn mock resolution and installs the custom static drop. The tunic clears the old native body texture material. `ArmorAttachmentPatch` selects the current body fit at the native item-prefab lookup in both VisEquipment.AttachArmor and AttachItem; unrelated items pass through. Both fits have the native 53-bone order and bindposes. These are skinned garments without cloth simulation.

`MeadowsClothingVisualBuilder` authors the custom meshes, materials, prefabs, transparent icons and an editor-only fitting scene at `Assets/Editor/HexBattleMage/CloakReference/PlainClothingPreview.unity`. The preview uses both body references. Normal preparation preserves existing meshes, material values and profile references; a repeat preparation preserved all 26 clothing assets byte-for-byte. Existing runtime identities, recipes and stats remain unchanged.

The hood now has four selectable appearances and dedicated helmet variant synchronization; see [PlainClothHood.md](PlainClothHood.md). Chest and legs still have one authored look; multiple entries are rejected for those slots until their propagation is implemented and tested. Existing cloak and wand style behavior is unchanged.

Use **HexBattleMage > Armor > Prepare Meadows Armor and Build Bundle**, or the normal authored-defaults builder. Existing fields and visual assets are preserved. Recompile the mod after building the bundle because it is embedded.

Validation runs actual Jotunn CreateClonedPrefab/AddItem/AddRecipe with native item/manager bootstrap fixtures. Checks cover independent clones, untouched vanilla stats, native slots, authored Eitr, removed Fenris bonuses, recipes, localized names, baked style recovery and icons. Custom clothing checks cover removed native visuals, body-fit selection, both native attachment transpilers, 53-bone bindings, normalized weights, non-emissive cloth and wrist-length sleeve reach. Installed Valheim methods and compiled Jotunn cloning were inspected. The editor fitting preview was inspected; actual movement, hair, lighting, save/reload and multiplayer require in-game testing.

References: [Jotunn PrefabManager](https://valheim-modding.github.io/Jotunn/api/Jotunn.Managers.PrefabManager.html), [Valheim Modding Wiki: attaching weapons and armor](https://github.com/Valheim-Modding/Wiki/wiki/Attaching-Weapons-and-Armor).
