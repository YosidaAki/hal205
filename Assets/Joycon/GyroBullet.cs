using UnityEngine;

public class GyroBullet : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Vector3 shootDir;
    public float speed = 10.0f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.LookRotation(shootDir);
        transform.position += Time.deltaTime * speed * shootDir;
    }
}
