using UnityEngine;


[RequireComponent(typeof(CharacterController))]
public class player_Move : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float walkSpeed = 0.0f;
    [SerializeField] float runSpeed = 0.2f;
    [SerializeField] float deadzone = 0.15f;

    [Header("Gravity (簡易)")]
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float groundedGravity = -1f;

    [Header("References")]
    [SerializeField] Animator animator;
    CharacterController controller;
    Transform cam;

    // 既存の後ろ向き保持
    bool isTurned = false;
    float baseYRotation;

    // 縦方向の速度
    Vector3 velY;

    // === SKILL2 用 Root Motion 適用 ===
    [Header("Root Motion for SKILL2")]
    [SerializeField] string skill2StateName = "SKILL 2"; // Animator のステート名に合わせて
    bool skill2RootActive = false; // 今フレーム SKILL2 の root を適用するか

    private PlayerMovement playerMovement;
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        cam = Camera.main ? Camera.main.transform : null;
    }
    void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    void Update()
    {
        if (!(controller && controller.enabled && gameObject.activeInHierarchy)) return;

        // ===== 状態の取得 =====
        bool isAttacking = (animator && HasParam(animator, "IsAttacking") && animator.GetBool("IsAttacking"));
        bool inSkill2 = IsInOrNextStateName(animator, 0, skill2StateName);

        // まず重力（常に）
        if (controller.isGrounded) velY.y = groundedGravity;
        else velY.y += gravity * Time.deltaTime;

        // =====================================
        // ★ SKILL2 中は絶対に移動禁止
        // =====================================
        if (inSkill2)
        {
            skill2RootActive = true;
            animator.applyRootMotion = true;

            // --- 重力処理だけ通す ---
            if (controller.isGrounded) velY.y = groundedGravity;
            else velY.y += gravity * Time.deltaTime;

            controller.Move(velY * Time.deltaTime);

            // 移動アニメフラグOFF
            if (HasParam(animator, "IsMoving")) animator.SetBool("IsMoving", false);
            if (HasParam(animator, "IsRunning")) animator.SetBool("IsRunning", false);

            return; // ← 完全移動禁止！！
        }

        // =====================================
        // ★ 通常攻撃（IsAttacking= true）のときも移動禁止
        // =====================================
        if (isAttacking)   // ★ player_attack.cs から来る変数
        {
            // RootMotionは使わないのでOFF
            animator.applyRootMotion = false;

            // 重力だけ適用
            if (controller.isGrounded) velY.y = groundedGravity;
            else velY.y += gravity * Time.deltaTime;

            controller.Move(velY * Time.deltaTime);

            // 移動アニメ停止
            if (HasParam(animator, "IsMoving")) animator.SetBool("IsMoving", false);
            if (HasParam(animator, "IsRunning")) animator.SetBool("IsRunning", false);

            return;  // ← 通常移動を完全に止める
        }



        // ===== ここから通常移動 =====
        skill2RootActive = false;
        animator.applyRootMotion = false;

        Vector2 input = ReadKeyboardMove();
        if (input.magnitude < deadzone) input = Vector2.zero;

        bool shiftHeld = playerMovement.Dash_isPressed();

        Vector3 dir;
        if (cam)
        {
            Vector3 fwd = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 right = cam.right;
            dir = fwd * input.y + right * input.x;
        }
        else
        {
            dir = new Vector3(input.x, 0f, input.y);
        }
        dir.y = 0f;
        dir = dir.normalized;

        bool isMoving = dir.sqrMagnitude > 0.0001f;
        bool isRunning = isMoving && shiftHeld;
        float speed = isRunning ? runSpeed : walkSpeed;

        // --- Sキー押しっぱなしで後ろ向きを維持 ---

        if (playerMovement.Move_Backward_isPressed())
        {
            if (!isTurned)
            {
                baseYRotation = transform.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0f, baseYRotation + 180f, 0f);
                isTurned = true;
            }
        }
        else if (isTurned)
        {
            transform.rotation = Quaternion.Euler(0f, baseYRotation, 0f);
            isTurned = false;
        }


        // --- 実移動（通常：水平 + 重力）---
        Vector3 moveDir = isTurned ? -dir : dir;
        Vector3 horizontal = moveDir * speed;
        Vector3 motion = horizontal * Time.deltaTime + velY * Time.deltaTime;
        controller.Move(motion);

        // --- 進行方向へ回転（後ろ向き固定時は回さない）---
        if (isMoving && !isTurned)
        {
            Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 0.2f);
        }

        // --- Animator フラグ ---
        if (animator)
        {
            if (HasParam(animator, "IsMoving")) animator.SetBool("IsMoving", isMoving);
            if (HasParam(animator, "IsRunning")) animator.SetBool("IsRunning", isRunning);
        }
    }

    // Root Motion をここで適用（SKILL2 の水平前進のみ）
    void OnAnimatorMove()
    {
        if (!animator || !controller) return;

        if (skill2RootActive && animator.applyRootMotion)
        {
            // クリップ由来の rootDelta
            Vector3 rootDelta = animator.deltaPosition;

            // 水平成分のみ適用（Yは重力に任せる）
            Vector3 horizontal = new Vector3(rootDelta.x, 0f, rootDelta.z);

            // 前進成分だけ使いたい場合は下記でもOK（必要なら置換）
            // Vector3 forwardOnly = Vector3.Project(horizontal, transform.forward);
            // horizontal = forwardOnly;

            controller.Move(horizontal);
            // ルート回転は適用しない（transform.rotation は Update 側が管理）
        }
    }

    Vector2 ReadKeyboardMove()
    {

        if (!playerMovement.Move_Forward_isPressed() &&
            !playerMovement.Move_Backward_isPressed()&&
            !playerMovement.Move_Left_isPressed()    &&
            !playerMovement.Move_Right_isPressed())return Vector2.zero;
        float x = 0f, y = 0f;
        if (playerMovement.Move_Left_isPressed()) x -= 1f;
        if (playerMovement.Move_Right_isPressed()) x += 1f;
        if (playerMovement.Move_Forward_isPressed()) y += 1f;
        if (playerMovement.Move_Backward_isPressed()) y -= 1f;
        Vector2 v = new Vector2(x, y);
        return v.sqrMagnitude > 1f ? v.normalized : v;
    }

    bool HasParam(Animator anim, string name)
    {
        foreach (var p in anim.parameters) if (p.name == name) return true;
        return false;
    }

    // 現在 or 遷移先が指定“ステート名”か？
    bool IsInOrNextStateName(Animator anim, int layer, string stateName)
    {
        if (anim == null) return false;
        var cur = anim.GetCurrentAnimatorStateInfo(layer);
        if (cur.IsName(stateName)) return true;
        if (anim.IsInTransition(layer))
        {
            var nxt = anim.GetNextAnimatorStateInfo(layer);
            if (nxt.IsName(stateName)) return true;
        }
        return false;
    }
}
