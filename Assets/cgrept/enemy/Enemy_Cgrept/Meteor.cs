using UnityEngine;

public class Meteor : MonoBehaviour
{
    public float speed = 15f;
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
        // Ë¶êŒÇè¡Ç∑
        Destroy(gameObject);
    }
}
