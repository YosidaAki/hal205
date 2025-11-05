using System.Collections.Generic;
using UnityEngine;
using System.Collections;

[System.Serializable]
public class BossPartData
{
    [Header("設定")]
    public string partName = "Body";
    public float damageMultiplier = 1f;
    public BossHealth bossHealth;

    [Header("エフェクト設定")]
    public List<GameObject> weakPointEffects = new List<GameObject>();
    public List<GameObject> normalHitEffects = new List<GameObject>();

    [Header("演出設定")]
    public bool enableSlowMotion = true;
    public bool enableBlinkEffect = true;
}

public class BossPartsManager : MonoBehaviour, IHitReceiver
{
    [Header("ボスの部位リスト")]
    public List<BossPartData> parts = new List<BossPartData>();

    public void OnHit(float attackPower, Vector3 hitPos)
    {
        // ヒットした部位をレイで判定（近いものを探す）
        BossPartData hitPart = GetClosestPart(hitPos);
        if (hitPart == null) return;

        float damage = attackPower * hitPart.damageMultiplier;
        hitPart.bossHealth.TakeDamage(damage);

        PlayEffects(hitPart, hitPos);

        Debug.Log($"[{hitPart.partName}] に {damage} ダメージ！(倍率:{hitPart.damageMultiplier})");
    }

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

    void PlayEffects(BossPartData part, Vector3 hitPos)
    {
        bool isWeakPoint = part.damageMultiplier > 1f;
        List<GameObject> list = isWeakPoint ? part.weakPointEffects : part.normalHitEffects;

        foreach (var fxPrefab in list)
        {
            if (fxPrefab == null) continue;
            GameObject fx = Instantiate(fxPrefab, hitPos, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (isWeakPoint)
        {
            if (part.enableSlowMotion) StartCoroutine(SlowMotion());
            if (part.enableBlinkEffect && part.bossHealth != null)
                StartCoroutine(Blink(part.bossHealth));
        }
    }

    IEnumerator SlowMotion()
    {
        float org = Time.timeScale;
        Time.timeScale = 0.2f;
        yield return new WaitForSecondsRealtime(0.1f);
        Time.timeScale = org;
    }

    IEnumerator Blink(BossHealth boss)
    {
        Renderer rend = boss.GetComponent<Renderer>();
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
}

