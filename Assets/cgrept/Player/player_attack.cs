using UnityEngine;
using UnityEngine.InputSystem; // Mouse.current, Keyboard.current

public class player_attack : MonoBehaviour
{
    [Header("Animator（Baseレイヤーのみなら 0 のまま）")]
    [SerializeField] Animator animator;
    [SerializeField] int animatorLayer = 0;

    [Header("攻撃判定スクリプト")]   // ★ 追加
    [SerializeField] player_attack_hit attackHit;  // ★ 追加

    [Header("Attack Core States（Animator のステート名に合わせる）")]
    public string stateAttack1_Core = "Attack1_Core";
    public string stateAttack2_Core = "Attack2_Core";
    public string stateAttack3_Core = "Attack3_Core";

    [Header("Attack Finish States（各攻撃の締め/納刀）")]
    public string stateAttack1_Finish = "Attack1_Finish";
    public string stateAttack2_Finish = "Attack2_Finish";
    public string stateAttack3_Finish = "Attack3_Finish";

    [Header("Locomotion（名orフルパス）")]
    public string stateIdle = "Idle";
    public string stateWalk = "Walk";
    public string stateRun = "Run";

    [Header("Cancel Settings")]
    [Tooltip("攻撃開始直後は誤キャンセル防止のため、キャンセル監視を有効化するまでの遅延秒")]
    public float cancelMinDelay = 0.10f;
    [Tooltip("移動入力のデッドゾーン（WASDのベクトル長）")]
    public float moveDeadzone = 0.20f;

    // 内部状態
    int currentCore = -1;   // いま再生中の Core（0/1/2）
    bool isAttacking = false;
    bool queueOpen = false; // 受付ウィンドウ開閉（OpenQueue/CoreEnd で制御）
    bool queuedNext = false; // 受付中に連打が入ったか
    int atk = 0;
    // 「今のCoreを完走したら移動に戻す」ための保留フラグ
    bool cancelAfterCore = false;
    bool runHeldAtCancel = false; // Shiftが押されていたか

    // 「押しっぱ開始→一度離すまでキャンセル無効」用
    bool moveHeldAtAttackStart = false;
    bool moveReleasedSinceStart = false;

    float cancelUnlockTime = 0f; // この時刻以降に移動入力を監視

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // 左クリック“した瞬間”のみ取得（ホールドは無視）
        bool clickDown = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        // ===== 攻撃していない：クリックで開始 =====
        if (!isAttacking)
        {
            if (clickDown) StartCombo();
            return;
        }

        // ===== 押しっぱ開始→一度離すまでの監視 =====
        bool movingNow = IsMovePressedRaw();
        if (moveHeldAtAttackStart && !moveReleasedSinceStart)
        {
            // 開始時に押されていた → 一度でも離されたら true に
            if (!movingNow) moveReleasedSinceStart = true;
        }

        // ===== 攻撃/納刀中：移動入力の監視（ただし条件を満たすまでキャンセル無効）=====
        if (Time.time >= cancelUnlockTime && HasMoveInput(out bool runHeld))
        {
            bool allowCancel =
                // 開始時に押されていなかった → いつでもOK
                (!moveHeldAtAttackStart)
                // 押しっぱ開始だった → 一度離されてから再度押した時だけOK
                || (moveHeldAtAttackStart && moveReleasedSinceStart);

            if (allowCancel)
            {
                // Finish中は最後まで再生したいので何もしない（FinishEndで復帰）
                if (!IsInFinishState())
                {
                    // Core中に移動入力が来た → このCoreが終わったら移動に戻す
                    cancelAfterCore = true;
                    runHeldAtCancel = runHeld;
                    // 連打予約は無効化（移動を優先）
                    queuedNext = false;
                }
            }
        }

