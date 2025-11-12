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
    public float sideJumpRange = 3.0f;   // 横レール検出距離
    public float sideJumpHeight = 1.2f;  // 横ジャンプ高さ
    public float jumpDuration = 0.5f;    // ジャンプ時間

    [Header("回転設定")]
    public bool alignRotation = true;
    public float rotationSpeed = 10f;

    CharacterController cc;
    Animator animator;
    int lastSegmentIndex = -1;
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
        lastSegmentIndex = -1; // ✅ 区間インデックスをリセット

        // 最初のセグメント速度に合わせる
        if (rail != null && rail.segments.Count > 0)
            speed = rail.segments[0].targetSpeed;
        else
            speed = initialSpeed;

        //if (animator != null)
        //    animator.Play("RailMove", 0); // Base Layer
    }

    void Update()
    {
        if (!onRail || currentRail == null) return;

        float dt = Time.deltaTime;
        float prevT = t;
        t += speed * dt;
        // ✅ ループ対応
        if (t >= 1f)
        {
            if (currentRail.loop)
            {
                t -= 1f;  // ループ継続
            }
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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotationSpeed * dt));
        }

        HandleSideJumpInput();
        HandleSegmentEvents(prevT, t);
    }

    // --- 横ジャンプ入力 ---
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
    // --- 横のレールを自動検出 ---
    RailSpline FindClosestRail(Vector3 searchPos)
    {
        // ✅ 新APIに対応
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

        // 距離判定
        return (minDist <= sideJumpRange) ? closest : null;
    }


    // --- 横ジャンプ処理 ---
    IEnumerator TransferToNextRail(RailSpline nextRail, float startT, float jumpHeight)
    {
        if (nextRail == null) yield break;
        onRail = false;

        Vector3 startPos = transform.position;
        Vector3 endPos = nextRail.GetWorldPointOnSpline(startT);
        float elapsed = 0f;

        if (animator != null)
            animator.Play("Jump", 0, 0f);

        // 🎥 カメラの一時的な中央寄せ処理
        Camera mainCam = Camera.main;
        Vector3 cameraOriginalPos = mainCam.transform.position;
        Quaternion cameraOriginalRot = mainCam.transform.rotation;

        // --- レール中央を求める ---
        Vector3 midPoint = (currentRail.GetWorldPointOnSpline(t) + nextRail.GetWorldPointOnSpline(startT)) * 0.5f;

        bool hasNeighbor = CheckRailParallel(currentRail, nextRail);
        if (hasNeighbor)
        {
            // 2本並んでいるなら真ん中へ一時的に移動
            StartCoroutine(MoveCameraToMid(mainCam, midPoint, 0.3f));
        }

        // --- ジャンプ中 ---
        while (elapsed < jumpDuration)
        {
            float nt = elapsed / jumpDuration;
            float height = Mathf.Sin(nt * Mathf.PI) * jumpHeight;
            transform.position = Vector3.Lerp(startPos, endPos, nt) + Vector3.up * height;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- ジャンプ完了 ---
        StartRail(nextRail, startT, speed);

        // カメラを元に戻す
        if (hasNeighbor)
            StartCoroutine(MoveCameraBack(mainCam, cameraOriginalPos, cameraOriginalRot, 0.3f));
    }

    // --- レールセグメント処理（スピード＆カメラ） ---
    // --- レールセグメント処理（区間ごとにスピード切り替え＆カメラ） ---
    void HandleSegmentEvents(float prevT, float currentT)
    {
        if (currentRail == null || currentRail.segments.Count < 2)
            return;

        int segCount = currentRail.loop ? currentRail.segments.Count : currentRail.segments.Count - 1;
        float scaledCurr = currentT * segCount;

        int segIndex = Mathf.FloorToInt(Mathf.Clamp(scaledCurr, 0, segCount - 1));

        // 現在区間（セグメント）取得
        var currentSeg = currentRail.segments[segIndex];

        // --- 区間ごとのスピード反映 ---
        // セグメント切り替え時（tが前回より進んだ時）に更新
        if (segIndex != lastSegmentIndex)
        {
            speed = currentSeg.targetSpeed;
            lastSegmentIndex = segIndex;
        }

        // --- カメラ切替 ---
        if (currentSeg.switchCamera && currentSeg.targetCamera != null)
        {
            currentSeg.targetCamera.enabled = true;
            if (currentSeg.cameraHoldTime > 0)
                StartCoroutine(ResetCamera(currentSeg.targetCamera, currentSeg.cameraHoldTime));
        }
    }


    IEnumerator ResetCamera(Camera cam, float delay)
    {
        yield return new WaitForSeconds(delay);
        cam.enabled = false;
    }

    // --- レール終了 ---
    public void EndRail()
    {
        onRail = false;
        currentRail = null;
        if (cc != null) cc.enabled = true;
        if (animator != null) animator.Play("Land", 0, 0f);
    }

    // --- レールが並んでるかを判定（近い＆ほぼ平行） ---
    bool CheckRailParallel(RailSpline railA, RailSpline railB)
    {
        if (railA == null || railB == null) return false;

        Vector3 a0 = railA.GetWorldPointOnSpline(0f);
        Vector3 a1 = railA.GetWorldPointOnSpline(1f);
        Vector3 b0 = railB.GetWorldPointOnSpline(0f);
        Vector3 b1 = railB.GetWorldPointOnSpline(1f);

        Vector3 dirA = (a1 - a0).normalized;
        Vector3 dirB = (b1 - b0).normalized;

        // 平行かつ近距離なら「並んでる」とみなす
        float parallel = Mathf.Abs(Vector3.Dot(dirA, dirB));
        float distance = Vector3.Distance((a0 + a1) * 0.5f, (b0 + b1) * 0.5f);

        return (parallel > 0.95f && distance < 6f);
    }

    // --- カメラを中央に寄せる ---
    IEnumerator MoveCameraToMid(Camera cam, Vector3 midPoint, float duration)
    {
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        Vector3 targetPos = midPoint + (cam.transform.forward * -5f) + Vector3.up * 1.5f;
        Quaternion targetRot = Quaternion.LookRotation(midPoint - targetPos);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // --- カメラを元に戻す ---
    IEnumerator MoveCameraBack(Camera cam, Vector3 originalPos, Quaternion originalRot, float duration)
    {
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            cam.transform.position = Vector3.Lerp(startPos, originalPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, originalRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
