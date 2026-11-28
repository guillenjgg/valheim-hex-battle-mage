# Black Forest prototype

Implemented 2026-11-21. All new balance numbers are provisional, approved for initial testing. Existing fitted male/female garment meshes and their cosmetic catalogs are reused without changing Meadows assets.

## Blight

Learn at Rune-Whittler's Block level 2 (Eikthyr defeated): 1 TrophySkeleton, 1 SurtlingCore, 5 MushroomYellow. Learning and activation remain character-owned and count against the existing one-active-attunement limit. Spirit is not offered as a learnable attunement; its old development profile remains separate.

Base effect: 60 total damage over 3 seconds, 0.5-second tick interval, no stacking. Reapplication resets time and replaces the remaining budget with a fresh application; it never adds a second independent effect. The latest application snapshots duration and total damage. Duration changes preserve total damage, including a partial final tick. Blight-only projectiles have zero impact AoE; contagion is their only way to infect neighbours. Fire retains its existing impact area (including a future mixed Fire/Blight selection). Native direct wand physical damage, skill calculations and range falloff are unchanged.

SE_HexBlight calls native ApplyDamage with untyped HitData.m_damage.m_damage on the target owner only. It does not use Spirit/Poison channels, ApplyResistance, or the armor path in RPC_Damage. Native difficulty scaling, health/death handling and attacker attribution remain. Tick damage therefore bypasses armor and damage-type immunity in this prototype. Native invulnerability/damage acceptance is not globally modified. Violet impact sparks, tip indication and local status motes identify Blight.

Contagion defaults to 0m (base Blight is single-target). Change Development / Elements / Blight / SpreadRadius to 2, 4 or 6 for testing. TotalDamage, Duration, TickInterval and BossTickMultiplier are configurable in the same section. Tuning / UseConfigOverrides controls saved override precedence. Named source defaults are in BlackForestTuning.Blight; visuals and contagion settings are inspectable on HexWandElementBlight.

A direct application resolves an iterative queue over native nearby-character queries, immediately, once per chain. Each target receives a full application and can bridge to the next target. A chain visits at most 128 targets and requires line of sight by default. Players, tame creatures, dead targets and non-hostile targets are excluded. Secondary zero-damage native hits carry itemLevel=-1, status hash and attacker, allowing the victim owner to apply the status without restarting another chain. Only the initial owning peer computes the chain. No per-frame or per-tick contagion scan is used.

Paid contagion enhancements, Fire enhancement purchases, biome damage-tier upgrades, and the proposed fixed-physical-damage redesign are not implemented by this pass. They require the still-undecided costs/progression rules in the handoff. Configurable shorter durations already preserve total damage, but no Accelerated Decay purchase is introduced.

## Gear

| Item | Base armor | Armor per quality | Max quality | Flat bonuses |
| --- | --- | --- | --- | --- |
| Forestbound Hood | 6 | 2 | 4 | +8 Eitr, +4 health |
| Forestbound Tunic | 6 | 2 | 4 | +8 Eitr, +4 health |
| Forestbound Trousers | 6 | 2 | 4 | +8 Eitr, +4 health |
| Forestbound Cloak | 4 | 1 | 3 | +8 Eitr, +8 stamina; inherited frost resistance |

Craft each at Mage Table level 2: 5 TrollHide, 4 DeerHide, 2 Bronze. Each upgrade costs 2 TrollHide, 2 DeerHide, 1 Bronze. Flat bonuses do not scale with quality. The existing native station-level progression remains: armor qualities need table levels 2/3/4/5. Current Mage Table extensions reach 3; higher qualities need future extensions. The Black Forest catalog appears after Eikthyr.

## Earth staff

Stonebinder Staff: HexBattleMage_Staff_BlackForest_01. Native StaffFireball animation/weapon baseline, new wood/stone visual, solid world-drop collider, and a stone projectile. All fire damage, fire status, explosive area and inherited fire hit effects are removed. It remains ammunition-free.

