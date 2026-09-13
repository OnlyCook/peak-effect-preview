using Peak.Afflictions;
using UnityEngine;

namespace EffectPreview.Preview
{
    // reads real (not previewed) remaining duration for invincibility/infinite stamina
    internal static class SpecialStatusDuration
    {
        internal static bool TryGetInvincibilityRemaining(Character character, out float remainingSeconds, out float remainingFraction)
        {
            return TryGetRemaining(character, Affliction.AfflictionType.Invincibility, out remainingSeconds, out remainingFraction);
        }

        // Scout's Ambition's aura
        // see Preview/InfiniteStaminaUnifiedTimer
        internal static bool TryGetRadiateInfiniteStamAffliction(Character character, out Affliction_RadiateInfiniteStam affliction)
        {
            affliction = null;
            if (character == null || character.refs == null || character.refs.afflictions == null)
            {
                return false;
            }
            if (character.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.RadiateInfiniteStam, out Affliction found))
            {
                affliction = found as Affliction_RadiateInfiniteStam;
            }
            return affliction != null;
        }

        // Big Lollypop/Energy Drink/Bugle-direct -- live instance needed for climbDelay, see Preview/InfiniteStaminaGraceTracker
        internal static bool TryGetDirectInfiniteStaminaAffliction(Character character, out Affliction_InfiniteStamina affliction)
        {
            affliction = null;
            if (character == null || character.refs == null || character.refs.afflictions == null)
            {
                return false;
            }
            if (character.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.InfiniteStamina, out Affliction found))
            {
                affliction = found as Affliction_InfiniteStamina;
            }
            return affliction != null;
        }

        private static bool TryGetRemaining(Character character, Affliction.AfflictionType type, out float remainingSeconds, out float remainingFraction)
        {
            remainingSeconds = 0f;
            remainingFraction = 0f;

            if (character == null || character.refs == null || character.refs.afflictions == null)
            {
                return false;
            }

            if (!character.refs.afflictions.HasAfflictionType(type, out Affliction affliction) || affliction == null || affliction.totalTime <= 0f)
            {
                return false;
            }

            remainingSeconds = Mathf.Max(0f, affliction.totalTime - affliction.timeElapsed);
            remainingFraction = Mathf.Clamp01(remainingSeconds / affliction.totalTime);
            return true;
        }
    }
}
