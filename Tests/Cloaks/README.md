# Cloak registration validation

`CloakRegistrationValidation.Run` loads the production bundle and actual compiled mod in the isolated runtime-profile-validation project. It exercises the same Jotunn registration fixtures documented in `Tests/Trinkets/README.md`: disable only PrefabManager, ItemManager and AssetManager static constructors in a disposable Jotunn copy, preserve the original and restore it afterward. Never patch installed DLLs.

Set HEXBATTLEMAGE_TEST_BUNDLE to the production bundle, copy the test into the isolated project's Assets/Validation directory and run Unity batch mode with `-executeMethod CloakRegistrationValidation.Run`. The project-root cloak-registration-report.txt must begin with PASS. Native baseline prefab and manager bootstrap are fixtures; deferred material mocking and full game cloth behavior are not tested here.

Checks cover registration despite the authored visual/name collision, native Shoulder equipment type, two-resource Mage Table recipe, icon, static world drop, male/female selection from current model index, unchanged vanilla lookup and successful application of the transpiler to native AttachArmor.

## Collision validation

`CloakCollisionValidation.Run` uses the same compiled-mod validation project and production bundle. It requires unmodified Jotunn: no manager bootstrap is invoked. Set HEXBATTLEMAGE_TEST_BUNDLE, copy the test under Assets/Validation, and execute this method in batch mode. The project-root cloak-collision-report.txt must begin with PASS. Physics geometry tests use real Unity box, nonconvex mesh and terrain colliders. Native SetupCloth is patched to test hook dispatch, with its actual simulation build skipped in the fixture. Body shape setup, bounds, list ownership, world scale and support lifecycle are exercised on both bundled fits. Complete Magica solver animation remains an in-game check.

## Native motion constraint

CloakMotionValidation.Run directly invokes the installed MotionConstraint.SolverConstraint with live native arrays, without a replacement solver or mocked math. It checks inward correction, outward freedom and maximum travel. Run in the same isolated project using `-executeMethod CloakMotionValidation.Run`; cloak-motion-report.txt must begin with PASS. This tests the actual constraint algorithm, not complete game animation. The bundle collision test additionally asserts single-surface vertex count for this specific mesh, two-sided rendering, authored backstop and travel-limit settings.

## Player-runtime body-data regression

CloakLoadPlayerBuilder.Build creates an isolated standalone player containing CloakLoadPlayer. It loads the production bundle and compiled mod, removes inline body arrays, and restores both sets of 14 capsules through the managed serializer. Run the resulting executable with -batchmode -nographics. cloak-player-load-report.txt in the workspace must begin with PASS. This exercises the actual player serialization path rather than assuming an Editor result applies to Valheim. The disposable game assembly in this test project has only platform-login bootstrap disabled; installed game assemblies remain unchanged.

## First cloak stats

CloakStatsValidation.Run checks the production prefab's armor 2 and MageEitrProfile (+5 cap, zero extra regen multiplier), then exercises actual native cape equip/unequip with the existing TrinketValidationPlayer fixture from the isolated validation project. It checks immediate no-food cap updates, no inventory-only bonus, removal/clamping and no re-equip refill. Visual setup, equip effects and network identity are isolated. The cloak-stats-report.txt must begin with PASS. Full regeneration ticks remain an in-game check; the established generic gear-Eitr path supplies regeneration.

## Armor-aware setup test

CloakLoadPlayer now also requires the extracted AshlandsMageChest.asset copied only into the disposable validation project's Assets/Resources/ValidationAshlandsChest.asset. Keep its native unreadable flag. Do not include that native reference in the production authoring/bundle dependency graph. The standalone test verifies posed geometry fitting, restoration on armor removal and collision-responsive shoulder paint for both bundled cloak fits. Native animation/solver appearance remains an in-game test.
