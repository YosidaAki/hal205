using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HPバーのUIを管理するスクリプト。
/// - Canvas上のSliderでHPを表示
/// - 外部スクリプト（BeamDamageなど）からHPを減らせる
/// - 静的アクセス対応（hpbar.Instance）
/// </summary>
public class hpbar : MonoBehaviour
{
    // ==============================================================
    // 静的インスタンス（どこからでもアクセス可能）
    // ==============================================================
    public static hpbar Instance;

    [Header("=== UI 参照 ===")]
    [SerializeField] private Slider hpSlider;      // 実際のHPバー（白い方）
    [SerializeField] private Slider damageSlider;  // 遅れて減る演出バー（赤い方）

    [Header("=== HP 設定 ===")]
    [SerializeField] private float maxHP = 100f;   // 最大HP
    [SerializeField] private float currentHP = 100f; // 現在のHP

    [Header("=== 演出設定 ===")]
    [SerializeField] private float delayTime = 3f; // 赤バーが遅れて減るまでの時間
    [SerializeField] private float lerpSpeed = 1f; // 赤バーが追従する速度

    private Coroutine damageRoutine;

    // Awakeで静的インスタンス登録
    private void Awake()
    {
        Instance = this;
        // このオブジェクトがシーンを跨いでも破棄されないようにする（任意）
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // スライダー初期設定
        hpSlider.minValue = 0;
        hpSlider.maxValue = maxHP;
        damageSlider.minValue = 0;
        damageSlider.maxValue = maxHP;

        hpSlider.value = maxHP;
        damageSlider.value = maxHP;

    }

    // ==============================================================
    // HPを減らす（内部処理）
    // ==============================================================
    private void TakeDamage(float amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP); // 0以下にしない
        hpSlider.value = currentHP; // メインバー更新

        // ダメージアニメーション処理
        if (damageRoutine != null)
            StopCoroutine(damageRoutine);
        damageRoutine = StartCoroutine(UpdateDamageBar());

        // デバッグ：HP残量表示
        Debug.Log($"[hpbar] Current HP: {currentHP}/{maxHP}");
    }

    // ==============================================================
    // 外部（BeamDamageなど）から呼び出す用
    // ==============================================================
    public void TakeDamageFromExternal(float amount)
    {
        TakeDamage(amount);
    }

    // ==============================================================
    // 赤バーの追従演出コルーチン
    // ==============================================================
    private IEnumerator UpdateDamageBar()
    {
        // 指定時間待ってから赤バーをゆっくり追従
        yield return new WaitForSeconds(delayTime);

        while (hpSlider.value <= damageSlider.value)
        {
            damageSlider.value = Mathf.Lerp(damageSlider.value, hpSlider.value, Time.deltaTime * lerpSpeed);
            yield return new WaitForEndOfFrame();
        }
        damageSlider.value = hpSlider.value;
    }
}
