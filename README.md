# Throat Balls

A Blade & Sorcery mod. Punch or kick a man in the throat or between the legs and he feels it.

## How it works

- **Throat:** hit him on the Adam's apple. He grabs his throat and gags for a few seconds.
- **Groin:** hit him between the legs. He doubles over with his hands on his groin.
- **Both, one after the other:** he grabs his groin and goes down. After a while he gets back up.
- Only men react, and only where no metal armor covers the spot. Leather and cloth don't protect.
- By default only blunt hits count: fists, kicks, maces, pommels.
- While he reacts, he doesn't fight back.

## Install

Copy the `ThroatBalls` folder into
`Blade & Sorcery/BladeAndSorcery_Data/StreamingAssets/Mods`, or install it with your mod manager.

## Mod Menu

Open the book, go to **Mods**, pick **Throat Balls**. Changes apply at once.

**Throat Balls**

| Setting | Default | Range | What it does |
|---|---|---|---|
| Throat Balls Enabled | on | | Turns the mod on or off |
| Men Only | on | | Off: women react too |
| Metal Armor Protects | on | | Metal armor on the spot stops the hit |
| Allowed Hits | Blunt Only | Blunt Only, Fists And Kicks Only, Any Hit | What can land the blow |
| Min Hit Speed | 1.5 m/s | 0–6 m/s | How hard you must hit |
| Max Hit Angle | 100° | 30–180° | How far to his side you may stand. 180 = even from behind |
| Drop Weapons | When They Go Down | When They Go Down, Always, Never | When he lets his weapons fall |
| Sounds | on | | He gags, groans and gasps |
| Groan Interval | 3 s | 0–10 s | How often. 0 = only once |

**Throat**

| Setting | Default | Range | What it does |
|---|---|---|---|
| Throat Hits | on | | Throat hits count |
| Throat Duration | 3 s | 1–15 s | How long he holds his throat |
| Throat Zone Size | 7 cm | 3–15 cm | How big the throat spot is |
| Throat Forward Offset | 5 cm | -5–15 cm | Moves the spot forward or back |
| Throat Height Offset | 5 cm | -10–15 cm | Moves the spot up or down |

**Groin**

| Setting | Default | Range | What it does |
|---|---|---|---|
| Groin Hits | on | | Groin hits count |
| Groin Duration | 4 s | 1–15 s | How long he stays doubled over |
| Groin Crouch Height | x0.6 | x0.3–x1 | How far he bends down |
| Groin Zone Size | 9 cm | 3–20 cm | How big the groin spot is |
| Groin Forward Offset | 8 cm | -5–20 cm | Moves the spot forward or back |
| Groin Height Offset | -8 cm | -25–10 cm | Moves the spot up or down |

**Both Hits**

| Setting | Default | Range | What it does |
|---|---|---|---|
| Both Hits Take Him Down | on | | Throat and groin in a row put him on the ground |
| Hit Order | Either Order | Either Order, Throat Then Groin, Groin Then Throat | Which hit must come first |
| Time Between Hits | 4 s | 1–10 s | How soon the second hit must follow |
| Down Duration | 8 s | 2–30 s | How long he stays down |
| Fall Style | Curl Up | Curl Up, Ragdoll | Curl Up: he sinks down holding his groin. Ragdoll: he drops limp |
| Lying Height | x0.2 | x0–x0.6 | Curl Up: how low he lies |
| Fall Time | 1 s | 0.2–3 s | How long he takes to go down |
| Get Up Time | 1.5 s | 0.5–4 s | How long he takes to get up |

**Debug**

| Setting | Default | What it does |
|---|---|---|
| Debug Logging | off | Writes every hit to the game log, for bug reports |
| Show Zones | off | Shows the throat and groin spots as colored balls, to adjust them |

## Works with

- Mods that change how enemies react to wounds (for example Realistic Deaths) may react to the same hit. Not tested together yet.

## Known limits

- Not tested in the game yet. The spots, the bend and the fall are first guesses: turn on Show Zones to see the spots.
