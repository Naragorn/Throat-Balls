using System;

namespace ThroatBalls.Logic
{
    /// <summary>What an NPC is doing because of a low blow.</summary>
    public enum ReactionKind
    {
        None,

        /// <summary>Hands to the throat, gagging.</summary>
        Throat,

        /// <summary>Hands to the groin, bent over.</summary>
        Groin,

        /// <summary>Hands to the groin, down on the ground: both spots one after the other, or a groin hit alone by chance.</summary>
        Down,
    }

    /// <summary>Which order of the two hits makes the combo. Keep <see cref="EitherOrder"/> first (enum default).</summary>
    public enum ComboOrder
    {
        EitherOrder = 0,
        ThroatThenGroin = 1,
        GroinThenThroat = 2,
    }

    public enum ReactionAction
    {
        None,

        /// <summary>A single reaction starts (or replaces the other single one).</summary>
        Start,

        /// <summary>The same spot again while it still reacts: it lasts longer.</summary>
        Extend,

        /// <summary>The second spot within the combo window: down it goes.</summary>
        StartCombo,

        /// <summary>A groin hit alone took him down (Groin Knockdown Chance).</summary>
        StartGroinDown,

        /// <summary>The reaction roll missed: he shrugs it off (the hit still counts as the first of both hits).</summary>
        Shrugged,

        /// <summary>A hit while he is down changes nothing.</summary>
        IgnoredWhileDown,

        /// <summary>The reaction is over (time up, or the mod turned off).</summary>
        End,
    }

    /// <summary>Tuning values, all of them Mod Menu settings. Times in seconds.</summary>
    public sealed class ReactionSettings
    {
        public bool ComboEnabled;
        public ComboOrder ComboOrder;
        public float ComboWindow;
        public float ThroatDuration;
        public float GroinDuration;
        public float DownDuration;

        // Chances, 0..1 (Nadi, 2026-11-22: groin alone 50 %, both hits 100 %, every chance adjustable).
        public float ThroatReactionChance;
        public float GroinReactionChance;
        public float GroinKnockdownChance;
        public float ComboKnockdownChance;
    }

    /// <summary>One NPC's reaction state; a plain value, replaced by every step.</summary>
    public struct ReactionState : IEquatable<ReactionState>
    {
        public static readonly ReactionState Idle = new ReactionState
        {
            Kind = ReactionKind.None,
            LastZone = HitZone.None,
            LastZoneAt = float.NegativeInfinity,
        };

        public ReactionKind Kind;
        public float StartedAt;
        public float Until;

        /// <summary>The spot hit last by a single reaction, for the combo; None after a combo.</summary>
        public HitZone LastZone;

        public float LastZoneAt;

        public bool IsActive => Kind != ReactionKind.None;

        public bool Equals(ReactionState other)
        {
            return Kind == other.Kind && StartedAt.Equals(other.StartedAt) && Until.Equals(other.Until)
                && LastZone == other.LastZone && LastZoneAt.Equals(other.LastZoneAt);
        }

