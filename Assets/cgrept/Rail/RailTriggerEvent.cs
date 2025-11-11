using UnityEngine;
[DisallowMultipleComponent]
public class RailTriggerEvent : MonoBehaviour
{
    [Header("接触時に動かすRailSpline")]
    public RailSpline targetRail;

    [Header("反応するタグ")]
    public string targetTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            if (targetRail != null)
            {
                targetRail.TriggerRailAppearance();
                Debug.Log($"[{name}] プレイヤー接触 → レールアニメーション開始");
            }
        }
    }
}
