using UnityEngine;

public class Check_smartphone : MonoBehaviour
{
    [Header("Animator Settings")]
    public Animator animator;                  // キャラの Animator
    public string animationName = "Check_smartphone";  // 再生したいアニメ名
    public int layer = 0;                      // 再生するレイヤー

    void Start()
    {
        // Inspector にセットしてなければ自動取得
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        // Cキーでアニメーション再生
        if (Input.GetKeyDown(KeyCode.C))
        {
            animator.CrossFadeInFixedTime(animationName, 0.1f, layer);
        }
    }
}
