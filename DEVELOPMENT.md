# Throat Balls - development notes

Everything about how the mod is built and tested: versions, the game APIs used and what is verified vs. assumed, how a hit and a reaction are decided, test status. The player-facing description and the Mod Menu reference are in [`README.md`](README.md); the spec is [`CLAUDE.md`](CLAUDE.md).

## Status

| Step | State |
|---|---|
| 1. Project, pure logic + unit tests, game glue, Mod Menu, scenarios | built 2026-11-22 on Windows. **Unit tests: 169/169 pass** (`dotnet test`; written first each time: the first run failed to compile with the logic missing (110 errors), the chance tests failed to compile before the rolls existed (9 errors); logs in `artifacts/tests/`, not committed). **Release build: 0 warnings, 0 errors** against the Windows install's `ThunderRoad.dll`. All five scenarios parse with BasHarness 1f678cf's parser (0 errors). |
| 1b. Chances (v0.2.0, Nadi 2026-11-22) | Groin Knockdown Chance (50 %), Both Hits Knockdown Chance (100 %), Throat and Groin Reaction Chance (100 %); 25 new unit tests (`ReactionChanceTests`, option specs). |
| 2. Headless runs (fix: v0.2.1) | **5/5 pass** on Linux (2026-11-22, `artifacts/harness/20261122-204123`). The first run failed `3-combo` (4/5): after the brain woke, the HumanDummy fought, walked off and the next groin hit landed 0.30 m from the zone. Cause: `SkillDiscombobulate.BrainToggle(conscious: true)` sets `brain.instance.updateTree = true`, so a brain that ran no tree (HumanDummy: `updateTree` false in `Brain_HumanDummy.json`) came back fighting. Fix: the reaction keeps `updateTree` from before the stun and gives it back after the wake (`Reaction.TreeWasRunning`). `1-throat` and `3-combo` now check `expect-npc tree False` after each wake (red before the fix: `tree is 'True'`, green after). Needs BasHarness with `expect-npc tree` (2026-11-22). |
| 3. Headset | **Started** 2026-11-23 (Windows, Debug Logging and Show Zones on, `Player.log`): throat and groin punches counted (`LOW BLOW` at 2.3 to 10.2 m/s), groin down with Curl Up, throat made longer by a second hit; slashes refused (`HitNotAllowed`), women refused. Not seen yet: both hits one after the other, getting up after Down Duration (every downed NPC was killed first), how it looks. |
| 3a. Fix: hit markers without collider (v0.2.2) | The headset log had 519 Unity errors "Destroying components immediately is not permitted during physics trigger/contact" and 1258 game warnings "Target physics material not found [/ThroatBalls.Hit]". A hit marker is made in the game's contact callback, where Unity refuses `DestroyImmediate`, so the marker kept its sphere collider and weapons bumped into it for 5 s. Now the collider is disabled at once and destroyed at the end of the frame. Red/green: `5-show-zones.txt` (`artifacts/harness/20261124-084502` red, `20261124-084545` green). Only with Show Zones on. |
| 3b. Own sounds, Allowed Hits titles (v0.3.0, Nadi 2026-11-24) | WAV files from the mod's `Sounds` folder: `throat*.wav` at the start and on every repeat, `groin*.wav` once at the start of Groin and Down; Own Sounds option (on), no files or off: the game's dialogs. Allowed Hits shows "Fists, Kicks, Blunt Weapons" / "Fists And Kicks Only" / "Any Hit, Blades Too". 46 new unit tests (`WavReaderTests`, `OwnSoundsTests`, `OptionTitlesTests`), red first (did not compile). Harness 6/6 with test tones (`artifacts/harness/20261124-095208`); the first run failed `2-groin` because `1-throat` left Own Sounds off in the run's saved options, now every scenario that needs it sets it. Not heard in the headset yet: there are no real sound files yet. |

The headless runs and one headset session (2026-11-23) are the in-game evidence so far. Every other "verified" below means read in the decompiled DLL and compiled against it.

## Versions

