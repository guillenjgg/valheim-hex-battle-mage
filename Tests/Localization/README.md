# Player text localization validation

Copy `PlayerTextLocalizationValidation.cs` into the isolated validation project that imports the compiled mod DLL, installed game assemblies, Harmony, BepInEx and Jotunn. Set `HEXBATTLEMAGE_TEST_BUNDLE` to the production asset bundle path and run Unity with `-executeMethod PlayerTextLocalizationValidation.Run`.

The test writes `player-text-localization-report.txt` beside the project's Assets directory. Require a successful Unity exit and a report beginning with PASS. It uses temporary config files within the validation project, not the Dev profile. The translation lookup/bootstrap is isolated while the actual mod resource loader, formatting code and tooltips run. It verifies all audited prefab tokens resolve, placeholders are valid, translated word order is respected, current settings are used and English fallback is retained. A second language is not shipped by the test.

The native gameplay harness can call `InitializeForTests(harmony)` to obtain the same isolated translation backend while exercising localized tooltips. Keep the returned Harmony owner's patches scoped to that test run.
