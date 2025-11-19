using UnityEngine;

public class Player_die : MonoBehaviour
{
    [Header("Death Debug")]
    public bool EnableDeathDebug = true; // ON = HP0なら死ぬ, OFF = HP0でも死なない

    [Header("Animator Settings")]
    public Animator animator;
    public string dieStateName = "Die1";   // 再生する死亡アニメ
    public int layer = 0;

    private bool isDead = false;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!EnableDeathDebug) return;

        float hp = hpbar.Instance != null ? GetCurrentHP() : 1f;

        if (!isDead && hp <= 0f)
        {
            Die();
        }
    }

    float GetCurrentHP()
    {
        // hpbar.cs の currentHP を反映させるには Reflection が必要なので
        // こちらは UI スライダーを参照して現在HPを取得する簡易版
        return hpbar.Instance.GetCurrentHP();
    }

    void Die()
    {
        isDead = true;

        // 移動・攻撃・ガードを停止
        DisablePlayerControls();

        // 死亡アニメ再生
        animator.CrossFadeInFixedTime(dieStateName, 0.1f, layer);

        Debug.Log("Player has died.");
    }

    void DisablePlayerControls()
    {
        // PlayerMovement や攻撃スクリプトを止める
        var pm = GetComponent<PlayerMovement>();
        if (pm) pm.enabled = false;

        var pMove = GetComponent<player_Move>();
        if (pMove) pMove.enabled = false;

        var atk = GetComponent<player_attack>();
        if (atk) atk.enabled = false;

        var guard = GetComponent<PlayerGuard>();
        if (guard) guard.enabled = false;
    }
}