| What | Version | Source |
|---|---|---|
| Game (Windows install) | `ThunderRoad.dll` 1.3.2, same as AchillesBAS | decompiled `C:\Steam\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\Managed\ThunderRoad.dll` with ilspycmd 9.1 on 2026-11-22 |
| Catalog | the game's `StreamingAssets/Default/bas.jsondb` (voices, animations) | unzipped and read on 2026-11-22 |
| Manifest `GameVersion` | `1.0.0.0` | as AchillesBAS (`ModManager` requires an exact match) |
| Target framework | .NET Framework 4.7.2, C# 9 | as AchillesBAS |
| Tests | net8.0, xunit 2.9.2 | |

## API check

### Verified in `ThunderRoad.dll` 1.3.2 (read and compiled against)

| Used for | API | Notes |
|---|---|---|
| Hits | `EventManager.onCreatureHit (Creature, CollisionInstance, EventTime)` | `OnEnd` only for hits that did not kill, after the damage. |
| Men only | `CreatureData.gender` (`None, Male, Female`) | The game itself picks male/female hurt sounds and the animator's femininity by it. |
| Metal armor | `ItemModuleWardrobe.isMetal` of `Equipment.GetEquipmentOnPart(part)` | Exactly the game's `RagdollPart.HasMetalArmor`. `GetEquipmentOnPart` logs a warning for a part without `wearable`, so that is checked first. Groin: the thighs (`LeftLeg/RightLeg`, `Section.Upper`), whose channel holds the Pants, Skirt, Armor and Underwear layers (`ArmorStats`). Throat: the neck part. |
| Body parts | `Ragdoll.GetPart(Neck)`, `Ragdoll.rootPart`, `GetPart(Torso, Lower)` | |
| Attacker, unarmed | `sourceColliderGroup.collisionHandler.item.lastHandler.creature`, `.ragdollPart.ragdoll.creature`, `casterHand.mana.creature` | Same chain as AchillesBAS. Unarmed = a body part source, no item, no spell. |
| Out of the fight | `SkillDiscombobulate.BrainToggle(creature, conscious)` (public static) | The game's stun: brain modules stop (hit reaction keeps running), AI tree stops, `brain.isUnconscious` set. The stun also zeroes `locomotion.moveDirection` every frame; so does this mod. Waking sets `brain.instance.updateTree = true` whatever the brain had; the mod restores the value from before (seen in the harness 2026-11-22: a HumanDummy fought after the wake). |
| Throat sway | `Creature.PlayAnimation("HumanStun")`, `StopAnimation()` | The stun skill's clip (start, loop, end). On the end, the brain comes back after 0.75 x the end clip, as `SkillDiscombobulate.EndStun` does. |
| Hands to the spot | `ragdoll.ik.SetHandAnchor(side, transform)`, `SetHandWeight(side, pos, rot)`; clear with `SetHandAnchor(side, null)` | `IKControllerFIK` sets the target and weight 1/1; this mod then sets rotation weight 0 (position only). The game's bow, cast and defense AI use it on NPCs. |
| Bent over, down | `Creature.SetAnimatorHeightRatio(h)`, `animator.SetFloat(Brain.hashInjured, 1)` | What `Burning` does to a burning NPC that drops. |
| Ragdoll fall | `Ragdoll.SetState(Destabilized)`, `Brain.AddNoStandUpModifier / RemoveNoStandUpModifier` | The hold is a list without duplicates; re-added every frame as AchillesBAS does. |
| Voice | `BrainModuleSpeak.Play(dialogId, abnormalNoise)` | Refuses while `brain.isUnconscious` (read once at its start); this mod lifts the flag around the call when it set it. Dialog ids `grappled`, `hit`, `heavybreath` are in all eight `HumanMale*` voices of the catalog. |
| Drop weapons | `RagdollHand.TryRelease()` | |
| Own sounds | `ModManager.ModData.fullPath` (the mod folder), `AudioClip.Create` + `SetData`, an `AudioSource` on `ragdoll.headPart` routed to `ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Voice)` | WAV decoded in pure code (`Logic/WavReader`), no web request. The male voices of the catalog (`Voices/Voice_HumanMale*.json`, read 2026-11-24) have the dialogs attackbow, attackcast, attackmelee, breathlessdeath, death, deathlong, enemyspotted, falling, grappled, heardnoise, heavybreath, hit, jump, muffled, muffleddeath: no "ow", and the game's `choke` dialog (`BrainModuleSpeak.speakOnChoke`) is in none of them. |
| Enum titles in the menu | `ModOptionInt(title, value)` from a value-source method | The game shows raw enum names otherwise ("BluntOnly"); `UIArrowListModOption` only takes its own parameter types (`ModOptionInt` etc.), so no custom subclass. The menu saves the index, so a new title keeps the player's choice. |
| Kill, despawn | `EventManager.onCreatureKill` (OnEnd), `onCreatureDespawn` (OnStart) | Hands, height, Injured and the hold are undone; the brain is **not** woken: `Brain.Stop` (death, despawn; AchillesBAS read it) clears `isUnconscious` and stops the brain, and a pooled NPC gets a fresh brain (`Brain.Load`). The game's own stun does wake a killed NPC (`WaitConditionAction` ends when it is killed, then `EndStun` toggles the brain on after the end clip); not copied, as it starts the modules of a dead body again. |

