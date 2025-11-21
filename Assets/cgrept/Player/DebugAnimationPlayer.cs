using UnityEngine;
using UnityEngine.Animations;

using UnityEngine.InputSystem.Controls;
using UnityEngine.Playables;

public class DebugAnimationPlayer : MonoBehaviour
{
    [Header("デバッグで再生したいアニメーション（LoopでもOK）")]
    public AnimationClip debugClip;

    [Header("再生キー (例：KeyCode.Alpha1 など)")]
    public KeyCode playKey = KeyCode.Alpha1;

    private Animator animator;
    private PlayableGraph graph;
    private AnimationClipPlayable clipPlayable;

    private bool isDebugPlaying = false;
    private float playStartTime = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        graph = PlayableGraph.Create("DebugAnimationGraph");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
    }

    void Update()
    {
        // 再生キーが押された
        KeyControl key = PlayerMovement.ToKeyControl(playKey);
        if (key != null && key.wasPressedThisFrame)
        {
            PlayDebugOnce();
        }

        // 再生中なら時間を監視
        if (isDebugPlaying)
        {
            float elapsed = Time.time - playStartTime;

            // AnimationClip の長さを1回分再生したら終了
            if (elapsed >= debugClip.length)
            {
                StopDebugAnimation();
            }
        }
    }

    // ======== 再生（Loop でも 1 回だけ） ========
    void PlayDebugOnce()
    {
        if (debugClip == null) return;

        // 既存のグラフを破棄してリセット
        if (graph.IsValid())
            graph.Destroy();

        // 新しい PlayableGraph
        graph = PlayableGraph.Create("DebugAnimationGraph");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        // Playable 作成
        var playableOutput = AnimationPlayableOutput.Create(graph, "Animation", animator);
        clipPlayable = AnimationClipPlayable.Create(graph, debugClip);

        // ★ループ無効化（Loopアニメでも1回だけ再生される）
        clipPlayable.SetDuration(debugClip.length);

        playableOutput.SetSourcePlayable(clipPlayable);
        graph.Play();

        playStartTime = Time.time;
        isDebugPlaying = true;

        Debug.Log($"[DebugAnim] {debugClip.name} を 1 回だけ再生開始");
    }

    // ======== 終了して元のアニメへ戻す ========
    void StopDebugAnimation()
    {
        isDebugPlaying = false;

        if (graph.IsValid())
            graph.Destroy();

        Debug.Log("[DebugAnim] 元のアニメーションに復帰");
    }

    void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();
    }
}