
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Shatter : MonoBehaviour
{
    public Camera cam;
    public float distance = 5f;
    public Transform sliceA;
    public Transform sliceB;
    public GameObject background;
    [Range(0f, 1f)] public float splitDistance = 0.2f;
    public float openspeed = 1f;
    public float closespeed = 0.2f;

    public AnimationCurve opencurve;
    public AnimationCurve closeCurve;
    
    public ParticleSystem particle;

    private float timer = 1f;

    public bool bishiding = true;


    private Coroutine animCoroutine;

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
        background.transform.localScale = new Vector3(planeWidth / 5f, 0.01f, planeHeight / 5f);

        sliceA.rotation = rot * fixRotation;
        sliceB.rotation = rot * fixRotation;
        background.transform.rotation = rot*fixRotation;

        float speed = 0.0f;


        if (bishiding)
        {
            timer -= Time.deltaTime*closespeed;
            speed = closeCurve.Evaluate(timer);

        }
        else
        {
            timer += Time.deltaTime*openspeed;
            speed = opencurve.Evaluate(timer);
        }

        timer = Mathf.Clamp01(timer);
        float t = Mathf.SmoothStep(0f, 1f, speed); 

        float offsetX = (planeWidth / 2f) * splitDistance * t;
        float offsetZ = (planeHeight / 2f) * splitDistance * t;

        Vector3 offsetA = sliceA.rotation * new Vector3(-offsetX, 0, offsetZ);
        Vector3 offsetB = sliceB.rotation * new Vector3(offsetX, 0, -offsetZ);

        sliceA.position = center + offsetA;
        sliceB.position = center + offsetB;
        background.transform.position = center + cam.transform.forward * 0.1f;
        particle.transform.position = center + cam.transform.forward * 0.1f;

        

    }

    public void Show()
    {
        bishiding = false;
        timer = 0f;
        if (sliceA) sliceA.gameObject.SetActive(true);
        if (sliceB) sliceB.gameObject.SetActive(true);
        if (background)background.gameObject.SetActive(true);
        if (particle) particle.Play();
        
    }

    public void Hide()
    {
        bishiding = true;
    }

    public void ShowForSeconds(float openDuration)
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(AnimateShowAndHide(openDuration));
    }

    private IEnumerator AnimateShowAndHide(float openDuration)
    {
        Show();
        yield return new WaitUntil(() => timer >= 1f);

        bishiding = true;
        yield return new WaitUntil(() => timer <= 0f);

        if (sliceA) sliceA.gameObject.SetActive(false);
        if (sliceB) sliceB.gameObject.SetActive(false);
        if (background)background.gameObject.SetActive(false);
        if (particle) particle.Stop();
        


        animCoroutine = null;
    }
}
