using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButtonLoadScene : MonoBehaviour
{
    [Header("Scene to load (must be in Build Settings)")]
    [SerializeField] private string sceneName = "GameScene";

    // Hook this to the Button OnClick()
    public void StartGame()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Scene name is empty. Set it in the Inspector.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
