using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class player_move : MonoBehaviour
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

    bool isTurned = false;
    float baseYRotation;
    Vector3 velY;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        cam = Camera.main ? Camera.main.transform : null;
    }

    void Update()
    {
        if (controller != null && controller.enabled && gameObject.activeInHierarchy) // ←ここを強化！
        {
            if (animator && HasParam(animator, "IsAttacking") && animator.GetBool("IsAttacking"))
            {
                if (controller.isGrounded) velY.y = groundedGravity;
                else velY.y += gravity * Time.deltaTime;

                controller.Move(velY * Time.deltaTime);
                if (HasParam(animator, "IsMoving")) animator.SetBool("IsMoving", false);
                if (HasParam(animator, "IsRunning")) animator.SetBool("IsRunning", false);
                return;
            }
        }
        Vector2 input = ReadKeyboardMove();
        if (input.magnitude < deadzone) input = Vector2.zero;

        bool shiftHeld = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

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
        if (Keyboard.current != null)
        {
            if (Keyboard.current.sKey.isPressed)
            {
                if (!isTurned)
                {
                    baseYRotation = transform.eulerAngles.y;
                    transform.rotation = Quaternion.Euler(0f, baseYRotation + 180f, 0f);
                    isTurned = true;
                }
            }
            else
            {
                if (isTurned)
                {
                    transform.rotation = Quaternion.Euler(0f, baseYRotation, 0f);
                    isTurned = false;
                }
            }
        }

        // --- 重力処理 ---
        if (controller.isGrounded) velY.y = groundedGravity;
        else velY.y += gravity * Time.deltaTime;

        // --- 実移動 ---
        if (controller != null && controller.enabled && gameObject.activeInHierarchy) // ←ここを強化！
        {
            // --- 移動方向（Sキー時は前進方向を反転させる）---
            Vector3 moveDir = dir;
            if (isTurned) moveDir = -dir;

            Vector3 horizontal = moveDir * speed;

            Vector3 motion = horizontal * Time.deltaTime + velY * Time.deltaTime;
            controller.Move(motion);
        }



        // --- 進行方向へ回転 ---
        if (isMoving && !isTurned)
        {
            Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 0.2f);
        }

        // --- Animator制御 ---
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
        return v.sqrMagnitude > 1f ? v.normalized : v;
    }

    bool HasParam(Animator anim, string name)
    {
        foreach (var p in anim.parameters) if (p.name == name) return true;
        return false;
    }
}