### Assumed - must be confirmed in the game

| Assumption | Why it matters | How it shows |
|---|---|---|
| `ragdoll.rootPart` is the pelvis of a human NPC | The groin zone sits in front of and below it | Show Zones; `2-groin.txt` hits `Torso section=Lower` + offset and expects a groin hit |
| The neck part's position is at the base of the neck, so +5 cm up and forward is the Adam's apple | Throat zone placement | Show Zones; `1-throat.txt` |
| The NPC's IK works while the brain is off, and moves the hands to the anchors | Hands on throat and groin | Screenshots in `1-throat`, `2-groin`, film in `3-combo` |
| A full-body dynamic clip would hide the animator's `Height`; so the groin and the combo play none | The bend and the fall | `2-groin` picture: bent over; if not, the Height route is wrong |
| `Height` 0.6 bends him over, 0.2 + `Injured` lays him down (AchillesBAS measured 0.2 + Injured as its sitting/lying crawl pose) | Groin crouch, Curl Up | Pictures; Debug Logging |
| The hips' wardrobe channel | Not used for the groin armor (if it were the torso's, a chest plate would shield the groin) | `SKIP`/`LOW BLOW` lines print `hips: <channel> [...]` with Debug Logging |
| `impactVelocity` is the closing speed of a punch | Min Hit Speed default 1.5 m/s | Real punches in the headset with Debug Logging (`speed x.x m/s`) |

## How a hit is decided

Pure logic in `src/ThroatBalls/Logic/LowBlowClassifier.cs`, unit-tested. Checks in this order; the first failing one is the reason in the log (`SKIP | <npc> | <reason> | ...`):

1. `SelfHit` - not logged.
2. `NotPlayerVsNpc` - attacker must be the player, the target an NPC.
3. `TargetDead`
4. `NotMale` - with Men Only.
5. `HitNotAllowed` - Allowed Hits: Blunt Only (damage type Blunt), Fists And Kicks Only (the player's own hand or foot), Any Hit.
6. `TooSlow` - `impactVelocity` below Min Hit Speed.
7. `OutsideZones` - the contact point is in neither sphere. Throat center = neck + Throat Forward Offset along the NPC's horizontal facing + Throat Height Offset up; groin center = hips + Groin Forward Offset + Groin Height Offset. The nearest zone is taken.
8. `ZoneTurnedOff` - Throat Hits or Groin Hits off.
9. `MetalArmor` - with Metal Armor Protects, metal on that spot.
10. `AttackAngleUndetermined` / `NotFromTheFront` - the attacker's horizontal angle from the NPC's facing above Max Hit Angle (180 = no check).

## How a reaction runs

`ReactionRules` (pure, unit-tested), one state per NPC:

Checked in this order for every counted hit. Each chance is a roll in [0, 1) (`System.Random.NextDouble`, as `UnityEngine.Random.value` can return 1) that hits below the chance, so 100 % always and 0 % never; the rolls are inputs of the pure logic, so every branch is unit-tested.

| Situation | Action |
|---|---|
| Any hit while down | Ignored |
| The other spot within Time Between Hits of the first (also after the first reaction ended), in an allowed Hit Order, Both Hits Knockdown Chance hits | Down for Down Duration (`Down start (both hits ...)`) |
| Otherwise: Throat or Groin Reaction Chance misses | Shrugged off; the hit still counts as the first of both hits |
| Groin hit and Groin Knockdown Chance hits (every groin hit that gets to him rolls, also while doubled over) | Down for Down Duration (`Down start (groin ...)`) |
| Same spot while it still reacts | Extend: lasts at least its duration from now |
| Otherwise | Start that spot's reaction for its duration (replaces the other spot's) |
| Time up, or the mod turned off | End: everything given back |

