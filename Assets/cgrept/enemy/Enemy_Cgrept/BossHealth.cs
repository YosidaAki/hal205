using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

public class BossHealth : MonoBehaviour, IHitReceiver
{
    [Header("HP設定")]
    public float maxHP = 1000f;
    [SerializeField] private float currentHP;

    [Header("UI設定（単一または複数スライダー対応）")]
    [Tooltip("単一バー用スライダー")]
    public Slider hpSlider;  // 旧仕様（1本だけ）
    [Tooltip("複数バーの場合はここに登録（例：赤→黄→緑）")]
    public List<Slider> hpSliders = new List<Slider>();

    [Header("死亡時エフェクト")]
    public GameObject deathEffect;

    [Header("イベント通知")]
    public UnityEvent<float> onHealthChanged; // HP割合を通知
    public UnityEvent onBossDefeated;

    [Header("デバッグ")]
    public bool showDebugLog = true;

    private bool isDead = false;
    private bool useMultiBars = false;

    // 複数バー用
    private float hpPerBar;
    private List<Image> fillImages = new List<Image>();
    private List<Color> originalColors = new List<Color>();

    void Start()
    {
        currentHP = maxHP;
        useMultiBars = (hpSliders != null && hpSliders.Count > 0);

        if (useMultiBars)
        {
            InitializeMultiBars();
        }
        else if (hpSlider != null)
        {
            InitializeSingleBar();
        }
    }

    void InitializeSingleBar()
    {
        hpSlider.maxValue = maxHP;
        hpSlider.value = maxHP;
    }

    void InitializeMultiBars()
    {
        hpPerBar = maxHP / hpSliders.Count;

        foreach (var slider in hpSliders)
        {
            if (slider == null) continue;

            slider.maxValue = hpPerBar;
            slider.value = hpPerBar;

            // Fill部分の色を記録
            Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
            if (fill != null)
            {
                fillImages.Add(fill);
                originalColors.Add(fill.color);
            }
            else
            {
                fillImages.Add(null);
                originalColors.Add(Color.white);
            }
        }
    }

    public void OnHit(float attackPower, Vector3 hitPos, int attackIndex = 0)
    {
        TakeDamage(attackPower);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);

        onHealthChanged?.Invoke(currentHP / maxHP);

        if (useMultiBars)
            UpdateMultiBars();
        else if (hpSlider != null)
            hpSlider.value = currentHP;

        if (showDebugLog)
            Debug.Log($"[BossHealth] -{damage:F1} → {currentHP}/{maxHP}");

        if (currentHP <= 0f)
            Die();
    }

    void UpdateMultiBars()
    {
        float remainingHP = currentHP;

        for (int i = 0; i < hpSliders.Count; i++)
        {
            if (hpSliders[i] == null) continue;

            if (remainingHP >= hpPerBar)
            {
                hpSliders[i].value = hpPerBar;
            }
            else
            {
                hpSliders[i].value = remainingHP;
            }

            // 元の色を維持
            if (fillImages[i] != null)
                fillImages[i].color = originalColors[i];

            remainingHP -= hpPerBar;
            if (remainingHP <= 0f) break;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // 全バーを非表示
        if (useMultiBars)
        {
            foreach (var s in hpSliders)
            {
                if (s != null) s.gameObject.SetActive(false);
            }
        }
        else if (hpSlider != null)
        {
            hpSlider.gameObject.SetActive(false);
        }

        onBossDefeated?.Invoke();
        if (showDebugLog) Debug.Log("[BossHealth] Boss defeated!");
        Destroy(gameObject, 1.5f);
    }
}
