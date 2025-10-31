using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class RailMover : MonoBehaviour
{
    [Header("レール設定")]
    public RailSpline currentRail;
    [Range(0f, 1f)] public float t = 0f;
    public float speed = 2f;
    public bool onRail = false;

    [Header("回転制御")]
    public bool alignRotation = true;
    public float rotationSpeed = 10f;

    [Header("高さ補正")]
    public float heightOffset = 0.05f; // 少し浮かせることで震え防止

    CharacterController cc;
    Animator animator;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (animator != null) animator.applyRootMotion = false;
    }

    void Update()
    {
        // Railに乗っていないときは通常処理しない
        if (!onRail || currentRail == null) return;

        float dt = Time.deltaTime;
        t += speed * dt;

        // 終点処理
        if (t >= 1f)
        {
            t = 1f;
            EndRail();
            return;
        }

        // 現在位置を取得
        Vector3 pos = currentRail.GetWorldPointOnSpline(t);
        pos += Vector3.up * heightOffset;

        // 次の位置で進行方向を算出
        float lookAheadT = Mathf.Min(t + 0.01f, 1f);
        Vector3 nextPos = currentRail.GetWorldPointOnSpline(lookAheadT);
        Vector3 dir = (nextPos - pos).normalized;
        dir.y = 0f;

        // ✅ 一度だけCCを無効にして直接位置固定
        if (cc.enabled)
            cc.enabled = false;

        transform.position = pos;

        // 向きをスムーズに合わせる
        if (alignRotation && dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                1f - Mathf.Exp(-rotationSpeed * dt)
            );
        }
    }

    // ✅ レール開始処理
    public void StartRail(RailSpline rail, float startT = 0f, float initialSpeed = 2f)
    {
        if (rail == null) return;

        currentRail = rail;
        t = Mathf.Clamp01(startT);
        speed = initialSpeed;
        onRail = true;

        // 入力停止
        var input = GetComponent<PlayerInput>();
        if (input != null) input.enabled = false;

        // アニメーション再生
        if (animator != null)
            animator.Play("RailRide", 0, 0f); // Rail用アニメ名を指定
    }

    // ✅ レール終了処理
    public void EndRail()
    {
        onRail = false;
        currentRail = null;

        // CCを再び有効化して通常操作に戻す
        if (cc != null)
            cc.enabled = true;

        // 入力を戻す
        var input = GetComponent<PlayerInput>();
        if (input != null)
            input.enabled = true;

        // アニメーション切り替え
        if (animator != null)
            animator.Play("Land", 0, 0f); // Rail終了アニメ
    }
}
