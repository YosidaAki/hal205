using UnityEngine;
using UnityEngine.InputSystem;

public class Enemy_Animation_Move : MonoBehaviour
{
    public Animator animator;

    public float walkSpeed = 2f;
    public float runSpeed = 4f;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleAttack();
    }

    void HandleMovement()
    {
        var keyboard = Keyboard.current;

        float h = 0;
        float v = 0;

        if (keyboard.leftArrowKey.isPressed) h = -1f;
        if (keyboard.rightArrowKey.isPressed) h = 1f;
        if (keyboard.upArrowKey.isPressed) v = 1f;
        if (keyboard.downArrowKey.isPressed) v = -1f;

        bool isRunning = keyboard.rightShiftKey.isPressed;

        int moveX = 0;   // Å© í‚é~éûÇÕ 0 Ç…Ç∑ÇÈ
        int moveZ = 0;   // Å© í‚é~éûÇÕ 0

        if (h < 0) moveX = -1;
        if (h > 0) moveX = 1;

        if (v < 0) moveZ = -1;
        if (v > 0) moveZ = 1;

        animator.SetInteger("MoveX", moveX);
        animator.SetInteger("MoveZ", moveZ);
        animator.SetInteger("Speed", isRunning ? 1 : 0);

        if (moveX != 0 || moveZ != 0)
        {
            Vector3 dir = new Vector3(h, 0, v).normalized;
            float spd = isRunning ? runSpeed : walkSpeed;

            transform.Translate(dir * spd * Time.deltaTime, Space.World);
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    void HandleJump()
    {
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            animator.SetTrigger("Jump");
        }
    }

    void HandleAttack()
    {
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            animator.SetTrigger("Attack");
        }
    }
}
