using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour, IHitReceiver
{
    [Header("HP設定")]
    [Tooltip("ボスの最大HP")]
    public float maxHP = 500f;

    [SerializeField, Tooltip("現在のHP（デバッグ確認用）")]
    private float currentHP;

    [Header("共通HPバーUI")]
    [Tooltip("全パーツ共通のHPバー")]
    public Slider hpSlider;

    [Header("死亡時エフェクト")]
    [Tooltip("死亡時に生成されるエフェクト")]
    public GameObject deathEffect;

    [Header("デバッグ出力")]
    public bool showDebugLog = true;

    void Start()
    {
        currentHP = maxHP;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = maxHP;
        }
    }

    /// <summary>
    /// IHitReceiver から呼ばれる（部位経由または直接攻撃）
    /// </summary>
    public void OnHit(float attackPower, Vector3 hitPos, int attackIndex = 0)
    {
        TakeDamage(attackPower);
    }

    /// <summary>
    /// ダメージを与える共通関数
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (currentHP <= 0f) return; // 既に死亡済みなら無視

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);

        if (hpSlider != null)
            hpSlider.value = currentHP;

        if (showDebugLog)
            Debug.Log($"[BossHealth] took {damage:F1} damage → HP: {currentHP}/{maxHP}");

        if (currentHP <= 0f)
            Die();
    }

    void Die()
    {
        if (showDebugLog)
            Debug.Log("[BossHealth] Boss Defeated!");

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        if (hpSlider != null)
            hpSlider.gameObject.SetActive(false);

        Destroy(gameObject, 1f);
    }
}
