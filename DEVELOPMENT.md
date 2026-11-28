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
| 3c. Game sounds, Throat Sound (v0.4.0, Nadi 2026-11-24) | Nadi listened to the game's male voice clips (read out of `audio_assets_all.bundle`) and picked: throat = the four clips of `Bas.AudioGroup.Voice.Male.BreathlessDeath`, Throat Sound Random (default) or Sound 1 to 4; groin start = `male2_get_hit_07` (MaleCottage2.Hit) or `Male_Hurt_10_Edwyn` (MaleEdwyn.Hit), taking turns. Order: own file (Own Sounds on and files there), then the game clip, then the voice's own dialog (`breathlessdeath`, `hit`) if the clips did not load (`Logic/GameSounds.SourceFor`). 36 new unit tests (`GameSoundsTests`, `OptionTitlesTests`), red first (did not compile, `artifacts/tests/0.4.0-red.log`), 251/251 green. Harness 6/6 (`artifacts/harness/20261124-132325`): `Game sounds: 4 throat, 2 groin` at load (all six clips found by name); Own Sounds off: `game Throat 'Male_Shout-of-Pain_087'`; Throat Sound 2: `Male_Shout-of-Pain_054` at the start and on every repeat; groin: `game Groin 'male2_get_hit_07' (0.2 s)`, repeats `heavybreath`. The groin check that the voice's `hit` did not play was first written as `| HumanMale | hit`, which cannot match (the label carries `#id`); corrected to `| hit` and replayed against that run's log (0 game lines after the mark), not re-run in the game. Not heard: the headless runs have no audio output. |
| 3d. Throat Zone Size 9 cm (v0.4.1, Nadi 2026-11-24: "go für 9cm") | In the headset session of 2026-11-23 two fist hits missed the 7 cm throat zone at 7.2 and 7.7 cm; the default is now 9 cm (still 3 to 15 cm). A saved menu value stays as the player set it. Unit test red first (expected 0.09, got 0.07), 252/252 green. Harness 6/6 (`artifacts/harness/20261124-212127`): smoke checks the 9 cm default; the chest punch that must be refused lands 0.326 m from the throat zone, still `OutsideZones`. |
| 3e. Punch with the choke (v0.5.0, Nadi 2026-11-25) | "die zusammen mit rand(der h2h punch sounds aus dem game) zusammenlegen": the first choke of a throat reaction comes with one of the eight clips of `Bas.AudioGroup.Collision.Punch` (`SFX_punch_intens1_01`-`04`) and `.PunchHeavy` (`SFX_punch_intens2_01`-`04`), at random and never the same twice in a row (`GameSounds.Punch`, `GameSounds.PunchDue`), light below Heavy Punch Speed and heavy from it on (`GameSounds.HeavyPunch`, `GameSounds.PunchChoices`; Nadi: "leicht abspielen wenn der schlag auch leichter ist und schwer wenn er schwerer ist"; default 4.5 m/s = the median 4.3 of the seven counted headset hits of 2026-11-23, 2.3/2.6/3.2/4.3/4.5/6.6/10.2 m/s, on the 0.5 step; the impact speed is `CollisionInstance.impactVelocity.magnitude`, the same value as Min Hit Speed), on its own head `AudioSource` through the Effect mixer at 0.6 (`SoundBank.PlayPunch`). Not on the repeats, not for groin and down. Punch Sound option (on, Throat). Read in the catalog 2026-11-25: the game's `Effect_HitRagdoll` (used by the Flesh, Leather, Chainmail and Plate materials) already plays these two groups at the hit for the `Punch`, `Kick` and `KickTracked` damagers, volume by intensity (Punch 0 to 0.6 to 0, PunchHeavy 0.2 to 0.6), so this punch comes on top of the game's own. 10 new unit tests for the punch, red first (did not compile, `artifacts/tests/0.5.0-red.log`), then 12 for light and heavy, red first (`artifacts/tests/0.5.0-weight-red.log`); 279/279 green (`artifacts/tests/0.5.0-weight-green.log`). Harness 6/6 (`artifacts/harness/20261125-154446`): `Game sounds: 4 throat, 2 groin, 8 punch` at load; throat hits at 5 m/s: `game punch 'SFX_punch_intens2_04' (0.5 s) | heavy, hit 5.0 m/s`, then `2_03`, `2_02` (never the same twice); at 3 m/s: `SFX_punch_intens1_01 | light, hit 3.0 m/s`; the repeats (Groan Interval 1 s) without a punch; Punch Sound off: no punch. Not heard: the headless runs have no audio output, and the game's own hit sound plays at the same moment. |
| 3f. The choke ships with the mod (v0.5.0, Nadi 2026-11-25) | "throat: gefällt mir nur: Male choking, being strangled or dying vocal, gag. Version 9 By ZapSplat und zwar die ersten 2 ausschläge", "soll ins release". Nadi downloaded the MP3 (free account: 48 kHz mono, 5.08 s). Bursts measured on 5 ms peak windows (threshold 30 dB below the peak, 60 ms of quiet ends one): 0.015-0.140 s and 0.260-0.530 s, then quiet until 0.775 s; the same two as the waveform on the ZapSplat page. `Sounds/throat-choke.wav` = 0.005-0.600 s, 8 ms fade in, 60 ms fade out from 0.535 s, +1.1 dB to a peak of -2.0 dBFS, 16-bit mono 48 kHz, 0.595 s, -21.5 LUFS (the four breathless-death clips: peaks -2.1 to -2.5 dBFS, -20.0 to -22.3 LUFS). Built into `dist/ThroatBalls/Sounds`, copied by `tools/deploy.sh` (md5 checked); README credits ZapSplat. As an own file it takes turns with the player's `throat*.wav`; Own Sounds off: the game's clips. 3 new unit tests (`ShippedSoundsTests`: the file reads as mono 48 kHz, 0.58 to 0.61 s, peak -2.3 to -1.7 dBFS), red first without the file (`artifacts/tests/0.5.0-choke-red.log`), 265/265 green. Harness 6/6 (`artifacts/harness/20261125-154446`): `Own sounds: 2 throat, 1 groin` (the shipped choke and the harness's test tone), no skip of `throat-choke.wav`; in `1-throat` the repeats take turns and both files play: `own Throat 'throat-choke.wav' (0.6 s)`. Nadi listened to the cut and the eight punch mixes on a page (2026-11-25): "sound ist gut". |
| 3g. Films with sound (2026-11-26, Nadi: "will aber nun das als video mit sound sehen. einmal chop in throat und in balls", then every reaction) | `harness/films/sound.txt`, run with `tools/harness.sh --as-shipped` (the mod without the harness's test sounds): seven films on the same man, each with the game's sound (BasHarness `record-start ... audio=true`: the AudioListener's mix, taken in `OnAudioFilterRead` on its object `AudioListenerManager`, 48 kHz stereo): throat 5 m/s, throat 3 m/s, throat with Own Sounds off, groin, groin knockdown, throat then groin (Curl Up), groin then throat (Ragdoll). PASS (`artifacts/harness/20261126-115125/sound`). The first full run (`20261126-113941`) showed the sound drifting: the ragdoll film had 16.33 s of pictures and 15.23 s of sound, because the game's mixer stalled and handed over less sound than the time that passed. BasHarness 000f2f3 fills such a stretch with silence and names it; the rerun: every film's sound as long as its pictures (within 0.07 s), gaps filled 0.09-0.10 s each. In sync, measured: choke + punch within 0.03 s of the log line in all four throat films. Loudness, measured (first run, 20261126-100740): choke + heavy punch peak -4.9 dBFS, the groin grunt `male2_get_hit_07` -11.0 dBFS. So the headless runs do run the game's sound mix (rows 3c and 3e said "no audio output"; whether it reaches the speakers was not checked). The hits come through `Creature.Damage` without a physics contact, so the game's own hit sound is not in the films; in the game it comes on top. |
| 3h. New choke cut and Choke Repeats (v0.6.0, Nadi 2026-11-26) | "nicht nur die ersten 2 spitzen, sondern auch die letzten 3 ... das dazwischen rausgeschnitten", then "wir nehmen startlaut und fertig den wiederholen wir dann halt paar mal, erstmal 2x einstellbar". Bursts of ZapSplat Version 9 measured on 5 ms windows (30 dB below the peak, 60 ms quiet ends one): 0.015-0.140, 0.260-0.530, ... 3.585-3.935, 4.075-4.240, 4.360-4.415 s. `Sounds/throat-choke.wav` is now 0.005-0.600 s joined to 3.520-4.475 s (8 ms fade in, 60 ms fade out each), 1.55 s, peak -2 dBFS; about 0.13 s of quiet at the join, like the gaps between bursts 3, 4 and 5. Choke Repeats (Throat, 2, 0 to 5): after the first throat sound, Choke Repeats more, each 0.6 s after the one before ended (`PoseRules.ChokeDue`, `ChokePause`); the throat reaction lasts until the last has ended (`PoseRules.ChokesEnd`), with 2 repeats and the shipped file 5.85 s instead of Throat Duration's 3 s; the punch only with the first; Groan Interval paces only groin and down. Nadi listened first on a page with the cut and the sequences (`Würgen mit Nachwürgen`). Unit tests red first (13 compile errors, `artifacts/tests/0.6.0-red.log`), 289/289 green (`0.6.0-green.log`). Harness 6/6 (`artifacts/harness/20261126-131739`): smoke with 39 options and Choke Repeats 2; `1-throat` three chokes with the two own files taking turns, the reaction past 3 s, Choke Repeats 0 = one choke and the end after Throat Duration. |
| 3i. Advanced tab (Nadi 2026-11-26: "ein advanced sub menü ... wie es das schon gibt für die mod", "hierfür kein harness run") | Category "Advanced" (last, as in Heel of Achilles): Max Hit Angle, Heavy Punch Speed, Groan Interval, the throat and groin zone sizes and offsets, Groin Crouch Height, Lying Height, Fall Time, Get Up Time. Only the menu attributes moved; build clean, 289/289. Not run in the game, as Nadi asked; `0-smoke.txt` expects the new categories and parses (BasHarness parser, offline). |

The headless runs and one headset session (2026-11-23) are the in-game evidence so far. Every other "verified" below means read in the decompiled DLL and compiled against it.

## Bug: NPCs stood still after a reaction (fixed in 0.6.1)

- Seen: Windows run 2026-11-28 (`Player.log`). After a groin or throat reaction ended, NPCs did not turn, walk or attack.
  HumanMale8 (`#-289242`, groin down until 11:59:27): yaw stays 45.7 from 11:59:42 to 11:59:47 while the player walks from -7 to 44 deg (AchillesTendon watch lines).
  HumanMale6 (`#-291756`, throat until 11:57:58): `busy=True`, no turn requests, 11:58:09 to 11:58:20.
  No NPC swung a weapon after the first reaction at 11:57:37 (DMCS logs every swing, last at 11:57:00).
- Cause (read in ThunderRoad 1.3.2): `Creature.Damage` fires `OnDamageEvent(OnEnd)` before `EventManager.onCreatureHit(OnEnd)`.
  So `BrainModuleHitReaction.TryPush` has often started `StaggerCoroutine` before the mod reacts. That coroutine sets `brain.currentStagger`, the `Hit` trigger and `SetAnimatorBusy(true)` by hand.
  The mod's `BrainToggle(false)` then runs `StopStagger`: it stops the coroutine and resets `Hit`, so the hit animation never plays. Without the toggle flag, it also puts `currentStagger` back.
  `IsBusy` is normally cleared by `ThunderRoad.Utilities.BusyState` when a busy state exits. No busy state ran, so it stayed true: `MoveTo`, `TurnTo`, `AttackMelee`, `DrawItem` and `PickupItem` fail. The stuck `currentStagger` turns off `canDodge` and `canBlock`.
  The game's own stun (`SkillDiscombobulate`) works on `OnStart`, before the stagger starts, and so avoids this.
- Also: `BrainToggle(true)` calls `OnBrainStart` on every module, but `BrainToggle(false)` without `toggleHitReaction` skips `OnBrainStop` on the hit reaction. Each reaction added one more `OnDamage` subscription, so every later hit was pushed twice, then three times.
- Fix (`ReactionBody.KnockOut`, `Logic/KnockOutRules.cs`): `BrainToggle(false, toggleHitReaction: true)` stops the hit reaction with the brain. That clears `currentStagger` and its subscriptions, and waking subscribes them once. After the toggle, `IsBusy` is set by the game's own rule, `BusyState.animatorBusyStates[animator] > 0`. This logs `BRAIN | ... | busy flag True -> False`.
- Side effect: while he reacts, later hits no longer play the game's stagger on top of the pose (the hit reaction is off with the brain).
- Not verified in the game yet: run the harness on Linux, or check in the headset.
- Open risk, not seen in this run: `WakeNow` puts back `updateTree` as it was at the hit. DMCS (kneel, back off) and HJ_Threat (panic) pause the tree for a while. If their release falls inside a reaction, the tree stays paused.

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
| Voice | `BrainModuleSpeak.Play(dialogId, abnormalNoise)` | Refuses while `brain.isUnconscious` (read once at its start); this mod lifts the flag around the call when it set it. Dialog ids `breathlessdeath`, `hit`, `heavybreath` are in all eight `HumanMale*` voices of the catalog. Used for the groin repeats, and for the throat and the groin start only when the game clips did not load. |
| Drop weapons | `RagdollHand.TryRelease()` | |
| Own sounds | `ModManager.ModData.fullPath` (the mod folder), `AudioClip.Create` + `SetData`, an `AudioSource` on `ragdoll.headPart` routed to `ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Voice)` | WAV decoded in pure code (`Logic/WavReader`), no web request. The male voices of the catalog (`Voices/Voice_HumanMale*.json`, read 2026-11-24) have the dialogs attackbow, attackcast, attackmelee, breathlessdeath, death, deathlong, enemyspotted, falling, grappled, heardnoise, heavybreath, hit, jump, muffled, muffleddeath: no "ow", and the game's `choke` dialog (`BrainModuleSpeak.speakOnChoke`) is in none of them. |
| Game clips | `Catalog.LoadAssetAsync<ScriptableObject>(address, callback, handlerName)`; `AudioContainer.sounds`; `AdvancedAudioContainer.audioContainer` | Loaded as `VoiceData.Dialog.LoadAddressableAssetsCoroutine` loads a dialog's group (a `ScriptableObject` that is an `AudioContainer` or an `AdvancedAudioContainer` around one); clips found by `AudioClip.name`, played on the same head `AudioSource` as the own files. Addresses from `Voices/Voice_HumanMale*.json` (breathlessdeath = `Bas.AudioGroup.Voice.Male.BreathlessDeath` in all eight; hit = `Bas.AudioGroup.Voice.MaleCottage2.Hit`, `Bas.AudioGroup.Voice.MaleEdwyn.Hit`). Clip names read from `StreamingAssets/Default/audio_assets_all.bundle` with UnityPy 1.25.4 (2026-11-24, Unity 2021.3.38f1); `catalog_bas.json` lists the groups as `.../AudioContainers/Voices/Male/VoiceMale_BreathlessDeath.asset`, `.../MaleCottage2/VoiceMale_CottageMale2_Hit.asset`, `.../MaleEdwyn/VoiceMale_Edwyn_Hit.asset`. Loaded in `ScriptLoaded`: `ModManager.LoadCoroutine` loads the addressable assets before `LoadModThunderScripts`, the catalog refresh comes after. |
| Punch with the choke | `AudioSource.PlayOneShot(clip, volume)` on a second head source routed to `ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect)` | The game's own collision sounds play on the Effect mixer (`EffectModuleAudio.audioMixer` = `Effect` in `Effect_HitRagdoll`). Groups `Bas.AudioGroup.Collision.Punch` and `.PunchHeavy`: keys in `catalog_bas.json` (decoded `m_KeyDataString`), assets `.../AudioContainers/Collisions/Punch.asset` and `PunchHeavy.asset`, four clips each, 0.5 s (UnityPy, 2026-11-25). `Collision.PunchStrong` (`Punch1`-`5`) exists but no effect in the catalog uses it. |
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
| `0-smoke.txt` | load lines, 36 options with categories and defaults (chances 100/100/50/100 %, Allowed Hits by its title, Throat Sound Random), own sounds loaded (1 throat, 1 groin, the broken file skipped), game clips found (4 throat, 2 groin), a live toggle, no exceptions |
| `1-throat.txt` | throat punch: `LOW BLOW`, Throat start, the own throat file plays (not a game clip), picture of the hands, end after 3 s, brain back without a tree (HumanDummy); chest punch `OutsideZones`; slash `HitNotAllowed`; Own Sounds off: a game throat clip (not the voice's line); Throat Sound 2: only `Male_Shout-of-Pain_054`, repeats included |
| `2-groin.txt` | groin punch at Groin Knockdown Chance 0 %: Groin start, the own groin file, picture bent over, the repeat is the game's `heavybreath`, end after 4 s; at 100 %: Down start (groin), the own groin file, picture down, end; Own Sounds off: a game groin clip (not the voice's `hit`), repeats `heavybreath` |
| `3-combo.txt` | throat then groin: Down start (both hits), film + picture down, a hit while down ignored, end, alive, no tree after the wake; Ragdoll: Destabilized, held, then up, no tree |
| `4-refusals.txt` | woman refused, then reacts with Men Only off; second hit after the window starts its own reaction; mod off mid-reaction ends it |
| `5-show-zones.txt` | Show Zones with a real physics hit (`player-stab` into a downed NPC, refused as Pierce): no "Destroying components immediately" error, nothing bumps into `ThroatBalls.Hit` |

`tools/harness.sh` runs a copy of the mod (`artifacts/harness-mod/ThroatBalls`) with the test sounds of `harness/fixtures/Sounds` added to its `Sounds` folder (`--as-shipped`: without them, for `harness/films/sound.txt`, the films with sound to listen to; `harness/films` is not part of the regression runs): `throat-test.wav` (16-bit mono tone), `groin-test.wav` (32-bit float stereo tone), `groin-broken.wav` (not a WAV), next to the shipped `throat-choke.wav`. The release ships `Sounds/README.txt` and `Sounds/throat-choke.wav`; `tools/deploy.sh` copies those two and never touches the player's other files.

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
