using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class hpbar : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Slider hpSlider;      
    [SerializeField] private Slider damageSlider;  

    [Header("HP Değerleri")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private float damage = 25f;

    [Header("Ayarlar")]
    [SerializeField] private float delayTime = 3f;  
    [SerializeField] private float lerpSpeed = 1f;   

    private Coroutine damageRoutine;

    private void Start()
    {
        hpSlider.minValue = 0;
        hpSlider.maxValue = maxHP;
        damageSlider.minValue = 0;
        damageSlider.maxValue = maxHP;

        hpSlider.value = maxHP;
        damageSlider.value = maxHP;
    }

    private void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            TakeDamage(damage);
        }
    }

    private void TakeDamage(float amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        hpSlider.value = currentHP;

        if (damageRoutine != null)
            StopCoroutine(damageRoutine);

        
        damageRoutine = StartCoroutine(UpdateDamageBar());
    }

    private IEnumerator UpdateDamageBar()
    {
        
        yield return new WaitForSeconds(delayTime);

        
        while (hpSlider.value <= damageSlider.value)
        {
            damageSlider.value = Mathf.Lerp(damageSlider.value, hpSlider.value, Time.deltaTime * lerpSpeed);
            yield return new WaitForEndOfFrame();
        }

        damageSlider.value = hpSlider.value; 
    }
}