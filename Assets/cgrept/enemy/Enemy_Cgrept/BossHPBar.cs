using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BossHPBar : MonoBehaviour
{
    [Header("対応するスライダー")]
    [SerializeField] private Slider hpSlider;

    [Header("バーの色を引き継ぐ（自動取得）")]
    [SerializeField] private Image fillImage;
    private Color originalColor;

    [Header("HP設定（自動）")]
    [SerializeField] private float maxHP;
    [SerializeField] private float currentHP;

    [Header("減少アニメーション設定")]
    [Tooltip("値を補間して滑らかに減少させる速度")]
    public float smoothSpeed = 5f;

    private float targetHP;

    public void Initialize(float max)
    {
        if (hpSlider == null)
            hpSlider = GetComponentInChildren<Slider>();

        if (hpSlider == null)
        {
            Debug.LogWarning($"[BossHPBar] スライダーが見つかりません: {name}");
            return;
        }

        maxHP = max;
        currentHP = max;
        targetHP = max;

        hpSlider.maxValue = max;
        hpSlider.value = max;

        // Fill部分のImageを取得して色を保存
        if (fillImage == null && hpSlider.fillRect != null)
            fillImage = hpSlider.fillRect.GetComponent<Image>();

        if (fillImage != null)
            originalColor = fillImage.color;
    }

    /// <summary>
    /// HP値を即時設定
    /// </summary>
    public void SetHP(float hp)
    {
        targetHP = Mathf.Clamp(hp, 0, maxHP);
    }

    void Update()
    {
        if (hpSlider == null) return;

        // スムーズに値を補間
        currentHP = Mathf.Lerp(currentHP, targetHP, Time.deltaTime * smoothSpeed);
        hpSlider.value = currentHP;

        // 元の色を維持
        if (fillImage != null)
            fillImage.color = originalColor;
    }
}

