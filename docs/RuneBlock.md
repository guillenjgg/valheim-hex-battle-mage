# Rune-Whittler's Block

The Meadows block is HexBattleMage_RuneBlock under Crafting/RuneBlock in Unity. It costs 10 Wood, is discovered through native Wood knowledge, and appears under Battle Mage in the Hammer menu. It needs no other crafting station, roof or fire. It uses the approved carving-stump concept: gnarled roots, cut end grain, constant blue carved ring, resting wand, flint knife, resin bowl, shavings and a hanging wooden charm. The decorative tools add no recipe requirements. Native ZNetView, Piece, WearNTear, CraftingStation and solid collision remain on the same root. Its icon is transparent, readable and compressed at 256 pixels.

## Automatic progression

There is one station prefab and no StationExtension components or separate upgrade pieces. HexBattleMageRuneBlockProfile authors BaseLevel, world boss keys, refresh interval, localized notification token and cumulative child visual paths. Boss flags are world-wide, matching vanilla world progression:

| Station level | World boss flag |
| --- | --- |
| 1 | None |
| 2 | defeated_eikthyr |
| 3 | defeated_gdking |
| 4 | defeated_bonemass |
| 5 | defeated_dragon |
| 6 | defeated_goblinking |
| 7 | defeated_queen |
| 8 | defeated_fader |

The highest defeated boss determines the current level. A scoped CraftingStation.GetLevel postfix reads current world flags. It does not modify Mage Table or vanilla stations. Each block refreshes cumulative fittings/charms from its authored Progression children, so existing blocks and newly loaded blocks update without rebuilding or payment. Multiplayer peers derive their visuals from synchronized vanilla world keys; no custom RPC or client-owned progression is added.

Notifications poll the local player and save the last announced level per world UID in Player.m_customData. Fresh Meadows is silent. First entry into an advanced world reports its current level once; later increases report once, and reloading does not repeatedly announce the same level. No current Eitr, resources or wand stats are changed by the notification.

## Crafting split

The physical starter wand crafts in the inventory Craft panel after discovering Wood and Stone. Initial cost: 10 Wood + 2 Stone. Its base damage is 5 blunt + 5 pierce, without fire. Stone of Reprisal crafts at the Rune Block. Both items repair at the block; armor/bracers remain at the Mage Table.

The wand also appears on the Rune Block Craft tab for the same 10 Wood + 2 Stone as inventory crafting. Its station recipe also handles quality upgrades for 5 Wood + 1 Stone per upgrade. Qualities 2/3/4 require block levels 2/3/4. A scoped native GetRequiredStation rule prevents the handcraft recipe from upgrading without the block. HexBattleMageWandProfile.CanCraftAtStation is baked true and explicitly tunable in WandTuning.ConfigureUpgrades. It controls the native recipe craft/upgrade-only flag. The block does not show unrelated inventory recipes.

The Attune tab manages character-bound magic at the Rune Block. Discovering Resin, Feathers and Wood reveals Fire; learning it costs 2 Resin + 3 Feathers + 10 Wood at block level 1. Learning does not automatically activate it. Activation is free and requires the Rune Block. The character starts with one active slot. Fire adds 15 direct fire damage at quality 1, +2 per item quality, and the existing burning effect; physical damage remains. Only Fire is currently authored as a gameplay attunement.

Learned and active elements live in Player.m_customData, independent of inventory, equipped items and world UID. Spare, transferred and replacement wands read the current wielder's selection. Native item quality, style and durability remain per item. Old development per-wand custom data is ignored; no compatibility import is provided. A character must learn Fire once through the new flow.

The window uses a character summary (active count/limit and names), an element list on the left and selected details on the right. Inspecting an element never activates it. One bottom action learns, activates or explicitly replaces a named active element. Learned, active, unlearned and discovery-locked states are text-labelled. Each column scrolls independently with a gold scrollbar when necessary. No wand selector remains.

HexBattleMageRuneBlockProfile owns MaximumActiveAttunements and the referenced definition prefabs. AttunementTuning.cs sets the source-side limit (currently 1), Fire learning cost and station requirement. Increasing the limit permits multiple learned active elements; at capacity the UI chooses a named replacement. Active elements combine their damage channels, and a short-lived grouped status dispatches each selected custom effect once with the actual attacker. No extra elemental definitions are unlocked by raising the limit alone. Full multiplayer combat remains an in-game verification step.

Runtime SharedData remains private per ItemData so temporary character-specific damage never contaminates another item's baseline. Native GetDamage refreshes current character state. Tip emission follows active elements (blended if several); the shaft runes and emitted light retain their accepted colors. Owner ZDO state synchronizes the equipped tip's element mask to peers.

ActiveElement is now a development selector only. Development / Elements / BypassAttunements defaults false; enable it explicitly to test other implemented elements without progression. Old ActiveElement settings do not grant fire automatically.

Wand appearance controls follow the Rune Block; armor appearance controls stay at the Mage Table. Unlocks filters entries by station. The wand catalog entry uses its upgrade recipe. There are no custom staff definitions yet; vanilla staff recipes are untouched.

## Authoring and code tuning

