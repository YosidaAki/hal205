// RailTrigger.cs�iAnimator�Łj
using UnityEngine;

public class RailTrigger : MonoBehaviour
{
    public RailSpline rail;
    public float startT = 0f;
    public float entrySpeed = 0.5f;
    public bool requireButton = false;

    //[Header("��Ԏ��A�j���[�V����")]
    //public AnimationClip rideAnimation; // Inspector�Ŏw��

    void OnTriggerEnter(Collider other)
    {
        var mover = other.GetComponent<RailMover>();
        if (mover == null) return;

        var animator = other.GetComponent<Animator>();
        //if (animator != null && rideAnimation != null)
        //{
        //    // Override Controller�ňꎞ�I�ɍ����ւ�
        //    var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        //    overrideController["DefaultRide"] = rideAnimation; // Animator����"DefaultRide"�Ƃ���State��p��
        //    animator.runtimeAnimatorController = overrideController;

        //    animator.Play("DefaultRide");
        //}

        mover.StartRail(rail, startT, entrySpeed);
    }
}
