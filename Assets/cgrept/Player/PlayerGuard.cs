using UnityEngine;

/// <summary>
/// プレイヤーのガード・回避動作を制御するスクリプト。
/// - Gキーで回避アニメーションをトリガー
/// - 回避中にクリックしたら「回避→攻撃」へスムーズに遷移
/// - Animator の各種パラメータを自動的に制御
/// </summary>
public class PlayerGuard : MonoBehaviour
{
    // ==============================================================
    // Animator 関連設定
    // ==============================================================
    [Header("Animator")]
    [SerializeField] Animator animator;     // 操作対象の Animator
    [SerializeField] int animatorLayer = 0; // 使用レイヤー（通常は0）

    // ==============================================================
    // 回避（Evade）関連設定
    // ==============================================================
    [Header("Evade Params")]
    public string paramEvadeTrigger = "EvadeTrigger"; // 回避開始用トリガー（AnyState→Evade_Core）
    public string paramIsEvading = "IsEvading";       // 回避中フラグ（他スクリプトが参照）
    public string paramEvadeHeld = "EvadeHeld";       // G押しっぱフラグ（Hold用ステート遷移など）

    // ==============================================================
    // 攻撃遷移用（回避直後の攻撃に繋ぐため）
    // ==============================================================
    [Header("Attack Params（回避直後の攻撃へ繋ぐため）")]
    public string paramIsAttacking = "IsAttacking"; // 攻撃中フラグ
    public string paramAttackTrigger = "AttackTrigger"; // 攻撃開始用トリガー
    public string paramAttackIndex = "AttackIndex"; // 攻撃段数（コンボ制御など）
    [Tooltip("攻撃の初段ステート名（Animator内の正確な名前）")]
    public string stateAttack1Core = "Attack1_Core"; // 攻撃初段ステート名（Animator内の名称）
    [Header("Guard Object（ガード中だけ表示）")]
    public GameObject guardObject;        // 表示したいオブジェクト
    // ==============================================================
    // 調整パラメータ
    // ==============================================================
    [Header("Tuning")]
    public float minInterval = 0.08f;           // Gキー連打防止（連続入力間隔）
    public float postEvadeAttackWindow = 0.25f; // 回避終了後のクリック猶予（0.25秒以内なら攻撃可能）

    // 内部制御用変数
    float lastEvadePressedTime; // 最後にGキーを押した時刻
    bool queuedAttack;          // 回避中にクリック入力を保持するフラグ
    float lastMouseClickTime;   // 直近のマウスクリック時刻
    private PlayerMovement playerMovement;
    // ==============================================================
    // 初期化処理
    // ==============================================================
    void Reset()
    {
        // 自動で子オブジェクトからAnimatorを取得
        animator = GetComponentInChildren<Animator>();
    }
    void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
    }
    // ==============================================================
    // 毎フレーム処理（入力監視＆状態制御）
    // ==============================================================
    void Update()
    {
        if (animator == null) return;

        // Gキー押下＆押しっぱ判定
        bool gDown = playerMovement.Guard_PressedThisFrame();
        bool gHeld = playerMovement.Guard_isPressed();

        // 左クリック押下判定

        bool clickDown = playerMovement.Atk_PressedThisFrame();
        if (clickDown) lastMouseClickTime = Time.time; // 最後のクリック時刻を記録

        // 回避中にクリックしたら攻撃入力をキュー（保持）
        if (GetBoolSafe(paramIsEvading) && clickDown)
            queuedAttack = true;

        // Gキーで回避開始（一定間隔を空ける）
        if (gDown && Time.time - lastEvadePressedTime >= minInterval)
        {

            lastEvadePressedTime = Time.time;
            StartEvade(); // 回避開始処理
        }

        // G押しっぱ状態をAnimatorに伝える（Holdアニメなど用）
        SetBoolIfExists(paramEvadeHeld, gHeld);
    }

    // ==============================================================
    // 回避処理開始
    // ==============================================================
    void StartEvade()
    {
        // ---- 進行中の攻撃を安全に中断 ----
        var atk = GetComponent<player_attack>() ?? GetComponentInChildren<player_attack>();
        if (atk != null) atk.ForceCancelAttack(); // 攻撃スクリプトのキャンセル処理

        // ---- Animatorの攻撃関連をリセット ----
        SetBoolIfExists(paramIsAttacking, false);
        ResetTriggerIfExists(paramAttackTrigger);
        SetIntIfExists(paramAttackIndex, 0);

        // ---- 回避ロックON ----
        SetBoolIfExists(paramIsEvading, true);

        // ---- G押しっぱ反映 ----

        SetBoolIfExists(paramEvadeHeld, playerMovement.Guard_isPressed());

        // ---- Evade_Core ステートへ遷移 ----
        ResetTriggerIfExists(paramEvadeTrigger);
        SetTriggerIfExists(paramEvadeTrigger);

        // ---- 攻撃キューを初期化 ----
        queuedAttack = false;
    }

    // ==============================================================
    // 回避アニメーション終了時（AnimationEventから呼ぶ）
    // ==============================================================
    public void EvadeFinishEnd()
    {
        // 回避ロック解除
        SetBoolIfExists(paramIsEvading, false);
        SetBoolIfExists(paramEvadeHeld, false);

        // 回避中 or 終了直後にクリックしていた場合 → 攻撃へ
        bool justClicked = (Time.time - lastMouseClickTime) <= postEvadeAttackWindow;
        if (queuedAttack || justClicked)
        {
            StartCoroutine(StartAttackNextFrame());
        }

        // 攻撃キューリセット
        queuedAttack = false;
    }

    // ==============================================================
    // 攻撃開始（1フレーム遅らせて確実に遷移）
    // ==============================================================
    System.Collections.IEnumerator StartAttackNextFrame()
    {
        yield return null; // 1フレーム待機（アニメーション遷移安定化）

        // 攻撃パラメータを整える
        SetBoolIfExists(paramIsAttacking, true);
        SetIntIfExists(paramAttackIndex, 0);

        // 攻撃ステート名をHash化して直接クロスフェード
        int hash = Animator.StringToHash(stateAttack1Core);
        if (animator.HasState(animatorLayer, hash))
        {
            // 攻撃初段ステートに遷移
            animator.CrossFadeInFixedTime(stateAttack1Core, 0.05f, animatorLayer, 0f);
        }
        else
        {
            // ステート名が一致しない場合はトリガーで代替
            ResetTriggerIfExists(paramAttackTrigger);
            SetTriggerIfExists(paramAttackTrigger);
            Debug.LogWarning($"[PlayerGuard] Attack state not found: {stateAttack1Core}");
        }
    }

    // ==============================================================
    // Helper 関数群（Animatorパラメータを安全に操作）
    // ==============================================================
    bool HasParam(string name, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(name)) return false;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }

    bool GetBoolSafe(string name)
    {
        if (animator == null || string.IsNullOrEmpty(name)) return false;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Bool)
                return animator.GetBool(name);
        return false;
    }

    void SetBoolIfExists(string name, bool v)
    {
        if (animator == null || string.IsNullOrEmpty(name)) return;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(name, v);
                return;
            }
    }

    void SetIntIfExists(string name, int v)
    {
        if (animator == null || string.IsNullOrEmpty(name)) return;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Int)
            {
                animator.SetInteger(name, v);
                return;
            }
    }

    void ResetTriggerIfExists(string name)
    {
        if (animator == null || string.IsNullOrEmpty(name)) return;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(name);
                return;
            }
    }

    void SetTriggerIfExists(string name)
    {
        if (animator == null || string.IsNullOrEmpty(name)) return;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(name);
                return;
            }
    }
}
