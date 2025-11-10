using UnityEngine;
using UnityEngine.InputSystem; // Mouse.current, Keyboard.current
using System.Collections;      // IEnumerator / Coroutines

public class player_attack : MonoBehaviour
{
    // ====== (A) ヒットボックス／攻撃力 ======
    [System.Serializable]
    public struct AttackTiming
    {
        [Header("攻撃判定までの遅延時間 (秒)")]
        [Range(0f, 0.2f)] public float delay;

        [Header("攻撃判定が有効な時間 (秒)")]
        [Range(0.05f, 1.5f)] public float activeTime;
    }

    [Header("Animator（Baseレイヤーのみなら 0 のまま）")]
    [SerializeField] Animator animator;
    [SerializeField] int animatorLayer = 0;

    [Header("攻撃判定スクリプト（刀などの当たり判定制御）")]
    [SerializeField] player_attack_hit attackHit;

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

    [Header("攻撃段階ごとの固定攻撃力")]
    [Tooltip("1段目,2段目,3段目の順")]
    public float[] attackPowers = new float[] { 10f, 20f, 30f };

    [Header("攻撃段階ごとのタイミング（遅延・有効時間）")]
    public AttackTiming[] attackTimings = new AttackTiming[]
    {
        new AttackTiming { delay = 0.00f, activeTime = 0.30f }, // 1段目
        new AttackTiming { delay = 0.00f, activeTime = 0.30f }, // 2段目
        new AttackTiming { delay = 0.02f, activeTime = 0.35f }, // 3段目
    };

    [Header("Cancel Settings（移動キャンセル制御）")]
    [Tooltip("攻撃開始直後、誤キャンセル防止の遅延秒")]
    public float cancelMinDelay = 0.10f;
    [Tooltip("WASDのデッドゾーン")]
    public float moveDeadzone = 0.20f;

    // ====== (B) 溜め攻撃 ======
    [Header("Charge Attack（溜め攻撃）")]
    [Tooltip("溜めループ（Loop ON）")]
    public string stateChargeLoop = "QUICK_SHIFT B";
    [Tooltip("解放（Loop OFF）")]
    public string stateChargeRelease = "SKILL 2";
    [Tooltip("長押し判定（秒）")]
    public float holdThreshold = 0.20f;
    [Tooltip("解放が終わらない時の保険（秒）")]
    public float chargeReleaseFailSafe = 1.2f;
    [Tooltip("溜め中ONにするBool。空なら未使用")]
    public string paramIsCharging = "IsCharging";

    [Header("Animator Parameters")]
    public string paramAttackTrigger = "AttackTrigger";
    public string paramAttackIndex = "AttackIndex";
    public string paramIsAttacking = "IsAttacking";

    // ====== 内部状態 ======
    int currentCore = -1;   // 0/1/2（-1 は非攻撃）
    bool isAttacking = false;
    bool queueOpen = false;  // Core受付ウィンドウ
    bool queuedNext = false;  // 次段連打受付
    bool cancelAfterCore = false;
    bool runHeldAtCancel = false;

    bool moveHeldAtAttackStart = false; // 押しっぱ開始？
    bool moveReleasedSinceStart = false;
    float cancelUnlockTime = 0f;         // この時刻以降はキャンセル監視ON

    // Hitbox関連
    float attackPower = 0f;

    // 溜め関連（非戦闘時の短/長押し判定）
    bool waitingClassify = false;
    float pressTime = 0f;
    bool isCharging = false;

    // コンボ中の長押し → Core終端で溜めへ
    bool comboHoldCounting = false;
    float comboHoldStartTime = 0f;
    bool chargeQueuedDuringCombo = false;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        bool clickDown = mouse.leftButton.wasPressedThisFrame;
        bool clickHeld = mouse.leftButton.isPressed;
        bool clickUp = mouse.leftButton.wasReleasedThisFrame;

