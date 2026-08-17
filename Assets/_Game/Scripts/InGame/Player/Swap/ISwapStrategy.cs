using Cysharp.Threading.Tasks;

namespace SpaceCaptain.Player.Swap
{
    public interface ISwapStrategy
    {
        UniTask PrepareAsync(SwapContextDTO context);
        UniTask AnimateAsync(SwapContextDTO context);
        UniTask FinalizeAsync(SwapContextDTO context);
    }
}
