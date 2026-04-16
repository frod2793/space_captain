using EasyTransition;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EasyTransitionLoader : ISceneLoader
{
    private readonly TransitionSettings transition;
    private readonly float startDelay;

    public EasyTransitionLoader(TransitionSettings settings, float delay = 0f)
    {
        transition = settings;
        startDelay = delay;
    }

    public void LoadScene(string _sceneName)
    {
        TransitionManager.Instance().Transition(_sceneName, transition, startDelay);
    }

    public void LoadScene(int _sceneIndex)
    {
        TransitionManager.Instance().Transition(_sceneIndex, transition, startDelay);
    }
}
