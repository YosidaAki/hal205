using UnityEngine;

public class AttackVFXSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public Transform socket;   
        public GameObject prefab;  
        public bool follow = true; 
    }

    public Slot[] slots = new Slot[3];
    public void SpawnByIndex(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length) return;
        var s = slots[index];
        if (s == null || s.socket == null || s.prefab == null) return;

        var go = Instantiate(s.prefab, s.socket.position, s.socket.rotation);

        if (s.follow) go.transform.SetParent(s.socket, worldPositionStays: true);

        ForcePlay(go);

        Destroy(go, CalcLifetime(go));
    }

    public void Spawn0() => SpawnByIndex(0);
    public void Spawn1() => SpawnByIndex(1);
    public void Spawn2() => SpawnByIndex(2);
    public void Spawn3() => SpawnByIndex(3);
    public void Spawn4() => SpawnByIndex(4);
    public void Spawn5() => SpawnByIndex(5);
    static void ForcePlay(GameObject go)
    {
        var pss = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss) { ps.Clear(true); ps.Play(true); }

#if UNITY_2019_3_OR_NEWER
        var vfxs = go.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true);
        foreach (var v in vfxs) v.Play();
#endif
    }

    static float CalcLifetime(GameObject go)
    {
        float t = 0.9f;
        var pss = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss)
        {
            var main = ps.main;
            float life = main.duration + main.startLifetime.constantMax;
            if (life > t) t = life;
        }
        return t + 0.1f;
    }
}