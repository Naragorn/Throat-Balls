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
- **Allowed Hits** (default Blunt Only: fists, kicks, maces, pommels; or Fists And Kicks Only; or Any Hit), **Min Hit Speed** (1.5 m/s), from the front (**Max Hit Angle** 100°). Player vs NPC only; only hits that do not kill.

**Effect**
- **Throat**: hands to the throat, the game's stun sway, gagging, out of the fight for **Throat Duration** (3 s).
- **Groin**: hands to the groin, bent over (**Groin Crouch Height**), groaning, out of the fight for **Groin Duration** (4 s).
- **Both one after the other** within **Time Between Hits** (4 s, counted from the first hit, also after its reaction ended; **Hit Order** either, or a fixed order): hands to the groin, down to the ground for **Down Duration** (8 s), then up again. **Fall Style**: Curl Up (animated: the game's injured lying pose, hands kept at the groin) or Ragdoll (the game's knock-down, gets up by itself). Hits while down change nothing.
- **Groin alone can take him down too** (Nadi, 2026-11-22: "in die eier schlagen/treten alleine kann männer auch zu boden gehen lassen. 50% chance. throat balls 100%. chance einstellbar für alles"): **Groin Knockdown Chance** (50 %), rolled on every groin hit that gets to him; **Both Hits Knockdown Chance** (100 %); **Throat Reaction Chance** and **Groin Reaction Chance** (100 %) gate the single reactions. A hit that is shrugged off still counts as the first of both hits; a missed both-hits roll gives the second hit its own reaction. Going down from the groin alone looks the same as from both hits (Down Duration, Fall Style).
- The same spot again while it reacts makes it last longer. **Drop Weapons**: when they go down (default), always, never; a hand that still holds something stays in the animation.
- Voice: the game's own dialogs of the NPC's voice (`grappled` for the throat, `hit` then `heavybreath` for the groin), repeated every **Groan Interval**.

**Animations: the game's own, no new assets** (decided here, cheapest route; own clips through the Unity SDK stay the route if these do not read right in the headset). Hands through the NPC's IK hand targets, as the game's bow and defense AI place hands; the bend and the fall through the animator's `Height` and `Injured` floats, as the game's burning NPC drops (and as Heel of Achilles' crawl does); the throat sway through the game's stun clip `HumanStun`.

## Mod Menu (required)

- Every option in the Mod Menu: categories "Throat Balls", "Throat", "Groin", "Going Down", "Debug". No magic numbers in code (the 0.3 s bend is a reflex constant in `PoseRules`, the hand placement offsets too).
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
