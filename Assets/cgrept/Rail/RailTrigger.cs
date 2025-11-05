using UnityEngine;

public class RailTrigger : MonoBehaviour
{
    public RailSpline rail;
    public float startT = 0f;
    public float entrySpeed = 2f;

    void OnTriggerEnter(Collider other)
    {
        var mover = other.GetComponent<RailMover>();
        if (mover == null) return;
        mover.StartRail(rail, startT, entrySpeed);
    }
}

