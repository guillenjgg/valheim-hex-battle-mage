# Equipment progression

## Planned mage gear and first trinket

The first hood, chest and pants prototypes are authored and registered as Jotunn clones. See [MeadowsArmor.md](MeadowsArmor.md) for provisional test values, recipes and native inheritance. Final clothing bonuses and upgrades remain undecided.

Multiple trinket definitions share the native single equipped slot. Shared registration, per-item settings, optional combat rewards and authoring steps are documented in [Trinkets.md](Trinkets.md).

The agreed gear lineup is head, chest, pants, cloak and one equipped trinket. Every mage gear piece will supply flat maximum Eitr, and gear-provided Eitr must regenerate without Eitr food. The first trinket supplies an immediate maximum-Eitr and Eitr-regeneration benefit, then rewards a successful parry with increased wand damage for 2 seconds. The trigger must recognize successful parries against bosses independently of whether the enemy can be staggered. Repeat parries refresh the duration rather than stack the damage bonus. Numerical bonuses remain undecided. No adrenaline buildup is required.

The stone pendant is now registered as `HexBattleMage_Trinket_Meadows_01` (Stone of Reprisal), using Valheim's native single trinket slot. Initial test defaults are +25 maximum Eitr, +15% Eitr regeneration and +25% wand projectile damage for 2 seconds after a successful parry. Parrying a boss adds 10 percentage points, granting +35% total for the same 2 seconds. The latest successful parry determines the reward strength; normal and boss rewards refresh the same status without stacking. It costs 10 Stone, 4 Resin and 2 Leather Scraps at a level-1 Mage Table. It appears in the Unlocks list and uses native material discovery. The first Meadows cloak uses the native cape slot and has three qualities: armor 2/3/4 and maximum Eitr 5/6/7. Both upgrades require Mage Table level 3 and fixed costs of 3 Leather Scraps, 2 Deer Hide and 2 Resin. Initial crafting remains level 1 with 6 Leather Scraps, 4 Deer Hide and 5 Resin. It adds no extra regeneration multiplier; its positive Eitr pool regenerates through the existing native gear-Eitr path without Eitr food. Remaining mage clothing and their bonuses are future work.

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

Appearance progression is cumulative per gear category: later tiers add looks while retaining earlier choices. Cosmetic unlock tiers and item quality/stat upgrades are distinct. Keep saved variant order stable when extending options. The exact player-unlock trigger remains undecided.

## Equipment summary and unlock list (2026-11-19)

The compendium's Battle Mage - Equipment page replaces the old wand-impact diagnostic page. It reads the current weapon and quality, positive damage components, live wand range samples, the actual native/Harmony-selected blocker and blocking skill, and TrinketEffects.EitrBonus for the equipped-gear Eitr total. Food and carried-but-unequipped items are excluded. No prefab identities, fallback inventory wand or latest-impact telemetry is shown. The author-requested full-damage range is now 15 metres; falloff remains linear over the next 20 metres to the existing 20 percent minimum. These values are prefab-authored and remain configurable; the page reads current tuning rather than embedding the example ranges.

Unlocks lists every catalog-registered recipe under its biome. Rows start collapsed and show an item icon, disclosure sign, explicit LOCKED/UNLOCKED text and a color indicator. Each row expands to discovered/missing materials and its crafting station. Unlock state comes from the current player's native learned-recipe set, not inventory stock or an invented unlock action. Requirements still use known-material discovery. The interface preserves expansion state, scroll offset and row focus while refreshing; text wraps within the panel. This does not introduce new progression or unlock rules.

EquipmentUiValidation checks the actual summary, current range tuning, bracer blocker override, equipped-only Eitr, learned-recipe states, expand/collapse, state refresh, bounded text and the bundled 15m default. UI/player/font bootstrap is isolated; complete live game input/navigation remains an in-game check.

## Boss-revealed biome catalog (2026-11-19)

The biome header now collapses its entire category. Revealed categories start expanded; individual gear rows are static, with requirements shown directly for locked recipes. Compendium headings and numbers use Valheim orange with white body text. The native unarmed fallback is omitted from weapon damage and blocking; an equipped fist weapon is still a weapon.

Catalog visibility is authored on MageEquipmentDefinition: CatalogRequiresBiomeDiscovery and CatalogBossDefeatKey. Registration carries these fields onto runtime item prefabs. Meadows requires discovery (or current arrival), without a boss key. Later tiers use shared world boss flags and do not require visiting the next biome first:

| Gear biome | Required world key |
| --- | --- |
| Black Forest | defeated_eikthyr |
| Swamp | defeated_gdking |
| Mountains | defeated_bonemass |
| Plains | defeated_dragon |
| Mistlands | defeated_goblinking |
| Ashlands | defeated_queen |
| Deep North | defeated_fader |

MageCatalogAssetPreparation seeds these defaults for new metadata fields; subsequent builds preserve authored edits. When cloning a Meadows item into a later tier, explicitly update both visibility fields to that tier's rule. These conditions control catalog visibility, not crafting eligibility. Individual lock labels still use native learned recipes, and existing resource/station crafting rules remain unchanged. There are no authored later-tier items yet, so defeating a boss will not create an empty or invented gear section.

