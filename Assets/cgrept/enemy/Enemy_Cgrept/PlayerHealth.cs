using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("体力設定")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI設定")]
    public Slider healthBar;
    public Gradient healthGradient;      // HPに応じた色変化用
    public Image fillImage;              // Slider の Fill にある Image

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;

            if (fillImage != null)
                fillImage.color = healthGradient.Evaluate(1f); // 100%の色
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
            if (fillImage != null)
            {
                float normalized = currentHealth / maxHealth;
                fillImage.color = healthGradient.Evaluate(normalized);
            }
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("プレイヤーは倒れました…💀");
        Destroy(gameObject);
    }
}
