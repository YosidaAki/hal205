using System.Collections;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class SceneTeleporter : MonoBehaviour
{
    public string targetScene;

    private FadeScreen fadeScreen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fadeScreen = FindAnyObjectByType<FadeScreen>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            fadeScreen.FadeOutAndLoadScene(targetScene);
        }
    }
}
