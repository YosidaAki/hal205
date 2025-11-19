using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class player_attack_hit : MonoBehaviour
{
    [Header("攻撃判定（子オブジェクトのColliderを指定）")]
    [SerializeField] private Collider hitbox;

    [Header("接続スクリプト")]
    [SerializeField] private player_attack attackController;

    [Header("ヒットストップ設定")]
    [Range(0f, 0.3f)] public float hitStopDuration = 0.06f;

    [Header("デバッグ設定")]
    [SerializeField] private bool showDebugLog = true;

    public Shatter shatter;

    void Reset()
    {
        if (hitbox == null) hitbox = GetComponentInChildren<Collider>();
        if (attackController == null) attackController = GetComponentInParent<player_attack>();
    }

    void Start()
    {
        if (hitbox != null)
        {
            hitbox.enabled = false;
            hitbox.isTrigger = true;
        }
    }

    public void EnableHitbox()
    {
        if (hitbox == null) return;
        hitbox.enabled = true;
        if (showDebugLog) Debug.Log("[player_attack_hit] 攻撃判定 ON");
    }

    public void DisableHitbox()
    {
        if (hitbox == null) return;
        hitbox.enabled = false;
        if (showDebugLog) Debug.Log("[player_attack_hit] 攻撃判定 OFF");
    }

    void OnTriggerEnter(Collider other)
    {
        if (attackController == null) return;

        int attackIndex = attackController.GetCurrentAttackIndex();

        // ============================================
        // ★ 攻撃力は「player_attack.cs が決めた値」を使用
        // ============================================
        float finalPower = attackController.GetCurrentAttackPower();

        if (other.TryGetComponent(out IHitReceiver receiver))
        {
            receiver.OnHit(finalPower, transform.position, attackIndex);

            if (showDebugLog)
                Debug.Log($"[player_attack_hit] {other.name} に命中（威力{finalPower:F1} / 段階 {attackIndex + 1}）");

            if (shatter != null && shatter.bishiding)
                shatter.ShowForSeconds(1.3f);

            StartCoroutine(HitStopCoroutine(hitStopDuration));
            DisableHitbox();
        }
    }

    IEnumerator HitStopCoroutine(float duration)
    {
        if (duration <= 0f) yield break;

        float original = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = original;
    }
}
