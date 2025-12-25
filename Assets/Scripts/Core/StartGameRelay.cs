// StartPage/StartGameRelay.cs
using UnityEngine;

public class StartGameRelay : MonoBehaviour
{

    public void OnClickConfirm()
    {
        if (SceneDirector.Instance != null)
        {
            SceneDirector.Instance.StartGameFromStartPage();
        }
        else
        {
            Debug.LogError("[StartGameRelay] SceneDirector.Instance is null. Is Boot loaded?");
        }
    }
}