        public override bool Equals(object obj) => obj is ReactionState other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;
                hash = (hash * 397) ^ StartedAt.GetHashCode();
                hash = (hash * 397) ^ Until.GetHashCode();
                hash = (hash * 397) ^ (int)LastZone;
                return (hash * 397) ^ LastZoneAt.GetHashCode();
            }
        }

        public override string ToString() => Kind + " until " + Until.ToString("0.00") + " (last " + LastZone + ")";
    }

    public readonly struct ReactionStep
    {
        public ReactionStep(ReactionState next, ReactionAction action)
        {
            Next = next;
            Action = action;
        }

        public ReactionState Next { get; }

        public ReactionAction Action { get; }
    }

    /// <summary>The three chance rolls of one hit, each a uniform value in [0, 1).</summary>
    public readonly struct ReactionRolls
    {
        /// <summary>Rolls that hit every chance above zero (tests, and nothing random).</summary>
        public static readonly ReactionRolls Lowest = new ReactionRolls(0f, 0f, 0f);

        public ReactionRolls(float combo, float reaction, float knockdown)
        {
            Combo = combo;
            Reaction = reaction;
            Knockdown = knockdown;
        }

        /// <summary>Against Both Hits Knockdown Chance.</summary>
        public float Combo { get; }

        /// <summary>Against Throat or Groin Reaction Chance.</summary>
        public float Reaction { get; }

        /// <summary>Against Groin Knockdown Chance.</summary>
        public float Knockdown { get; }
    }

    /// <summary>
    /// The reaction state machine. A hit on one spot starts its reaction (Throat or Groin Reaction Chance; a miss
    /// is shrugged off but still counts as the first of both hits). The other spot within the window (counted from
    /// the first hit, also after its reaction ended) takes him down (Both Hits Knockdown Chance); a groin hit alone
    /// can too (Groin Knockdown Chance, rolled only when the groin reaction happens).
    /// </summary>
    public static class ReactionRules
    {
        public static ReactionStep OnHit(ReactionState state, HitZone zone, float now, ReactionSettings settings, ReactionRolls rolls)
        {
            if (zone == HitZone.None)
            {
                return new ReactionStep(state, ReactionAction.None);
            }
            if (state.Kind == ReactionKind.Down && now < state.Until)
            {
                return new ReactionStep(state, ReactionAction.IgnoredWhileDown);
            }
            if (settings.ComboEnabled
                && now - state.LastZoneAt <= settings.ComboWindow
                && CompletesCombo(state.LastZone, zone, settings.ComboOrder)
                && Hits(rolls.Combo, settings.ComboKnockdownChance))
            {
                return new ReactionStep(Down(now, settings), ReactionAction.StartCombo);
            }

            var next = state;
            next.LastZone = zone;
            next.LastZoneAt = now;
            var reactionChance = zone == HitZone.Throat ? settings.ThroatReactionChance : settings.GroinReactionChance;
            if (!Hits(rolls.Reaction, reactionChance))
            {
                return new ReactionStep(next, ReactionAction.Shrugged);
            }
            if (zone == HitZone.Groin && Hits(rolls.Knockdown, settings.GroinKnockdownChance))
            {
                return new ReactionStep(Down(now, settings), ReactionAction.StartGroinDown);
            }

            var kind = zone == HitZone.Throat ? ReactionKind.Throat : ReactionKind.Groin;
            var duration = zone == HitZone.Throat ? settings.ThroatDuration : settings.GroinDuration;
            if (state.Kind == kind && now < state.Until)
            {
                next.Until = Math.Max(state.Until, now + duration);
                return new ReactionStep(next, ReactionAction.Extend);
            }
            next.Kind = kind;
            next.StartedAt = now;
            next.Until = now + duration;
            return new ReactionStep(next, ReactionAction.Start);
        }

        /// <summary>Every frame: ends a reaction whose time is up, or at once when the mod is off.</summary>
        public static ReactionStep OnTick(ReactionState state, float now, bool enabled)
        {
            if (!state.IsActive || (enabled && now < state.Until))
            {
                return new ReactionStep(state, ReactionAction.None);
            }
            if (state.Kind == ReactionKind.Down)
            {
                return new ReactionStep(ReactionState.Idle, ReactionAction.End);
            }
            var next = state;
            next.Kind = ReactionKind.None;
            return new ReactionStep(next, ReactionAction.End);
        }

        /// <summary>A roll in [0, 1) hits below the chance: 100 % always, 0 % never.</summary>
        public static bool Hits(float roll, float chance)
        {
            return roll < chance;
        }

        private static ReactionState Down(float now, ReactionSettings settings)
        {
            var down = ReactionState.Idle;
            down.Kind = ReactionKind.Down;
            down.StartedAt = now;
            down.Until = now + settings.DownDuration;
            return down;
        }

        /// <summary>Whether a hit on <paramref name="second"/> after one on <paramref name="first"/> makes the combo.</summary>
        public static bool CompletesCombo(HitZone first, HitZone second, ComboOrder order)
        {
            if (first == HitZone.None || second == HitZone.None || first == second)
            {
                return false;
            }
            switch (order)
            {
                case ComboOrder.ThroatThenGroin:
                    return first == HitZone.Throat;
                case ComboOrder.GroinThenThroat:
                    return first == HitZone.Groin;
                default:
                    return true;
            }
        }

        /// <summary>An idle NPC whose last hit is older than the combo window: nothing left to remember.</summary>
        public static bool Forgettable(ReactionState state, float now, ReactionSettings settings)
        {
            return !state.IsActive && now - state.LastZoneAt > settings.ComboWindow;
        }
    }
}
