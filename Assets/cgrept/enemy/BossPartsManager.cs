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

    [Tooltip("共通のBossHealth参照（同じものを指定）")]
    public BossHealth bossHealth;

    [Header("攻撃段階ごとの倍率設定（0=1段目, 1=2段目...）")]
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
        // 「当たったCollider」から部位を特定できるように
        BossPartData hitPart = GetHitPartFromPosition(hitPos);

        // 該当部位が見つからない → 本体ダメージ
        if (hitPart == null)
        {
            if (showDebugLog)
                Debug.LogWarning("[BossPartsManager] ヒットした部位が見つかりません → Boss本体にダメージ");

            // parts の最初または共通BossHealthへ直接
            if (parts.Count > 0 && parts[0].bossHealth != null)
                parts[0].bossHealth.TakeDamage(attackPower);
            return;
        }

        // ダメージ計算
        float damage = CalculateDamage(hitPart, attackPower, attackIndex);

        // HP減算
        if (hitPart.bossHealth != null)
            hitPart.bossHealth.TakeDamage(damage);

        // エフェクト再生
        PlayEffects(hitPart, hitPos);

        // デバッグ表示
        if (showDebugLog)
            Debug.Log(
                $"[BossPartsManager] [{hitPart.partName}] に {damage:F1} ダメージ！（段階:{attackIndex + 1} 倍率:{hitPart.baseMultiplier:F2}）"
            );
    }


    // ===============================
    // もっとも近い部位を取得
    // ===============================
    BossPartData GetClosestPart(Vector3 hitPos)
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
    // ダメージ計算
    // ===============================
    float CalculateDamage(BossPartData part, float attackPower, int attackIndex)
    {
        float stageMul = 1.0f;
        if (part.attackStageMultipliers.Count > attackIndex)
            stageMul = part.attackStageMultipliers[attackIndex];

        return attackPower * part.baseMultiplier * stageMul;
    }

    // ===============================
    // エフェクト再生処理
    // ===============================
    void PlayEffects(BossPartData part, Vector3 hitPos)
    {
        bool isWeakPoint = part.IsWeakPoint;
        var list = isWeakPoint ? part.weakPointEffects : part.normalHitEffects;

        foreach (var fxPrefab in list)
        {
            if (fxPrefab == null) continue;
            var fx = Instantiate(fxPrefab, hitPos, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (isWeakPoint)
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
        Time.timeScale = 0.2f;
        yield return new WaitForSecondsRealtime(0.1f);
        Time.timeScale = org;
    }

    // ===============================
    // 点滅演出（ダメージ時）
    // ===============================
    IEnumerator Blink(BossHealth boss)
    {
        var rend = boss.GetComponent<Renderer>();
        if (rend == null) yield break;

        Color original = rend.material.color;
        for (int i = 0; i < 3; i++)
        {
            rend.material.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            rend.material.color = original;
            yield return new WaitForSeconds(0.05f);
        }
    }

    BossPartData GetHitPartFromPosition(Vector3 hitPos)
    {
        BossPartData closest = null;
        float minDist = Mathf.Infinity;

        foreach (var part in parts)
        {
            if (part.bossHealth == null) continue;

            // まず、距離でざっくり判定（Colliderを持たない場合に対応）
            float dist = Vector3.Distance(hitPos, part.bossHealth.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = part;
            }
        }

        return closest;
    }

}
