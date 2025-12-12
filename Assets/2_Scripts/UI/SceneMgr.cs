using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMgr : MonoBehaviour
{
    public static SceneMgr I { get; private set; }

    [Header("씬 이름들")]
    [SerializeField] string sceneMenu = "1_Menu";
    [SerializeField] string sceneGame = "2_Game";
    [SerializeField] string sceneEnd = "3_End";

    [Header("UI")]
    [SerializeField] GameObject ControlsPanel;

    void Start()
    {
        // 메뉴 씬에서 처음 뜰 때 바로 메뉴 BGM 재생
        SoundManager.I?.PlayMenu();
    }

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        // ✅ 메뉴 전용 매니저라면 DontDestroyOnLoad 제거
        // DontDestroyOnLoad(gameObject); ← 이거 제거!

        if (ControlsPanel) ControlsPanel.SetActive(false);
    }

    // 🔸 메인 메뉴에서 호출되는 버튼
    public void OnClickStart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        // 게임 상태 완전 초기화
        StageDirector.ResetGameState();

        SceneManager.LoadScene(sceneGame);

        SoundManager.I?.PlayStageBgm(1);
    }

    public void OnClickExit()
    {
        Debug.Log("[SceneMgr] Exit requested");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowControls() { if (ControlsPanel) ControlsPanel.SetActive(true); }
    public void HideControls() { if (ControlsPanel) ControlsPanel.SetActive(false); }
    public void ToggleControls()
    {
        if (ControlsPanel)
            ControlsPanel.SetActive(!ControlsPanel.activeSelf);
    }

    // 🔸 인게임 → 엔드씬 전환 (몹 전멸 시 호출)
    public void GoToEndScene()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SoundManager.I?.PlayMenu(); // 엔딩 BGM 없으면 메뉴용으로 재생

        SceneManager.LoadScene(sceneEnd);
    }

    // 🔸 엔드씬 버튼용
    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        
        // 게임 상태 완전 초기화
        StageDirector.ResetGameState();
        
        SoundManager.I?.PlayStageBgm(1);
        SceneManager.LoadScene(sceneGame);
    }

    public void OnClickMenu()
    {
        Time.timeScale = 1f;
        
        // 게임 상태 완전 초기화
        StageDirector.ResetGameState();
        
        SoundManager.I?.PlayMenu();
        SceneManager.LoadScene(sceneMenu);
    }
}
