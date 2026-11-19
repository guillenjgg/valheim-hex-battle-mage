# Code tuning

Edit `HexBattleMage/Core/Tuning/`. These are ordinary C# assignments to existing prefab components.

| File | Tuning location |
| --- | --- |
| WandTuning.cs | Meadows01: physical damage, quality limit and referenced fire profile |
| ArmorTuning.cs | HelmetMeadows01, ChestMeadows01, LegsMeadows01: one method per item |
| CloakTuning.cs | Meadows01: armor, quality, frost resistance and stamina |
| BracerTuning.cs | Meadows01: block power, force and parry |
| TrinketTuning.cs | Meadows01: Eitr, combat reward and recipe |

Only assigned fields override Unity. Other values remain authored. For example, add `armor.Armor = 3f;` inside ChestMeadows01 to change that chest alone. Access additional profiles with `PrefabSettings.Require<MageEitrProfile>(prefab)` or `PrefabSettings.Require<MageRecipeProfile>(prefab)`. Keep player-facing strings as localization tokens.

Startup order: **bundle defaults, managed recipe hydration, code tuning, configuration binding, item registration**. Rebuild the DLL and restart after code changes. No bundle rebuild is needed. Unity displays the authored baseline, not runtime overrides.

`[Tuning] UseConfigOverrides = true` preserves existing testing settings. Set it to `false` once to use code tuning instead of saved development/element/trinket balance values. False reads binding defaults from the tuned profiles. Saved values remain intact; switching back to true restores them. The existing config reload path reads the switch live. Element selection and debug logging always remain live controls. Recipes and armor/bracer/cloak stats have no config overrides and always use their tuned profiles.

Add an exact item identity branch and a named method for each new item. Armor/cloak/trinket dispatch uses ItemPrefabName because authoring asset names can differ from runtime names. The legacy bracer uses its source visual name. Add a utility tuning file when utility profiles and registration exist; no utility gear exists yet.

The current wand manager still registers one wand with shared development settings. Independent configuration for multiple wands remains future work. Referenced element assets may be shared: modifying one affects all items referencing it. Cosmetic catalogs retain their separate Unity authoring/managed loading in this pass.

PrefabTuningValidation checks dispatch, unchanged fields, managed recipes and config precedence. Native balance and registration checks run separately. This layer adds no game hooks, dependencies or bundle components.

CraftingTuning.cs now contains the two Mage Table extension recipes. Its Configure method binds per-material construction costs under Crafting / Mage Table / <prefab>. These bindings respect UseConfigOverrides and require a restart. ArmorTuning.ApplyMeadowsQuality supplies the shared four-quality starter-armor curve and upgrade materials.

Meadows recipes now use fixed cheaper upgrade costs. Each category tuning file owns its item recipe. MageUpgradeProfile.FixedResourceCosts controls material scaling separately from UseFixedStationLevel; false retains native quality-based station levels. Defaults are baked with MeadowsCostAssetBuilder.
