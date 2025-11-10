using UnityEngine;
using UnityEngine.InputSystem; // Keyboard.current / Mouse.current

public class PlayerGuard : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] Animator animator;
    [SerializeField] int animatorLayer = 0; // 通常 0

    [Header("Evade Params")]
    public string paramEvadeTrigger = "EvadeTrigger"; // AnyState→Evade_Core 開始用
    public string paramIsEvading = "IsEvading";     // 回避ロック（move側参照する場合に使用）
    public string paramEvadeHeld = "EvadeHeld";     // G押しっぱ（Core→Hold分岐に使う）

    [Header("Attack Params（回避直後の攻撃へ繋ぐため）")]
    public string paramIsAttacking = "IsAttacking";
    public string paramAttackTrigger = "AttackTrigger";
    public string paramAttackIndex = "AttackIndex";
    [Tooltip("攻撃の初段ステート名（Animator内の正確な名前）")]
    public string stateAttack1Core = "Attack1_Core";

    [Header("Tuning")]
    public float minInterval = 0.08f;           // G連打抑止
    public float postEvadeAttackWindow = 0.25f; // Evade終了直後のクリック猶予

    float lastEvadePressedTime;
    bool queuedAttack;         // ガード中クリックを保持
    float lastMouseClickTime;  // 直近クリック時刻

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (animator == null) return;

        var kb = Keyboard.current;
        var mouse = Mouse.current;

        bool gDown = kb != null && kb.gKey.wasPressedThisFrame;
        bool gHeld = kb != null && kb.gKey.isPressed;

        bool clickDown = mouse != null && mouse.leftButton.wasPressedThisFrame;
        if (clickDown) lastMouseClickTime = Time.time;

        // ガード中のクリックはキュー
        if (GetBoolSafe(paramIsEvading) && clickDown)
            queuedAttack = true;

        // G押下でEvade開始
        if (gDown && Time.time - lastEvadePressedTime >= minInterval)
        {
            lastEvadePressedTime = Time.time;
            StartEvade();
        }

        // 押しっぱ管理（Core→Hold/Finish分岐用）
        SetBoolIfExists(paramEvadeHeld, gHeld);
    }

    void StartEvade()
    {
        // 進行中の攻撃を安全に中断
        var atk = GetComponent<player_attack>() ?? GetComponentInChildren<player_attack>();
        if (atk != null) atk.ForceCancelAttack();

        // Animator側の保険
        SetBoolIfExists(paramIsAttacking, false);
        ResetTriggerIfExists(paramAttackTrigger);
        SetIntIfExists(paramAttackIndex, 0);

        // 回避ロックON
        SetBoolIfExists(paramIsEvading, true);

        // 押しっぱ状態反映
        SetBoolIfExists(paramEvadeHeld, Keyboard.current != null && Keyboard.current.gKey.isPressed);

        // Evade_Coreへ
        ResetTriggerIfExists(paramEvadeTrigger);
        SetTriggerIfExists(paramEvadeTrigger);

        // ここからのクリックをキュー対象に
        queuedAttack = false;
    }

    // === Evade_Finish 末尾 AnimationEvent から呼ぶ ===
    public void EvadeFinishEnd()
    {
        // 回避ロックOFF
        SetBoolIfExists(paramIsEvading, false);
        SetBoolIfExists(paramEvadeHeld, false);

        // ガード中/直後のクリックで攻撃へ
        bool justClicked = (Time.time - lastMouseClickTime) <= postEvadeAttackWindow;
        if (queuedAttack || justClicked)
        {
            StartCoroutine(StartAttackNextFrame());
        }

        queuedAttack = false;
    }

    System.Collections.IEnumerator StartAttackNextFrame()
    {
        // 1フレ待ってから遷移評価を確実に
        yield return null;

        // 先にIsAttacking/Indexを整える
        SetBoolIfExists(paramIsAttacking, true);
        SetIntIfExists(paramAttackIndex, 0);

        // Attack1_Core に直接クロスフェード
        int hash = Animator.StringToHash(stateAttack1Core);
        if (animator.HasState(animatorLayer, hash))
        {
            animator.CrossFadeInFixedTime(stateAttack1Core, 0.05f, animatorLayer, 0f);
        }
        else
        {
            // ステート名が違った場合のフォールバック
            ResetTriggerIfExists(paramAttackTrigger);
            SetTriggerIfExists(paramAttackTrigger);
            Debug.LogWarning($"[PlayerGuard] Attack state not found: {stateAttack1Core}");
        }
    }

    // ---------- Helpers ----------
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
            { animator.SetBool(name, v); return; }
    }

    void SetIntIfExists(string name, int v)
    {
        if (animator == null || string.IsNullOrEmpty(name)) return;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Int)
            { animator.SetInteger(name, v); return; }
    }

    void ResetTriggerIfExists(string name)
    {
        if (animator == null || string.IsNullOrEmpty(name)) return;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Trigger)
            { animator.ResetTrigger(name); return; }
    }

    void SetTriggerIfExists(string name)
    {
        if (animator == null || string.IsNullOrEmpty(name)) return;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Trigger)
            { animator.SetTrigger(name); return; }
    }
}
