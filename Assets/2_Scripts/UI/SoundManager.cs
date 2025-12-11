using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager I { get; private set; }

    [Header("기본 BGM")]
    public AudioClip bgmMenu;
    public AudioClip bgmGameOver;

    [Header("스테이지별 BGM (index = stage-1)")]
    public AudioClip[] stageBgms;

    [Header("볼륨 설정")]
    [Range(0f, 1f)] public float musicVolume = 1f;

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
        src.volume = musicVolume;
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
    // 볼륨 조절 (설정 메뉴에서 호출)
    // -------------------------------------------------------

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        
        // 현재 재생 중인 모든 BGM에 즉시 적용
        if (src) src.volume = musicVolume;
    }
}