Base 30 blunt +20 pickaxe, quality increments +6/+4, maximum quality 3, native mining tool tier 1. Costs 5 base stamina and 8 base Eitr per cast, subject to native modifiers. Uses Elemental Magic as its single native skill. Pickaxe damage is a native damage channel, not a new immunity/resistance type; ordinary creature and mineable-target handling remains native.

Craft at Rune Block level 2: 10 RoundLog, 10 Stone, 4 Bronze. Each upgrade: 5 RoundLog, 5 Stone, 2 Bronze. Native required levels 2/3/4 follow Eikthyr/Elder/Bonemass. BlackForestTuning.EarthStaff and the staff recipe branch provide source overrides. HexBattleMageStaffProfile contains inspectable defaults and referenced model/projectile assets. The compendium now lists pickaxe damage.

## Authoring and validation

BlackForestAssetBuilder.Build seeds missing assets and preserves existing authored values. It appends the Blight profile and learning definition to the existing wand/Rune Block catalogs. New assets live in Assets/_CustomItems/HexBattleMage/Progression/BlackForest and Elements/HexWandElementBlight.prefab. No new bundle name is used: hexbattlemage remains embedded in the DLL. Scripts, metadata GUIDs, prefabs and bundle are synchronized to the main Unity project.

BlackForestValidation exercises actual SEMan lifecycle, complete damage budgets, shorter duration, non-stacking refresh, owner-only ticks, multi-hop propagation, no chain loops, exclusions, wall blocking, actual Jotunn staff and all four gear registrations. Damage transport/health storage, native base objects and manager bootstrap use isolated fixtures. StarterWandProgressionValidation verifies existing handcraft/station recipes, Fire learning and character selection with the expanded catalog. Actual casting, mining copper, animated fits, drops, save/reload and co-op ownership migration still need in-game tests.

Attachment references checked: https://github.com/Valheim-Modding/Wiki/wiki/Attaching-Weapons-and-Armor. Native member access and damage/status behavior were inspected in the installed assembly_valheim.dll, with Jotunn cloning/registration exercised through the compiled library. ValheimRIP and installed game/Jotunn assemblies were not modified.


## Earth projectile freeze investigation and unlock UI (2026-11-21)

The reported incident was a freeze during earth-staff casting. BepInEx/Player logs contain no exception or native crash stack. They show mining impacts, an invalid MineRock5 hit-area message, then menu input. These logs do not establish a specific freeze cause.

A concrete inheritance defect was found: the native staff_fireball_projectile includes a root CinderSpawner with spawn-on-projectile-hit enabled. Removing child graphics and Projectile spawn fields did not remove that separate component. Installed assembly inspection confirms its Awake subscribes to Projectile.m_onHit and the callback spawns native fire cinders. Earth registration now removes CinderSpawner from its clone and clears inherited hit callbacks. The original native fireball is unchanged. Native-impact checks now include this component in the source fixture and prove it is absent from the earth clone, blunt/pickaxe damage reaches a creature hit once, and native impact marks the projectile stopped. This fixes the inheritance defect; the original freeze still needs an in-game retest to confirm causality.

The projectile now uses a worn, irregular 20-face boulder mesh, about 0.6m across. HexBattleMageStaffProfile authors ProjectileVisualScale=2.75, ProjectileCollisionRadius=0.22 and SuppressNativeCinders=true; BlackForestTuning.EarthStaff provides source overrides. BoulderVersion seeds the explicit new mesh/settings migration once; later rebuilds preserve edits. With existing development logging enabled, staff impacts write begin/completed markers and the remaining cinder-spawner count, so a later freeze can be narrowed down without flooding normal non-debug logs.

Attune details always list one-time unlock materials and required Rune Block level, even after learning. Learned requirements are marked paid, not charged again. Unlearned requirements show owned/required quantities. The boss name comes from the Rune Block's inspectable BossDisplayNames array parallel to BossDefeatKeys. Level-gated rows show Locked; Blight explicitly lists Eikthyr, level 2, 1 Skeleton Trophy, 1 Surtling Core and 5 Yellow Mushrooms. Wide/narrow UI checks verify requirements survive selection and learned-state changes.
