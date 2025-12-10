// SceneMgrRelay.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMgrRelay : MonoBehaviour
{
    [Header("씬 이름")]
    public string menuScene = "1_Menu";
    public string gameScene = "2_Game";
    
    public void StartGame()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(gameScene);
    }
    
    public void Restart()
    {
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        SceneManager.LoadScene(gameScene);
    }
    
    public void GoMenu()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(menuScene);
    }
    
    public void ExitApp()
    {
        Debug.Log("[SceneMgrRelay] Exit");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