EquipmentUiValidation verifies first Meadows arrival and retained discovery, actual native world-key queries for Eikthyr/Elder, removal of world keys, knowledge not bypassing catalog visibility, whole-biome collapse/expand, unarmed filtering, orange values and existing equipped-only Eitr/blocker checks. Biome-discovery and world bootstrap are isolated fixtures; boss-key dictionary lookup uses the installed native implementation.

## Starter balance (2026-11-19)

Stone of Reprisal now costs 1 TrophyBoar, 10 Stone, 5 BoneFragments and 2 LeatherScraps. Resources and managed baked recipe data are updated on the prefab.

The first wand has four qualities (three upgrades): blunt/pierce/fire damage is 5/5/15, 6/6/17, 7/7/19 and 8/8/21. Physical per-quality increments are authored on HexBattleMageWandProfile and exposed alongside base physical damage in development tuning. The fire profile authors direct damage 15 and per-quality damage 2. Element changes preserve tuned physical increments. Existing other elemental effects, damage falloff and upgrade costs/station requirements are unchanged.

HexBattleMageVitalityProfile stores flat MaximumHealth and MaximumStamina bonuses, without any quality multiplier. Meadows helmet/chest/legs each author +2 health (all three equipped total +6); cloaks author +5 stamina. Existing Eitr bonuses remain. Registration copies the profile to the real item. Native food totals add bonuses only for equipped inventory items. Owner-only equip/unequip refresh updates caps immediately, preserves existing resources during replacement, and clamps on removal without refilling on re-equip. Native food updates continue to handle food changes. Tooltips list the perks before durability; the compendium includes the equipped totals.

StarterBalanceValidation uses actual native damage calculation, food totals and equip/unequip paths. It checks all qualities, baked recipe recovery with inline resources removed, flat quality behavior, food stacking, retained Eitr, ownership, replacement preservation, cap removal and no refill exploit, plus tooltip ordering and compendium totals. Network/visual bootstrap and fixture base capacities are isolated. The balance import command is an explicit migration; normal preparation preserves authored values.


## Meadows qualities and extension costs (2026-11-19)

Hood, tunic and trousers now permit quality 1 through 4 (base plus three upgrades). Armor is 2/4/6/8. Native station requirements are Mage Table 1/2/3/4, starting from authored minimum level 1. The two existing distinct extensions reach table level 3, so quality 4 remains unavailable until a future level-4 extension exists. No third extension or boss progression is invented here. Cloak, bracer and wand quality rules are unchanged.

Base armor recipes remain 6 DeerHide and 2 Resin. Leather-armor upgrade resource increments are 6 DeerHide and 5 BoneFragments, with no upgrade Resin. Installed native Piece.Requirement.GetAmount yields 6/5 at Q2, 12/10 at Q3 and 24/20 at Q4; its current Q4 multiplier is four, not three. No custom material-scaling patch is used. Armor health remains flat +2 and Eitr remains flat +5 per item at all qualities. Other custom bonuses remain unchanged.

Rune Stone uses chopping block costs: 10 Wood + 10 Flint. Reagent Stand uses tanning rack costs: 10 Wood + 15 Flint + 20 LeatherScraps + 5 DeerHide. These match the read-only native piece_workbench_ext1/ext2 assets. Their existing extension radius/non-stacking/connection behavior is preserved.

Code overrides live in Core/Tuning/ArmorTuning.cs and CraftingTuning.cs. Config sections Crafting / Mage Table / HexMageRuneStone and HexMageReagentStand expose each material count (0-1000). Zero removes the material cost. Config respects Tuning / UseConfigOverrides; construction costs are applied at startup and require restarting after edits. Names/material lists can be changed in code or prefab authoring. Config does not alter already placed extensions or refund past construction costs retroactively.

MeadowsQualityAssetBuilder.Build is the explicit one-time authoring migration, mirrored into Unity. Normal builders preserve existing values and seed these defaults only for new assets. Updated native registration tests verify all qualities, station levels and material costs, and unchanged Eitr/health. Tuning validation verifies the extension costs and config/code precedence.


## Fixed cheaper Meadows upgrades (2026-11-19; supersedes native leather material costs above)

| Item | Initial craft | Each upgrade |
| --- | --- | --- |
| Hood, tunic, trousers (each) | 6 DeerHide, 2 Resin | 3 DeerHide, 1 Resin |
| Wand | 20 Wood, 2 Resin | 10 Wood, 1 Resin |
| Bracer | 10 Wood, 4 Resin, 2 LeatherScraps | 5 Wood, 2 Resin, 1 LeatherScraps |
| Cloak (unchanged) | 6 LeatherScraps, 4 DeerHide, 5 Resin | 3 LeatherScraps, 2 DeerHide, 2 Resin |
| Stone of Reprisal | 1 TrophyBoar, 10 Stone, 5 BoneFragments, 2 LeatherScraps | None; maximum quality 1 |

