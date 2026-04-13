using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace SpaceCaptain.Player.Swap
{
    public class FieldSwapStrategy : ISwapStrategy
    {
        public async UniTask PrepareAsync(SwapContextDTO context)
        {
            if (context == null || !context.IsValid) return;

            context.LeavingCharacter.IsDragging = false;
            context.LeavingCharacter.SetActive(false);
            
            context.EnteringCharacter.gameObject.SetActive(true);
            
            await UniTask.CompletedTask;
        }

        public async UniTask AnimateAsync(SwapContextDTO context)
        {
            var seq = DOTween.Sequence()
                .Join(context.LeavingCharacter.transform.DOMove(context.EnteringOriginPos, context.SwapDuration).SetEase(Ease.OutSine))
                .Join(context.EnteringCharacter.transform.DOMove(context.ActivePosition.position, context.SwapDuration).SetEase(Ease.OutSine));

            await seq.Play().ToUniTask(cancellationToken: context.CancellationToken);
        }

        public async UniTask FinalizeAsync(SwapContextDTO context)
        {
            context.EnteringCharacter.SetActive(true);
            context.EnteringCharacter.MoveToX(context.ActivePosition.position.x, true);
            
            context.LeavingCharacter.MoveToX(context.EnteringOriginPos.x, true);
            if (context.LeavingCharacter.Stats.CurrentHp > 0)
            {
                context.LeavingCharacter.gameObject.SetActive(true);
            }
            else
            {
                context.LeavingCharacter.gameObject.SetActive(false);
            }
            
            await UniTask.CompletedTask;
        }
    }
}
