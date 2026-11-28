namespace ThroatBalls.Logic
{
    /// <summary>
    /// How the knock-out leaves the game's stagger state (ThunderRoad 1.3.2).
    ///
    /// The mod reacts on EventTime.OnEnd. By then BrainModuleHitReaction.OnDamage (OnDamageEvent runs before
    /// EventManager.onCreatureHit) has often started its StaggerCoroutine: brain.currentStagger set, the Hit trigger
    /// set and Creature.SetAnimatorBusy(true) by hand, ahead of the hit animation. BrainToggle(false) stops that
    /// coroutine and resets the Hit trigger, so the hit animation never runs and nothing clears either flag:
    /// IsBusy stuck true makes MoveTo, TurnTo, AttackMelee, DrawItem fail, currentStagger stuck makes canDodge and
    /// canBlock false. The NPC woke up and stood still (Windows run 2026-11-28, HumanMale8: no turn 15-20 s after).
    /// The game's own stun avoids this by stunning on OnStart, before the stagger starts.
    /// </summary>
    public static class KnockOutRules
    {
        /// <summary>
        /// BrainToggle's toggleHitReaction: the hit reaction stops with the brain (OnBrainStop: currentStagger None,
        /// its damage events off) and starts with it. Without it, OnBrainStart on waking subscribes OnDamage a second
        /// time, so every later hit is pushed twice.
        /// </summary>
        public const bool ToggleHitReaction = true;

        /// <summary>
        /// IsBusy after the knock-out, by the game's own rule (ThunderRoad.Utilities.BusyState.UpdateBusyBool):
        /// busy while a busy animator state runs. This withdraws the cut stagger's hand-set flag.
        /// </summary>
        public static bool BusyAfterKnockOut(bool hasCount, int busyStates) => hasCount && busyStates > 0;
    }
}