Each upgrade uses the same fixed cheaper material count, independent of quality. BoneFragments are exclusive to the Meadows trinket recipe. MageUpgradeProfile separates FixedResourceCosts from UseFixedStationLevel; material overrides retain native station-level progression for armor, wand and bracer. Existing cloak table-level-3 upgrade rules remain intact. Registration copies the policy onto cloned gear and tracks recipe requirements for the scoped cost patch; vanilla recipes are unaffected.

Edit counts in the existing category tuning files; defaults and policy are also baked in Unity. Apply Meadows Craft and Upgrade Costs is the explicit one-time migration. Ordinary preparation preserves authored settings. Native registration checks cover armor Q2-Q4 costs, copied wand/bracer policy, vanilla pass-through and trinket quality cap. Baked-policy validation runs before code tuning.

A dedicated wand/staff crafting and attunement station is planned. No new build piece is created here; the current wand remains at the Mage Table until that station is designed.


Unlocks navigation: a permanent 18px gold scrollbar sits beside a separately clipped list, with wheel sensitivity 140 (previously 30). A fixed native-styled Collapse All button toggles to Expand All when all visible biome sections are collapsed; it affects revealed categories only and returns the list to the top. Individual biome controls and unlock state remain independent. EquipmentUiValidation checks wheel movement, scrollbar bounds/bottom access, all-section collapse/expand and existing unlock refresh/layout behavior. Runtime UI only; no bundle rebuild.


## Rune-Whittler's Block implemented (2026-11-19)

See [RuneBlock.md](RuneBlock.md). The mod wand and trinket now craft/repair at one 10-Wood stump station. Boss flags automatically control its level and cumulative visual children, with saved per-world notifications. No Rune Block extension pieces are introduced. Mage Table armor/bracer rules are preserved. Material attunements and element unlock/selection gameplay remain a separate next phase.


## Inventory starter wand and first attunement (2026-11-19)

Supersedes the earlier default-fire/station crafting entries. Discover Wood and Stone to reveal the inventory-crafted wand: 10 Wood + 2 Stone, physical damage 5/5 only. At the Rune Block, discovering DeerHide and Resin reveals paid Fire attunement: 2 DeerHide + 2 Resin per wand. Fire adds 15 (+2 per quality); physical quality increments stay +1/+1. Saved per-wand attunement replaces the global development selector in normal play. The wand also crafts at the Rune Block for 10 Wood + 2 Stone. Block upgrades cost 5 Wood + 1 Stone each; qualities 2/3/4 require station levels 2/3/4. See RuneBlock.md for authored profiles, tuning and validation. Other gear progression is unchanged.


## Character-bound attunements (2026-11-20)

Supersedes per-wand learned/selected element state. The Rune Block teaches the character Fire for 2 DeerHide + 2 Resin; a separate free Activate Fire action applies it to every wand that character wields. The active-slot limit is 1, authored on HexBattleMageRuneBlockProfile and adjustable in AttunementTuning.cs. Learned elements persist in character custom data across spare/replacement wands. At full capacity the station requires an explicit replacement. Spirit/Frost remain design-preview examples, not newly available gameplay recipes. Existing item quality and gear balance remain unchanged.


## Wand skill and Fire learning cost (2026-11-20)

Fire now requires 2 Resin + 3 Feathers + 10 Wood, replacing the earlier DeerHide recipe. The wand uses Clubs for native hit XP and initial attack damage scaling across blunt, pierce and active elemental channels. Bow XP is not enabled. Custom DoT tick values remain independently configured. WandSkillValidation invokes actual native Projectile.OnHit and verifies one Clubs award on a qualifying creature hit, none for misses/world colliders, and all-channel native damage multiplication. Skill storage, target validity, network damage and adrenaline bootstrap are fixtures.


2026-11-20 update: Stone of Reprisal passive defaults are +10% Eitr regeneration and +10% stamina regeneration, without flat Eitr. Gear supplies flat pools. Existing parry damage rewards remain unchanged. Rune Block authored version 5 replaces the below-origin capsule with a fitted box (bottom at ground origin); rebuild previously placed blocks to correct their saved height.


2026-11-20: Apprentice Wand uses 5 base stamina per shot. Edit `WandTuning.Meadows01` / `wand.AttackStamina` for source tuning. `HexBattleMageWandProfile.AttackStamina` and native Attack.m_attackStamina are baked by starter progression migration 4; normal rebuilds preserve authored values. Runtime ApplySettings transfers the tuned value to each attack. Native Attack performs the stamina check/spending and applies normal skill/equipment/status cost modifiers. Eitr settings are unchanged. Rebuild the embedded bundle and DLL together.


Apprentice bracer: fixed +5 passive armor while worn, independent of quality and blocking. MagicBracerProfile.Armor is baked by passive armor migration 1; BracerTuning.Meadows01 supplies the source override. Registration sets native armor=5 and armor-per-level=0; Player.GetBodyArmor adds the separately equipped bracer once. Qualities 1�3 retain ShieldWood block 6/12/18, block force 20/25/30, parry 1.5x, durability 200/250/300. Armor never increases on upgrade. Normal rebuilds preserve authored values; rebuild bundle and DLL together.
