# Wand prefab validation

Run WandPrefabValidation.Run in the isolated Unity validation project containing the compiled mod DLL, BepInEx, Jotunn and installed Valheim assemblies. Set HEXBATTLEMAGE_TEST_BUNDLE to the production bundle path. Do not import the mod DLL alongside the editable authoring scripts.

The test uses a temporary configuration and actual bundle. It checks identity, shared profile and dependency references, native value projections, localization tokens, config defaults and overrides, runtime tuning without changing the baseline, and recipe recovery by the canonical prefab identity. The report beside Assets must begin with PASS and Unity must exit successfully. It does not run a full game session or validate multiplayer.
