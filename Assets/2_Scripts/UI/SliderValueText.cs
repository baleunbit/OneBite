using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 슬라이더 값을 텍스트로 표시 (0~100 또는 0~20 등)
/// </summary>
public class SliderValueText : MonoBehaviour
{
    [Header("연결")]
    public Slider slider;
    public TextMeshProUGUI valueText;  // TMP 사용
    
    [Header("표시 설정")]
    public float displayMultiplier = 20f;  // 0~1 → 0~20으로 표시
    public string format = "0";            // 소수점 없이 정수로 표시
    
    void Start()
    {
        if (slider)
        {
            slider.onValueChanged.AddListener(UpdateText);
            UpdateText(slider.value);
        }
    }
    
    void UpdateText(float value)
    {
        if (valueText)
        {
            float displayValue = value * displayMultiplier;
            valueText.text = displayValue.ToString(format);
        }
    }
}

