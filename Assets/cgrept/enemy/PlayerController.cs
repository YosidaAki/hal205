using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    private Rigidbody rb;
    private bool isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // --- �ړ� ---
        float h = Input.GetAxis("Horizontal"); // A,D or ��,��
        float v = Input.GetAxis("Vertical");   // W,S or ��,��

        Vector3 move = new Vector3(h, 0, v) * moveSpeed;
        Vector3 newPos = rb.position + move * Time.deltaTime;
        rb.MovePosition(newPos);

        // --- �W�����v ---
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // �n�ʂɐG�ꂽ��W�����v�\��
        isGrounded = true;
    }
}
