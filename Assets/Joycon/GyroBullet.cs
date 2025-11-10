using UnityEngine;

public class GyroBullet : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Vector3 shootDir;
    public float speed = 10.0f;

    void Start()
    {
        transform.rotation = Quaternion.LookRotation(shootDir);
        GetComponent<Rigidbody>().linearVelocity = speed * shootDir;

        GameObject.Destroy(gameObject, 3.0f);
    }

    // Update is called once per frame

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IHitReceiver receiver))
        {
            GameObject.Destroy(gameObject);
            receiver.OnHit(50.0f, transform.position, 0);
        }
    }

    void Update()
    {
    }
}
