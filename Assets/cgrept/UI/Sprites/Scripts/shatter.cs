
using System.Collections;
using UnityEngine;

public class Shatter : MonoBehaviour
{
    public Camera cam;
    public float distance = 5f;
    public Transform sliceA;
    public Transform sliceB;
    [Range(0f, 1f)] public float splitDistance = 0.2f;
    public float animationSpeed = 1f;

    private float timer = 1f;

    public bool bishiding = true;


    Coroutine hideCoroutine;

    private void Start()
    {
        bishiding = true;
    }

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

        Vector3 center = cam.transform.position + cam.transform.forward * distance;
        Quaternion rot = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);

        Quaternion fixRotation = Quaternion.Euler(90f, 180f, 0f);

        transform.position = center;
        transform.rotation = rot * fixRotation;

        sliceA.localScale = new Vector3(planeWidth / 10f, 0.01f, planeHeight / 10f);
        sliceB.localScale = new Vector3(planeWidth / 10f, 0.01f, planeHeight / 10f);

        sliceA.rotation = rot * fixRotation;
        sliceB.rotation = rot * fixRotation;

        timer += Time.deltaTime * animationSpeed;
        if (bishiding) timer = 0f;
        else if (timer > 1f) timer = 1f;
        float t = Mathf.Clamp01(timer);

        float offsetX = (planeWidth / 2f) * splitDistance * t;
        float offsetZ = (planeHeight / 2f) * splitDistance * t;

        Vector3 offsetA = sliceA.rotation * new Vector3(-offsetX, 0, offsetZ);
        Vector3 offsetB = sliceB.rotation * new Vector3(offsetX, 0, -offsetZ);

        sliceA.position = center + offsetA;
        sliceB.position = center + offsetB;

        

    }

    public void Show()
    {

        bishiding = false;
        if (sliceA) sliceA.gameObject.SetActive(true);
        if (sliceB) sliceB.gameObject.SetActive(true);

    }


    public void Hide()
    {
        if (sliceA) sliceA.gameObject.SetActive(false);
        if (sliceB) sliceB.gameObject.SetActive(false);
        bishiding = true;


    }

    public void ShowForSeconds(float seconds)
    {

        Show();

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(AutoHideCoroutine(seconds));


    }

    IEnumerator AutoHideCoroutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Hide();
        hideCoroutine = null;
    }
}
