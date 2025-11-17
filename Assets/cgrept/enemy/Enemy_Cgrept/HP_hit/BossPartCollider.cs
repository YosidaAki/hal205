using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossPartCollider : MonoBehaviour, IHitReceiver
{
    [Header("この部位の設定データ")]
    public BossPartData partData;

    [Header("親ボス管理")]
    public BossPartsManager manager;

    public void OnHit(float attackPower, Vector3 hitPos, int attackIndex = 0)
    {
        if (manager != null)
            manager.ApplyDamage(partData, attackPower, hitPos, attackIndex);
    }
}

