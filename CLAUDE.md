# Throat Balls (Blade & Sorcery)

Implementation spec for this repo. Standalone mod, independent of DMCS, Heel of Achilles (AchillesBAS) and Slam Disarm (SlamDisarmBAS).

Blade & Sorcery (VR) mod built against the game's own `ThunderRoad.dll` (Unity + C#, `ThunderRoad` namespace), same setup as AchillesBAS.

> **Important for the implementer:** API names below (`EventManager.onCreatureHit`, `SkillDiscombobulate.BrainToggle`, `IkController.SetHandAnchor`, `ItemModuleWardrobe.isMetal`, etc.) were read in the decompiled `ThunderRoad.dll` 1.3.2 (`DEVELOPMENT.md`, "API check"). **Verify every API against the current game version before relying on it.** If an API differs, adapt — don't invent.

---

## The mechanic (Nadi, 2026-11-22)

"throat balls: ich kann gegner aber nur bei männer wenn dort keine rüstung ist (keine stahl usw, aber leder ok) in den adams apfel oder die eier hauen. eins von beidem oder beides nacheinander. dann entsprechende animation. bei beidem nacheinander halten sie sich die eier und fallen zu boden. alles einstellbar in mod settings."

**Trigger**
- The player hits an NPC on the **throat** (Adam's apple, a zone in front of the neck) or the **groin** (a zone in front of and below the hips).
- Only **men** (`CreatureData.gender == Male`; Men Only, default on).
- Only where no **metal armor** is worn (Metal Armor Protects, default on): the game's own `ItemModuleWardrobe.isMetal` of what is worn on the neck (throat) or the legs channel (groin). Leather and cloth do not protect.
- **Allowed Hits** (default "Fists, Kicks, Blunt Weapons", enum `BluntOnly`: fists and kicks are Blunt damage in the game, plus maces, pommels; or Fists And Kicks Only; or "Any Hit, Blades Too"; menu titles from `Logic/OptionTitles`, renamed 2026-11-24 because "Blunt Only" read as if fists did not count), **Min Hit Speed** (1.5 m/s), from the front (**Max Hit Angle** 100°). Player vs NPC only; only hits that do not kill.

**Effect**
- **Throat**: hands to the throat, the game's stun sway, gagging, out of the fight for **Throat Duration** (3 s).
- **Groin**: hands to the groin, bent over (**Groin Crouch Height**), groaning, out of the fight for **Groin Duration** (4 s).
- **Both one after the other** within **Time Between Hits** (4 s, counted from the first hit, also after its reaction ended; **Hit Order** either, or a fixed order): hands to the groin, down to the ground for **Down Duration** (8 s), then up again. **Fall Style**: Curl Up (animated: the game's injured lying pose, hands kept at the groin) or Ragdoll (the game's knock-down, gets up by itself). Hits while down change nothing.
- **Groin alone can take him down too** (Nadi, 2026-11-22: "in die eier schlagen/treten alleine kann männer auch zu boden gehen lassen. 50% chance. throat balls 100%. chance einstellbar für alles"): **Groin Knockdown Chance** (50 %), rolled on every groin hit that gets to him; **Both Hits Knockdown Chance** (100 %); **Throat Reaction Chance** and **Groin Reaction Chance** (100 %) gate the single reactions. A hit that is shrugged off still counts as the first of both hits; a missed both-hits roll gives the second hit its own reaction. Going down from the groin alone looks the same as from both hits (Down Duration, Fall Style).
- The same spot again while it reacts makes it last longer. **Drop Weapons**: when they go down (default), always, never; a hand that still holds something stays in the animation.
- Voice (Nadi, 2026-11-24, picked by ear from the game's male voice clips: "rand(BreathlessDeath1-4)) oder geben dem player einen setting auf eins der 4 von BreathlessDeath. balls: rand von CottageMale2 7, Edwyn 10"): throat: the four clips of the game's breathless death (`Bas.AudioGroup.Voice.Male.BreathlessDeath`), **Throat Sound** Random (default, never the same twice in a row) or Sound 1 to 4; groin start (Groin and Down): the 7th hit clip of the cottage man 2's voice (`male2_get_hit_07`) or the 10th of Edwyn's (`Male_Hurt_10_Edwyn`), taking turns, for every man; groin repeats: the voice's `heavybreath`. Repeated every **Groan Interval**. The numbers are the clips' places in their group by name (`Logic/GameSounds`). If the game clips do not load: the voice's own dialogs `breathlessdeath` (throat) and `hit` (groin).
- Punch with the choke (Nadi, 2026-11-25: "die zusammen mit rand(der h2h punch sounds aus dem game) zusammenlegen"): the first choke of a throat reaction comes with one of the game's own fist hits, at random and never the same twice in a row: the clips of `Bas.AudioGroup.Collision.Punch` (four light) and `.PunchHeavy` (four heavy), what the game's HitRagdoll effect plays for fists and kicks, on the game's Effect mixer at 0.6 (the top of the game's own volume curve). Light or heavy by the hit (Nadi, 2026-11-25: "leicht abspielen wenn der schlag auch leichter ist und schwer wenn er schwerer ist"): from **Heavy Punch Speed** (4.5 m/s, the median of the headset hits of 2026-11-23) on a heavy one, below it a light one. Not on the repeats. **Punch Sound** (on) under Throat. The game still plays its own hit sound at the hit; this one comes on top.
- **Own Sounds** (Nadi, 2026-11-24: "choke bei throat und Owww bei eier schlag", own files chosen over the game's voices, which have neither): WAV files in the mod's `Sounds` folder, `throat*.wav` (start and every repeat) and `groin*.wav` (once at the start of Groin and Down; repeats stay `heavybreath`). No files or the option off: the game sounds as above. The release ships one: `Sounds/throat-choke.wav` (Nadi, 2026-11-25: "throat: gefällt mir nur: Male choking, being strangled or dying vocal, gag. Version 9 By ZapSplat und zwar die ersten 2 ausschläge", "soll ins release"), the first two bursts of that ZapSplat sound (0.005 to 0.600 s), 16-bit mono 48 kHz, peak -2 dBFS like the game's breathless-death clips; credit "ZapSplat" in README (free ZapSplat account: credit required). It takes turns with any `throat*.wav` the player adds. The shipped file changed 2026-11-26 (Nadi: "nicht nur die ersten 2 spitzen, sondern auch die letzten 3 ... das dazwischen rausgeschnitten"): 0.005-0.600 s and 3.520-4.475 s of Version 9 joined, 1.55 s, peak -2 dBFS.
- **Choke Repeats** (Nadi, 2026-11-26: "den wiederholen wir dann halt paar mal, erstmal 2x einstellbar"): after the first throat sound, a throat sound (by the same rules as the first: own files taking turns, Throat Sound, voice line) plays Choke Repeats more times (2, 0 to 5), each 0.6 s after the one before ended (`PoseRules.ChokePause`); the punch only with the first. The throat reaction lasts until the last one has ended, even past Throat Duration (`PoseRules.ChokesEnd`). Groan Interval now only paces the groin and down repeats.

**Animations: the game's own, no new assets** (decided here, cheapest route; own clips through the Unity SDK stay the route if these do not read right in the headset). Hands through the NPC's IK hand targets, as the game's bow and defense AI place hands; the bend and the fall through the animator's `Height` and `Injured` floats, as the game's burning NPC drops (and as Heel of Achilles' crawl does); the throat sway through the game's stun clip `HumanStun`.

## Mod Menu (required)

- Every option in the Mod Menu: categories "Throat Balls", "Throat", "Groin", "Going Down", "Debug", "Advanced" (Nadi, 2026-11-26: "ein advanced sub menü ... wie es das schon gibt für die mod": the fine tuning, as Heel of Achilles has it: Max Hit Angle, Heavy Punch Speed, Groan Interval, the six zone sizes and offsets, Groin Crouch Height, Lying Height, Fall Time, Get Up Time). No magic numbers in code (the 0.3 s bend is a reflex constant in `PoseRules`, the hand placement offsets too).
- Option names unique per mod; an enum default must be the first entry; float value lists from `Logic/OptionSpecs` (decimal builder).
- Live: a changed value applies at once. Turning the mod off mid-reaction ends every reaction at once and gives everything back.
- Debug: Debug Logging (every judged hit with its reason, every reaction start/end, armor worn at the spot) and Show Zones (the two zones as colored spheres, hit markers).

## Code rules

- Decisions in pure code under `src/ThroatBalls/Logic/` (no Unity, no game types), unit-tested for every flow incl. refusal paths, test first. Game glue outside `Logic/` stays thin.
- Per frame only the NPCs that react or wait for a second hit; no allocation per frame.
- New functions ≤ 100 lines, files ≤ 1000 lines.
- `README.md` is for players only. Everything technical goes in `DEVELOPMENT.md`.

## Build order

1. Project setup, pure logic with unit tests, game glue, Mod Menu, harness scenarios. **Done 2026-11-22** (see `DEVELOPMENT.md`, "Status").
2. Headless runs on Linux (`tools/harness.sh`): smoke, throat, groin, both hits, refusals. **Done 2026-11-22**, 5/5 pass (one fix: the brain's tree setting is given back after the stun).
3. Headset check: zone placement (Show Zones), how the hands, the bend and the fall look, real punches and kicks. **Started 2026-11-23**: real punches count; hit markers kept a collider (fixed v0.2.2, `5-show-zones.txt`). Owed: both hits in a row, getting up, the look.

**Deploy: always both installs.** `tools/deploy.sh` (Linux) builds Release, refuses uncommitted `src` changes, copies DLL + manifest into the Linux and the Windows Mods folder and checks the md5.
