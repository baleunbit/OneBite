using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("일시정지 패널")]
    [SerializeField] GameObject menuRoot;
    
    [Header("프리팹 사용 (선택)")]
    [Tooltip("프리팹을 사용하면 메뉴 씬과 동일한 패널을 자동 생성")]
    [SerializeField] GameObject pausePanelPrefab;
    [SerializeField] Transform canvasParent; // UI Canvas

    [Header("옵션")]
    [SerializeField] bool showCursorOnPause = true;

    [SerializeField] private string menuSceneName = "1_Menu";

    bool paused;

    void Awake()
    {
        // 프리팹이 설정되어 있으면 자동 생성
        if (pausePanelPrefab && canvasParent && !menuRoot)
        {
            menuRoot = Instantiate(pausePanelPrefab, canvasParent);
            menuRoot.transform.SetAsLastSibling(); // 맨 위에 표시
        }
        
        if (menuRoot) menuRoot.SetActive(false);
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
            
            // RectTransform 강제 리셋
            var rt = menuRoot.GetComponent<RectTransform>();
            if (rt)
            {
                // Anchor를 중앙으로 설정
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                
                // 크기가 음수거나 너무 작으면 강제 설정
                if (rt.sizeDelta.x <= 0 || rt.sizeDelta.y <= 0)
                {
                    rt.sizeDelta = new Vector2(800f, 500f); // 기본 크기
                }
            }
            
            // CanvasGroup 투명도 확인
            var cg = menuRoot.GetComponent<CanvasGroup>();
            if (cg)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }
            
            // 맨 앞으로 가져오기
            menuRoot.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogError("[PauseMenu] menuRoot가 null입니다!");
        }
        
        if (showCursorOnPause) Cursor.visible = true;
    }

    public void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (menuRoot) menuRoot.SetActive(false);
        // Cursor.visible = false; // 필요시
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        var cur = SceneManager.GetActiveScene();
        SceneManager.LoadScene(cur.buildIndex);
    }

    // ✅ 매개변수 제거: 필드값 사용
    public void BackToMenu()
    {
        // ✅ 안전하게 초기화
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // ✅ 현재 존재하는 EventSystem 강제 제거 (중복 방지)
        var ev = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (ev) Destroy(ev.gameObject);

        // ✅ 씬 전환
        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogError("[PauseMenu] menuSceneName이 비어 있습니다.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogError($"[PauseMenu] '{menuSceneName}' 씬이 Build Settings에 없거나 이름이 틀렸습니다.");
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }

    public bool IsPaused() => paused;
}
