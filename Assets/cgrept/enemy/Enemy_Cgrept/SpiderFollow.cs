using UnityEngine;
using System.Collections;

public class SpiderFollow : MonoBehaviour
{
    [Header("追尾設定")]
    public Transform player;
    public float rotationSpeed = 3f;
    public Transform firePoint;
    public BeamDamage beamDamage;

    [Header("攻撃スクリプト")]
    public SpiderDashAttack dashAttack;
    public EnemyTurret meteorAttack;

    [Header("攻撃設定")]
    public float closeRange = 15f;
    public float attackCooldown = 3f;   // ← ★全攻撃共通クールタイム★

    private bool isAttacking = false;   // ← 攻撃中 or クールタイム中

    void Start()
    {
        // BeamDamageが未設定なら取得
        if (beamDamage == null)
            beamDamage = GetComponent<BeamDamage>();

        // ★ゲーム開始時クールタイムを入れる
        isAttacking = true;
        StartCoroutine(InitialCooldown());
    }

    IEnumerator InitialCooldown()
    {
        yield return new WaitForSeconds(attackCooldown); // 例: 3秒
        isAttacking = false;
    }
    void Update()
    {
        if (player == null) return;

        // ビーム中、クールタイム中、攻撃中は回転しない
        // ビーム発射中だけ回転を止める
        if (IsChargingOrFiring())
            return;

        // 追尾回転
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(-direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

        if (firePoint != null)
            firePoint.LookAt(player.position);

        // 攻撃選択
        TryAttack();
    }

    private bool IsChargingOrFiring()
    {
        if (beamDamage == null) return false;

        try
        {
            return beamDamage.isCharging || beamDamage.isFiring;
        }
        catch { return false; }
    }

    // -----------------------------
    // ★共通クールタイム付き攻撃選択処理
    // -----------------------------
    void TryAttack()
    {
        if (isAttacking) return;

        float distance = Vector3.Distance(transform.position, player.position);
        StartCoroutine(SelectAttack(distance));
    }

    IEnumerator SelectAttack(float distance)
    {
        isAttacking = true; // ← 攻撃開始（クールタイム開始）

        if (distance <= closeRange)
        {
            // ★近距離：隕石 or 突進
            int choice = Random.Range(0, 2);

            if (choice == 0)
            {
                Debug.Log("🕷️【近距離】突進");
                if (dashAttack != null)
                    dashAttack.StartDash();
            }
            else
            {
                Debug.Log("🕷️【近距離】隕石");
                if (meteorAttack != null)
                    meteorAttack.ShootMeteor();
            }
        }
        else
        {
            // ★遠距離：ビーム or 突進
            int choice = Random.Range(0, 2);

            if (choice == 0)
            {
                Debug.Log("🕷️【遠距離】ビーム");
                if (beamDamage != null)
                    beamDamage.FireBeam();
            }
            else
            {
                Debug.Log("🕷️【遠距離】突進");
                if (dashAttack != null)
                    dashAttack.StartDash();
            }
        }

        // ★全攻撃共通クールタイム！
        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }
}
