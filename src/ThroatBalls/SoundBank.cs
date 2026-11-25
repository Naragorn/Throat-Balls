using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ThroatBalls.Logic;
using ThunderRoad;
using UnityEngine;

namespace ThroatBalls
{
    /// <summary>
    /// The sounds the mod plays itself, from the NPC's head through the game's Voice mixer (so the game's voice volume
    /// applies): the player's own files from the mod's Sounds folder (<see cref="OwnSounds"/>, read once when the mod
    /// loads) and the game clips picked for the throat and the groin (<see cref="GameSounds"/>, their audio groups
    /// loaded once by address, as the game's voices load them). Which of them plays: <see cref="GameSounds.SourceFor"/>.
    /// The punch with the first choke (<see cref="PlayPunch"/>) goes through the Effect mixer, as the game's own hits do.
    /// </summary>
    internal sealed class SoundBank
    {
        private const float MinDistance = 1f;
        private const float MaxDistance = 20f;
        private const string HandlerName = "ThroatBalls";

        private readonly Dictionary<SoundSlot, List<AudioClip>> own = new Dictionary<SoundSlot, List<AudioClip>>
        {
            [SoundSlot.Throat] = new List<AudioClip>(),
            [SoundSlot.Groin] = new List<AudioClip>(),
        };

        private readonly Dictionary<SoundSlot, AudioClip[]> game = new Dictionary<SoundSlot, AudioClip[]>
        {
            [SoundSlot.Throat] = new AudioClip[GameSounds.Throat.Count],
            [SoundSlot.Groin] = new AudioClip[GameSounds.Groin.Count],
        };

        private readonly Dictionary<SoundSlot, bool[]> gameLoaded = new Dictionary<SoundSlot, bool[]>
        {
            [SoundSlot.Throat] = new bool[GameSounds.Throat.Count],
            [SoundSlot.Groin] = new bool[GameSounds.Groin.Count],
        };

        private readonly Dictionary<SoundSlot, int> lastOwn = new Dictionary<SoundSlot, int>
        {
            [SoundSlot.Throat] = -1,
            [SoundSlot.Groin] = -1,
        };

        private readonly Dictionary<SoundSlot, int> lastGame = new Dictionary<SoundSlot, int>
        {
            [SoundSlot.Throat] = -1,
            [SoundSlot.Groin] = -1,
        };

        private readonly AudioClip[] punch = new AudioClip[GameSounds.Punch.Count];
        private readonly bool[] punchLoaded = new bool[GameSounds.Punch.Count];
        private int lastPunch = -1;

        private readonly System.Random dice = new System.Random();
        private int groupsPending;

        public void Load(string modFolder)
        {
            var folder = Path.Combine(modFolder ?? string.Empty, OwnSounds.FolderName);
            if (!Directory.Exists(folder))
            {
                ModLog.Info("Own sounds: no " + OwnSounds.FolderName + " folder");
                return;
            }
            var files = Directory.GetFiles(folder);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (var path in files)
            {
                var name = Path.GetFileName(path);
                var slot = OwnSounds.Classify(name);
                if (slot == SoundSlot.None)
                {
                    continue;
                }
                if (!WavReader.TryRead(File.ReadAllBytes(path), out var wav, out var error))
                {
                    ModLog.Warn("Own sounds: skipped " + name + ": " + error);
                    continue;
                }
                var clip = AudioClip.Create(name, wav.Samples.Length / wav.Channels, wav.Channels, wav.SampleRate, false);
                clip.SetData(wav.Samples, 0);
                own[slot].Add(clip);
            }
            ModLog.Info("Own sounds: " + Number(own[SoundSlot.Throat].Count) + " throat, " + Number(own[SoundSlot.Groin].Count) + " groin (" + folder + ")");
        }

        /// <summary>Starts loading the audio groups of the game clips; once all are in, the log says how many were found.</summary>
        public void LoadGameClips()
        {
            var addresses = GameSounds.Addresses();
            groupsPending = addresses.Count;
            foreach (var address in addresses)
            {
                var requested = address;
                Catalog.LoadAssetAsync<ScriptableObject>(requested, group => OnGroupLoaded(requested, group), HandlerName);
            }
        }

        /// <summary>
        /// Plays the slot's sound from the NPC's head: an own file or a game clip. <see cref="SoundSource.Dialog"/>:
        /// nothing was played and the caller has the voice say its own line.
        /// </summary>
        public SoundSource Play(Reaction r, SoundSlot slot, bool ownOn, ThroatSound throatSound)
        {
            var source = GameSounds.SourceFor(slot, ownOn, OwnCount(slot), GameCount(slot));
            if (source == SoundSource.Dialog)
            {
                if (slot != SoundSlot.None)
                {
                    ModLog.Verbose("SOUND | " + GameAdapters.Label(r.Creature) + " | no own file and no game clip for " + slot);
                }
                return source;
            }
            var clip = source == SoundSource.Own ? PickOwn(slot) : PickGame(slot, throatSound);
            var player = clip != null ? SourceOf(r) : null;
            if (player == null)
            {
                ModLog.Verbose("SOUND | " + GameAdapters.Label(r.Creature) + " | " + (clip == null ? "no clip" : "no head to play from") + " for " + slot);
                return SoundSource.Dialog;
            }
            player.Stop();
            player.clip = clip;
            player.Play();
            ModLog.Verbose("SOUND | " + GameAdapters.Label(r.Creature) + " | " + (source == SoundSource.Own ? "own " : "game ") + slot
                + " '" + clip.name + "' (" + clip.length.ToString("0.0", CultureInfo.InvariantCulture) + " s)");
            return source;
        }

