using UnityEngine;

public class Player_die : MonoBehaviour
{
    [Header("Death Debug")]
    public bool EnableDeathDebug = true; // ON = HP0なら死ぬ, OFF = HP0でも死なない

    [Header("Animator Settings")]
    public Animator animator;
    public string dieStateName = "Die1";
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

        float hp = ReadHP();
        if (!isDead && hp <= 0f)
        {
            Die();
        }
    }

    float ReadHP()
    {
        if (hpbar.Instance == null) return 1f;

        // Slider にアクセスせず、内部の currentHP を返すよう hpbar 側に関数を作る想定
        return hpbar.Instance.GetCurrentHP();
    }

    void Die()
    {
        isDead = true;

        DisablePlayerControls();
        animator.CrossFadeInFixedTime(dieStateName, 0.1f, layer);

        Debug.Log("Player has died.");
    }

    void DisablePlayerControls()
    {
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
