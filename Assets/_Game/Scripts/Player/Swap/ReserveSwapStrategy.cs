using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace SpaceCaptain.Player.Swap
{
    public class ReserveSwapStrategy : ISwapStrategy
    {
        private const float REGEN_PERCENT = 0.05f;

        public async UniTask PrepareAsync(SwapContextDTO context)
        {
            if (context == null || !context.IsValid) return;

            context.LeavingCharacter.IsDragging = false;
            context.LeavingCharacter.SetActive(false);
            
            context.EnteringCharacter.gameObject.SetActive(true);
            context.EnteringCharacter.RestoreComponents();
            
            await UniTask.CompletedTask;
        }

        public async UniTask AnimateAsync(SwapContextDTO context)
        {
            Vector3 exitPos = context.LeavingCharacter.transform.position + Vector3.down * 5f;

            var seq = DOTween.Sequence()
                .Join(context.LeavingCharacter.transform.DOMove(exitPos, context.SwapDuration).SetEase(Ease.InBack))
                .Join(context.EnteringCharacter.transform.DOMove(context.ActivePosition.position, context.SwapDuration).SetEase(Ease.OutBack));

            await seq.Play().ToUniTask(cancellationToken: context.CancellationToken);
        }

        public async UniTask FinalizeAsync(SwapContextDTO context)
        {
            context.EnteringCharacter.SetActive(true);
            context.EnteringCharacter.MoveToX(context.ActivePosition.position.x, true);
            
            context.LeavingCharacter.gameObject.SetActive(false);
            
            if (context.EnteringCharacter.Stats.CurrentHp <= 0)
            {
                context.EnteringCharacter.gameObject.SetActive(false);
            }
            
            await UniTask.CompletedTask;
        }
    }
}
