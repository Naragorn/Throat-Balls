using ThroatBalls.Logic;
using ThunderRoad;
using UnityEngine;

namespace ThroatBalls
{
    /// <summary>One NPC's reaction: the logic state plus what was changed on its body, so exactly that is undone.</summary>
    internal sealed class Reaction
    {
        public Reaction(Creature creature)
        {
            Creature = creature;
        }

        public Creature Creature { get; }

        public ReactionState State = ReactionState.Idle;

        /// <summary>This mod turned the brain off (it was on), so this mod turns it back on.</summary>
        public bool BrainOff;

        /// <summary>
        /// The brain ran its behaviour tree when this mod turned it off. The game's BrainToggle turns the tree on with
        /// the brain, so a brain without a running tree (HumanDummy: BrainData updateTree false) would come back fighting.
        /// </summary>
        public bool TreeWasRunning;

        /// <summary>The game's stun clip runs (throat).</summary>
        public bool PlayedClip;

        public bool HeightSet;
        public bool InjuredSet;

        /// <summary>Ragdoll fall: knocked down and held there (no-stand-up hold).</summary>
        public bool KnockedDown;

        public BodyTarget HandsAt;
        public GameObject LeftAnchor;
        public GameObject RightAnchor;
        public float LastGroanAt;

        /// <summary>Plays the own files and the game clips from his head (<see cref="SoundBank"/>); made on the first one.</summary>
        public AudioSource Voice;

        /// <summary>Set when the reaction ended; the delayed wake-up only runs for this one.</summary>
        public int Generation;
    }
}