        // 受付中：連打を受理
        if (queueOpen && clickDown) queuedNext = true;
    }

    // ====== 開始／終了 ======
    void StartCombo()
    {
        isAttacking = true;
        queueOpen = false;
        queuedNext = false;
        cancelAfterCore = false;

        currentCore = 0; // 初段
        HitboxOn();              // 攻撃判定ON ★
        // 「押しっぱ開始→一度離すまでキャンセル無効」初期化
        moveHeldAtAttackStart = IsMovePressedRaw(); // 押しっぱで始まったか
        moveReleasedSinceStart = !moveHeldAtAttackStart; // 押しっぱでない開始ならすでに解除済み扱い

        SetBoolIfExists("IsAttacking", true);
        animator.SetInteger("AttackIndex", 0);

        // クリック即、初段 Core へ（Triggerは叩かない：自己再入防止）
        CrossFadeSafe(stateAttack1_Core, 0.05f);
        // 少し遅れてから移動入力監視を有効化
        cancelUnlockTime = Time.time + cancelMinDelay;
    }

    void EndCombo()
    {
        // Finish 中も移動ロックを維持したいので、ここでは IsAttacking を落とさない
        queueOpen = false;
        queuedNext = false;

        animator.SetInteger("AttackIndex", 0);

        string finish = GetFinishStateName(currentCore);
        if (!string.IsNullOrEmpty(finish) && animator.HasState(animatorLayer, Animator.StringToHash(finish)))
        {
            CrossFadeSafe(finish, 0.05f);
            // Finish → Idle は Animator の Exit Time で戻る
            // Finish の末尾に Animation Event: FinishEnd() を置いておくこと
        }
        else
        {
            // 保険：直接ロコモーションへ（この場合は解除）
            bool moving = GetBoolIfExists("IsMoving");
            bool running = GetBoolIfExists("IsRunning");
            string dest = running ? stateRun : (moving ? stateWalk : stateIdle);
            CrossFadeSafe(dest, 0.05f);

            isAttacking = false;
            SetBoolIfExists("IsAttacking", false);
        }

        currentCore = -1;
    }

    // ====== 次段へ（CoreEnd から呼ばれる） ======
    void GoNextCore()
    {
        queuedNext = false;           // 消費

        HitboxOn();              // 攻撃判定ON ★

        currentCore = (currentCore + 1) % 3;

        animator.SetInteger("AttackIndex", currentCore);
        animator.ResetTrigger("AttackTrigger");
        animator.SetTrigger("AttackTrigger"); // AnyState→AttackX_Core（ExitTime=OFF前提）

        // 次のCoreに入ったら、再びキャンセル解禁までの遅延を入れる
        cancelUnlockTime = Time.time + cancelMinDelay;
        cancelAfterCore = false;
    }

    // ====== Animation Event から呼ぶ関数 ======
    // Core の“受付開始”フレームに置く
    public void OpenQueue()
    {
        queueOpen = true;
        queuedNext = false; // 前の入力はリセット
    }

    // （任意）受付を閉じたいときに Core 内で置く
    public void CloseQueue()
    {
        queueOpen = false;
    }

    // Core の“終端”フレームに置く（ここで次段/終了/移動復帰を確定）
    public void CoreEnd()
    {
        // 攻撃判定OFF ★
        HitboxOff();
        queueOpen = false; // ここで締める
        if (cancelAfterCore)
        {
            CancelToLocomotion(runHeldAtCancel);
        }
        else if (queuedNext)
        {
            GoNextCore();
        }
        else
        {
            EndCombo();
        }
    }

    // ★ Finish の“最後のフレーム”で呼ぶ（各 Finish クリップ末尾に Animation Event を追加）
    public void FinishEnd()
    {
        isAttacking = false;
        SetBoolIfExists("IsAttacking", false);

        // Finish後にすでに移動入力があるなら、即Walk/Runへ
        if (HasMoveInput(out bool runHeld))
        {
            if (HasParam(animator, "IsMoving")) animator.SetBool("IsMoving", true);
            if (HasParam(animator, "IsRunning")) animator.SetBool("IsRunning", runHeld);
            string dest = runHeld ? stateRun : stateWalk;
            CrossFadeSafe(dest, 0.05f);
        }
    }

    // ====== キャンセル処理（Core完走後に呼ばれる） ======
    void CancelToLocomotion(bool runHeld)
    {
        isAttacking = false;
        cancelAfterCore = false;
        queuedNext = false;

        SetBoolIfExists("IsAttacking", false);
        animator.SetInteger("AttackIndex", 0);
        animator.ResetTrigger("AttackTrigger");

        if (HasParam(animator, "IsMoving")) animator.SetBool("IsMoving", true);
        if (HasParam(animator, "IsRunning")) animator.SetBool("IsRunning", runHeld);

        string dest = runHeld ? stateRun : stateWalk;
        CrossFadeSafe(dest, 0.05f);
    }

    // ====== Helpers ======
    bool HasMoveInput(out bool runHeld)
    {
        runHeld = false;
        if (Keyboard.current == null) return false;

        float x = 0f, y = 0f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.wKey.isPressed) y += 1f;
        if (Keyboard.current.sKey.isPressed) y -= 1f;

        Vector2 v = new Vector2(x, y);
        if (v.sqrMagnitude > 1f) v = v.normalized;

        bool moving = v.magnitude >= moveDeadzone;
        runHeld = moving && Keyboard.current.leftShiftKey.isPressed;
        return moving;
    }

    // “押されているか”だけを見たいのでデッドゾーンなしの生押下判定
    bool IsMovePressedRaw()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current.wKey.isPressed ||
               Keyboard.current.aKey.isPressed ||
               Keyboard.current.sKey.isPressed ||
               Keyboard.current.dKey.isPressed;
    }

    bool IsInFinishState()
    {
        var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
        return info.IsName(stateAttack1_Finish) ||
               info.IsName(stateAttack2_Finish) ||
               info.IsName(stateAttack3_Finish);
    }

    string GetFinishStateName(int coreIdx)
    {
        switch (coreIdx)
        {
            case 0: return stateAttack1_Finish;
            case 1: return stateAttack2_Finish;
            case 2: return stateAttack3_Finish;
        }
        return null;
    }

    void CrossFadeSafe(string stateName, float fixedDuration)
    {
        int hash = Animator.StringToHash(stateName);
        if (animator.HasState(animatorLayer, hash))
            animator.CrossFadeInFixedTime(stateName, fixedDuration, animatorLayer, 0f);
        else
            Debug.LogWarning($"[player_attack] State not found on layer {animatorLayer}: {stateName}");
    }

    void SetBoolIfExists(string name, bool value)
    {
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Bool)
            { animator.SetBool(name, value); return; }
    }

    bool GetBoolIfExists(string name)
    {
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Bool)
                return animator.GetBool(name);
        return false;
    }

    bool HasParam(Animator anim, string name)
    {
        foreach (var p in anim.parameters) if (p.name == name) return true;
        return false;
    }
    // 現在の攻撃段階を返す（0=1段目, 1=2段目, 2=3段目）
    public int GetCurrentAttackIndex()
    {
        return atk = Mathf.Max(0, currentCore);
    }
    // アニメーションイベント用：攻撃判定ON
    public void HitboxOn()
    {
        attackHit.EnableHitbox();
        Debug.Log("[player_attack] Hitbox ON");
    }

    // アニメーションイベント用：攻撃判定OFF
    public void HitboxOff()
    {
        attackHit.DisableHitbox();
        Debug.Log("[player_attack] Hitbox OFF");
    }

}