        // ===== (0) コンボ中の長押し検知 =====
        if (isAttacking && !isCharging)
        {
            if (clickHeld)
            {
                if (!comboHoldCounting)
                {
                    comboHoldCounting = true;
                    comboHoldStartTime = Time.time;
                }
                else if ((Time.time - comboHoldStartTime) >= holdThreshold)
                {
                    chargeQueuedDuringCombo = true; // Core終端で溜めへ
                }
            }
            if (clickUp)
            {
                comboHoldCounting = false;
                comboHoldStartTime = 0f;
            }
        }

        // ===== (1) 溜め／解放の司令塔（非戦闘時） =====
        if (isCharging)
        {
            if (clickUp) ReleaseCharge(); // SKILL2 へ
            return;
        }

        if (!isAttacking)
        {
            if (waitingClassify)
            {
                if (clickHeld && (Time.time - pressTime) >= holdThreshold)
                {
                    StartCharge(); // 非戦闘時：長押しで即溜め
                    return;
                }
                if (clickUp)
                {
                    waitingClassify = false;
                    StartCombo();   // 短押し→通常攻撃
                    return;
                }
            }
            else
            {
                if (clickDown)
                {
                    waitingClassify = true;
                    pressTime = Time.time;
                    return;
                }
            }
        }

        // ===== (2) 通常攻撃：移動キャンセル監視 =====
        bool movingNow = IsMovePressedRaw();
        if (isAttacking && moveHeldAtAttackStart && !moveReleasedSinceStart)
        {
            if (!movingNow) moveReleasedSinceStart = true;
        }

        if (isAttacking && Time.time >= cancelUnlockTime && HasMoveInput(out bool runHeld))
        {
            bool allowCancel =
                (!moveHeldAtAttackStart) || (moveHeldAtAttackStart && moveReleasedSinceStart);

            if (allowCancel)
            {
                if (!IsInFinishState())
                {
                    cancelAfterCore = true;
                    runHeldAtCancel = runHeld;
                    queuedNext = false; // 連打より移動を優先
                }
            }
        }

