using UnityEngine;
using System.Collections.Generic;

public class player_attack_hit : MonoBehaviour
{
    [Header("攻撃判定（子オブジェクトのColliderを使用）")]
    [SerializeField] private Collider hitbox;

    [Header("攻撃力（現在値）")]
    public float attackPower = 10f;

    private bool isActive = false;
    private int currentAttackIndex = 0; // 1,2,3段目など

    private HashSet<Collider> hitTargets = new HashSet<Collider>();

    void Start()
    {
        if (hitbox == null)
            hitbox = GetComponentInChildren<Collider>();

        if (hitbox != null)
        {
            hitbox.enabled = false;
            hitbox.isTrigger = true;
        }
        else
        {
            Debug.LogWarning("[player_attack_hit] ヒットボックスが設定されていません。");
        }
    }

    // 攻撃力と段階を設定
    public void SetAttackPower(float value, int attackIndex)
    {
        attackPower = value;
        currentAttackIndex = attackIndex;
    }

    public void EnableHitbox()
    {
        if (hitbox == null) return;
        hitbox.enabled = true;
        isActive = true;
        hitTargets.Clear();
        Debug.Log("[player_attack_hit] 攻撃判定 ON");
    }

    public void DisableHitbox()
    {
        if (hitbox == null) return;
        hitbox.enabled = false;
        isActive = false;
        hitTargets.Clear();
        Debug.Log("[player_attack_hit] 攻撃判定 OFF");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive || other == null || hitTargets.Contains(other)) return;

        var damageable = other.GetComponent<IHitReceiver>();
        if (damageable != null)
        {
            Debug.Log("[player_attack_hit] BossHealth を検出。ダメージを与えます。");
            damageable.OnHit(attackPower, transform.position, currentAttackIndex);
            hitTargets.Add(other);
        }
    }
}
