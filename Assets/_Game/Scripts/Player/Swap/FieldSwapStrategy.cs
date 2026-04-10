using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace SpaceCaptain.Player.Swap
{
    public class FieldSwapStrategy : ISwapStrategy
    {
        private Vector3 m_leavingOriginPos;
        private Vector3 m_enteringOriginPos;

        public async UniTask PrepareAsync(SwapContextDTO context)
        {
            if (context == null || !context.IsValid) return;

            context.LeavingCharacter.IsDragging = false;
            context.LeavingCharacter.SetActive(false);
            
            m_leavingOriginPos = context.LeavingCharacter.transform.position;
            m_enteringOriginPos = context.EnteringCharacter.transform.position;
            
            context.EnteringCharacter.gameObject.SetActive(true);
            
            await UniTask.CompletedTask;
        }

        public async UniTask AnimateAsync(SwapContextDTO context)
        {
            var seq = DOTween.Sequence()
                .Join(context.LeavingCharacter.transform.DOMove(m_enteringOriginPos, context.SwapDuration).SetEase(Ease.OutSine))
                .Join(context.EnteringCharacter.transform.DOMove(context.ActivePosition.position, context.SwapDuration).SetEase(Ease.OutSine));

            await seq.Play().ToUniTask(cancellationToken: context.CancellationToken);
        }

        public async UniTask FinalizeAsync(SwapContextDTO context)
        {
            context.EnteringCharacter.SetActive(true);
            context.EnteringCharacter.MoveToX(context.ActivePosition.position.x, true);
            
            context.LeavingCharacter.MoveToX(m_enteringOriginPos.x, true);
            context.LeavingCharacter.gameObject.SetActive(true);
            
            await UniTask.CompletedTask;
        }
    }
}
