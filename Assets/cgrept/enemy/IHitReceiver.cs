using UnityEngine;

public interface IHitReceiver
{
    // 他の変数や関数があってもOK

    public void OnHit(float attackPower, Vector3 hitPos, int attackIndex = 0)
    {
        Debug.Log($"[BossPartsManager] {attackPower} の攻撃を受け取りました！（{attackIndex + 1}段目）");

        // ここで処理を振り分けたり、共通ダメージ処理を行えます
        // 例：最も近い部位にダメージを転送など
    }
}
