using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem; // 新Input System対応

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
    public RailSpline railToTrigger;
    public int railIndex = 0;
    public float railStartT = 0f;
    public float railSpeed = 2f;
    public float delayBeforeRail = 1.0f;

    [Header("死亡演出関連")]
    public Animator bossAnimator;                     // 🔹 Spider_Armature の Animator
    public string deathAnimationName = "Spider_Armature|die"; // 🔹 追加：再生するアニメーション名
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

    void Update()
    {
        // 🔹 Hキーで500ダメージ（テスト用）
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            TakeDamage(5000f);
            Debug.Log("[BossHealth] テスト: HキーでHP -500");
        }
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

        if (currentHP <= 0f)
        {
            if (currentStockIndex < stockSliders.Count)
                stockSliders[currentStockIndex].value = 0f;

            // 🔹 残りゲージの処理
            if (currentStockIndex < stockSliders.Count - 1)
            {
                if (railToTrigger != null && currentStockIndex == railIndex)
                    railToTrigger.TriggerRailAppearance();

                currentStockIndex++;
                currentHP = maxHP;
                mainSlider.value = maxHP;
                UpdateMainColor();
            }
            else
            {
                // 🔹 最後のゲージが尽きたら死亡処理
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

        // 🔹 死亡アニメーションを再生
        if (bossAnimator != null)
        {
            bossAnimator.Play(deathAnimationName, 0, 0f);
            Debug.Log("[BossHealth] 死亡アニメーション再生: " + deathAnimationName);
        }
        else
        {
            Debug.LogWarning("[BossHealth] Animator が設定されていません。");
        }

        // 🔹 死亡エフェクト
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        onBossDefeated?.Invoke();

        if (showDebugLog)
            Debug.Log("[BossHealth] 💀 Boss defeated!");
    }

    private IEnumerator StartRailAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeRail);

        var player = Object.FindFirstObjectByType<RailMover>();
        if (player != null && railToTrigger != null)
        {
            player.StartRail(railToTrigger, railStartT, railSpeed);
            Debug.Log("[BossHealth] レールアニメーション開始");
        }

        onLastGaugeReached?.Invoke();
    }
}