        /// <summary>
        /// One of the game's punch clips (<see cref="GameSounds.Punch"/>) of the hit's weight, never the same twice in a
        /// row, from the NPC's head through the game's Effect mixer (where the game's own hit sounds play), as loud as the
        /// game's own punch at its loudest. Its own audio source, so the choke on the voice source plays on at the same time.
        /// </summary>
        public void PlayPunch(Reaction r, bool heavy)
        {
            var index = GameSounds.Pick(GameSounds.PunchChoices(punchLoaded, heavy), -1, lastPunch, dice.NextDouble());
            var player = index >= 0 ? SourceOf(r, ref r.Punch, "ThroatBalls.Punch", AudioMixerName.Effect) : null;
            if (player == null)
            {
                ModLog.Verbose("SOUND | " + GameAdapters.Label(r.Creature) + " | " + (index < 0 ? "no punch clip" : "no head to play from") + " for the punch");
                return;
            }
            lastPunch = index;
            var clip = punch[index];
            player.PlayOneShot(clip, GameSounds.PunchVolume);
            ModLog.Verbose("SOUND | " + GameAdapters.Label(r.Creature) + " | game punch '" + clip.name + "' ("
                + clip.length.ToString("0.0", CultureInfo.InvariantCulture) + " s) | " + (heavy ? "heavy" : "light") + ", hit "
                + r.HitSpeed.ToString("0.0", CultureInfo.InvariantCulture) + " m/s");
        }

        /// <summary>Removes the reaction's audio sources (they end with the reaction; a sound still playing is cut).</summary>
        public static void Release(Reaction r)
        {
            if (r.Voice != null)
            {
                UnityEngine.Object.Destroy(r.Voice.gameObject);
            }
            r.Voice = null;
            if (r.Punch != null)
            {
                UnityEngine.Object.Destroy(r.Punch.gameObject);
            }
            r.Punch = null;
        }

        private void OnGroupLoaded(string address, ScriptableObject group)
        {
            // A voice's audio group is an AudioContainer or an AdvancedAudioContainer around one (VoiceData.Dialog).
            var container = group is AdvancedAudioContainer advanced ? advanced.audioContainer : group as AudioContainer;
            if (container == null || container.sounds == null)
            {
                ModLog.Warn("Game sounds: no audio group at " + address + (group == null ? string.Empty : " (" + group.GetType().Name + ")"));
            }
            else
            {
                Take(GameSounds.Throat, game[SoundSlot.Throat], gameLoaded[SoundSlot.Throat], address, container);
                Take(GameSounds.Groin, game[SoundSlot.Groin], gameLoaded[SoundSlot.Groin], address, container);
                Take(GameSounds.Punch, punch, punchLoaded, address, container);
            }
            groupsPending--;
            if (groupsPending == 0)
            {
                ModLog.Info("Game sounds: " + Number(GameCount(SoundSlot.Throat)) + " throat, " + Number(GameCount(SoundSlot.Groin)) + " groin, "
                    + Number(Count(punchLoaded)) + " punch");
            }
        }

        /// <summary>The wanted clips of this audio group, found by name; a missing one is named in the log with what the group has.</summary>
        private static void Take(IReadOnlyList<GameClip> wanted, AudioClip[] clips, bool[] loaded, string address, AudioContainer container)
        {
            for (var i = 0; i < wanted.Count; i++)
            {
                if (wanted[i].Address != address)
                {
                    continue;
                }
                var clip = container.sounds.Find(c => c != null && c.name == wanted[i].Clip);
                clips[i] = clip;
                loaded[i] = clip != null;
                if (clip == null)
                {
                    ModLog.Warn("Game sounds: " + wanted[i].Clip + " is not in " + container.name + " (" + address + "), it has: "
                        + string.Join(", ", container.sounds.ConvertAll(c => c != null ? c.name : "null")));
                }
            }
        }

        private AudioClip PickOwn(SoundSlot slot)
        {
            var list = own[slot];
            var index = OwnSounds.Pick(list.Count, lastOwn[slot], dice.NextDouble());
            lastOwn[slot] = index;
            return index >= 0 ? list[index] : null;
        }

        private AudioClip PickGame(SoundSlot slot, ThroatSound throatSound)
        {
            var fixedIndex = slot == SoundSlot.Throat ? GameSounds.FixedIndex(throatSound) : -1;
            var index = GameSounds.Pick(gameLoaded[slot], fixedIndex, lastGame[slot], dice.NextDouble());
            lastGame[slot] = index;
            return index >= 0 ? game[slot][index] : null;
        }

        private int OwnCount(SoundSlot slot)
        {
            return own.TryGetValue(slot, out var list) ? list.Count : 0;
        }

        private int GameCount(SoundSlot slot)
        {
            return gameLoaded.TryGetValue(slot, out var loaded) ? Count(loaded) : 0;
        }

        private static int Count(bool[] loaded)
        {
            var count = 0;
            foreach (var present in loaded)
            {
                if (present)
                {
                    count++;
                }
            }
            return count;
        }

        private static AudioSource SourceOf(Reaction r)
        {
            return SourceOf(r, ref r.Voice, "ThroatBalls.Voice", AudioMixerName.Voice);
        }

        /// <summary>The reaction's audio source in <paramref name="slot"/> on the NPC's head, made on first use; null without a head.</summary>
        private static AudioSource SourceOf(Reaction r, ref AudioSource slot, string name, AudioMixerName mixer)
        {
            if (slot != null)
            {
                return slot;
            }
            var head = r.Creature.ragdoll != null ? r.Creature.ragdoll.headPart : null;
            if (head == null)
            {
                return null;
            }
            var holder = new GameObject(name);
            holder.transform.SetParent(head.transform, false);
            var source = holder.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = MinDistance;
            source.maxDistance = MaxDistance;
            source.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(mixer);
            slot = source;
            return source;
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
