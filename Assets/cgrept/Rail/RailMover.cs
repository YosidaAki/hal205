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

    [Header("高さ補正（追加で少し浮かせたい場合に使用）")]
    public float extraHeightOffset = 0.0f;

    CharacterController cc;
    Animator animator;
    Vector3 lastPos;
    bool firstFrame = false;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (animator != null) animator.applyRootMotion = false;
    }

    void Update()
    {
        if (!onRail || currentRail == null) return;

        float dt = Time.deltaTime;
        t += speed * dt;

        if (t >= 1f)
        {
            t = 1f;
            EndRail();
            return;
        }

        // ---- レール上の位置 ----
        Vector3 pos = currentRail.GetWorldPointOnSpline(t);

        // ---- 足元補正 ----
        if (cc != null)
        {
            // CharacterControllerの中心と高さを考慮して、足元位置に合わせる
            float footOffset = cc.center.y - (cc.height * 0.5f);
            pos.y -= footOffset;
        }

        // ---- 追加の微調整 ----
        pos += Vector3.up * extraHeightOffset;

        // ---- 進行方向 ----
        float lookAheadT = Mathf.Min(t + 0.01f, 1f);
        Vector3 nextPos = currentRail.GetWorldPointOnSpline(lookAheadT);
        Vector3 dir = (nextPos - pos).normalized;
        dir.y = 0f;

        // ---- 初回スナップ ----
        if (firstFrame)
        {
            transform.position = pos;
            lastPos = pos;
            firstFrame = false;
            return;
        }

        // ---- 差分移動 ----
        Vector3 delta = pos - lastPos;
        lastPos = pos;

        if (cc != null && cc.enabled)
        {
            cc.Move(delta);
        }
        else
        {
            transform.position = pos;
        }

        // ---- 向き合わせ ----
        if (alignRotation && dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotationSpeed * dt));
        }
    }

    public void StartRail(RailSpline rail, float startT = 0f, float initialSpeed = 2f)
    {
        if (rail == null) return;

        currentRail = rail;
        t = Mathf.Clamp01(startT);
        speed = initialSpeed;
        onRail = true;

        firstFrame = true;
        lastPos = currentRail.GetWorldPointOnSpline(t);

        // 足元補正を反映
        if (cc != null)
        {
            float footOffset = cc.center.y - (cc.height * 0.5f);
            lastPos.y -= footOffset;
        }
        lastPos += Vector3.up * extraHeightOffset;

        var input = GetComponent<PlayerInput>();
        if (input != null) input.enabled = false;

        if (animator != null)
            animator.Play("RailRide", 0, 0f);
    }

    public void EndRail()
    {
        onRail = false;
        currentRail = null;

        var input = GetComponent<PlayerInput>();
        if (input != null) input.enabled = true;

        if (animator != null)
            animator.Play("Land", 0, 0f);
    }
}
