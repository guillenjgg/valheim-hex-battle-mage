# Player-facing text

Player-facing prefab values store `$` tokens. English wording lives in `HexBattleMage/Assets/Translations/English.json`; the DLL embeds every JSON file in that directory and loads it through Jotunn's localization system. Add another file named for the Valheim language, using the same keys. Missing translations fall back to English.

The trinket's name, description, section headings, conditions, refresh note and buff text are tokens in HexBattleMageTrinketProfile. Names and descriptions for both Mage Table extensions and all five elemental status names are also tokenized. Existing wand, bracer, table, unlock-list and compendium text already uses localization tokens.

Runtime trinket stat lines are complete translation templates with numeric placeholders. Values come from the current per-trinket configuration, not authored English text or cached strings. Translators can reorder Eitr amounts, percentages, totals and durations. Rich-text emphasis remains supported. Do not translate prefab IDs, reward IDs, resource IDs, config sections or bone names.

For another trinket, use item-specific name/description keys such as `$item_hexbattlemage_trinket_meadows_02` and `$item_hexbattlemage_trinket_meadows_02_description`. Reuse shared heading/condition tokens when the meaning is the same. Add its English entries before bundling. Authoring builders seed tokens for new assets and preserve existing authored token values.

**HexBattleMage > Tokenize Player Text and Build Bundle** is the explicit migration/preparation command for the existing assets. It replaces known authored English values with their tokens and preserves non-text settings. Unknown customized text is rejected for review rather than silently replaced. Technical identifiers remain unchanged. Rebuild the DLL afterward to embed the updated bundle and translation resources.

## Verification

`Tests/Localization/PlayerTextLocalizationValidation.cs` checks embedded resources, placeholder validity, missing prefab tokens, English output, live values and reordered translations. It isolates Jotunn's language lookup and game bootstrap; the mod's actual resource loader, formatting and tooltip code run. Its alternate-language strings are test fixtures, not a shipped translation. Native Jotunn language lookup and English fallback were checked in the installed DLL. In-game language switching and final UI layout still require verification.

The gameplay harness also runs with the localized text, while separate native extension checks cover unchanged station-level discovery and duplicate handling. Non-text settings were compared across 33 prefabs before the authored text was copied back into the editable project.
