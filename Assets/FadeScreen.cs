using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FadeScreen : MonoBehaviour
{
    public float fadeTime;
    private float timer = 0.0f;
    private bool bLoadScene = false;
    private AsyncOperation operation = null;
    private Image fadeImage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fadeImage = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (bLoadScene)
        {
            timer += Time.deltaTime / fadeTime;
            timer = Mathf.Clamp01(timer);

            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, timer);

            if (timer >= 1.0f && operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;
            }
        }

        
    }

    public void FadeOutAndLoadScene(string sceneName)
    {
        operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        timer = 0.0f;
        bLoadScene = true;
    }
}
