// RailTrigger.cs（Animator版）
using UnityEngine;

public class RailTrigger : MonoBehaviour
{
    public RailSpline rail;
    public float startT = 0f;
    public float entrySpeed = 0.5f;
    public bool requireButton = false;

    //[Header("乗車時アニメーション")]
    //public AnimationClip rideAnimation; // Inspectorで指定

    void OnTriggerEnter(Collider other)
    {
        var mover = other.GetComponent<RailMover>();
        if (mover == null) return;

        var animator = other.GetComponent<Animator>();
        //if (animator != null && rideAnimation != null)
        //{
        //    // Override Controllerで一時的に差し替え
        //    var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        //    overrideController["DefaultRide"] = rideAnimation; // Animator内に"DefaultRide"というStateを用意
        //    animator.runtimeAnimatorController = overrideController;

        //    animator.Play("DefaultRide");
        //}

        mover.StartRail(rail, startT, entrySpeed);
    }
}
