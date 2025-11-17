using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class RailMover : MonoBehaviour
{
    [Header("レール設定")]
    public RailSpline currentRail;
    [Range(0f, 1f)] public float t = 0f;
    public float speed = 2f;
    public bool onRail = false;

    [Header("補正設定")]
    public float heightOffset = 0.05f;

    [Header("横移動設定")]
    public float sideJumpRange = 3.0f;
    public float sideJumpHeight = 1.2f;
    public float jumpDuration = 0.5f;

    [Header("回転設定")]
    public bool alignRotation = true;
    public float rotationSpeed = 10f;

    private CharacterController cc;
    private Animator animator;

    private int lastSegmentIndex = -1;

    // ★ 各区間の変形情報（RailSplineSegment から読み込み）
    private bool segmentHasTransform = false;
    private RailSplineSegment.TransformMode segmentTransformMode;
    private Vector3 segStartScale;
    private Vector3 segEndScale;
    private Quaternion segStartRot;
    private Quaternion segEndRot;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (animator != null) animator.applyRootMotion = false;
    }

    // --- レール乗車開始 ---
    public void StartRail(RailSpline rail, float startT = 0f, float initialSpeed = 2f)
    {
        currentRail = rail;
        t = Mathf.Clamp01(startT);
        onRail = true;
        lastSegmentIndex = -1;

        if (rail != null && rail.segments.Count > 0)
            speed = rail.segments[0].targetSpeed;
        else
            speed = initialSpeed;
    }

    void Update()
    {
        if (!onRail || currentRail == null) return;

        float dt = Time.deltaTime;
        float prevT = t;
        t += speed * dt;

        // ループ対応
        if (t >= 1f)
        {
            if (currentRail.loop)
                t -= 1f;
            else
            {
                t = 1f;
                EndRail();
                return;
            }
        }

        // --- レール上の位置更新 ---
        Vector3 pos = currentRail.GetWorldPointOnSpline(t);
        pos += Vector3.up * heightOffset;

        float lookAheadT = Mathf.Min(t + 0.01f, 1f);
        Vector3 nextPos = currentRail.GetWorldPointOnSpline(lookAheadT);
        Vector3 dir = (nextPos - pos).normalized;
        dir.y = 0f;

        if (cc.enabled) cc.enabled = false;
        transform.position = pos;

        if (alignRotation && dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                1f - Mathf.Exp(-rotationSpeed * dt)
            );
        }

        HandleSideJumpInput();
        HandleSegmentEvents(prevT, t);
        ApplySegmentTransform();    // ★ 変形適用（今回の追加）
    }

    // --- 横入力処理 ---
    void HandleSideJumpInput()
    {
        if (!onRail || Keyboard.current == null) return;

        Vector3 currentPos = transform.position;

        // ▶ 右方向
        if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            RailSpline target = FindClosestRail(currentPos + transform.right * 1.5f);
            if (target != null)
                StartCoroutine(TransferToNextRail(target, t, sideJumpHeight));
        }

        // ◀ 左方向
        if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            RailSpline target = FindClosestRail(currentPos - transform.right * 1.5f);
            if (target != null)
                StartCoroutine(TransferToNextRail(target, t, sideJumpHeight));
        }
    }

    // --- 近くのレール検出 ---
    RailSpline FindClosestRail(Vector3 searchPos)
    {
        RailSpline[] allRails = Object.FindObjectsByType<RailSpline>(FindObjectsSortMode.None);
        RailSpline closest = null;
        float minDist = float.MaxValue;

        foreach (var rail in allRails)
        {
            if (rail == currentRail) continue;

            Vector3 candidate = rail.GetWorldPointOnSpline(t);
            float dist = Vector3.Distance(searchPos, candidate);

            if (dist < minDist)
            {
                minDist = dist;
                closest = rail;
            }
        }

        return (minDist <= sideJumpRange) ? closest : null;
    }

    // --- レールジャンプ ---
    IEnumerator TransferToNextRail(RailSpline nextRail, float startT, float jumpHeight)
    {
        if (nextRail == null) yield break;
        onRail = false;

        Vector3 startPos = transform.position;
        Vector3 endPos = nextRail.GetWorldPointOnSpline(startT);
        float elapsed = 0f;

        if (animator != null)
            animator.Play("Jump", 0, 0f);

        Camera mainCam = Camera.main;
        Vector3 camOriginalPos = mainCam.transform.position;
        Quaternion camOriginalRot = mainCam.transform.rotation;

        bool hasNeighbor = CheckRailParallel(currentRail, nextRail);
        Vector3 midPoint = (currentRail.GetWorldPointOnSpline(t) + nextRail.GetWorldPointOnSpline(startT)) * 0.5f;

        if (hasNeighbor)
            StartCoroutine(MoveCameraToMid(mainCam, midPoint, 0.3f));

        while (elapsed < jumpDuration)
        {
            float nt = elapsed / jumpDuration;
            float h = Mathf.Sin(nt * Mathf.PI) * jumpHeight;

            transform.position = Vector3.Lerp(startPos, endPos, nt) + Vector3.up * h;

            elapsed += Time.deltaTime;
            yield return null;
        }

        StartRail(nextRail, startT, speed);

        if (hasNeighbor)
            StartCoroutine(MoveCameraBack(mainCam, camOriginalPos, camOriginalRot, 0.3f));
    }

    // --- 区間別イベント（速度・カメラ・変形） ---
    void HandleSegmentEvents(float prevT, float currentT)
    {
        int count = currentRail.segments.Count;
        int segCount = currentRail.loop ? count : Mathf.Max(1, count - 1);

        float scaled = currentT * segCount;
        int segIndex = Mathf.FloorToInt(Mathf.Clamp(scaled, 0, segCount - 1));

        RailSplineSegment seg = currentRail.segments[segIndex];

        // ● セグメント切り替え時
        if (segIndex != lastSegmentIndex)
        {
            // --- スピード
            speed = seg.targetSpeed;

            // --- 変形設定
            if (seg.applyTransform)
            {
                segmentHasTransform = true;
                segmentTransformMode = seg.transformMode;

                segStartScale = seg.startScale;
                segEndScale = seg.endScale;

                segStartRot = Quaternion.Euler(seg.startRotationEuler);
                segEndRot = Quaternion.Euler(seg.endRotationEuler);

                if (segmentTransformMode == RailSplineSegment.TransformMode.AtPointB)
                {
                    transform.localScale = segStartScale;
                    transform.localRotation = segStartRot;
                }
            }
            else
            {
                segmentHasTransform = false;
            }

            // --- カメラ切り替え
            if (seg.switchCamera && seg.targetCamera != null)
            {
                seg.targetCamera.enabled = true;
                StartCoroutine(ResetCamera(seg.targetCamera, seg.cameraHoldTime));
            }

            lastSegmentIndex = segIndex;
        }
    }

    // --- カメラ戻し ---
    IEnumerator ResetCamera(Camera cam, float delay)
    {
        yield return new WaitForSeconds(delay);
        cam.enabled = false;
    }

    // --- 区間内の割合を算出して変形適用 ---
    void ApplySegmentTransform()
    {
        if (!segmentHasTransform) return;

        int count = currentRail.segments.Count;
        int segCount = currentRail.loop ? count : Mathf.Max(1, count - 1);

        float scaled = t * segCount;
        int segIndex = Mathf.FloorToInt(Mathf.Clamp(scaled, 0, segCount - 1));
        float segT = scaled - segIndex;   // 0〜1 の区間内割合

        if (segmentTransformMode == RailSplineSegment.TransformMode.InterpolateAB)
        {
            // 補間
            transform.localScale = Vector3.Lerp(segStartScale, segEndScale, segT);
            transform.localRotation = Quaternion.Slerp(segStartRot, segEndRot, segT);
        }
        else if (segmentTransformMode == RailSplineSegment.TransformMode.AtPointB)
        {
            if (segT >= 0.999f)
            {
                transform.localScale = segEndScale;
                transform.localRotation = segEndRot;
            }
        }
    }

    // --- レール終了 ---
    public void EndRail()
    {
        onRail = false;
        currentRail = null;

        if (cc != null) cc.enabled = true;
        if (animator != null) animator.Play("Land", 0, 0f);
    }

    // --- レールが並んでるか判定 ---
    bool CheckRailParallel(RailSpline railA, RailSpline railB)
    {
        Vector3 a0 = railA.GetWorldPointOnSpline(0f);
        Vector3 a1 = railA.GetWorldPointOnSpline(1f);
        Vector3 b0 = railB.GetWorldPointOnSpline(0f);
        Vector3 b1 = railB.GetWorldPointOnSpline(1f);

        Vector3 dirA = (a1 - a0).normalized;
        Vector3 dirB = (b1 - b0).normalized;

        float parallel = Mathf.Abs(Vector3.Dot(dirA, dirB));
        float distance = Vector3.Distance((a0 + a1) * 0.5f, (b0 + b1) * 0.5f);

        return (parallel > 0.95f && distance < 6f);
    }

    IEnumerator MoveCameraToMid(Camera cam, Vector3 mid, float dur)
    {
        Vector3 startP = cam.transform.position;
        Quaternion startR = cam.transform.rotation;

        Vector3 targetP = mid + (cam.transform.forward * -5f) + Vector3.up * 1.5f;
        Quaternion targetR = Quaternion.LookRotation(mid - targetP);

        float e = 0f;
        while (e < dur)
        {
            float nt = e / dur;
            cam.transform.position = Vector3.Lerp(startP, targetP, nt);
            cam.transform.rotation = Quaternion.Slerp(startR, targetR, nt);
            e += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator MoveCameraBack(Camera cam, Vector3 pos, Quaternion rot, float dur)
    {
        Vector3 startP = cam.transform.position;
        Quaternion startR = cam.transform.rotation;

        float e = 0f;
        while (e < dur)
        {
            float nt = e / dur;
            cam.transform.position = Vector3.Lerp(startP, pos, nt);
            cam.transform.rotation = Quaternion.Slerp(startR, rot, nt);
            e += Time.deltaTime;
            yield return null;
        }
    }
}
