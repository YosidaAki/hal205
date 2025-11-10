using UnityEngine;

public class AngledSliceFitter : MonoBehaviour
{
    public Camera cam;
    public float distance = 5f;
    public Transform sliceA;
    public Transform sliceB;
    [Range(0f, 1f)]
    public float splitDistance = 0.2f;
    public float animationSpeed = 1f;

    private float timer = 0f;

    void Update()
    {
        if (!cam) cam = Camera.main;

        // --- Plane boyutu ---
        float planeWidth, planeHeight;
        if (cam.orthographic)
        {
            planeHeight = cam.orthographicSize * 2f;
            planeWidth = planeHeight * cam.aspect;
        }
        else
        {
            planeHeight = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            planeWidth = planeHeight * cam.aspect;
        }

        // --- Plane center ve rotation (kamera önünde ve paralel) ---
        Vector3 center = cam.transform.position + cam.transform.forward * distance;
        Quaternion rot = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);

        // --- X 90° ve Y 180° düzeltmesi ---
        Quaternion fixRotation = Quaternion.Euler(90f, 180f, 0f);

        transform.position = center;
        transform.rotation = rot * fixRotation;

        // --- Scale (Plane mesh 10x10) ---
        sliceA.localScale = new Vector3(planeWidth / 10f, 0.01f, planeHeight / 10f);
        sliceB.localScale = new Vector3(planeWidth / 10f, 0.01f, planeHeight / 10f);

        sliceA.rotation = rot * fixRotation;
        sliceB.rotation = rot * fixRotation;

        // --- Açılma animasyonu ---
        timer += Time.deltaTime * animationSpeed;
        float t = (Mathf.Sin(timer * Mathf.PI * 2f) * 0.5f) + 0.5f;

        float offsetX = (planeWidth / 2f) * splitDistance * t;
        float offsetZ = (planeHeight / 2f) * splitDistance * t;

        Vector3 offsetA = sliceA.rotation * new Vector3(-offsetX, 0, offsetZ);
        Vector3 offsetB = sliceB.rotation * new Vector3(offsetX, 0, -offsetZ);

        sliceA.position = center + offsetA;
        sliceB.position = center + offsetB;
    }
}
