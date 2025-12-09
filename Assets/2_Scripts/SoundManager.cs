// SoundManager.cs (수정 완료 버전)
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager I { get; private set; }

    [Header("공통 BGM")]
    public AudioClip bgmMenu;
    public AudioClip bgmGameOver;

    [Header("스테이지별 BGM (index = stage-1)")]
    public AudioClip[] stageBgms;

    [Header("볼륨 설정")]
    [Range(0f, 1f)] public float menuVolume = 1f;     // 메뉴 볼륨
    [Range(0f, 1f)] public float musicVolume = 1f;    // 인게임 음악 볼륨

    AudioSource src;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        src = gameObject.AddComponent<AudioSource>();
        src.loop = true;
        DontDestroyOnLoad(gameObject);
    }

    // -------------------------------------------------------
    // BGM 재생
    // -------------------------------------------------------

    public void PlayMenu()
    {
        Play(bgmMenu);
        src.volume = menuVolume;
    }

    public void PlayGameOver()
    {
        Play(bgmGameOver);
        src.volume = musicVolume;
    }

    public void PlayStageBgm(int stage)
    {
        if (stage <= 0) { Stop(); return; }

        int idx = stage - 1;
        if (idx < stageBgms.Length && stageBgms[idx])
        {
            Play(stageBgms[idx]);
            src.volume = musicVolume;
        }
    }

    void Play(AudioClip clip)
    {
        if (!clip) return;
        if (src.clip == clip && src.isPlaying) return;

        src.clip = clip;
        src.Play();
    }

    public void Stop()
    {
        src.Stop();
        src.clip = null;
    }

    // -------------------------------------------------------
    // 설정 메뉴에서 호출되는 부분
    // -------------------------------------------------------

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);

        // 인게임 BGM 재생 중일 경우 반영
        if (src.clip != bgmMenu)
            src.volume = musicVolume;
    }

    public void SetMenuVolume(float v)
    {
        menuVolume = Mathf.Clamp01(v);

        // 메뉴 BGM 재생 중일 경우 반영
        if (src.clip == bgmMenu)
            src.volume = menuVolume;
    }
}
