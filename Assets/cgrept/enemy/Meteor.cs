using UnityEngine;

public class Meteor : MonoBehaviour
{
    public float speed = 15f;
    public float damage = 25f;
    public float lifeTime = 5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            PlayerHealth ph = collision.collider.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
        }

        // Ë¶êŒÇè¡Ç∑
        Destroy(gameObject);
    }
}
