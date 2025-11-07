using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class BossPartData
{
    [Header("設定")]
    public string partName = "Body";

    [Tooltip("この部位の基本ダメージ倍率（1=等倍、2=弱点など）")]
    public float baseMultiplier = 1f;

    [Tooltip("この部位が参照する BossHealth（共通のものでもOK）")]
    public BossHealth bossHealth;

    [Header("攻撃段階ごとの倍率（0=1段目, 1=2段目, 2=3段目など）")]
    public List<float> attackStageMultipliers = new List<float> { 1.0f, 1.2f, 1.5f };

    [Header("エフェクト設定")]
    public List<GameObject> weakPointEffects = new List<GameObject>();
    public List<GameObject> normalHitEffects = new List<GameObject>();

    [Header("演出設定")]
    public bool enableSlowMotion = true;
    public bool enableBlinkEffect = true;

    public bool IsWeakPoint => baseMultiplier > 1f;
}

public class BossPartsManager : MonoBehaviour, IHitReceiver
{
    [Header("ボスの部位リスト")]
    [Tooltip("部位をここに自由に追加削除できます")]
    public List<BossPartData> parts = new List<BossPartData>();

    [Header("デバッグ出力")]
    public bool showDebugLog = true;

    // ===============================
    // 攻撃を受けたときに呼ばれる
    // ===============================
    public void OnHit(float attackPower, Vector3 hitPos, int attackIndex = 0)
    {
        if (parts.Count == 0)
        {
            Debug.LogWarning("[BossPartsManager] 部位リストが空です。");
            return;
        }

        // 最も近い部位を取得
        BossPartData hitPart = GetHitPartFromPosition(hitPos);
        if (hitPart == null)
        {
            Debug.LogWarning("[BossPartsManager] 該当する部位が見つかりません。");
            return;
        }

        // ダメージ計算
        float damage = CalculateDamage(hitPart, attackPower, attackIndex);

        // HP減算
        if (hitPart.bossHealth != null)
            hitPart.bossHealth.TakeDamage(damage);

        // エフェクト再生
        PlayEffects(hitPart, hitPos);

        if (showDebugLog)
        {
            Debug.Log($"[BossPartsManager] 部位:{hitPart.partName} 段階:{attackIndex + 1} ダメージ:{damage:F1}");
        }
    }

    // ===============================
    // ダメージ計算
    // ===============================
    float CalculateDamage(BossPartData part, float attackPower, int attackIndex)
    {
        float stageMul = 1.0f;

        // 段階ごとの倍率が存在すれば使用
        if (attackIndex >= 0 && attackIndex < part.attackStageMultipliers.Count)
            stageMul = part.attackStageMultipliers[attackIndex];

        return attackPower * part.baseMultiplier * stageMul;
    }

    // ===============================
    // もっとも近い部位を取得（ColliderがなくてもOK）
    // ===============================
    BossPartData GetHitPartFromPosition(Vector3 hitPos)
    {
        BossPartData closest = null;
        float minDist = Mathf.Infinity;

        foreach (var part in parts)
        {
            if (part.bossHealth == null) continue;

            float dist = Vector3.Distance(hitPos, part.bossHealth.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = part;
            }
        }

        return closest;
    }

    // ===============================
    // エフェクト再生処理
    // ===============================
    void PlayEffects(BossPartData part, Vector3 hitPos)
    {
        var list = part.IsWeakPoint ? part.weakPointEffects : part.normalHitEffects;

        foreach (var fxPrefab in list)
        {
            if (fxPrefab == null) continue;
            var fx = Instantiate(fxPrefab, hitPos, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (part.IsWeakPoint)
        {
            if (part.enableSlowMotion)
                StartCoroutine(SlowMotion());
            if (part.enableBlinkEffect && part.bossHealth != null)
                StartCoroutine(Blink(part.bossHealth));
        }
    }

    // ===============================
    // スローモーション演出
    // ===============================
    IEnumerator SlowMotion()
    {
        float org = Time.timeScale;
        Time.timeScale = 0.25f;
        yield return new WaitForSecondsRealtime(0.1f);
        Time.timeScale = org;
    }

    // ===============================
    // 点滅演出（ダメージ時）
    // ===============================
    IEnumerator Blink(BossHealth boss)
    {
        var rend = boss.GetComponentInChildren<Renderer>();
        if (rend == null) yield break;

        var mat = rend.material;
        Color original = mat.color;

        for (int i = 0; i < 3; i++)
        {
            mat.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            mat.color = original;
            yield return new WaitForSeconds(0.05f);
        }
    }
}
