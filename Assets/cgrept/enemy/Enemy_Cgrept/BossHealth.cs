using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class BossHealth : MonoBehaviour, IHitReceiver
{
    [Header("1ゲージあたりのHP設定")]
    public float maxHP = 1000f;
    private float currentHP;

    [Header("親HPスライダー（実際に減るバー）")]
    public Slider mainSlider;

    [Header("子HPスライダー（ストックとして並べる）")]
    [Tooltip("左から順に登録（例：緑→黄→赤）")]
    public List<Slider> stockSliders = new List<Slider>();

    [Header("死亡演出関連")]
    public Animator bossAnimator;                // ボスAnimator
    public string deathAnimationTrigger = "Die"; // 死亡アニメーションのトリガー名
    public GameObject deathEffect;               // 死亡エフェクト（任意）

    [Header("イベント通知")]
    public UnityEvent<float> onHealthChanged;
    public UnityEvent onBossDefeated;

    [Header("デバッグ")]
    public bool showDebugLog = true;

    private int currentStockIndex = 0;  // 現在のゲージインデックス
    private bool isDead = false;
    private Image mainFill;

    void Start()
    {
        InitializeHP();
    }

    void InitializeHP()
    {
        if (mainSlider == null)
        {
            Debug.LogError("[BossHealthKingdom] mainSlider が設定されていません。");
            return;
        }

        if (stockSliders == null || stockSliders.Count == 0)
        {
            Debug.LogError("[BossHealthKingdom] stockSliders が設定されていません。");
            return;
        }

        currentStockIndex = 0;
        currentHP = maxHP;

        mainSlider.maxValue = maxHP;
        mainSlider.value = maxHP;

        // Fillを取得して初期色設定
        mainFill = mainSlider.fillRect.GetComponent<Image>();
        UpdateMainColor();

        // 子スライダー全てを満タンに
        foreach (var s in stockSliders)
        {
            if (s != null)
            {
                s.maxValue = maxHP;
                s.value = maxHP;
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
        mainSlider.value = currentHP;

        onHealthChanged?.Invoke(currentHP / maxHP);

        if (showDebugLog)
            Debug.Log($"[BossHealthKingdom] -{damage:F1} → {currentHP}/{maxHP} (ゲージ {currentStockIndex + 1}/{stockSliders.Count})");

        // HPが0になった時の処理
        if (currentHP <= 0f)
        {
            if (currentStockIndex < stockSliders.Count)
            {
                // 現在の子スライダーを空に
                stockSliders[currentStockIndex].value = 0f;
            }

            if (currentStockIndex < stockSliders.Count - 1)
            {
                //次のゲージへ切り替え
                currentStockIndex++;
                currentHP = maxHP;
                mainSlider.value = maxHP;
                UpdateMainColor();

                if (showDebugLog)
                    Debug.Log($"[BossHealthKingdom] ▶ 次のゲージへ切り替え ({currentStockIndex + 1}/{stockSliders.Count})");
            }
            else
            {
                //最後のゲージが尽きた
                // → 親バーを完全に空（0）にして停止
                mainSlider.value = 0f;
                Die();
            }
        }
    }

    void UpdateMainColor()
    {
        if (mainFill == null || stockSliders.Count == 0) return;

        // 現在のゲージのスライダーのFill色を取得
        Image stockFill = stockSliders[currentStockIndex].fillRect.GetComponent<Image>();
        if (stockFill != null)
            mainFill.color = stockFill.color;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // エフェクト再生
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // 死亡アニメーション再生
        if (bossAnimator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
            bossAnimator.SetTrigger(deathAnimationTrigger);

        onBossDefeated?.Invoke();

            if (showDebugLog)
                Debug.Log("[BossHealthKingdom] 💀 Boss defeated!");

        // UIは消さずそのまま表示（赤バーが完全に空の状態で停止）
    }
}