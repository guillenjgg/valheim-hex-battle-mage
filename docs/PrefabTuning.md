# Code tuning

Edit `HexBattleMage/Core/Tuning/`. These are ordinary C# assignments to existing prefab components.

| File | Tuning location |
| --- | --- |
| WandTuning.cs | Meadows01: physical damage, quality limit and starter costs; ConfigureUpgrades: station crafting toggle, station craft/upgrade costs and minimum level; Fire learning now lives in AttunementTuning.cs |
| AttunementTuning.cs | Character active-slot limit and Rune Block learning recipes/requirements; default limit 1 |
| WandElementTuning.cs | Fire, Frost, Spirit, Lightning, Poison: hit/tick damage, quality scaling, durations, slows, spreading, stack cap and boss DoT multiplier |
| CraftingTuning.cs | RuneBlock: build cost, initial level, ordered boss keys and refresh interval; extension cases: Mage Table upgrade costs |
| ArmorTuning.cs | HelmetMeadows01, ChestMeadows01, LegsMeadows01: per-item health; ApplyMeadowsQuality: shared base armor, quality limit/scaling, flat Eitr and recipe |
| CloakTuning.cs | Meadows01: armor/quality, frost resistance, stamina, Eitr/scaling, fixed upgrade level and recipe |
| BracerTuning.cs | Meadows01: quality limit, block power, force and parry; Apply: recipe and minimum station level |
| TrinketTuning.cs | Meadows01: Eitr, combat reward and recipe |

Only assigned fields override Unity. Other values remain authored. For example, add `armor.Armor = 3f;` inside ChestMeadows01 to change that chest alone. Access additional profiles with `PrefabSettings.Require<MageEitrProfile>(prefab)` or `PrefabSettings.Require<MageRecipeProfile>(prefab)`. Keep player-facing strings as localization tokens.

Startup order: **bundle defaults, managed recipe hydration, code tuning, configuration binding, item registration**. Rebuild the DLL and restart after code changes. No bundle rebuild is needed. Unity displays the authored baseline, not runtime overrides.

`[Tuning] UseConfigOverrides = true` preserves existing testing settings. Set it to `false` once to use code tuning instead of saved development/element/trinket balance values. False reads binding defaults from the tuned profiles. Saved values remain intact; switching back to true restores them. The existing config reload path reads the switch live. Development element selection is live only when BypassAttunements is explicitly enabled; normal play uses the character's saved active selection. Debug logging remains live. Recipes and armor/bracer/cloak stats have no config overrides and always use their tuned profiles.

Add an exact item identity branch and a named method for each new item. Armor/cloak/trinket dispatch uses ItemPrefabName because authoring asset names can differ from runtime names. The legacy bracer uses its source visual name. Add a utility tuning file when utility profiles and registration exist; no utility gear exists yet.

The current wand manager still registers one wand with shared development settings. Independent configuration for multiple wands remains future work. Referenced element assets may be shared: modifying one affects all items referencing it. Cosmetic catalogs retain their separate Unity authoring/managed loading in this pass.

PrefabTuningValidation checks dispatch, unchanged fields, managed recipes and config precedence. Native balance and registration checks run separately. This layer adds no game hooks, dependencies or bundle components.

CraftingTuning.cs now contains the two Mage Table extension recipes. Its Configure method binds per-material construction costs under Crafting / Mage Table / <prefab>. These bindings respect UseConfigOverrides and require a restart. ArmorTuning.ApplyMeadowsQuality supplies the shared four-quality starter-armor curve and upgrade materials.

Meadows recipes now use fixed cheaper upgrade costs. Each category tuning file owns its item recipe. MageUpgradeProfile.FixedResourceCosts controls material scaling separately from UseFixedStationLevel; false retains native quality-based station levels. Defaults are baked with MeadowsCostAssetBuilder.

WandTuning.Meadows01 hydrates referenced upgrade and attunement recipes before assigning their code overrides, so later managed-data reads retain them. Inspect those referenced prefabs in Unity for their baked baseline costs.

## Source-tuning audit (2026-11-19)

The recent progression and starter-gear values now have explicit source edit locations in the table above. Current balance is preserved. Prefab baseline → managed recipe hydration → source tuning → optional saved-config overrides → runtime registration remains the precedence. To test source balance without saved development values masking it, set `[Tuning] UseConfigOverrides = false`. Recipe changes and other settings without config entries always use source tuning.