        // 受付中の連打
        if (isAttacking && queueOpen && clickDown) queuedNext = true;
    }

    // ====== 溜め：開始／解放／保険 ======
    void StartCharge()
    {
        waitingClassify = false;
        isCharging = true;

        SetBoolIfExists(paramIsAttacking, true); // 移動ロック用
        if (!string.IsNullOrEmpty(paramIsCharging)) SetBoolIfExists(paramIsCharging, true);

        // 進行中の通常攻撃の“内部”をクリア（Paramは極力触らない）
        ForceCancelAttackInternalsOnly();

        CrossFadeSafe(stateChargeLoop, 0.05f); // ループへ
    }

    void ReleaseCharge()
    {
        isCharging = false;

        // 解放中もロック維持（SKILL2 中は動かない）
        SetBoolIfExists(paramIsAttacking, true);
        if (!string.IsNullOrEmpty(paramIsCharging)) SetBoolIfExists(paramIsCharging, false);

        // 自動攻撃を防ぐ掃除
        animator.ResetTrigger(paramAttackTrigger);
        animator.SetInteger(paramAttackIndex, 0);

        CrossFadeSafe(stateChargeRelease, 0.05f); // SKILL2 へ

        // イベント保険
        StopAllCoroutines();
        StartCoroutine(ChargeReleaseFailSafe());
    }

    IEnumerator ChargeReleaseFailSafe()
    {
        yield return new WaitForSeconds(chargeReleaseFailSafe);

        if (GetBoolIfExists(paramIsAttacking))
            SetBoolIfExists(paramIsAttacking, false);

        animator.ResetTrigger(paramAttackTrigger);
        animator.SetInteger(paramAttackIndex, 0);
    }

    // SKILL2 の最後の直前フレームに Animation Event で呼ぶ
    public void ChargeReleaseEnd()
    {
        SetBoolIfExists(paramIsAttacking, false);
        animator.ResetTrigger(paramAttackTrigger);
        animator.SetInteger(paramAttackIndex, 0);
        if (!string.IsNullOrEmpty(paramIsCharging)) SetBoolIfExists(paramIsCharging, false);
    }

    // ====== 通常攻撃：開始／終了 ======
    void StartCombo()
    {
        isAttacking = true;
        queueOpen = false;
        queuedNext = false;
        cancelAfterCore = false;

        currentCore = 0; // 初段
        chargeQueuedDuringCombo = false;
        comboHoldCounting = false;
        comboHoldStartTime = 0f;

        moveHeldAtAttackStart = IsMovePressedRaw();
        moveReleasedSinceStart = !moveHeldAtAttackStart;

        SetBoolIfExists(paramIsAttacking, true);
        animator.SetInteger(paramAttackIndex, 0);

        CrossFadeSafe(stateAttack1_Core, 0.05f);

        // Core突入時にすでに押されていれば、この段の長押し計測を開始
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            comboHoldCounting = true;
            comboHoldStartTime = Time.time;
        }

        // ★ヒットボックス（1段目）
        TryStartHitboxCoroutine(0);

        cancelUnlockTime = Time.time + cancelMinDelay;
    }

    void EndCombo()
    {
        queueOpen = false;
        queuedNext = false;

        animator.SetInteger(paramAttackIndex, 0);

        string finish = GetFinishStateName(currentCore);
        if (!string.IsNullOrEmpty(finish) && animator.HasState(animatorLayer, Animator.StringToHash(finish)))
        {
            CrossFadeSafe(finish, 0.05f);
            // Finish末尾に Animation Event: FinishEnd() を置く
        }
        else
        {
            // 保険：直接ロコモーションへ
            bool moving = GetBoolIfExists("IsMoving");
            bool running = GetBoolIfExists("IsRunning");
            string dest = running ? stateRun : (moving ? stateWalk : stateIdle);
            CrossFadeSafe(dest, 0.05f);

            isAttacking = false;
            SetBoolIfExists(paramIsAttacking, false);
        }

        currentCore = -1;
    }

    // 次段へ（CoreEnd から呼ぶ）
    void GoNextCore()
    {
        queuedNext = false;
        currentCore = (currentCore + 1) % 3;

        animator.SetInteger(paramAttackIndex, currentCore);
        animator.ResetTrigger(paramAttackTrigger);
        animator.SetTrigger(paramAttackTrigger); // AnyState→攻撃入口

        // ★ヒットボックス（段に応じて）
        TryStartHitboxCoroutine(currentCore);

        cancelUnlockTime = Time.time + cancelMinDelay;
        cancelAfterCore = false;

        // 新Core突入時、押下継続なら長押し計測を再スタート
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            comboHoldCounting = true;
            comboHoldStartTime = Time.time;
        }
        else
        {
            comboHoldCounting = false;
            comboHoldStartTime = 0f;
        }
    }

    // ===== Animation Events（Core中に配置） =====
    public void OpenQueue()
    {
        queueOpen = true;
        queuedNext = false;
    }

    public void CloseQueue()
    {
        queueOpen = false;
    }

    // Core終端（最重要分岐）
    public void CoreEnd()
    {
        // 攻撃判定OFF（安全）
        HitboxOff();

        queueOpen = false;

        if (cancelAfterCore)
        {
            CancelToLocomotion(runHeldAtCancel); // Core完走→移動へ
            // 溜めキューは破棄
            chargeQueuedDuringCombo = false;
            comboHoldCounting = false;
            comboHoldStartTime = 0f;
        }
        else if (chargeQueuedDuringCombo)
        {
            // ★優先度：溜め > 次段連打
            chargeQueuedDuringCombo = false;
            comboHoldCounting = false;
            comboHoldStartTime = 0f;
            StartCharge(); // 溜めへ切替
        }
        else if (queuedNext)
        {
            GoNextCore();
        }
        else
        {
            EndCombo(); // 連打なし→Finishへ
        }
    }

    // Finish末尾（Animation Event）
    public void FinishEnd()
    {
        isAttacking = false;
        SetBoolIfExists(paramIsAttacking, false);

        // 既に移動入力があるならWalk/Runへ
        if (HasMoveInput(out bool runHeld))
        {
            if (HasParam(animator, "IsMoving")) animator.SetBool("IsMoving", true);
            if (HasParam(animator, "IsRunning")) animator.SetBool("IsRunning", runHeld);
            string dest = runHeld ? stateRun : stateWalk;
            CrossFadeSafe(dest, 0.05f);
        }
    }

    // ===== キャンセル（Core完走後に移動へ） =====
    void CancelToLocomotion(bool runHeld)
    {
        isAttacking = false;
        cancelAfterCore = false;
        queuedNext = false;

        SetBoolIfExists(paramIsAttacking, false);
        animator.SetInteger(paramAttackIndex, 0);
        animator.ResetTrigger(paramAttackTrigger);

        if (HasParam(animator, "IsMoving")) animator.SetBool("IsMoving", true);
        if (HasParam(animator, "IsRunning")) animator.SetBool("IsRunning", runHeld);

        string dest = runHeld ? stateRun : stateWalk;
        CrossFadeSafe(dest, 0.05f);
    }

    // ===== 外部（ガードなど）からの強制終了 =====
    public void ForceCancelAttack()
    {
        waitingClassify = false;
        isCharging = false;

        chargeQueuedDuringCombo = false;
        comboHoldCounting = false;
        comboHoldStartTime = 0f;

        isAttacking = false;
        queueOpen = false;
        queuedNext = false;
        cancelAfterCore = false;
        runHeldAtCancel = false;
        moveHeldAtAttackStart = false;
        moveReleasedSinceStart = false;

        if (!string.IsNullOrEmpty(paramIsCharging)) SetBoolIfExists(paramIsCharging, false);
        SetBoolIfExists(paramIsAttacking, false);
        animator.SetInteger(paramAttackIndex, 0);
        animator.ResetTrigger(paramAttackTrigger);

        // 念のため攻撃判定OFF
        HitboxOff();
        StopAllCoroutines();
    }

    // （溜め開始時など内部のみ素早く停止：Paramは最小限）
    void ForceCancelAttackInternalsOnly()
    {
        isAttacking = false;
        queueOpen = false;
        queuedNext = false;
        cancelAfterCore = false;
        runHeldAtCancel = false;
        moveHeldAtAttackStart = false;
        moveReleasedSinceStart = false;

        chargeQueuedDuringCombo = false;
        comboHoldCounting = false;
        comboHoldStartTime = 0f;

        animator.ResetTrigger(paramAttackTrigger);
        animator.SetInteger(paramAttackIndex, 0);

        HitboxOff();
        StopAllCoroutines();
    }

    // ====== Helpers ======
    // ★★★ player_attack_hit.cs 互換：現在の攻撃段（0/1/2）を返す ★★★
    public int GetCurrentAttackIndex()
    {
        // -1（非攻撃）の場合は 0 を返す（プレースホルダ）
        return Mathf.Clamp(currentCore, 0, 2);
    }

    // ヒットボックス（段に応じてコルーチン開始）
    void TryStartHitboxCoroutine(int coreIndex)
    {
        if (attackHit == null || attackTimings == null) return;
        if (coreIndex < 0 || coreIndex >= attackTimings.Length) return;

        var timing = attackTimings[coreIndex];
        StartCoroutine(EnableHitboxWithDelay(timing.delay, timing.activeTime, coreIndex));
    }

    IEnumerator EnableHitboxWithDelay(float delay, float activeTime, int attackIndex)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // 攻撃力セット
        attackPower = SetAttackPowerByIndex(attackIndex);

        attackHit.EnableHitbox();
        if (activeTime > 0f) yield return new WaitForSeconds(activeTime);
        attackHit.DisableHitbox();
    }

    public void HitboxOff()
    {
        if (attackHit != null) attackHit.DisableHitbox();
    }

    // ★★★ player_attack_hit.cs 互換：攻撃力の設定（既存メソッド） ★★★
    public float SetAttackPowerByIndex(int index)
    {
        if (attackPowers != null && index >= 0 && index < attackPowers.Length)
            attackPower = attackPowers[index];
        else
            attackPower = (attackPowers != null && attackPowers.Length > 0) ? attackPowers[0] : 10f;

        return attackPower;
    }

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
}