`PoseRules` (pure, unit-tested): the animator height over a reaction (bend to Groin Crouch Height in 0.3 s; the combo then down to Lying Height over Fall Time; up over Get Up Time at the end; the higher of the two curves, so short reactions never drop too far), the ragdoll moment (end of bend + fall), where the hands go, when weapons drop, the voice lines.

`ReactionController` + `ReactionBody` (game side): Begin = voice, drop weapons, brain off, stun clip (throat only), IK hands. Each frame = height, Injured (Curl Up), ragdoll fall + hold (Ragdoll), no walking, groans. End = hands, height, Injured, hold back; brain on (after the stun clip's end for the throat). Death, despawn, unload: undo at once. Debug Logging: `LOW BLOW`, `SKIP`, `REACTION | <npc> | Throat start / end (time up)`, `HANDS`, `SOUND`.

## Performance

Per hit: one classification (a few vector operations, three wardrobe lookups). Per frame: nothing while nobody reacts; else one small state step per reacting NPC (no allocation: settings objects are kept and refilled). Show Zones (debug, off by default) walks the active creatures every frame. Not measured in the game.

## Harness

Scenarios (Linux, `tools/harness.sh`; one fresh prefix per run, files run in name order, smoke first):

| File | Checks |
|---|---|
| `0-smoke.txt` | load lines, 35 options with categories and defaults (chances 100/100/50/100 %, Allowed Hits by its title), own sounds loaded (1 throat, 1 groin, the broken file skipped), a live toggle, no exceptions |
| `1-throat.txt` | throat punch: `LOW BLOW`, Throat start, the own throat file plays (not `grappled`), picture of the hands, end after 3 s, brain back without a tree (HumanDummy); chest punch `OutsideZones`; slash `HitNotAllowed`; Own Sounds off: `grappled` again |
| `2-groin.txt` | groin punch at Groin Knockdown Chance 0 %: Groin start, the own groin file, picture bent over, the repeat is the game's `heavybreath`, end after 4 s; at 100 %: Down start (groin), the own groin file, picture down, end |
| `3-combo.txt` | throat then groin: Down start (both hits), film + picture down, a hit while down ignored, end, alive, no tree after the wake; Ragdoll: Destabilized, held, then up, no tree |
| `4-refusals.txt` | woman refused, then reacts with Men Only off; second hit after the window starts its own reaction; mod off mid-reaction ends it |
| `5-show-zones.txt` | Show Zones with a real physics hit (`player-stab` into a downed NPC, refused as Pierce): no "Destroying components immediately" error, nothing bumps into `ThroatBalls.Hit` |

`tools/harness.sh` runs a copy of the mod (`artifacts/harness-mod/ThroatBalls`) with the test sounds of `harness/fixtures/Sounds` in its `Sounds` folder: `throat-test.wav` (16-bit mono tone), `groin-test.wav` (32-bit float stereo tone), `groin-broken.wav` (not a WAV). The release ships only `Sounds/README.txt`; `tools/deploy.sh` copies that README and never touches the player's own files.

Every scenario but the smoke pins the four chances (reactions 100 %, groin knockdown 0 % unless the case tests it, both hits 100 %), so no run depends on a roll.

`hit-npc` raises the hit through `Creature.Damage` (no physics contact, so it runs outside the contact callback; `5-show-zones.txt` uses a real stab for that reason); the contact point is the part's position moved with `at=` (BasHarness 1f678cf, added for this mod).

## Build

```sh
dotnet test                                                  # unit tests (no game needed)
dotnet build src/ThroatBalls -c Release                      # mod in dist/ThroatBalls
dotnet build src/ThroatBalls -c Release -p:DeployToGame=true # copy into the game's Mods folder
tools/harness.sh                                             # headless scenarios via BasHarness (Linux)
tools/deploy.sh                                              # both installs (Linux)
# other install path:  -p:BasDir="/path/to/Blade & Sorcery"
```

Log: `Player.log` - all lines start with `[ThroatBalls]`.
