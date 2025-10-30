using UnityEngine;
using UnityEngine.InputSystem; // Keyboard.current 用

[RequireComponent(typeof(CharacterController))]
public class player_move : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float walkSpeed = 2.0f;
    [SerializeField] float runSpeed = 4.0f;   // Shiftで走る速度
    [SerializeField] float deadzone = 0.15f;  // 入力の遊び

    [Header("Gravity (簡易)")]
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float groundedGravity = -1f;

    [Header("References")]
    [SerializeField] Animator animator; // 未設定なら自動取得
    CharacterController controller;
    Transform cam;

    // 縦方向の速度（y以外は0）
    Vector3 velY;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        cam = Camera.main ? Camera.main.transform : null;
    }

    void Update()
    {
        // ===== 攻撃中は移動/回転を止める（重力のみ適用）=====
        if (animator && HasParam(animator, "IsAttacking") && animator.GetBool("IsAttacking"))
        {
            // 水平移動ゼロ、重力だけ
            if (controller.isGrounded) velY.y = groundedGravity;
            else velY.y += gravity * Time.deltaTime;

            controller.Move(velY * Time.deltaTime);

            // アニメ側のフラグを明示的に落としておく
            if (HasParam(animator, "IsMoving")) animator.SetBool("IsMoving", false);
            if (HasParam(animator, "IsRunning")) animator.SetBool("IsRunning", false);
            return;
        }
        // ===============================================

        // ---- キーボード（WASD）入力 ----
        Vector2 input = ReadKeyboardMove();
        if (input.magnitude < deadzone) input = Vector2.zero;

        // ---- Shift（走り）判定 ----
        bool shiftHeld = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        // ---- カメラ基準ベクトル ----
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

        // ---- 速度決定（移動 + Shift）----
        bool isMoving = dir.sqrMagnitude > 0.0001f;
        bool isRunning = isMoving && shiftHeld;

        float speed = isRunning ? runSpeed : walkSpeed;
        Vector3 horizontal = dir * speed;

        // ---- 重力 ----
        if (controller.isGrounded) velY.y = groundedGravity;
        else velY.y += gravity * Time.deltaTime;

        // ---- 実移動 ----
        Vector3 motion = horizontal * Time.deltaTime + velY * Time.deltaTime;
        controller.Move(motion);

        // ---- 進行方向へ回転（任意）----
        if (isMoving)
        {
            Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 0.2f);
        }

        // ---- Animator ----
        if (animator)
        {
            if (HasParam(animator, "IsMoving")) animator.SetBool("IsMoving", isMoving);
            if (HasParam(animator, "IsRunning")) animator.SetBool("IsRunning", isRunning);
        }
    }

    Vector2 ReadKeyboardMove()
    {
        if (Keyboard.current == null) return Vector2.zero;
        float x = 0f, y = 0f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.wKey.isPressed) y += 1f;
        if (Keyboard.current.sKey.isPressed) y -= 1f;
        Vector2 v = new Vector2(x, y);
        return v.sqrMagnitude > 1f ? v.normalized : v; // 斜めは正規化
    }

    bool HasParam(Animator anim, string name)
    {
        foreach (var p in anim.parameters) if (p.name == name) return true;
        return false;
    }
}
