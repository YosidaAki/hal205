using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("追従対象（プレイヤーのTransformをドラッグ）")]
    public Transform target;

    [Header("ターゲットからのオフセット（ワールド基準）")]
    public Vector3 offset = new Vector3(0f, 1.8f, -5f); // ←最初に試す値

    [Header("どこを見るか（頭付近を見る高さ）")]
    public float lookHeight = 1.5f;

    void LateUpdate()
    {
        if (!target) return;

        // 位置：プレイヤー位置 + オフセット
        transform.position = target.position + offset;

        // 注視：プレイヤーの頭付近
        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        transform.LookAt(lookPoint);
    }
}