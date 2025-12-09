using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    readonly int[] refreshRates = { 60, 90, 120, 144, 165, 240 };

    void Start()
    {
        BuildResolutionList();
        BuildDisplayModeList();
        BuildRefreshRateList();
        LoadSettings();
        ApplyGraphics();
        ApplyAudio();
    }

    // ======================================================
    // UI 구성
    // ======================================================

    void BuildResolutionList()
    {
        resList = Screen.resolutions
            .Select(r => new Resolution { width = r.width, height = r.height })
            .Distinct(new ResComparer())
            .OrderBy(r => r.width * r.height)
            .ToArray();

        ddResolution.ClearOptions();
        ddResolution.AddOptions(resList.Select(r => $"{r.width} x {r.height}").ToList());
    }

    void BuildDisplayModeList()
    {
        ddDisplayMode.ClearOptions();
        ddDisplayMode.AddOptions(new List<string> { "전체 화면", "창 모드", "전체 창 모드" });
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
        // 해상도
        int iRes = ddResolution.value;
        var r = resList[Mathf.Clamp(iRes, 0, resList.Length - 1)];

        // 화면 모드
        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
        switch (ddDisplayMode.value)
        {
            case 1: mode = FullScreenMode.Windowed; break;
            case 2: mode = FullScreenMode.FullScreenWindow; break;
        }

        Screen.SetResolution(r.width, r.height, mode);

        // FPS = 주사율 동기화
        int rr = refreshRates[Mathf.Clamp(ddRefreshRate.value, 0, refreshRates.Length - 1)];
        Application.targetFrameRate = rr;
        QualitySettings.vSyncCount = 0;

        // 저장
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

        AudioListener.volume = vMaster;  // 전체 볼륨 기본 시스템 반영

        SoundManager.I?.SetMusicVolume(vMusic);
        SoundManager.I?.SetMenuVolume(vMenu);

        PlayerPrefs.SetFloat(K_VM, vMaster);
        PlayerPrefs.SetFloat(K_VB, vMusic);
        PlayerPrefs.SetFloat(K_VMENU, vMenu);
        PlayerPrefs.Save();
    }

    // ======================================================
    // 불러오기
    // ======================================================

    void LoadSettings()
    {
        int w = PlayerPrefs.GetInt(K_W, DEFAULT_W);
        int h = PlayerPrefs.GetInt(K_H, DEFAULT_H);
        int m = PlayerPrefs.GetInt(K_MODE, DEFAULT_MODE);
        int rr = PlayerPrefs.GetInt(K_RR, DEFAULT_REFRESH);

        // 해상도 선택
        int iRes = Array.FindIndex(resList, r => r.width == w && r.height == h);
        ddResolution.value = (iRes >= 0 ? iRes : 0);

        // 모드 선택
        FullScreenMode fm = (FullScreenMode)m;
        ddDisplayMode.value = fm switch
        {
            FullScreenMode.Windowed => 1,
            FullScreenMode.FullScreenWindow => 2,
            _ => 0,
        };

        // 주사율 선택
        int iRR = Array.IndexOf(refreshRates, rr);
        ddRefreshRate.value = (iRR >= 0 ? iRR : 0);

        // 오디오
        slMaster.value = PlayerPrefs.GetFloat(K_VM, DEFAULT_MASTER);
        slMusic.value = PlayerPrefs.GetFloat(K_VB, DEFAULT_MUSIC);
        slMenu.value = PlayerPrefs.GetFloat(K_VMENU, DEFAULT_MENU);
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
        if (txtMaster) txtMaster.text = Mathf.RoundToInt(v * 100).ToString();
    }

    public void UpdateMusicText(float v)
    {
        if (txtMusic) txtMusic.text = Mathf.RoundToInt(v * 100).ToString();
    }

    public void UpdateMenuText(float v)
    {
        if (txtMenu) txtMenu.text = Mathf.RoundToInt(v * 100).ToString();
    }

    public void OpenSettings()
    {
        gameObject.SetActive(true);
    }

    public void CloseSettings()
    {
        gameObject.SetActive(false);
    }

    // ======================================================
    class ResComparer : IEqualityComparer<Resolution>
    {
        public bool Equals(Resolution a, Resolution b) => a.width == b.width && a.height == b.height;
        public int GetHashCode(Resolution r) => r.width ^ r.height;
    }
}
