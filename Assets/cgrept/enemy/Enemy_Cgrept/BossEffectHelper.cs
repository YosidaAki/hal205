using UnityEngine;
using System.Collections;

public static class BossEffectHelper
{
    private static bool isSlowActive = false;

    public static void StartSlowMotion(float scale, float duration)
    {
        if (isSlowActive) return;
        var helper = new GameObject("BossEffectHelper").AddComponent<MonoBehaviourHelper>();
        helper.StartCoroutine(SlowMotionRoutine(scale, duration));
    }

    static IEnumerator SlowMotionRoutine(float scale, float duration)
    {
        isSlowActive = true;
        Time.timeScale = scale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        isSlowActive = false;
    }

    public static void BlinkAtPosition(Vector3 pos, Color color)
    {
        // シンプルなデバッグ用点滅
        Debug.DrawRay(pos, Vector3.up * 2, color, 0.3f);
    }

    private class MonoBehaviourHelper : MonoBehaviour { }
}

