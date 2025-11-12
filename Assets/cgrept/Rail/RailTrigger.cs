using UnityEngine;

public class RailTrigger : MonoBehaviour
{
    [Header("レール設定")]
    public RailSpline rail;

    [Header("移動設定")]
    [Range(0f, 1f)] public float startT = 0f;
    public float entrySpeed = 2f;

    void OnTriggerEnter(Collider other)
    {
        // RailMover を持たない場合スキップ
        var mover = other.GetComponent<RailMover>();
        if (mover == null) return;
        // RailSpline が設定されていない場合スキップ
        if (rail == null)
        {
            Debug.LogWarning("[RailTrigger] Rail が未設定です。");
            return;
        }
        // レールが透明（未伸び）なら乗せない
        if (rail.visibleLength < 1f)
        {
            Debug.Log($"[RailTrigger] {rail.name} はまだ伸びていないため無効。");
            return;
        }
        // 通常処理：レールに乗せる
        mover.StartRail(rail, startT, entrySpeed);
        Debug.Log($"[RailTrigger] {other.name} を {rail.name} に乗せました。");
    }
}