RuneBlockAssetBuilder.Build creates missing assets, performs its explicit authoring migration and preserves existing art/defaults during normal rebuilds. It runs the versioned StarterWandAssetBuilder migration, updates trinket station references and refreshes managed recipe data before rebuilding hexbattlemage. New wand recipe builders seed handcrafting with Rune Block repairs; trinkets seed the Rune Block. Textures use max size 256, Read/Write, Normal compression, Crunch 100; end grain uses Point filtering. All visual stages are children of the same prefab and can be inspected independently in Unity.

CraftingTuning owns the code-side block recipe. Crafting / Rune Block / Wood allows optional saved-config tuning of its construction cost, honoring Tuning / UseConfigOverrides; restart after changing costs. Boss keys and visuals remain authored on the profile. Build the bundle and DLL after prefab/profile/material changes; code tuning requires only the DLL and restart.

## Validation

RuneBlockValidation exercises actual native world-key lookups and station GetLevel at levels 1-8, cumulative visual activation, vanilla pass-through, saved/world-specific notification deduplication, baked recipe migration, station-scoped catalogs/style guards and real Jotunn CustomPiece/AddPiece registration with 10 Wood and no prior station. The BepInEx/Jotunn world bootstrap is an isolated fixture. Native equipment/combat and UI validations run separately. Construction, interactions, visual clearance and multiplayer still need in-game verification.

The solid stump capsule is deliberately separate from the optional native station effect collider. Installed native GetExtensions resizes the latter to the build radius, so this station leaves that optional effect reference null. Validation forces native extension refresh and checks that the physical radius remains below one metre. Notifications wait until the HUD exists before recording/showing a newly reached level.

StarterWandAssetBuilder bakes upgrade and Fire attunement data into referenced settings prefabs beside the wand. Each uses MageRecipeProfile plus managed recipe JSON; Fire also has HexBattleMageWandAttunementProfile with element and localized presentation. Normal rebuilds retain authored edits after the one-time versioned migration. WandTuning.cs owns the corresponding code overrides.

The block's WearNTear.m_fragmentRoots is empty. Removing the inherited Mage Table visual had left a null fragment root; native RPC_CreateFragments dereferenced it during Hammer removal. An empty array uses native visible-mesh fallback. Version 4 migrates existing authoring without changing geometry. RuneBlockValidation invokes that actual RPC with only highlight/fragment spawning bootstrap isolated.

StarterWandProgressionValidation checks actual Jotunn recipe registration, native discovery/station selection, station bypass prevention, payment/failure, independent wand stats, native clone/save/load and narrow-panel text/button layout. Balance, UI, tuning and localization regressions also run. Full early progression and multiplayer still need in-game testing.


## Crafting location and attunement clarity (2026-11-20)

The inventory wand recipe is excluded whenever another crafting station is open. This prevents the vanilla Workbench's show-basic-recipes feature from inheriting it. The Rune Block uses its own wand recipe; unrelated vanilla handcraft recipes are unaffected. StarterWandRecipeRules.InventoryCraftingStationPatch scopes the native RequiredCraftingStation result to the registered inventory wand recipe only.

Attune now separates the selected wand, active element summary and per-element cards. Cards explicitly distinguish learned/active, learned/inactive, unlearned and discovery-locked states using localized text as well as color. Active cards have a blue stripe and no redundant disabled action. Learned inactive elements offer Attune to <element>; unlearned elements show material counts and Learn <element>. All authored elements remain listed as locked before discovery, rather than disappearing. Single-wand inventories omit redundant wand-selector buttons. Multiple wands retain individual saved learned/selected states.

Native recipe-selection checks cover the vanilla Workbench, inventory and Rune Block, plus vanilla recipe pass-through. Narrow-panel checks cover wrapping and learned/active/inactive/unlearned states on two independent wands. Runtime code/localization changes only; no asset bundle rebuild is required.


## Character-bound validation (2026-11-20)

The approved character-focused design supersedes the earlier per-wand UI notes. Tests cover exact learning costs, failed payment, separate activation, no-wand learning, duplicate-learning rejection, native inventory/Rune Block/Workbench filtering, spare/replacement wands, item transfer between characters, restored character custom data and native inventory save/load. They also cover cap 1 replacement, cap 2 combined damage, grouped effect dispatch/attacker propagation, reduced-cap enforcement, selected-versus-active UI state and readable wide/narrow layouts. Existing quality, gear, localization and Rune Block tests run separately. World bootstrap and native message display remain isolated fixtures; full game save/death/relocation and co-op testing are still required.

Wand skill baseline: the item authors Clubs and SkillGain=1 on HexBattleMageWandProfile, projected to ItemDrop/Attack. Source overrides live in WandTuning.cs. Native projectile impacts award the configured skill once on a qualifying character hit; no extra XP hook or Bow XP is added. The native attack multiplier scales all initial damage channels, including elemental damage. Custom status tick damage remains separately tuned. Installed StaffFireball uses ElementalMagic for its blunt/fire mix; native Attack.ModifyDamage multiplies all channels together.


2026-11-20 update: Stone of Reprisal passive defaults are +10% Eitr regeneration and +10% stamina regeneration, without flat Eitr. Gear supplies flat pools. Existing parry damage rewards remain unchanged. Rune Block authored version 5 replaces the below-origin capsule with a fitted box (bottom at ground origin); rebuild previously placed blocks to correct their saved height.
