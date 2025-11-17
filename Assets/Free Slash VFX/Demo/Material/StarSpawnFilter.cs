using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class StarSpawnFilter : MonoBehaviour
{
    public Transform noSpawnCenter;   // 中心位置（例えば惑星の位置）
    public float noSpawnRadius = 20f; // この半径内には星が出ない

    ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
    }

    void Update()
    {
        // パーティクルを全部読み出して位置調整
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.particleCount];
        int count = ps.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            float dist = Vector3.Distance(particles[i].position, noSpawnCenter.position);

            // 指定範囲に入った場合、星の位置を外周へ押し出す
            if (dist < noSpawnRadius)
            {
                Vector3 dir = (particles[i].position - noSpawnCenter.position).normalized;
                particles[i].position = noSpawnCenter.position + dir * noSpawnRadius;
            }
        }

        ps.SetParticles(particles, count);
    }
}
