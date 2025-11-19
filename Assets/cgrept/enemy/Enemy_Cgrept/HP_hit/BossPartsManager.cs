using UnityEngine;
using System.Collections.Generic;

public class BossPartsManager : MonoBehaviour
{
    [Header("共通ボスHP")]
    public BossHealth bossHealth;

    [Header("自動検出された部位")]
    public List<BossPartCollider> partColliders = new List<BossPartCollider>();

    [Header("デバッグ出力")]
    public bool showDebugLog = true;

    void Awake()
    {
        if (bossHealth == null)
            bossHealth = GetComponent<BossHealth>();

        partColliders.Clear();
        foreach (var col in GetComponentsInChildren<BossPartCollider>())
        {
            col.manager = this;
            partColliders.Add(col);
        }
    }

    public void ApplyDamage(BossPartData partData, float attackPower, Vector3 hitPos, int attackIndex)
    {
        if (bossHealth == null || partData == null) return;

        float stageMul = 1f;
        foreach (var ds in partData.damageSettings)
        {
            if (ds.attackIndex == attackIndex)
            {
                stageMul = ds.damageMultiplier;
                break;
            }
        }

        float totalDamage = attackPower * partData.baseMultiplier * stageMul;
        bossHealth.TakeDamage(totalDamage);

        if (showDebugLog)
            Debug.Log($"[BossPartsManager] {partData.partName} hit: {totalDamage:F1}");

        PlayEffects(partData, hitPos);
    }

    void PlayEffects(BossPartData data, Vector3 pos)
    {
        GameObject fx = data.baseMultiplier > 1f ? data.weakPointEffect : data.normalHitEffect;
        if (fx != null)
        {
            var go = Instantiate(fx, pos, Quaternion.identity);
            Destroy(go, 2f);
        }

        if (data.baseMultiplier > 1f)
        {
            if (data.enableSlowMotion) BossEffectHelper.StartSlowMotion(0.3f, 0.15f);
            if (data.enableBlinkEffect) BossEffectHelper.BlinkAtPosition(pos, Color.red);
        }
    }
}

