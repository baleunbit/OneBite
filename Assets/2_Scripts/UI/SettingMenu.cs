using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingMenu : MonoBehaviour
{
    [Header("Graphics UI")]
    public TMP_Dropdown ddResolution;
    public TMP_Dropdown ddDisplayMode;
    public TMP_Dropdown ddRefreshRate;

    [Header("Audio UI")]
    public Slider slMaster;
    public Slider slMusic;
    public Slider slMenu;

    [Header("Text UI")]
    public TMP_Text txtMaster;
    public TMP_Text txtMusic;
    public TMP_Text txtMenu;

    // 기본값
    const int DEFAULT_W = 1920;
    const int DEFAULT_H = 1080;
    const int DEFAULT_MODE = (int)FullScreenMode.ExclusiveFullScreen;
    const int DEFAULT_REFRESH = 240;
    const float DEFAULT_MASTER = 0.5f;
    const float DEFAULT_MUSIC = 1f;
    const float DEFAULT_MENU = 1f;

    // PlayerPrefs Keys
    const string K_W = "SET_W";
    const string K_H = "SET_H";
    const string K_MODE = "SET_MODE";
    const string K_RR = "SET_RR";
    const string K_VM = "SET_VOL_MASTER";
    const string K_VB = "SET_VOL_MUSIC";
    const string K_VMENU = "SET_VOL_MENU";

    Resolution[] resList;

    // ✔ 네가 요청한 주사율 목록만 사용
    readonly int[] refreshRates = { 60, 90, 120, 144, 165, 180, 200, 240 };
    
    bool initialized = false;

    void Awake()
    {
        Initialize();
    }
    
    void OnEnable()
    {
        // 패널이 활성화될 때마다 저장된 설정 불러오기
        if (initialized)
        {
            LoadSettings();
        }
    }
    
    void Initialize()
    {
        if (initialized) return;
        initialized = true;
        
        BuildResolutionList();
        BuildDisplayModeList();
        BuildRefreshRateList();

        FixDropdownHeight(ddResolution, resList.Length);
        FixDropdownHeight(ddDisplayMode, ddDisplayMode.options.Count);
        FixDropdownHeight(ddRefreshRate, refreshRates.Length);

        // 드롭다운 값 변경 시 자동 적용
        ddResolution.onValueChanged.AddListener(_ => ApplyGraphics());
        ddDisplayMode.onValueChanged.AddListener(_ => ApplyGraphics());
        ddRefreshRate.onValueChanged.AddListener(_ => ApplyGraphics());

        // 슬라이더 값 변경 시 자동 적용
        slMaster.onValueChanged.AddListener(_ => ApplyAudio());
        slMusic.onValueChanged.AddListener(_ => ApplyAudio());
        slMenu.onValueChanged.AddListener(_ => ApplyAudio());

        LoadSettings();
        ApplyGraphics();
        ApplyAudio();
    }


    // ======================================================
    // UI 구성
    // ======================================================

    void BuildResolutionList()
    {
        var custom = new List<Resolution>
        {
            new Resolution { width = 1280, height = 720 },
            new Resolution { width = 1440, height = 1080 },
            new Resolution { width = 1680, height = 1050 },
            new Resolution { width = 1728, height = 1080 },
            new Resolution { width = 1920, height = 1080 },
            new Resolution { width = 2560, height = 1440 },
            new Resolution { width = 3440, height = 1440 }
        };

        resList = custom.ToArray();

        ddResolution.ClearOptions();
        ddResolution.AddOptions(
            custom.Select(r => $"{r.width} x {r.height}").ToList()
        );
    }

    void BuildDisplayModeList()
    {
        ddDisplayMode.ClearOptions();
        ddDisplayMode.AddOptions(new List<string>
        {
            "전체 화면",       // ExclusiveFullScreen
            "창 모드",         // Windowed
            "테두리 없는 창모드" // FullScreenWindow
        });
    }

    void BuildRefreshRateList()
    {
        ddRefreshRate.ClearOptions();
        ddRefreshRate.AddOptions(refreshRates.Select(r => $"{r}hz").ToList());
    }

    // ======================================================
    // 설정 적용
    // ======================================================

    public void ApplyGraphics()
    {
        int iRes = ddResolution.value;
        var r = resList[Mathf.Clamp(iRes, 0, resList.Length - 1)];

        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
        switch (ddDisplayMode.value)
        {
            case 1: mode = FullScreenMode.Windowed; break;
            case 2: mode = FullScreenMode.FullScreenWindow; break;
        }

        Screen.SetResolution(r.width, r.height, mode);

        int rr = refreshRates[Mathf.Clamp(ddRefreshRate.value, 0, refreshRates.Length - 1)];
        Application.targetFrameRate = rr;
        QualitySettings.vSyncCount = 0;

        PlayerPrefs.SetInt(K_W, r.width);
        PlayerPrefs.SetInt(K_H, r.height);
        PlayerPrefs.SetInt(K_MODE, (int)mode);
        PlayerPrefs.SetInt(K_RR, rr);
        PlayerPrefs.Save();
    }

    public void ApplyAudio()
    {
        float vMaster = slMaster.value;
        float vMusic = slMusic.value;
        float vMenu = slMenu.value;

        AudioListener.volume = vMaster / 100f;

        SoundManager.I?.SetMusicVolume(vMusic / 100f);
        SoundManager.I?.SetMenuVolume(vMenu / 100f);

        PlayerPrefs.SetFloat(K_VM, vMaster);
        PlayerPrefs.SetFloat(K_VB, vMusic);
        PlayerPrefs.SetFloat(K_VMENU, vMenu);

        UpdateMasterText(vMaster);
        UpdateMusicText(vMusic);
        UpdateMenuText(vMenu);
    }

    // ======================================================
    // 값 불러오기
    // ======================================================

    void LoadSettings()
    {
        int w = PlayerPrefs.GetInt(K_W, DEFAULT_W);
        int h = PlayerPrefs.GetInt(K_H, DEFAULT_H);
        int m = PlayerPrefs.GetInt(K_MODE, DEFAULT_MODE);
        int rr = PlayerPrefs.GetInt(K_RR, DEFAULT_REFRESH);

        int iRes = Array.FindIndex(resList, r => r.width == w && r.height == h);
        ddResolution.value = (iRes >= 0 ? iRes : 0);

        FullScreenMode fm = (FullScreenMode)m;
        ddDisplayMode.value = fm switch
        {
            FullScreenMode.Windowed => 1,
            FullScreenMode.FullScreenWindow => 2,
            _ => 0,
        };

        int iRR = Array.IndexOf(refreshRates, rr);
        ddRefreshRate.value = (iRR >= 0 ? iRR : 0);

        slMaster.value = PlayerPrefs.GetFloat(K_VM, DEFAULT_MASTER * 100f);
        slMusic.value = PlayerPrefs.GetFloat(K_VB, DEFAULT_MUSIC * 100f);
        slMenu.value = PlayerPrefs.GetFloat(K_VMENU, DEFAULT_MENU * 100f);
    }

    // ======================================================
    // 초기화 버튼
    // ======================================================

    public void ResetToDefault()
    {
        PlayerPrefs.DeleteKey(K_W);
        PlayerPrefs.DeleteKey(K_H);
        PlayerPrefs.DeleteKey(K_MODE);
        PlayerPrefs.DeleteKey(K_RR);
        PlayerPrefs.DeleteKey(K_VM);
        PlayerPrefs.DeleteKey(K_VB);
        PlayerPrefs.DeleteKey(K_VMENU);
        PlayerPrefs.Save();

        LoadSettings();
        ApplyGraphics();
        ApplyAudio();
    }

    public void UpdateMasterText(float v)
    {
        float real = slMaster.value;
        txtMaster.text = ((int)real).ToString();
    }

    public void UpdateMusicText(float v)
    {
        float real = slMusic.value;
        txtMusic.text = ((int)real).ToString();
    }

    public void UpdateMenuText(float v)
    {
        float real = slMenu.value;
        txtMenu.text = ((int)real).ToString();
    }

    void FixDropdownHeight(TMP_Dropdown dd, int itemCount)
    {
        if (dd == null || dd.template == null) return;

        // 템플릿 가져오기
        var template = dd.template;
        var viewport = template.Find("Viewport") as RectTransform;
        var content = viewport.Find("Content") as RectTransform;

        // 항목 하나의 높이 계산
        float itemHeight = dd.itemText.rectTransform.rect.height;
        if (itemHeight < 20f) itemHeight = 30f; // 최소 보정

        // 전체 리스트 길이 = 항목 높이 × 항목 개수
        float fullHeight = itemHeight * itemCount;

        // 템플릿이 보여줄 최대 Height
        float maxHeight = 300f;

        // 실제 template Height
        float finalHeight = Mathf.Min(fullHeight, maxHeight);

        // 템플릿 높이 수정
        template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);

        // viewport 높이 수정
        viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);

        // content 높이 수정
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fullHeight);

        // 스크롤 강제 재빌드
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    // 열기/닫기
    public void OpenSettings() => gameObject.SetActive(true);
    public void CloseSettings() => gameObject.SetActive(false);

    class ResComparer : IEqualityComparer<Resolution>
    {
        public bool Equals(Resolution a, Resolution b) =>
            a.width == b.width && a.height == b.height;

        public int GetHashCode(Resolution r) =>
            r.width ^ r.height;
    }
}
