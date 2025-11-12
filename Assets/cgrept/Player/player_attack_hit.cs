using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class player_attack_hit : MonoBehaviour
{
    [Header("攻撃判定（子オブジェクトのColliderを指定）")]
    [SerializeField] private Collider hitbox;

    [Header("接続スクリプト")]
    [Tooltip("攻撃元プレイヤー（攻撃段階を参照）")]
    [SerializeField] private player_attack attackController;

    [Header("ヒットストップ設定")]
    [Range(0f, 0.3f)] public float hitStopDuration = 0.06f;

    [Header("デバッグ設定")]
    [SerializeField] private bool showDebugLog = true;

    public AngledSliceFitter shatter;

    void Reset()
    {
        if (hitbox == null) hitbox = GetComponentInChildren<Collider>();
        if (attackController == null) attackController = GetComponentInParent<player_attack>();
    }

    void Start()
    {
        if (hitbox == null)
            hitbox = GetComponentInChildren<Collider>();
        if (attackController == null)
            attackController = GetComponentInParent<player_attack>();

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
        if (attackController == null)
        {
            Debug.LogWarning("[player_attack_hit] attackController が未設定です。");
            return;
        }

        int attackIndex = attackController.GetCurrentAttackIndex();
        float finalPower = attackController.SetAttackPowerByIndex(attackIndex);
        

        // ✅ どんな敵でも IHitReceiver に統一
        if (other.TryGetComponent(out IHitReceiver receiver))
        {
            receiver.OnHit(finalPower, transform.position, attackIndex);

            if (showDebugLog)
                Debug.Log($"[player_attack_hit] {other.name} に命中（威力{finalPower:F1} / 段階 {attackIndex + 1}）");

            StartCoroutine(HitStopCoroutine(hitStopDuration));
            DisableHitbox();
        }
    }

    IEnumerator HitStopCoroutine(float duration)
    {
        
        if (duration <= 0f) yield break;
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = originalTimeScale;
    }
}

