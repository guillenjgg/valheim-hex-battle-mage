# Trinket registry validation

`TrinketRegistryValidation.cs` runs in an isolated Unity validation project that imports the actual compiled `HexBattleMage.dll`, BepInEx, Harmony, Jötunn and the installed Valheim assemblies. Do not import that DLL into the editable authoring project alongside its data scripts.

Copy the test into the validation project's Assets directory and run Unity in batch mode with `-executeMethod TrinketRegistryValidation.Run`. It writes `trinket-registry-report.txt` beside the validation project's Assets directory. Temporary config paths stay under that project's Temp directory; the test does not edit game configuration or register test items in a world.

The test creates three temporary definitions. It checks independent config entries and reward hashes, override precedence, passive-only behavior, duplicate identity/config/status rejection, and retaining the previous registry if validation fails. The report must begin with `PASS` and Unity must exit successfully.

Native equipment/parry/world integration remains a separate isolated harness and in-game test step; this registry test does not exercise a complete Valheim session.

## Registration name collision

TrinketRegistrationValidation.Run exercises the actual compiled RegisterAll and Jotunn ClonePrefab, AddPrefab, AddItem and AddRecipe paths while the production visual prefab is loaded under the registered item name. It checks the gameplay clone, native trinket type, recipe target/station, three authored resource requirements, inventory icon and worn attachment. Deferred mock-reference resolution and a full game session are outside this test.

This separate registration test requires manager bootstrap disabled only in a disposable copy of Jotunn.dll: replace the static constructor bodies of Jotunn.Managers.PrefabManager, AssetManager and ItemManager with ret using Mono.Cecil. Retain an unmodified copy and restore it afterward. Never modify the installed game/profile DLLs. The test supplies uninitialized manager fixtures, native-base item fixture and mock-reference storage; Harmony isolates AddAsset storage, plugin metadata lookup and ItemDrop Awake/OnValidate. Other registration methods run unchanged. Set HEXBATTLEMAGE_TEST_BUNDLE to the production bundle and run Unity with -executeMethod TrinketRegistrationValidation.Run. The report trinket-registration-report.txt beside Assets must begin with PASS and Unity must exit successfully.
