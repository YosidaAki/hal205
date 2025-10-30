using UnityEngine;

public class AutoFollowCamera : MonoBehaviour
{
    public Transform target;      // プレイヤー
    public Vector3 offset = new Vector3(0, 3f, -6f);  // 相対位置
    public float followSpeed = 5f;   // 位置の追従スピード
    public float rotationSpeed = 5f; // 回転の追従スピード

    void LateUpdate()
    {
        if (target == null) return;

        // === プレイヤーの「向いている方向」に合わせてカメラを配置 ===
        // プレイヤーの向きを基準にオフセットを回転
        Vector3 desiredPosition = target.position + target.rotation * offset;

        // スムーズに位置を移動
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

        // プレイヤーの方向を滑らかに見る
        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSpeed);
    }
}

