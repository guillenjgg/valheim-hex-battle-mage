# Equipment progression

## Planned mage gear and first trinket

Multiple trinket definitions share the native single equipped slot. Shared registration, per-item settings, optional combat rewards and authoring steps are documented in [Trinkets.md](Trinkets.md).

The agreed gear lineup is head, chest, pants, cloak and one equipped trinket. Every mage gear piece will supply flat maximum Eitr, and gear-provided Eitr must regenerate without Eitr food. The first trinket supplies an immediate maximum-Eitr and Eitr-regeneration benefit, then rewards a successful parry with increased wand damage for 2 seconds. The trigger must recognize successful parries against bosses independently of whether the enemy can be staggered. Repeat parries refresh the duration rather than stack the damage bonus. Numerical bonuses remain undecided. No adrenaline buildup is required.

The stone pendant is now registered as `HexBattleMage_Trinket_Meadows_01` (Stone of Reprisal), using Valheim's native single trinket slot. Initial test defaults are +25 maximum Eitr, +15% Eitr regeneration and +25% wand projectile damage for 2 seconds after a successful parry. Parrying a boss adds 10 percentage points, granting +35% total for the same 2 seconds. The latest successful parry determines the reward strength; normal and boss rewards refresh the same status without stacking. It costs 10 Stone, 4 Resin and 2 Leather Scraps at a level-1 Mage Table. It appears in the Unlocks list and uses native material discovery. The first Meadows cloak is now 2 armor and +5 maximum Eitr while equipped, using the native cape slot. It adds no extra regeneration multiplier; its positive Eitr pool regenerates through the existing native gear-Eitr path without Eitr food. Remaining mage clothing and their bonuses are future work.

The reward hooks the native successful timed-block branch before the attacker-stagger condition, so bosses need not stagger. Ordinary blocks, guard breaks and stamina failures do not grant the reward. The timed Rune Reprisal status shows the trinket icon; re-parrying refreshes rather than stacks it. Unequipping, replacing the trinket or dying clears the reward. Equipped Eitr adds to the native food-derived maximum; native regeneration works with that positive maximum without Eitr food. Equip/unequip updates the cap without refilling current Eitr. Native regeneration delays and attack/dodge/block modifiers still apply. Shot damage is copied before modification, including physical and elemental channels, so shared item stats and subsequent projectiles do not compound the bonus. Separately configured elemental tick damage is unchanged. Wand resource costs are unchanged in this pass.

See AssetBundles.md for authoring locations and PrefabSettings.md for configuration precedence. Isolated integration checks exercise native equipment and parry methods; actual game startup/crafting, visible armor clearance, regeneration ticks, save/reload and multiplayer still require in-game testing.

Current authored settings and component locations are described in [PrefabSettings.md](PrefabSettings.md). The configurable elemental prototype is described in [WandElements.md](WandElements.md). Damage-type selection is currently a development setting; Mage Table selection and tier-based element unlocks are not implemented yet.

- One Mage Table, with station extensions providing future crafting upgrades.
- Meadows station prototype: base Mage Table is level 1. The Rune Stone and Reagent Stand each contribute one native station level within 5 metres, reaching level 3 with both. Duplicate copies do not stack. They are independent extensions at present; sequential unlocks and resource-discovery gates are not implemented. These station levels permit existing native quality recipes; new item tiers, power curves and element unlocks remain separate design work.
- The player keeps one wand through progression. The first prefab and registered item share the progression identity `HexBattleMage_Wand_Meadows_01`; its Meadows display name is Apprentice Wand.
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



## Wand damage and compendium

Every wand shot carries positive blunt and pierce damage, alongside its elemental damage. Apprentice defaults are 10 blunt + 10 pierce + the existing 30 fire (quality 1), preserving the previous total of 50. Elemental quality scaling is retained. Development / Wand Damage controls each physical component with a positive minimum; range falloff scales the full damage mix. Enemy defenses can still resist individual damage types.

The compendium shows current wand quality damage before skill/attack/range modifiers, plus the latest local impact after attack modifiers and range falloff but before enemy defenses. Direct and AoE modes are identified separately; AoE is per target and replaces the direct hit.

## Apprentice rune assets

The Apprentice wand has one Meadows rune, repeated around six sides of the shaft for visibility as the viewing angle changes. Its emission is brighter than the initial version. It is an emissive inlay over a dark engraving border, conformed to the branch mesh, with a 2.4 mm glowing stroke and a 4 mm dark border; it does not add a light source or change combat stats. All Apprentice cosmetic finishes share the same rune.

Unity assets live under Assets/_CustomItems/HexBattleMage/Progression/Wands/Apprentice: ApprenticeRune.prefab, RuneGlow.mat, RuneGroove.mat, and the two surface meshes. The wand contains this nested prefab at attach/WandModel/ProgressionRunes/RuneSlot01/Tier01_Apprentice. Keep subsequent tier rune prefabs organized under Progression/Wands with one named child per tier. Future tiers should retain the Apprentice rune and add their own. Item quality is not yet mapped to progression tiers; unlock conditions remain undecided.

