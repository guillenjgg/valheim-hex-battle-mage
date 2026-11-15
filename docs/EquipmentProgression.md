# Equipment progression

- One Mage Table, with station extensions providing future crafting upgrades.
- The player keeps one wand through progression. Preserve the registered `HexBeechWand` identity for existing saves; its Meadows display name is Apprentice Wand.
- Staffs retain their identities and roles through progression: a Fire Staff remains a Fire Staff as it gains power. Gear follows the same progression model.
- Equipment progression controls power, abilities, tier display names, and which appearances are available. Cosmetic style selection must not change power.
- Apprentice currently has three branch finishes (warm bark, dark weathered bark, and pale birch), each with its own material and base-color texture. Both are Meadows cosmetics; neither grants a combat bonus.
- Use the Style dialog from either Craft or Upgrade at the Mage Table. Selection immediately restyles the wand selected under Upgrade, otherwise the equipped wand, otherwise the only wand in inventory. Multiple unequipped wands require the player to equip or select the intended wand. No crafting, item upgrade, or materials are required. With no owned wand, the selected appearance applies when crafting the first wand. Native item upgrades retain the selected appearance.
- Store the appearance choice in Valheim's existing item variant field. Jötunn extends equipment variant synchronization to the wand's equipment slot; the visual component applies the selected finish through `IEquipmentVisual.Setup`.
- Future mesh appearances should provide their own tip anchors. The current branch uses the mesh's positive Z tip for the light.
- Boss gates, resource gates, tier names beyond Apprentice, upgrade costs, damage scaling, and style unlock rules remain undecided. No later tiers are currently implemented.

## Verification

The installed Valheim assembly was checked for the variant dialog, item variant field, equipment visual setup, and native upgrade behavior. Jötunn 2.30.0 and the installed 2.30.2 both expose the equipment variant enable method, on an internal type accessed through reflection. These hooks need rechecking when updating Valheim or Jötunn.

- [Valheim Modding Wiki: Item Variants](https://github.com/Valheim-Modding/Wiki/wiki/Item-Variants)
- [Jötunn: Item Variants](https://valheim-modding.github.io/Jotunn/tutorials/item-variants.html)


