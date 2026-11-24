using System;
using System.Collections.Generic;
using System.IO;
using ThroatBalls.Logic;
using ThunderRoad;
using UnityEngine;

namespace ThroatBalls
{
    /// <summary>
    /// The player's own sound files from the mod's Sounds folder (<see cref="OwnSounds"/>), loaded once when the mod
    /// loads, played from the NPC's head through the game's Voice mixer (so the game's voice volume applies).
    /// </summary>
    internal sealed class SoundBank
    {
        private const float MinDistance = 1f;
        private const float MaxDistance = 20f;

        private readonly Dictionary<SoundSlot, List<AudioClip>> clips = new Dictionary<SoundSlot, List<AudioClip>>
        {
            [SoundSlot.Throat] = new List<AudioClip>(),
            [SoundSlot.Groin] = new List<AudioClip>(),
        };

        private readonly Dictionary<SoundSlot, int> lastPicked = new Dictionary<SoundSlot, int>
        {
            [SoundSlot.Throat] = -1,
            [SoundSlot.Groin] = -1,
        };

        private readonly System.Random dice = new System.Random();

        public void Load(string modFolder)
        {
            var folder = Path.Combine(modFolder ?? string.Empty, OwnSounds.FolderName);
            if (!Directory.Exists(folder))
            {
                ModLog.Info("Own sounds: no " + OwnSounds.FolderName + " folder, the game's voice plays");
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
                clips[slot].Add(clip);
            }
            ModLog.Info("Own sounds: " + Describe(SoundSlot.Throat) + " throat, " + Describe(SoundSlot.Groin) + " groin (" + folder + ")");
        }

        /// <summary>Plays one of the slot's files from the NPC's head; false when the slot has none (the caller uses the game's voice).</summary>
        public bool TryPlay(Reaction r, SoundSlot slot, out string why)
        {
            why = null;
            if (slot == SoundSlot.None)
            {
                return false;
            }
            if (!clips.TryGetValue(slot, out var list) || list.Count == 0)
            {
                why = "no " + slot + " files";
                return false;
            }
            var source = SourceOf(r);
            if (source == null)
            {
                why = "no head to play from";
                return false;
            }
            var index = OwnSounds.Pick(list.Count, lastPicked[slot], dice.NextDouble());
            lastPicked[slot] = index;
            source.Stop();
            source.clip = list[index];
            source.Play();
            ModLog.Verbose("SOUND | " + GameAdapters.Label(r.Creature) + " | own " + slot + " '" + list[index].name + "' (" + list[index].length.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s)");
            return true;
        }

        /// <summary>Removes the reaction's audio source (it ends with the reaction; a sound still playing is cut).</summary>
        public static void Release(Reaction r)
        {
            if (r.Voice != null)
            {
                UnityEngine.Object.Destroy(r.Voice.gameObject);
            }
            r.Voice = null;
        }

        private static AudioSource SourceOf(Reaction r)
        {
            if (r.Voice != null)
            {
                return r.Voice;
            }
            var head = r.Creature.ragdoll != null ? r.Creature.ragdoll.headPart : null;
            if (head == null)
            {
                return null;
            }
            var holder = new GameObject("ThroatBalls.Voice");
            holder.transform.SetParent(head.transform, false);
            var source = holder.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = MinDistance;
            source.maxDistance = MaxDistance;
            source.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Voice);
            r.Voice = source;
            return source;
        }

        private string Describe(SoundSlot slot)
        {
            return clips[slot].Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
