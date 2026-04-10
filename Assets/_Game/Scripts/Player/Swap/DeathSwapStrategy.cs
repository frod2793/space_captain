using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace SpaceCaptain.Player.Swap
{
    public class DeathSwapStrategy : ISwapStrategy
    {
        public async UniTask PrepareAsync(SwapContextDTO context)
        {
            context.EnteringCharacter.gameObject.SetActive(true);
            context.EnteringCharacter.RestoreComponents();
            
            if (context.IsDraggingActive)
            {
                context.EnteringCharacter.IsDragging = true;
            }
            
            await UniTask.CompletedTask;
        }

        public async UniTask AnimateAsync(SwapContextDTO context)
        {
            Vector3 targetPos = context.LeavingCharacter.transform.position;
            targetPos.y = context.ActivePosition.position.y;
            
            await context.EnteringCharacter.transform
                .DOMove(targetPos, context.SwapDuration)
                .SetEase(Ease.OutCubic)
                .ToUniTask(cancellationToken: context.CancellationToken);
        }

        public async UniTask FinalizeAsync(SwapContextDTO context)
        {
            context.EnteringCharacter.SetActive(true);
            context.EnteringCharacter.MoveToX(context.EnteringCharacter.transform.position.x, true);
            
            context.EnteringCharacter.SetSwapCooldown(0);
            
            await UniTask.CompletedTask;
        }
    }
}
