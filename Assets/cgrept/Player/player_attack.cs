using System.Collections;
using UnityEngine;

/// <summary>
/// 通常攻撃1/2/3段 + 溜め攻撃(SKILL2)
/// 攻撃力の基礎値のみ決定し、
/// 部位倍率 + チャージ倍率は player_attack_hit.cs で決定する。
/// </summary>
public class player_attack : MonoBehaviour
{
    // =======================
    // 基本設定
    // =======================
    [Header("Animator")]
    public Animator animator;
    public int layer = 0;

    [Header("Hitbox Script")]
    public player_attack_hit attackHit;

    // 通常攻撃ステート
    public string atk1 = "Attack1_Core";
    public string atk2 = "Attack2_Core";
    public string atk3 = "Attack3_Core";

    public string atk1_end = "Attack1_Finish";
    public string atk2_end = "Attack2_Finish";
    public string atk3_end = "Attack3_Finish";

    [Header("攻撃力（基礎値）")]
    public float[] attackPowers = new float[] { 10, 20, 30 };
    public float chargeAttackPower = 50f;

    [System.Serializable]
    public struct AttackTiming
    {
        public float delay;
        public float activeTime;
    }

    [Header("攻撃判定タイミング")]
    public AttackTiming[] timings = new AttackTiming[]
    {
        new AttackTiming(){ delay=0.1f, activeTime=1000.00f },
        new AttackTiming(){ delay=0.15f, activeTime=1000.00f },
        new AttackTiming(){ delay=0.2f, activeTime=1000.00f },
    };

    // =======================
    // チャージ攻撃
    // =======================
    public string chargeLoop = "QUICK_SHIFT B";
    public string chargeRelease = "SKILL 2";
    public string paramIsCharging = "IsCharging";
    public float holdTime = 0.3f;

    [Header("Input 判定")]
    public string paramIsAttacking = "IsAttacking";
    public string paramAttackTrigger = "AttackTrigger";
    public string paramAttackIndex = "AttackIndex";

    // 内部状態
    private bool isAttacking = false;
    private bool isCharging = false;

    private float basePower = 0f;     // ← 基礎攻撃力のみ（最終計算はHit側）
    private int core = 0;             // 現在の段階(0,1,2)

    private PlayerMovement input;
    private GyroShooter gyro;

    private bool waiting = false;
    private float pressStart = 0f;

    private bool queueOpen = false;
    private bool queuedNext = false;

    private bool chargeRequested = false;
    public bool showDebugLog = true;
    void Start()
    {
        input = FindFirstObjectByType<PlayerMovement>();
        gyro = FindFirstObjectByType<GyroShooter>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        bool down = input.Atk_PressedThisFrame();
        bool held = input.Atk_isPressed();
        bool up = !held;

        // ==============================
        // 溜め中なら ReleaseCharge 判定
        // ==============================
        if (isCharging)
        {
            if (up)
            {
                ReleaseCharge();
            }
            return;
        }

        // ==============================
        // 攻撃してない → 長押しか短押しか判定
        // ==============================
        if (!isAttacking)
        {
            if (waiting)
            {
                // 長押し
                if (held && (Time.time - pressStart) >= holdTime)
                {
                    StartCharge();
                    return;
                }

                // 短押し
                if (up)
                {
                    waiting = false;
                    StartCombo();
                    return;
                }
            }
            else if (down)
            {
                waiting = true;
                pressStart = Time.time;
                return;
            }
        }

        // ==============================
        // 攻撃中の1/2/3段目予約
        // ==============================
        if (isAttacking && queueOpen && down)
            queuedNext = true;

        // 溜め攻撃への移行予約（コンボ中長押し）
        if (isAttacking && held && !isCharging)
        {
            if ((Time.time - pressStart) >= holdTime)
                chargeRequested = true;
        }
        if (up)
        {
            pressStart = Time.time;
        }
    }

    // ==============================
    // 通常攻撃開始
    // ==============================
    void StartCombo()
    {
        isAttacking = true;
        core = 0;

        SetBool(paramIsAttacking, true);
        animator.SetInteger(paramAttackIndex, core);
        CrossFade(atk1, 0.05f);

        StartCoroutine(HitRoutine(0));
    }

    void NextCore()
    {
        queuedNext = false;
        core++;

        if (core > 2)
        {
            EndCombo();
            return;
        }

        animator.SetInteger(paramAttackIndex, core);
        animator.SetTrigger(paramAttackTrigger);

        string s = core switch
        {
            0 => atk1,
            1 => atk2,
            2 => atk3,
            _ => atk1
        };
        if (showDebugLog)
            Debug.Log($"[PlayerHit] 攻撃段階{s}");
        CrossFade(s, 0.05f);

        StartCoroutine(HitRoutine(core));
    }

    void EndCombo()
    {
        isAttacking = false;
        SetBool(paramIsAttacking, false);

        string s = core switch
        {
            0 => atk1_end,
            1 => atk2_end,
            2 => atk3_end,
            _ => atk1_end
        };

        CrossFade(s, 0.05f);
    }

    // ==============================
    // 溜め攻撃
    // ==============================
    void StartCharge()
    {
        waiting = false;
        isAttacking = true;
        isCharging = true;

        SetBool(paramIsAttacking, true);
        SetBool(paramIsCharging, true);

        CrossFade(chargeLoop, 0.05f);
    }

    void ReleaseCharge()
    {
        isCharging = false;
        SetBool(paramIsCharging, false);

        basePower = chargeAttackPower;    // ← 基礎攻撃力のみ

        if (attackHit)
            attackHit.EnableHitbox();

        CrossFade(chargeRelease, 0.05f);
        StartCoroutine(ChargeFailSafe());
    }

    IEnumerator ChargeFailSafe()
    {
        yield return new WaitForSeconds(1.2f);
        isCharging = false;
        isAttacking = false;
        SetBool(paramIsAttacking, false);
    }

    // ==============================
    // ヒット処理
    // ==============================
    IEnumerator HitRoutine(int index)
    {
        AttackTiming t = timings[index];

        if (t.delay > 0) yield return new WaitForSeconds(t.delay);

        basePower = attackPowers[index];  // ← 基礎攻撃力のみ

        attackHit.EnableHitbox();

        if (t.activeTime > 0)
            yield return new WaitForSeconds(t.activeTime);

        attackHit.DisableHitbox();
    }

    // ==============================
    // Animation Events
    // ==============================
    public void OpenQueue() => queueOpen = true;
    public void CloseQueue() => queueOpen = false;

    public void CoreEnd()
    {
        attackHit.DisableHitbox();
        queueOpen = false;

        if (chargeRequested)
        {
            chargeRequested = false;
            StartCharge();
            return;
        }

        if (queuedNext)
        {
            NextCore();
            return;
        }

        EndCombo();
    }

    // ==============================
    // 外部アクセス
    // ==============================
    public float GetCurrentAttackPower() => basePower;
    public int GetCurrentAttackIndex() => core;
    public bool IsChargeAttack() => isCharging;

    // ==============================
    // Utility
    // ==============================
    void SetBool(string p, bool v)
    {
        if (animator && !string.IsNullOrEmpty(p))
            animator.SetBool(p, v);
    }

    void CrossFade(string state, float time)
    {
        if (!animator) return;

        int hash = Animator.StringToHash(state);
        if (animator.HasState(layer, hash))
            animator.CrossFadeInFixedTime(state, time, layer);
    }
    public bool GetWaiting()
    {
        return waiting;
    }
}