UnityEditor/WandRuneAssetBuilder.cs provides HexBattleMage > Prepare Apprentice Wand Rune. Initial generation projects a 2.6 cm branch-shaped glyph onto the shaft and writes a preview. Existing rune geometry and authored settings are now preserved during normal preparation. The one-shot Temp/HexBattleMageWandRunes.request workflow also rebuilds and copies the bundle. Save open prefab edits before running it. Changing the shaft mesh requires regenerating the conforming inlay meshes.

The shaft reserves ten named sockets (RuneSlot01 through RuneSlot10) from 40% to 90% of the shaft mesh length. Each engraving is 2.6 cm long, leaving gaps within the roughly 4 cm slot spacing. Only the first socket contains an Apprentice rune. The other sockets reserve space without enabling later tiers. Glyph meshes are centered on their own prefab origin; socket placement supplies their longitudinal position. If the shaft or capacity changes, the builder verifies that glyphs still leave a gap.

## Apprentice ward bracer prototype

`HexMagicBracer` (Apprentice Ward Bracer) is wearable equipment with its own logical bracer slot. Equip or unequip it through the normal inventory/hotbar controls. It does not occupy either hand, the utility/belt slot, or another clothing slot. Only one bracer can be worn at a time. It stays worn when weapons are sheathed or changed, and works without a wand. Merely carrying an unequipped bracer provides no shield.

The bracer is crafted and repaired at the Mage Table. The provisional recipe is 10 Wood, 4 Resin, and 2 Leather Scraps. Qualities 1–3 use the wood shield stat curve: block power 6/12/18, block force 20/25/30, durability 200/250/300 and parry bonus 1.5. Upgrade resource increments are 5 Wood, 2 Resin and 2 Leather Scraps through native quality scaling. Station-level requirements still apply at the Mage Table; later equipment tiers remain undecided. Registration clones the installed `ShieldWood` through Jotunn to retain block power, parrying, stamina use, durability and block effects, then changes the item category to Hands and removes the shield movement penalty. Native shields and the approved wand are not modified. The registered identity remains `HexMagicBracer` for existing saves.

Bracer-only Harmony patches handle equip/unequip, equipability and equipped-state queries. MageEquipmentDefinition identifies the Bracer category and slot; SupportsBlocking controls the shield role. These checks read the drop prefab component instead of matching its name. MagicBracerProfile independently supplies the shield stats. The slot uses Valheim's existing per-item `m_equipped` flag, which native item serialization already saves. Equipping another bracer replaces the worn one; restoring an already flagged bracer during `Player.EquipInventoryItems` succeeds. Native weapon and armor equipment fields remain independent. No additional dependencies or equipment-slot integrations are required.

`MagicShieldManager.GetBlocker` selects the actual worn, usable bracer. `Humanoid.GetCurrentBlocker` is patched to return that item so native blocking uses its current power and durability. A broken bracer can remain visible as clothing but cannot supply shielding until repaired. The shield pose is selected only while actively blocking; otherwise native weapon-specific animation states are preserved. State transitions and held blocks refresh the stance through Harmony delegates, while native `ZSyncAnimation` handles animation synchronization.

`MagicBracerVisualController` renders the bracer whenever worn and hides it on unequip. The owner publishes worn/usable flags through ZDO data under `HexBattleMage.BracerWorn` and `HexBattleMage.BracerUsable`. Peers use these flags with the native replicated blocking state. The separate inventory-only ward and shield holster attachment have been removed.

Blocking reveals a cyan Nordic-inspired rune field with a nominal diameter of 1.44 metres. Its custom procedural animation expands and fades in over 0.14 seconds, rotates the rune ring, and gently pulses. Release immediately hides the field while leaving the bracer visible. The field is visual only: native directional blocking determines protection, with no additional collider, damage interception, eitr cost, or status effect.

The player equipment anchor is `LeftHand_Attach`, below `LeftHand`, which is below `LeftForeArm`. The cuff uses the actual forearm and wrist bones, centered 62% of the way from elbow to wrist with its narrow end facing the wrist. It follows forearm twist and scales with forearm length. Male and female bodies share this rig but have different meshes; radial fit reads the current body mesh each frame. Base radial scales are 1.2 for male and 1.05 for female, before character scaling. These fits were checked against read-only ValheimRIP body assets.

Verification: the new files compile against the installed unmodified Valheim assembly and compiled Jotunn 2.30.0. Unity tests exercise the actual Harmony equip/unequip hooks, equipped/type queries, belt/hand independence, one-bracer invariant, native saved-equipment restoration and inventory toggling, blocking without a wand, idle animation preservation, worn/field visibility, broken-item handling and explicit unequip. Existing male/female fit and 13 field checks also pass. Full in-game UI, save-file reload, armor clearance, animation blending and multiplayer still need verification. This is a runtime-code change; it reuses the approved visual bundle.
