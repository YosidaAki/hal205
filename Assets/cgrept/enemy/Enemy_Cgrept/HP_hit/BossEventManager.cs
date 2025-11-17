using UnityEngine;
using System.Collections;

public class BossEventManager : MonoBehaviour
{
    [Header("プレイヤーのRailMover")]
    public RailMover playerMover;

    [Header("演出用Rail（第二形態などで乗るレール）")]
    public RailSpline specialRail;

    [Header("開始までの待機秒数")]
    public float delayBeforeStart = 3f;

    [Header("開始速度")]
    public float startSpeed = 3f;

    [Header("ボス演出Animator")]
    public Animator bossAnimator;

    [Header("演出トリガー名")]
    public string phase2Trigger = "Phase2Start";

    [Header("カメラ切り替えオブジェクト（任意）")]
    public GameObject phase2Camera;

    private bool triggered = false;

    public void StartSecondPhaseSequence()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(SecondPhaseRoutine());
    }

    private IEnumerator SecondPhaseRoutine()
    {
        Debug.Log("[BossEventManager] 第二形態演出開始");

        // カメラ切り替え
        if (phase2Camera != null)
            phase2Camera.SetActive(true);

        // アニメーション再生
        if (bossAnimator != null && !string.IsNullOrEmpty(phase2Trigger))
            bossAnimator.SetTrigger(phase2Trigger);

        // 待機（演出時間）
        yield return new WaitForSeconds(delayBeforeStart);

        // レールアクション開始
        if (playerMover != null && specialRail != null)
        {
            Debug.Log("[BossEventManager] レール開始");
            playerMover.StartRail(specialRail, 0f, startSpeed);
        }
    }
}

