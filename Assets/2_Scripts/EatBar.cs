// EatBar.cs (핵심 변경만)
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EatBar : MonoBehaviour
{
    public static EatBar Instance { get; private set; }

    [SerializeField] Image fillImage;
    [SerializeField] TextMeshProUGUI valueText;

    [SerializeField] int maxFullness = 100;
    [SerializeField] int startFullness = 50;
    [SerializeField] float drainPerSecond = 2f;
    [SerializeField] float minSpoilFactor = 0.3f;
    [SerializeField] float spoilFullSeconds = 180f;

    int current;
    float t0;
    float drainAcc = 0f;
    bool notifiedDeath = false;  // ⬅️ 중복 호출 방지

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        current = Mathf.Clamp(startFullness, 0, maxFullness);
        t0 = Time.time;
        UpdateUI();
    }

    void Update()
    {
        if (current <= 0) return;

        drainAcc += drainPerSecond * Time.deltaTime;
        if (drainAcc >= 1f)
        {
            int dec = Mathf.FloorToInt(drainAcc);
            current = Mathf.Max(0, current - dec);
            drainAcc -= dec;

            UpdateUI();

            if (current <= 3 && !notifiedDeath)
            {
                notifiedDeath = true;
                var p = GameObject.FindGameObjectWithTag("Player");
                var player = p ? p.GetComponent<Player>() : null;
                player?.DieFromHunger();
            }
        }
    }

    void UpdateUI()
    {
        float f = Mathf.Clamp01((float)current / maxFullness);
        if (fillImage) fillImage.fillAmount = f;
        if (valueText) valueText.text = $"{current} / {maxFullness}";
    }

    public void AddFromEat(int baseGain)
    {
        if (current <= 0 || baseGain <= 0) return;
        float t = Mathf.Clamp01((Time.time - t0) / Mathf.Max(1f, spoilFullSeconds));
        float factor = Mathf.Lerp(1f, minSpoilFactor, t);
        int gain = Mathf.Max(1, Mathf.RoundToInt(baseGain * factor));
        current = Mathf.Min(maxFullness, current + gain);
        notifiedDeath = false; // 회복했으면 다시 살 수 있으니 플래그 리셋
        UpdateUI();
    }

    public void AddRaw(int delta)
    {
        current = Mathf.Clamp(current + delta, 0, maxFullness);
        if (current > 0) notifiedDeath = false;
        UpdateUI();
    }
}
