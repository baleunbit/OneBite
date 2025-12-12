using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseSystem : MonoBehaviour
{
    [Header("일시정지 패널")]
    [SerializeField] GameObject menuRoot;

    [Header("옵션")]
    [SerializeField] bool showCursorOnPause = true;
    [SerializeField] string menuSceneName = "1_Menu";

    bool paused;

    void Awake()
    {
        if (menuRoot) menuRoot.SetActive(false);
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        Debug.Log($"[PauseSystem] 초기화 완료. menuRoot: {(menuRoot ? menuRoot.name : "NULL")}");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[PauseSystem] ESC 키 감지!");
            
            if (paused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        
        if (menuRoot)
        {
            menuRoot.SetActive(true);
            Debug.Log("[PauseSystem] 패널 활성화됨");
        }
        else
        {
            Debug.LogError("[PauseSystem] menuRoot가 null입니다! Inspector에서 연결해주세요.");
        }
        
        if (showCursorOnPause) Cursor.visible = true;
    }

    public void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (menuRoot) menuRoot.SetActive(false);
        Debug.Log("[PauseSystem] 게임 재개");
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        // 게임 상태 완전 초기화
        StageDirector.ResetGameState();
        
        var cur = SceneManager.GetActiveScene();
        SceneManager.LoadScene(cur.buildIndex);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // 게임 상태 완전 초기화
        StageDirector.ResetGameState();

        var ev = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (ev) Destroy(ev.gameObject);

        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogError("[PauseSystem] menuSceneName이 비어 있습니다.");
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }

    public bool IsPaused() => paused;
}

