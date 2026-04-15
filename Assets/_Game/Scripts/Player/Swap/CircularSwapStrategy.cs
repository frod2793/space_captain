using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace SpaceCaptain.Player.Swap
{
    public class CircularSwapStrategy : ISwapStrategy
    {
        public async UniTask PrepareAsync(SwapContextDTO context)
        {
            if (context == null || !context.IsValid)
            {
                return;
            }

            context.LeavingCharacter.IsDragging = false;
            context.LeavingCharacter.SetActive(false);
            
            context.EnteringCharacter.gameObject.SetActive(true);
            
            await UniTask.CompletedTask;
        }

        public async UniTask AnimateAsync(SwapContextDTO context)
        {
            Vector3 startPosL = context.LeavingOriginPos;
            Vector3 endPosL = context.EnteringOriginPos;
            
            Vector3 startPosE = context.EnteringOriginPos;
            Vector3 endPosE = context.ActivePosition.position;

            Vector3 midL = (startPosL + endPosL) * 0.5f;
            Vector3 midE = (startPosE + endPosE) * 0.5f;

            Vector3 direction = (endPosL - startPosL).normalized;
            Vector3 normal = Vector3.Cross(direction, Vector3.forward).normalized;
            
            Vector3 offset = normal * context.SwapOffset;

            Vector3[] pathL = new Vector3[] { startPosL, midL + offset, endPosL };
            Vector3[] pathE = new Vector3[] { startPosE, midE - offset, endPosE };

            var seq = DOTween.Sequence()
                .Join(context.LeavingCharacter.transform.DOPath(pathL, context.SwapDuration, PathType.CatmullRom).SetEase(Ease.OutSine))
                .Join(context.EnteringCharacter.transform.DOPath(pathE, context.SwapDuration, PathType.CatmullRom).SetEase(Ease.OutSine));

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
