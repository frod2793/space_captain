using UnityEngine;
using System.Threading;

namespace SpaceCaptain.Player.Swap
{
    public class SwapContextDTO
    {
        public PlayerCharacterController EnteringCharacter;
        public PlayerCharacterController LeavingCharacter;
        public Transform ActivePosition;
        public float SwapDuration;
        public Camera MainCamera;
        public bool IsDraggingActive;
        public CancellationToken CancellationToken;

        public bool IsValid => EnteringCharacter != null && LeavingCharacter != null && ActivePosition != null;
    }
}