Changing these existing values requires rebuilding/installing the DLL and restarting Valheim; the embedded bundle need not be rebuilt. Editing Unity baseline values, adding meshes, introducing new referenced attunement/element assets or changing serialized profile structure still requires a bundle rebuild. A field initializer alone does not update an existing prefab. Cosmetic data not explicitly overridden by tuning remains authored in Unity; this audit does not claim every historical visual/layout setting has a dedicated tuning field.

Discovering an attunement currently means learning every material in its tuned cost list. Eligibility is rechecked when paying; selection/learning is saved per wand. Boss keys determine station levels, not automatic element grants. Quality requirements use native minimum-level + quality - 1, except profiles explicitly opting into a fixed upgrade level (the cloak uses 3). Rune Block notifications now honor a tuned initial level and refresh interval instead of assuming level 1 in their runtime logic.

Regression checks cover source overrides for effect ticks/slows/stacks, boss progression, referenced recipes after hydration, cloak Eitr/upgrade costs, native payment/save/load and notification behavior with a changed initial level. The repository instructions require the same tuning entry points for future balance/progression additions.

Rune Block crafting correction: `WandTuning.ConfigureUpgrades` sets `CanCraftAtStation = true`. Its recipe uses Amount for the initial station craft (10 Wood + 2 Stone), and AmountPerQuality for each fixed cheaper upgrade (5 Wood + 1 Stone). The existing UpgradeRecipe asset serves both station actions. Inventory crafting remains available independently.

Workbench exclusion is scoped in StarterWandRecipeRules.InventoryCraftingStationPatch: the handcraft wand recipe is inventory-only even when a vanilla station shows basic recipes. WandTuning.ConfigureUpgrades still controls Rune Block crafting and its costs. Attunement wording lives in Assets/Translations/English.json; layout and state colors live in Core/UI/WandAttunementTab.cs. Saved character learning and active state are the authority for UI badges.


Character attunements (2026-11-20) supersede the per-wand tuning notes above. Change `AttunementTuning.Apply` for MaximumActiveAttunements (currently 1) and Fire learning costs/station level. Rune Block profile references list the available definition assets; source changes do not invent missing definitions. Source tuning runs after recipe hydration. Learning, activation and replacement recheck the current station and character data. Balance edits need only DLL rebuild/restart; adding definitions or editing baked Rune Block defaults also needs a bundle rebuild.

Player-facing character attunement UI uses `hexbattlemage_attunements_*` localization tokens. Old wand-selector strings are removed. The development bypass remains an explicit testing override; leave it false for progression testing. Existing individual item quality/durability/appearance settings are unchanged.

WandTuning.Meadows01 owns WeaponSkill (Clubs) and SkillGain (native raise factor 1). AttunementTuning owns the current Fire learning recipe: 2 Resin, 3 Feathers, 10 Wood. These values are also baked through versioned StarterWandAssetBuilder migration and projected by normal prefab preparation. Qualifying native impacts handle skill XP; damage-type channels do not independently select skills.


2026-11-20: `TrinketTuning.Meadows01` sets Stone of Reprisal to zero flat Eitr, +10% Eitr regeneration and +10% stamina regeneration. Passive defaults are baked in MageEitrProfile and HexBattleMageTrinketProfile; per-item configuration can override both rates. Parry bonuses remain +25% for two seconds, plus 10 percentage points for bosses. `CraftingTuning.RuneBlock` overrides the inspectable BodyColliderCenter/BodyColliderSize fields and updates the solid BoxCollider; its bottom is the mesh ground origin. Existing placed blocks need rebuilding to correct their saved height.


2026-11-20: Apprentice Wand uses 5 base stamina per shot. Edit `WandTuning.Meadows01` / `wand.AttackStamina` for source tuning. `HexBattleMageWandProfile.AttackStamina` and native Attack.m_attackStamina are baked by starter progression migration 4; normal rebuilds preserve authored values. Runtime ApplySettings transfers the tuned value to each attack. Native Attack performs the stamina check/spending and applies normal skill/equipment/status cost modifiers. Eitr settings are unchanged. Rebuild the embedded bundle and DLL together.


Apprentice bracer: fixed +5 passive armor while worn, independent of quality and blocking. MagicBracerProfile.Armor is baked by passive armor migration 1; BracerTuning.Meadows01 supplies the source override. Registration sets native armor=5 and armor-per-level=0; Player.GetBodyArmor adds the separately equipped bracer once. Qualities 1�3 retain ShieldWood block 6/12/18, block force 20/25/30, parry 1.5x, durability 200/250/300. Armor never increases on upgrade. Normal rebuilds preserve authored values; rebuild bundle and DLL together.
