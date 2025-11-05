using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 攻撃段階ごとの倍率設定クラス
[System.Serializable]
public class DamageSetting
{
    [Tooltip("何段目の攻撃に対応するか (0=1段目, 1=2段目, 2=3段目...)")]
    public int attackIndex = 0;

    [Tooltip("この攻撃段階に対する倍率（例：1.0=等倍, 2.0=2倍）")]
    public float damageMultiplier = 1.0f;
}

// ===============================
// ✅ BossPart：GameObjectではなく「データ構造」
// ===============================
[System.Serializable]
public class BossPart
{
    [Header("設定")]
    public string partName = "Body";
    public float baseMultiplier = 1.0f;
    public BossHealth bossHealth;

    [Header("攻撃段階ごとの倍率設定")]
    public List<DamageSetting> damageSettings = new List<DamageSetting>()
    {
        new DamageSetting(){ attackIndex = 0, damageMultiplier = 1.0f },
        new DamageSetting(){ attackIndex = 1, damageMultiplier = 1.2f },
        new DamageSetting(){ attackIndex = 2, damageMultiplier = 1.5f },
    };

    [Header("エフェクト設定")]
    public List<GameObject> weakPointEffects = new List<GameObject>();
    public List<GameObject> normalHitEffects = new List<GameObject>();

    [Header("演出設定")]
    public bool enableSlowMotion = true;
    public bool enableBlinkEffect = true;

    public bool IsWeakPoint => baseMultiplier > 1f;

    // 攻撃処理
    public void OnHit(float attackPower, Vector3 hitPos, int attackIndex = 0)
    {
        float damage = CalculateDamage(attackPower, attackIndex);
        bossHealth?.TakeDamage(damage);
        PlayEffects(hitPos);
        Debug.Log($"[{partName}] に {damage} ダメージ！（{attackIndex + 1}段目）");
    }

    // 攻撃段階倍率を計算
    float CalculateDamage(float attackPower, int attackIndex)
    {
        float stepMul = 1f;
        foreach (var set in damageSettings)
        {
            if (set.attackIndex == attackIndex)
            {
                stepMul = set.damageMultiplier;
                break;
            }
        }
        return attackPower * baseMultiplier * stepMul;
    }

    void PlayEffects(Vector3 hitPos)
    {
        var list = IsWeakPoint ? weakPointEffects : normalHitEffects;
        foreach (var fxPrefab in list)
        {
            if (fxPrefab == null) continue;
            var fx = GameObject.Instantiate(fxPrefab, hitPos, Quaternion.identity);
            GameObject.Destroy(fx, 2f);
        }

        //if (IsWeakPoint)
        //{
            //if (enableSlowMotion) BossEffectHelper.StartSlowMotion();
            //if (enableBlinkEffect) BossEffectHelper.BlinkAtPosition(hitPos);
        //}
    }
}
