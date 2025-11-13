using UnityEngine;

public class SpiderFollow : MonoBehaviour
{
    public Transform player;
    public float rotationSpeed = 3f;
    public Transform firePoint;
    public BeamDamage beamDamage;   // ← ここでBeamDamageを参照

    void Update()
    {
        if (player == null) return;

        // 🔒 ビームチャージ中 or 発射中なら回転しない
        if (beamDamage != null && (IsChargingOrFiring()))
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        // 蜘蛛の前が反対方向を向いていた場合
        Quaternion targetRotation = Quaternion.LookRotation(-direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

        if (firePoint != null)
            firePoint.LookAt(player.position);
    }

    private bool IsChargingOrFiring()
    {
        return (bool)beamDamage.GetType()
            .GetField("isCharging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(beamDamage)
            ||
            (bool)beamDamage.GetType()
            .GetField("isFiring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(beamDamage);
    }
}
