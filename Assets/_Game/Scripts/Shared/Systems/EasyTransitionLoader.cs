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
        try
        {
            EasyTransition.TransitionManager manager = EasyTransition.TransitionManager.Instance();
            if (manager != null)
            {
                manager.Transition(_sceneName, transition, startDelay);
            }
            else
            {
                SceneManager.LoadScene(_sceneName);
            }
        }
        catch (System.Exception)
        {
            SceneManager.LoadScene(_sceneName);
        }
    }

    public void LoadScene(int _sceneIndex)
    {
        try
        {
            EasyTransition.TransitionManager manager = EasyTransition.TransitionManager.Instance();
            if (manager != null)
            {
                manager.Transition(_sceneIndex, transition, startDelay);
            }
            else
            {
                SceneManager.LoadScene(_sceneIndex);
            }
        }
        catch (System.Exception)
        {
            SceneManager.LoadScene(_sceneIndex);
        }
    }
}
