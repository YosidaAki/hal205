using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[DisallowMultipleComponent]
public class player_attack_hit : MonoBehaviour
{
    [Header("攻撃判定コライダー（子に付ける）")]
    public Collider hitbox;

    [Header("攻撃管理スクリプト")]
    public player_attack attackController;

    [Header("ヒットストップ")]
    [Range(0f, 0.3f)] public float hitStopDuration = 0.06f;

    public bool showDebugLog = true;

    public Shatter shatter;

    private GyroShooter gyro;

    void Start()
    {
        if (hitbox != null)
        {
            hitbox.enabled = false;
            hitbox.isTrigger = true;
        }

        gyro = FindFirstObjectByType<GyroShooter>();
    }

    public void EnableHitbox()
    {
        hitbox.enabled = true;
        if (showDebugLog) Debug.Log("[Hitbox] ENABLED");
    }

    public void DisableHitbox()
    {
        hitbox.enabled = false;
        if (showDebugLog) Debug.Log("[Hitbox] DISABLED");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (attackController == null) return;

        // ★ 部位コライダーを取得（これが正しい）
        var partCol = other.GetComponent<BossPartCollider>();
        if (partCol == null) return;

        BossPartsManager manager = partCol.manager;
        BossPartData partData = partCol.partData;

        if (manager == null || partData == null) return;

        shatter.ShowForSeconds(1.3f); //shatter エフェクト
        // 攻撃力
        int attackIndex = attackController.GetCurrentAttackIndex();
        float basePower = attackController.GetCurrentAttackPower();
        float indexMultiplier = 1f;

        BossPartData part = partCol.partData;

        // 部位の基本倍率（弱点など）
        if (part != null)
        {
            // ★ 攻撃段階ごとの部位倍率
            var ds = part.damageSettings.Find(x => x.attackIndex == attackIndex);
            if (ds != null)
            {
                indexMultiplier = ds.damageMultiplier;
            }
            else
            {
                indexMultiplier = 2f; // 見つからないときは 1倍
            }
        }
        float finalPower =
        basePower *            // 通常 or チャージ攻撃の基本倍率
        indexMultiplier;       // 部位 × 攻撃段階の倍率
                                 //chargeMultiplier;    // 溜め時間による倍率（任意）
                                 // ★ Boss にダメージ処理を委譲（最重要）
        manager.ApplyDamage(partData, finalPower, transform.position, attackIndex);

        
        if (showDebugLog)
            Debug.Log($"[PlayerHit] part:{partData.partName} ダメージ:{basePower} 倍率:{indexMultiplier}");

        gyro.resetatkbar();
        StartCoroutine(HitStopCoroutine(hitStopDuration));
        DisableHitbox();
    }

    IEnumerator HitStopCoroutine(float duration)
    {
        if (duration <= 0f) yield break;
        float t = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = t;
    }
}
