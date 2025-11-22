using UnityEngine;
using System.Collections;

public class MoveAfterDelay : MonoBehaviour
{
    public float delayTime = 7.0f;
    public float moveSpeed = 5.0f;
    public float moveDuration = 3.0f;

    bool isTurned = false;

    [SerializeField] Animator animator; 
    CharacterController controller;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        controller = GetComponent<CharacterController>();

        StartCoroutine(MoveWithDelay());
    }

    IEnumerator MoveWithDelay()
    {
        yield return new WaitForSeconds(delayTime);

        if (animator != null)
            animator.SetBool("IsMoving", true);

        float t = 0f;
        while (t < moveDuration)
        {
            Vector3 move = transform.forward * moveSpeed * Time.deltaTime;

            if (controller != null)
                controller.Move(move);
            else
                transform.Translate(move, Space.World);

            t += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
            animator.SetBool("IsMoving", false);
    }
}