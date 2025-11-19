using UnityEngine;

public class PlayerGuard : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] Animator animator;
    //[SerializeField] int animatorLayer = 0;

    [Header("Evade / Guard Params")]
    public string paramEvadeTrigger = "EvadeTrigger";
    public string paramEvadeHeld = "EvadeHeld";
    public string paramIsEvading = "IsEvading";

    [Header("Guard Object")]
    public GameObject guardObject;

    [Header("Guard Time Settings")]
    public float maxGuardTime = 5f;          // ★ ガード最大秒数（調節可）
    [Range(0, 5)] public float guardDrainRate = 1f;         // ★ ガード消費速度（1秒に1減るなど）
    [Range(0, 1)] public float guardRecoveryRate = 1f;      // ★ 自然回復速度
    [Range(0, 10)] public float guardCooldown = 1.0f;        // ★ ガードを離してから回復が始まるまでの時間

    float currentGuardTime;
    float lastGuardReleaseTime;               // ★ 回復開始タイミング管理
    bool canGuard = true;

    Renderer guardRenderer;
    public Color fullGuardColor = Color.cyan;
    public Color lowGuardColor = Color.red;

    [Header("Evade / Tap Settings")]
    public float evadeTapThreshold = 0.15f;
    public float minInterval = 0.1f;
    public float postEvadeAttackWindow = 0.25f;

    float gPressStartTime;

    PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
        currentGuardTime = maxGuardTime;

        if (guardObject)
        {
            guardObject.SetActive(false);
            guardRenderer = guardObject.GetComponent<Renderer>();
        }
    }

    void Update()
    {
        bool gDown = playerMovement.Guard_PressedThisFrame();
        bool gHeld = playerMovement.Guard_isPressed();
        bool gUp = !gHeld && gPressStartTime > 0f;

        // ======= ガード開始（押した瞬間） =======
        if (gDown && canGuard)
        {
            gPressStartTime = Time.time;

            SetBool(paramIsEvading, true);
            ResetTrigger(paramEvadeTrigger);
            SetTrigger(paramEvadeTrigger);
            SetBool(paramEvadeHeld, true);
        }

        // ======= ガード中（押しっぱ） =======
        if (gHeld && GetBool(paramIsEvading) && canGuard)
        {
            currentGuardTime -= guardDrainRate * Time.deltaTime;
            if (currentGuardTime <= 0f)
            {
                currentGuardTime = 0f;
                canGuard = false;
                SetBool(paramEvadeHeld, false);
            }
        }

        // ======= ガード解除（離した瞬間） =======
        if (gUp)
        {
            gPressStartTime = 0f;
            SetBool(paramEvadeHeld, false);
            SetBool(paramIsEvading, false);

            lastGuardReleaseTime = Time.time; // ★ 回復開始タイマーセット
        }

        // ======= 自然回復処理（クールダウン後） =======
        if (!gHeld && Time.time - lastGuardReleaseTime >= guardCooldown)
        {
            if (currentGuardTime < maxGuardTime)
            {
                currentGuardTime += guardRecoveryRate * Time.deltaTime;
                if (currentGuardTime >= maxGuardTime)
                {
                    currentGuardTime = maxGuardTime;
                    canGuard = true;
                }
            }
        }

        // ======= ガード可視化 =======
        if (guardObject)
            guardObject.SetActive(gHeld && canGuard);

        // ======= 色変化 =======
        if (guardRenderer)
        {
            float ratio = currentGuardTime / maxGuardTime;
            guardRenderer.material.color = Color.Lerp(lowGuardColor, fullGuardColor, ratio);
        }
    }

    // ★ AnimationEvent 用（Evade_Finish の終わり）
    public void EvadeFinishEnd()
    {
        SetBool(paramIsEvading, false);
        SetBool(paramEvadeHeld, false);
    }


    // ============== Animator Helper ==============
    bool HasParam(string n, AnimatorControllerParameterType t)
    {
        foreach (var p in animator.parameters)
            if (p.name == n && p.type == t) return true;
        return false;
    }
    void SetBool(string n, bool v)
    { if (HasParam(n, AnimatorControllerParameterType.Bool)) animator.SetBool(n, v); }
    bool GetBool(string n)
    { if (HasParam(n, AnimatorControllerParameterType.Bool)) return animator.GetBool(n); return false; }
    void SetTrigger(string n)
    { if (HasParam(n, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(n); }
    void ResetTrigger(string n)
    { if (HasParam(n, AnimatorControllerParameterType.Trigger)) animator.ResetTrigger(n); }
    void SetInt(string n, int v)
    { if (HasParam(n, AnimatorControllerParameterType.Int)) animator.SetInteger(n, v); }
}
