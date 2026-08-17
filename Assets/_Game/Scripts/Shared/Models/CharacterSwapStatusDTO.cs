using SpaceCaptain.Player;

namespace SpaceCaptain.Models
{
    public readonly struct CharacterSwapStatusDTO
    {
        public readonly float CooldownRatio;
        public readonly float RemainingCooldown;
        public readonly CharacterSwapState State;
        public readonly bool IsAvailable;

        public CharacterSwapStatusDTO(float ratio, float remaining, CharacterSwapState state, bool isAvailable)
        {
            CooldownRatio = ratio;
            RemainingCooldown = remaining;
            State = state;
            IsAvailable = isAvailable;
        }
    }
}
