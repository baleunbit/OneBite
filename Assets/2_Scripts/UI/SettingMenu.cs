using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingMenu : MonoBehaviour
{
    [Header("Graphics UI (화면 모드, 주사율만)")]
    public TMP_Dropdown ddDisplayMode;
    public TMP_Dropdown ddRefreshRate;

    [Header("Audio UI")]
    public Slider slMaster;
    public Slider slMusic;

    [Header("Text UI")]
    public TMP_Text txtMaster;
    public TMP_Text txtMusic;

    // 기본값
    const int DEFAULT_MODE = (int)FullScreenMode.ExclusiveFullScreen;
    const int DEFAULT_REFRESH = 60;
    const float DEFAULT_MASTER = 1f;  // 100%
    const float DEFAULT_MUSIC = 1f;   // 100%

    // PlayerPrefs Keys
    const string K_MODE = "SET_MODE";
    const string K_RR = "SET_RR";
    const string K_VM = "SET_VOL_MASTER";
    const string K_VB = "SET_VOL_MUSIC";

    // 주사율 목록
    readonly int[] refreshRates = { 60, 90, 120, 144, 165, 180, 200, 240 };
    
    bool initialized = false;

    void Awake()
    {
        Initialize();
    }
    
    void OnEnable()
    {
        if (initialized)
        {
            LoadSettings();
        }
    }
    
    void Initialize()
    {
        if (initialized) return;
        initialized = true;
        
        BuildDisplayModeList();
        BuildRefreshRateList();

        if (ddDisplayMode) FixDropdownHeight(ddDisplayMode, ddDisplayMode.options.Count);
        if (ddRefreshRate) FixDropdownHeight(ddRefreshRate, refreshRates.Length);

        // 드롭다운 값 변경 시 자동 적용
        if (ddDisplayMode) ddDisplayMode.onValueChanged.AddListener(_ => ApplyGraphics());
        if (ddRefreshRate) ddRefreshRate.onValueChanged.AddListener(_ => ApplyGraphics());

        // 슬라이더 값 변경 시 자동 적용
        if (slMaster) slMaster.onValueChanged.AddListener(_ => ApplyAudio());
        if (slMusic) slMusic.onValueChanged.AddListener(_ => ApplyAudio());

        LoadSettings();
        ApplyGraphics();
        ApplyAudio();
    }

    // ======================================================
    // UI 구성
    // ======================================================

    void BuildDisplayModeList()
    {
        if (!ddDisplayMode) return;
        ddDisplayMode.ClearOptions();
        ddDisplayMode.AddOptions(new List<string>
        {
            "전체 화면",
            "창 모드",
            "테두리 없는 창모드"
        });
    }

    void BuildRefreshRateList()
    {
        if (!ddRefreshRate) return;
        ddRefreshRate.ClearOptions();
        ddRefreshRate.AddOptions(refreshRates.Select(r => $"{r}hz").ToList());
    }

    // ======================================================
    // 설정 적용
    // ======================================================

    public void ApplyGraphics()
    {
        // 화면 모드만 적용 (해상도는 변경 안 함)
        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
        if (ddDisplayMode)
        {
            switch (ddDisplayMode.value)
            {
                case 1: mode = FullScreenMode.Windowed; break;
                case 2: mode = FullScreenMode.FullScreenWindow; break;
            }
            Screen.fullScreenMode = mode;
            PlayerPrefs.SetInt(K_MODE, (int)mode);
        }

        // 주사율 적용
        if (ddRefreshRate)
        {
            int rr = refreshRates[Mathf.Clamp(ddRefreshRate.value, 0, refreshRates.Length - 1)];
            Application.targetFrameRate = rr;
            QualitySettings.vSyncCount = 0;
            PlayerPrefs.SetInt(K_RR, rr);
        }
        
        PlayerPrefs.Save();
    }

    public void ApplyAudio()
    {
        float vMaster = slMaster ? slMaster.value : DEFAULT_MASTER * 100f;
        float vMusic = slMusic ? slMusic.value : DEFAULT_MUSIC * 100f;

        AudioListener.volume = vMaster / 100f;
        SoundManager.I?.SetMusicVolume(vMusic / 100f);

        PlayerPrefs.SetFloat(K_VM, vMaster);
        PlayerPrefs.SetFloat(K_VB, vMusic);

        UpdateMasterText(vMaster);
        UpdateMusicText(vMusic);
    }

    // ======================================================
    // 값 불러오기
    // ======================================================

    void LoadSettings()
    {
        int m = PlayerPrefs.GetInt(K_MODE, DEFAULT_MODE);
        int rr = PlayerPrefs.GetInt(K_RR, DEFAULT_REFRESH);

        if (ddDisplayMode)
        {
            FullScreenMode fm = (FullScreenMode)m;
            ddDisplayMode.value = fm switch
            {
                FullScreenMode.Windowed => 1,
                FullScreenMode.FullScreenWindow => 2,
                _ => 0,
            };
        }

        if (ddRefreshRate)
        {
            int iRR = System.Array.IndexOf(refreshRates, rr);
            ddRefreshRate.value = (iRR >= 0 ? iRR : refreshRates.Length - 1);
        }

        if (slMaster) slMaster.value = PlayerPrefs.GetFloat(K_VM, DEFAULT_MASTER * 100f);
        if (slMusic) slMusic.value = PlayerPrefs.GetFloat(K_VB, DEFAULT_MUSIC * 100f);
    }

    // ======================================================
    // 초기화 버튼
    // ======================================================

    public void ResetToDefault()
    {
        PlayerPrefs.DeleteKey(K_MODE);
        PlayerPrefs.DeleteKey(K_RR);
        PlayerPrefs.DeleteKey(K_VM);
        PlayerPrefs.DeleteKey(K_VB);
        PlayerPrefs.Save();

        LoadSettings();
        ApplyGraphics();
        ApplyAudio();
    }

    public void UpdateMasterText(float v)
    {
        if (txtMaster && slMaster) txtMaster.text = ((int)slMaster.value).ToString();
    }

    public void UpdateMusicText(float v)
    {
        if (txtMusic && slMusic) txtMusic.text = ((int)slMusic.value).ToString();
    }

    void FixDropdownHeight(TMP_Dropdown dd, int itemCount)
    {
        if (dd == null || dd.template == null) return;

        var template = dd.template;
        var viewport = template.Find("Viewport") as RectTransform;
        if (viewport == null) return;
        var content = viewport.Find("Content") as RectTransform;
        if (content == null) return;

        float itemHeight = dd.itemText.rectTransform.rect.height;
        if (itemHeight < 20f) itemHeight = 30f;

        float fullHeight = itemHeight * itemCount;
        float maxHeight = 300f;
        float finalHeight = Mathf.Min(fullHeight, maxHeight);

        template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);
        viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fullHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    public void OpenSettings() => gameObject.SetActive(true);
    public void CloseSettings() => gameObject.SetActive(false);
}
