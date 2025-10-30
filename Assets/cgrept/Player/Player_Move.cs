using UnityEngine;
using UnityEngine.InputSystem; // Keyboard.current �p

[RequireComponent(typeof(CharacterController))]
public class player_move : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float walkSpeed = 2.0f;
    [SerializeField] float runSpeed = 4.0f;   // Shift�ő��鑬�x
    [SerializeField] float deadzone = 0.15f;  // ���̗͂V��

    [Header("Gravity (�Ȉ�)")]
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float groundedGravity = -1f;

    [Header("References")]
    [SerializeField] Animator animator; // ���ݒ�Ȃ玩���擾
    CharacterController controller;
    Transform cam;

    // �c�����̑��x�iy�ȊO��0�j
    Vector3 velY;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        cam = Camera.main ? Camera.main.transform : null;
    }

    void Update()
    {
        // ===== �U�����͈ړ�/��]���~�߂�i�d�͂̂ݓK�p�j=====
        if (animator && HasParam(animator, "IsAttacking") && animator.GetBool("IsAttacking"))
        {
            // �����ړ��[���A�d�͂���
            if (controller.isGrounded) velY.y = groundedGravity;
            else velY.y += gravity * Time.deltaTime;

            controller.Move(velY * Time.deltaTime);

            // �A�j�����̃t���O�𖾎��I�ɗ��Ƃ��Ă���
            if (HasParam(animator, "IsMoving")) animator.SetBool("IsMoving", false);
            if (HasParam(animator, "IsRunning")) animator.SetBool("IsRunning", false);
            return;
        }
        // ===============================================

        // ---- �L�[�{�[�h�iWASD�j���� ----
        Vector2 input = ReadKeyboardMove();
        if (input.magnitude < deadzone) input = Vector2.zero;

        // ---- Shift�i����j���� ----
        bool shiftHeld = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        // ---- �J������x�N�g�� ----
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

        // ---- ���x����i�ړ� + Shift�j----
        bool isMoving = dir.sqrMagnitude > 0.0001f;
        bool isRunning = isMoving && shiftHeld;

        float speed = isRunning ? runSpeed : walkSpeed;
        Vector3 horizontal = dir * speed;

        // ---- �d�� ----
        if (controller.isGrounded) velY.y = groundedGravity;
        else velY.y += gravity * Time.deltaTime;

        // ---- ���ړ� ----
        Vector3 motion = horizontal * Time.deltaTime + velY * Time.deltaTime;
        controller.Move(motion);

        // ---- �i�s�����։�]�i�C�Ӂj----
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
        return v.sqrMagnitude > 1f ? v.normalized : v; // �΂߂͐��K��
    }

    bool HasParam(Animator anim, string name)
    {
        foreach (var p in anim.parameters) if (p.name == name) return true;
        return false;
    }
}
