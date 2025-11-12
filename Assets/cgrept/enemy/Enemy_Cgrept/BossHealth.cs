using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    [Header("Inspectorでレールを設定")]
    public RailSpline railToTrigger; // Inspectorでレールを設定
    public int   railIndex = 0; // 使用するレールのインデックス
    public float railStartT = 0f;
    public float railSpeed = 2f;
    public float delayBeforeRail = 1.0f; // 待機秒数

    [Header("死亡演出関連")]
    public Animator bossAnimator;
    public string deathAnimationTrigger = "Die";
    public GameObject deathEffect;

    [Header("イベント通知")]
    public UnityEvent<float> onHealthChanged;
    public UnityEvent onBossDefeated;

    [Header("HPゲージ数イベント")]
    [Tooltip("残り1ゲージになった時に呼ばれるイベント（ボス演出など）")]
    public UnityEvent onLastGaugeStart;

    [Header("デバッグ")]
    public bool showDebugLog = true;

    private int currentStockIndex = 0;
    private bool isDead = false;
    private Image mainFill;
    public UnityEngine.Events.UnityEvent onLastGaugeReached;
    void Start()
    {
        InitializeHP();
    }

    void InitializeHP()
    {
        if (mainSlider == null || stockSliders.Count == 0)
        {
            Debug.LogError("[BossHealth] mainSlider or stockSliders 未設定。");
            return;
        }

        currentStockIndex = 0;
        currentHP = maxHP;

        mainSlider.maxValue = maxHP;
        mainSlider.value = maxHP;

        mainFill = mainSlider.fillRect.GetComponent<Image>();
        UpdateMainColor();

        foreach (var s in stockSliders)
        {
            s.maxValue = maxHP;
            s.value = maxHP;
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

        // HPが0になった時の処理
        if (currentHP <= 0f)
        {
            if (currentStockIndex < stockSliders.Count)
            {
                stockSliders[currentStockIndex].value = 0f;
            }

            // --- ここでチェック ---
            if (currentStockIndex < stockSliders.Count - 1)
            {
                // レールアニメーションを開始
                if (railToTrigger != null && currentStockIndex == railIndex)
                {
                    railToTrigger.TriggerRailAppearance();
                }
                currentStockIndex++;
                currentHP = maxHP;
                mainSlider.value = maxHP;
                UpdateMainColor();
            }
            else
            {
                // 最後のゲージが尽きた
                mainSlider.value = 0f;
                Die();
            }
        }
    }

    void UpdateMainColor()
    {
        if (mainFill == null || stockSliders.Count == 0) return;

        Image stockFill = stockSliders[currentStockIndex].fillRect.GetComponent<Image>();
        if (stockFill != null)
            mainFill.color = stockFill.color;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        if (bossAnimator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
            bossAnimator.SetTrigger(deathAnimationTrigger);

        onBossDefeated?.Invoke();

        if (showDebugLog)
            Debug.Log("[BossHealth] 💀 Boss defeated!");
    }

    private IEnumerator StartRailAfterDelay()
    {
        // 任意の演出待機時間
        yield return new WaitForSeconds(delayBeforeRail);

        // PlayerMover を取得してレール開始
        var player = Object.FindFirstObjectByType<RailMover>();
        if (player != null && railToTrigger != null)
        {
            player.StartRail(railToTrigger, railStartT, railSpeed);
            Debug.Log("[BossHealth] レールアニメーション開始");
        }

        onLastGaugeReached?.Invoke();
    }

}
