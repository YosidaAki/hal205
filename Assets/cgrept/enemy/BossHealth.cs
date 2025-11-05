using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour, IHitReceiver
{
    [Header("HP設定")]
    public float maxHP = 500f;
    private float currentHP;

    [Header("共通HPバーUI")]
    public Slider hpSlider;

    [Header("死亡時エフェクト")]
    public GameObject deathEffect;

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
        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);

        if (hpSlider != null)
            hpSlider.value = currentHP;

        Debug.Log($"[BossHealth] HP: {currentHP}/{maxHP}");

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        if (hpSlider != null)
            hpSlider.gameObject.SetActive(false);

        Destroy(gameObject, 1f);
    }
}
