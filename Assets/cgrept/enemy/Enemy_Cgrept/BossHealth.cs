using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class BossHealth : MonoBehaviour, IHitReceiver
{
    [Header("HP設定")]
    public float maxHP = 1000f;
    [SerializeField] private float currentHP;

    [Header("UI設定")]
    public Slider hpSlider;

    [Header("死亡時エフェクト")]
    public GameObject deathEffect;

    [Header("イベント通知")]
    public UnityEvent<float> onHealthChanged; // HP割合を通知
    public UnityEvent onBossDefeated;

    [Header("デバッグ")]
    public bool showDebugLog = true;
    private bool isDead = false;

    void Start()
    {
        currentHP = maxHP;
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = maxHP;
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

        if (hpSlider != null) hpSlider.value = currentHP;
        onHealthChanged?.Invoke(currentHP / maxHP);

        if (showDebugLog)
            Debug.Log($"[BossHealth] -{damage:F1} → {currentHP}/{maxHP}");

        if (currentHP <= 0f) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        if (hpSlider != null)
            hpSlider.gameObject.SetActive(false);

        onBossDefeated?.Invoke();
        if (showDebugLog) Debug.Log("[BossHealth] Boss defeated!");
        Destroy(gameObject, 1.5f);
    }
}
