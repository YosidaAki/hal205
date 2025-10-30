
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class hpbar : MonoBehaviour
{

    [SerializeField] Slider slider;

    [SerializeField] Image fillcolor;

    [SerializeField] Color mincolor=Color.red;
    [SerializeField] Color maxcolor=Color.green;


    float damage = 25f;
    float hp = 100f;
    float maxhp = 100f;
  

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateColor(slider.value);

        slider.onValueChanged.AddListener(UpdateColor);
    }

    private void UpdateColor(float value)
    {
        float t = Mathf.InverseLerp(slider.minValue,slider.maxValue,value);
        fillcolor.color = Color.Lerp(mincolor, maxcolor, t);
    }

    // Update is called once per frame
    void Update()
    {
        
        slider.value = hp;
        //if (InputAction)
        //{
        //    hp -= damage;
        //    hp = Mathf.Clamp(hp, 0, maxhp);
        //    UpdateColor(hp);
        //}
    }
}
