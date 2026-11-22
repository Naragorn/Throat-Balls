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

        /// <summary>Both spots one after the other: hands to the groin, down on the ground.</summary>
        Combo,
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

        /// <summary>A hit while it lies down from the combo changes nothing.</summary>
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
    }

    /// <summary>One NPC's reaction state; a plain value, replaced by every step.</summary>
    public struct ReactionState : IEquatable<ReactionState>
    {
        public static readonly ReactionState Idle = new ReactionState
        {
            Kind = ReactionKind.None,
            LastZone = Zone.None,
            LastZoneAt = float.NegativeInfinity,
        };

        public ReactionKind Kind;
        public float StartedAt;
        public float Until;

        /// <summary>The spot hit last by a single reaction, for the combo; None after a combo.</summary>
        public Zone LastZone;

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

    /// <summary>
    /// The reaction state machine: a hit on one spot starts its reaction; the other spot within the
    /// combo window (counted from the first hit, also after its reaction ended) starts the combo.
    /// </summary>
    public static class ReactionRules
    {
        public static ReactionStep OnHit(ReactionState state, Zone zone, float now, ReactionSettings settings)
        {
            if (zone == Zone.None)
            {
                return new ReactionStep(state, ReactionAction.None);
            }
            if (state.Kind == ReactionKind.Combo && now < state.Until)
            {
                return new ReactionStep(state, ReactionAction.IgnoredWhileDown);
            }
            if (settings.ComboEnabled
                && now - state.LastZoneAt <= settings.ComboWindow
                && CompletesCombo(state.LastZone, zone, settings.ComboOrder))
            {
                var combo = ReactionState.Idle;
                combo.Kind = ReactionKind.Combo;
                combo.StartedAt = now;
                combo.Until = now + settings.DownDuration;
                return new ReactionStep(combo, ReactionAction.StartCombo);
            }

            var kind = zone == Zone.Throat ? ReactionKind.Throat : ReactionKind.Groin;
            var duration = zone == Zone.Throat ? settings.ThroatDuration : settings.GroinDuration;
            var next = state;
            next.LastZone = zone;
            next.LastZoneAt = now;
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
            if (state.Kind == ReactionKind.Combo)
            {
                return new ReactionStep(ReactionState.Idle, ReactionAction.End);
            }
            var next = state;
            next.Kind = ReactionKind.None;
            return new ReactionStep(next, ReactionAction.End);
        }

        /// <summary>Whether a hit on <paramref name="second"/> after one on <paramref name="first"/> makes the combo.</summary>
        public static bool CompletesCombo(Zone first, Zone second, ComboOrder order)
        {
            if (first == Zone.None || second == Zone.None || first == second)
            {
                return false;
            }
            switch (order)
            {
                case ComboOrder.ThroatThenGroin:
                    return first == Zone.Throat;
                case ComboOrder.GroinThenThroat:
                    return first == Zone.Groin;
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
