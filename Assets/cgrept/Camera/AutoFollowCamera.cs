using UnityEngine;

public class AutoFollowCamera : MonoBehaviour
{
    [Header("ターゲット設定")]
    public Transform target;      // プレイヤー

    [Header("カメラ追従設定")]
    public Vector3 offset = new Vector3(0, 3f, -6f);  // プレイヤーからの相対位置
    public float followSpeed = 5f;   // 位置の追従スピード
    public float rotationSpeed = 5f; // 回転の追従スピード

    // 前回のターゲットY回転を保存
    private float lastTargetYRotation = 0f;

    void LateUpdate()
    {
        if (target == null) return;

        // === プレイヤーの「向いている方向」に合わせてカメラを配置 ===
        Vector3 desiredPosition = target.position + target.rotation * offset;

        // --- 振り向き検出（Y回転が大きく変わったら即追従）---
        float targetY = target.eulerAngles.y;
        float deltaY = Mathf.DeltaAngle(lastTargetYRotation, targetY);

        bool instantTurn = Mathf.Abs(deltaY) > 100f; // 約180°反転なら即追従扱い

        if (!instantTurn)
        {
            // 通常はスムーズに追従
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

            Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSpeed);
        }

        // 現在のY回転を記録
        lastTargetYRotation = targetY;
    }
}

