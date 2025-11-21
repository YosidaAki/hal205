using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Player_die : MonoBehaviour
{
    [Header("Death Debug")]
    public bool EnableDeathDebug = true; // ON = HP0なら死ぬ, OFF = HP0でも死なない

    [Header("Animator Settings")]
    public Animator animator;
    public string dieStateName = "Die1";
    public int layer = 0;

    [Header("Hitstop Settings (実時間で止める)")]
    [Tooltip("秒数（Realtime）: 例えば 0.15f")]
    public float hitstopDuration = 0.15f;

    [Header("Scene Change Settings")]
    [Tooltip("空文字だとシーン遷移しません")]
    public string sceneToLoad = ""; // 例: "GameOver"
    [Tooltip("true = ヒットストップ後にシーン切替（アニメはシーン切替後に再生されません）。false = ヒットストップ後に死亡アニメを再生し、その後シーン切替（もし sceneToLoad が指定されていれば）。")]
    public bool changeSceneBeforeAnimation = true;
    [Tooltip("アニメ再生後にシーン切替する場合の遅延（秒）")]
    public float delayBeforeSceneLoadAfterAnimation = 1.5f;

    private bool isDead = false;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!EnableDeathDebug) return;

        float hp = ReadHP();
        if (!isDead && hp <= 0f)
        {
            StartCoroutine(DieRoutine());
        }
    }

    float ReadHP()
    {
        if (hpbar.Instance == null) return 1f;

        // Slider にアクセスせず、内部の currentHP を返すよう hpbar 側に関数を作る想定
        return hpbar.Instance.GetCurrentHP();
    }

    IEnumerator DieRoutine()
    {
        isDead = true;

        // まず操作を無効化（ヒットストップ中にさらに操作されないように）
        DisablePlayerControls();

        // --- ヒットストップ（Realtime） ---
        if (hitstopDuration > 0f)
        {
            float prevTimeScale = Time.timeScale;
            Time.timeScale = 0f; // ゲーム時間を止める
            yield return new WaitForSecondsRealtime(hitstopDuration);
            Time.timeScale = prevTimeScale; // 復帰
        }

        // --- シーン切替を先に行う設定ならここで切替 ---
        if (!string.IsNullOrEmpty(sceneToLoad) && changeSceneBeforeAnimation)
        {
            Debug.Log($"Player died. Hitstop done. Loading scene '{sceneToLoad}' now.");
            SceneManager.LoadScene(sceneToLoad);
            yield break; // シーン切替で現在のオブジェクトはアンロードされる
        }

        // --- 死亡アニメーション再生 ---
        if (animator != null)
        {
            animator.CrossFadeInFixedTime(dieStateName, 0.1f, layer);
        }

        Debug.Log("Player has died. (Animation played)");

        // --- アニメ後にシーン切替する設定なら待ってから切替 ---
        if (!string.IsNullOrEmpty(sceneToLoad) && !changeSceneBeforeAnimation)
        {
            // アニメの長さに合わせて適宜待つ。ここでは inspector の遅延値を利用。
            yield return new WaitForSeconds(delayBeforeSceneLoadAfterAnimation);
            Debug.Log($"Loading scene '{sceneToLoad}' after death animation.");
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    void DisablePlayerControls()
    {
        var pm = GetComponent<PlayerMovement>();
        if (pm) pm.enabled = false;

        var pMove = GetComponent<player_Move>();
        if (pMove) pMove.enabled = false;

        var atk = GetComponent<player_attack>();
        if (atk) atk.enabled = false;

        var guard = GetComponent<PlayerGuard>();
        if (guard) guard.enabled = false;
    }
}

