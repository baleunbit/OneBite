using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager I { get; private set; }

    [Header("BGM")]
    public AudioClip bgmMenu;
    public AudioClip bgmStage;      // 스테이지 BGM (하나로 통일)
    public AudioClip bgmGameOver;

    [Header("볼륨 설정")]
    [Range(0f, 1f)] public float musicVolume = 1f;

    AudioSource src;
    string currentBgmType = "";  // 현재 재생 중인 BGM 타입

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        src = gameObject.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        DontDestroyOnLoad(gameObject);

        // 씬 로드 이벤트 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 씬 로드 시 자동으로 적절한 BGM 재생
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name.ToLower();

        if (sceneName.Contains("menu"))
        {
            PlayMenu();
        }
        else if (sceneName.Contains("game"))
        {
            PlayStageBgm();
        }
        else if (sceneName.Contains("end"))
        {
            PlayMenu();  // 엔딩은 메뉴 BGM 사용
        }
    }

    // -------------------------------------------------------
    // BGM 재생
    // -------------------------------------------------------

    public void PlayMenu()
    {
        if (currentBgmType == "menu" && src.isPlaying) return;
        
        Play(bgmMenu);
        currentBgmType = "menu";
    }

    public void PlayGameOver()
    {
        if (currentBgmType == "gameover" && src.isPlaying) return;
        
        Play(bgmGameOver);
        currentBgmType = "gameover";
    }

    public void PlayStageBgm(int stage = 1)
    {
        // 이미 스테이지 BGM 재생 중이면 그대로 유지
        if (currentBgmType == "stage" && src.isPlaying) return;

        Play(bgmStage);
        currentBgmType = "stage";
    }

    void Play(AudioClip clip)
    {
        if (!clip) return;

        src.clip = clip;
        src.volume = musicVolume;
        src.Play();
    }

    public void Stop()
    {
        src.Stop();
        src.clip = null;
        currentBgmType = "";
    }

    // -------------------------------------------------------
    // 볼륨 조절 (설정 메뉴에서 호출)
    // -------------------------------------------------------

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        if (src) src.volume = musicVolume;
    }
}
