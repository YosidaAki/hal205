using System.Collections;
using UnityEngine;

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
    public float[] attackPowers = new float[] { 10f, 20f, 30f };

    [Header("攻撃段階ごとのタイミング（遅延・有効時間）")]
    public AttackTiming[] attackTimings = new AttackTiming[]
    {
        new AttackTiming { delay = 0.00f, activeTime = 0.30f },
        new AttackTiming { delay = 0.00f, activeTime = 0.30f },
        new AttackTiming { delay = 0.02f, activeTime = 0.35f },
    };

    [Header("Cancel Settings")]
    public float cancelMinDelay = 0.10f;
    public float moveDeadzone = 0.20f;

    // ====== (B) 溜め攻撃 ======
    [Header("Charge Attack（溜め攻撃）")]
    public float chargeAttackPower = 50f;
    public string stateChargeLoop = "QUICK_SHIFT B";
    public string stateChargeRelease = "SKILL 2";
    public float holdThreshold = 0.30f;
    public float chargeReleaseFailSafe = 1.2f;
    public string paramIsCharging = "IsCharging";

    [Header("Animator Parameters")]
    public string paramAttackTrigger = "AttackTrigger";
    public string paramAttackIndex = "AttackIndex";
    public string paramIsAttacking = "IsAttacking";

    // ====== 内部状態 ======
    int currentCore = -1;
    bool isAttacking = false;
    bool queueOpen = false;
    bool queuedNext = false;
    bool cancelAfterCore = false;
    bool runHeldAtCancel = false;

    bool moveHeldAtAttackStart = false;
    bool moveReleasedSinceStart = false;
    float cancelUnlockTime = 0f;

    float attackPower = 0f;

    bool waitingClassify = false;
    float pressTime = 0f;
    bool isCharging = false;

    bool comboHoldCounting = false;
    float comboHoldStartTime = 0f;
    bool chargeQueuedDuringCombo = false;

    bool skill2OnCooldown = false;
    [SerializeField] float skill2CooldownTime = 1.0f;

    private PlayerMovement playerMovement;
    private GyroShooter gyroShooter;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        gyroShooter = FindFirstObjectByType<GyroShooter>();
    }

    void Update()
    {
        if (!playerMovement.Atk_isPressed() && !playerMovement.Atk_PressedThisFrame()) return;

        bool clickDown = playerMovement.Atk_PressedThisFrame();
        bool clickHeld = playerMovement.Atk_isPressed();
        bool clickUp = playerMovement.Atk_PressedThisFrame();

        if (isAttacking)
        {
            waitingClassify = false;
        }

        // ===== コンボ中の長押し判定 =====
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
                    chargeQueuedDuringCombo = true;
                }
            }
            if (clickUp)
            {
                comboHoldCounting = false;
            }
        }

        // ===== 溜め中の処理 =====
        if (isCharging)
        {
            if (clickUp)
            {
                ReleaseCharge();
            }
            return;
        }

        if (IsInSkill2())
        {
            waitingClassify = false;
            comboHoldCounting = false;
            return;
        }

        // ===== 攻撃開始（短押し） =====
        if (!isAttacking)
        {
            if (waitingClassify)
            {
                if (clickHeld && (Time.time - pressTime) >= holdThreshold)
                {
                    StartCharge();
                    return;
                }
                if (clickUp)
                {
                    waitingClassify = false;
                    StartCombo();
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

        // ===== 移動キャンセル =====
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
                    queuedNext = false;
                }
            }
        }

        if (isAttacking && queueOpen && clickDown)
            queuedNext = true;
    }

    // ==========================
    //    溜め攻撃：開始
    // ==========================
    void StartCharge()
    {
        isAttacking = true;
        queueOpen = false;
        queuedNext = false;

        waitingClassify = false;
        isCharging = true;

        SetBoolIfExists(paramIsAttacking, true);
        if (!string.IsNullOrEmpty(paramIsCharging)) SetBoolIfExists(paramIsCharging, true);

        CrossFadeSafe(stateChargeLoop, 0.05f);
    }

    // ==========================
    //    溜め攻撃：解放
    // ==========================
    void ReleaseCharge()
    {
        if (skill2OnCooldown) return;

        //ResetForSkill2();
        isCharging = false;

        skill2OnCooldown = true;
        StartCoroutine(Skill2CooldownRoutine());

        isAttacking = true;
        SetBoolIfExists(paramIsAttacking, true);

        if (!string.IsNullOrEmpty(paramIsCharging))
            SetBoolIfExists(paramIsCharging, false);

        animator.ResetTrigger(paramAttackTrigger);
        animator.SetInteger(paramAttackIndex, 0);

        // ======================================
        // ★ 溜め攻撃の攻撃力をここで決定（重要）
        // ======================================
        attackPower = chargeAttackPower;

        if (attackHit != null) attackHit.EnableHitbox();

        CrossFadeSafe(stateChargeRelease, 0.05f);

        StartCoroutine(ChargeReleaseFailSafe());
    }

    IEnumerator ChargeReleaseFailSafe()
    {
        yield return new WaitForSeconds(chargeReleaseFailSafe);

        if (!IsInSkill2()) yield break;

        SetBoolIfExists(paramIsAttacking, false);
        isAttacking = false;
    }

    public void ChargeReleaseEnd()
    {
        SetBoolIfExists(paramIsAttacking, false);
        animator.ResetTrigger(paramAttackTrigger);
        animator.SetInteger(paramAttackIndex, 0);

        if (!string.IsNullOrEmpty(paramIsCharging))
            SetBoolIfExists(paramIsCharging, false);
    }

    // ==========================
    //    通常攻撃開始
    // ==========================
    void StartCombo()
    {
        isAttacking = true;
        queueOpen = false;
        queuedNext = false;

        currentCore = 0;

        moveHeldAtAttackStart = IsMovePressedRaw();
        moveReleasedSinceStart = !moveHeldAtAttackStart;

        SetBoolIfExists(paramIsAttacking, true);
        animator.SetInteger(paramAttackIndex, 0);

        CrossFadeSafe(stateAttack1_Core, 0.05f);

        TryStartHitboxCoroutine(0);

        cancelUnlockTime = Time.time + cancelMinDelay;
    }

    void EndCombo()
    {
        queueOpen = false;
        queuedNext = false;

        animator.SetInteger(paramAttackIndex, 0);

        string finish = GetFinishStateName(currentCore);
        if (!string.IsNullOrEmpty(finish) &&
            animator.HasState(animatorLayer, Animator.StringToHash(finish)))
        {
            CrossFadeSafe(finish, 0.05f);
        }
        else
        {
            bool moving = GetBoolIfExists("IsMoving");
            bool running = GetBoolIfExists("IsRunning");
            string dest = running ? stateRun : (moving ? stateWalk : stateIdle);
            CrossFadeSafe(dest, 0.05f);

            isAttacking = false;
            SetBoolIfExists(paramIsAttacking, false);
        }

        currentCore = -1;
    }

    void GoNextCore()
    {
        queuedNext = false;
        currentCore = (currentCore + 1) % 3;

        animator.SetInteger(paramAttackIndex, currentCore);
        animator.ResetTrigger(paramAttackTrigger);
        animator.SetTrigger(paramAttackTrigger);

        TryStartHitboxCoroutine(currentCore);

        cancelUnlockTime = Time.time + cancelMinDelay;
        cancelAfterCore = false;
    }

    // ==========================
    //   Animation Events
    // ==========================
    public void OpenQueue()
    {
        queueOpen = true;
        queuedNext = false;
    }

    public void CloseQueue()
    {
        queueOpen = false;
    }

    public void CoreEnd()
    {
        HitboxOff();
        queueOpen = false;

        if (cancelAfterCore)
        {
            CancelToLocomotion(runHeldAtCancel);
        }
        else if (chargeQueuedDuringCombo)
        {
            chargeQueuedDuringCombo = false;
            StartCharge();
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

    public void FinishEnd()
    {
        isAttacking = false;
        SetBoolIfExists(paramIsAttacking, false);

        if (HasMoveInput(out bool runHeld))
        {
            string dest = runHeld ? stateRun : stateWalk;
            CrossFadeSafe(dest, 0.05f);
        }

        waitingClassify = false;
        pressTime = 0f;
    }

    IEnumerator Skill2CooldownRoutine()
    {
        yield return new WaitForSeconds(skill2CooldownTime);
        skill2OnCooldown = false;
    }

    // ==========================
    //      キャンセル処理
    // ==========================
    void CancelToLocomotion(bool runHeld)
    {
        isAttacking = false;
        cancelAfterCore = false;
        queuedNext = false;

        SetBoolIfExists(paramIsAttacking, false);
        animator.SetInteger(paramAttackIndex, 0);
        animator.ResetTrigger(paramAttackTrigger);

        string dest = runHeld ? stateRun : stateWalk;
        CrossFadeSafe(dest, 0.05f);
    }

    // ==========================
    //    外部向け停止
    // ==========================
    public void ForceCancelAttack()
    {
        waitingClassify = false;
        isCharging = false;

        chargeQueuedDuringCombo = false;
        comboHoldCounting = false;

        isAttacking = false;
        queueOpen = false;
        queuedNext = false;
        cancelAfterCore = false;

        SetBoolIfExists(paramIsAttacking, false);
        animator.SetInteger(paramAttackIndex, 0);
        animator.ResetTrigger(paramAttackTrigger);

        HitboxOff();
        StopAllCoroutines();
    }

    // ==========================
    //  ヒットボックス管理
    // ==========================
    void TryStartHitboxCoroutine(int coreIndex)
    {
        if (attackHit == null || attackTimings == null) return;
        if (coreIndex < 0 || coreIndex >= attackTimings.Length) return;

        var timing = attackTimings[coreIndex];
        StartCoroutine(EnableHitboxWithDelay(timing.delay, timing.activeTime, coreIndex));
    }

    IEnumerator EnableHitboxWithDelay(float delay, float activeTime, int attackIndex)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // =========================================
        // ★ 通常攻撃の攻撃力をここで決定する
        // =========================================
        SetAttackPower_NormalAttack(attackIndex);

        attackHit.EnableHitbox();
        if (activeTime > 0f) yield return new WaitForSeconds(activeTime);
        attackHit.DisableHitbox();
    }

    public void HitboxOff()
    {
        if (attackHit != null) attackHit.DisableHitbox();
    }

    // ==========================
    //  通常攻撃の攻撃力決定
    // ==========================
    void SetAttackPower_NormalAttack(int index)
    {
        if (index == 0 || index == 1)
        {
            attackPower = attackPowers[index];
            return;
        }

        if (index == 2)
        {
            float basePower = attackPowers[2];
            float multi = gyroShooter.getatkbarTimer();
            attackPower = basePower * multi;
            gyroShooter.resetatkbar();
            return;
        }

        attackPower = attackPowers[0];
    }

    // 軽量アクセサ
    public int GetCurrentAttackIndex()
    {
        return Mathf.Clamp(currentCore, 0, 2);
    }

    public float GetCurrentAttackPower()
    {
        return attackPower;
    }

    // ==========================
    //   各種ユーティリティ
    // ==========================
    bool HasMoveInput(out bool runHeld)
    {
        runHeld = false;

        if (!playerMovement.Move_Forward_isPressed() &&
            !playerMovement.Move_Backward_isPressed() &&
            !playerMovement.Move_Left_isPressed() &&
            !playerMovement.Move_Right_isPressed())
            return false;

        runHeld = playerMovement.Dash_isPressed();
        return true;
    }

    bool IsMovePressedRaw()
    {
        return playerMovement.Move_Forward_isPressed() ||
               playerMovement.Move_Left_isPressed() ||
               playerMovement.Move_Backward_isPressed() ||
               playerMovement.Move_Right_isPressed();
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
    }

    void SetBoolIfExists(string name, bool value)
    {
        foreach (var p in animator.parameters)
        {
            if (p.name == name && p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(name, value);
                return;
            }
        }
    }

    bool GetBoolIfExists(string name)
    {
        foreach (var p in animator.parameters)
        {
            if (p.name == name && p.type == AnimatorControllerParameterType.Bool)
                return animator.GetBool(name);
        }
        return false;
    }

    bool HasParam(Animator anim, string name)
    {
        foreach (var p in anim.parameters)
            if (p.name == name)
                return true;
        return false;
    }

    bool IsInSkill2()
    {
        var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
        if (info.IsName(stateChargeRelease)) return true;

        if (animator.IsInTransition(animatorLayer))
        {
            var next = animator.GetNextAnimatorStateInfo(animatorLayer);
            if (next.IsName(stateChargeRelease)) return true;
        }
        return false;
    }
}
