using UnityEngine;
using System.Collections;

public class EnemyTurret : MonoBehaviour
{
    [Header("攻撃範囲設定")]
    public float attackRange = 20f;      // ビーム射程
    public float closeRange = 40f;        // 隕石射程

    [Header("参照設定")]
    public Transform firePoint;          // ビーム発射位置
    public Transform meteorPoint;        // 隕石発射開始位置（上空）
    public GameObject meteorPrefab;      // 隕石プレハブ
    public Transform player;             // プレイヤー

    [Header("隕石攻撃設定")]
    public int meteorCount = 5;      // 同時に落とす数
    public float playerRadius = 5f;  // プレイヤーの周囲に落とす範囲
    public float spawnDelay = 0.2f;  // 1発ごとの間隔
    public float meteorFallSpeed = 10f; // スクリプトで落とす場合の速度


    [Header("クールタイム")]
    public float meteorCooldown = 2f;    // 隕石攻撃の間隔

    private BeamDamage beamDamage;       // ビーム処理
    private float lastMeteorTime = -Mathf.Infinity;

    void Start()
    {
        // firePoint にアタッチされている BeamDamage を取得
        beamDamage = firePoint.GetComponent<BeamDamage>();
        if (beamDamage == null)
        {
            Debug.LogError("BeamDamage が firePoint にアタッチされていません！");
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // 遠距離攻撃：ビーム
        if (distance > closeRange)
        {
            if (beamDamage != null)
            {
                beamDamage.FireBeam();
            }
        }
        // 近距離攻撃：隕石
        else if (distance <= closeRange)
        {
            if (Time.time - lastMeteorTime >= meteorCooldown)
            {
                DropMeteor();
                lastMeteorTime = Time.time;
            }
        }
    }

    void DropMeteor()
    {
        if (meteorPrefab == null || meteorPoint == null) return;
        StartCoroutine(SpawnMultipleMeteors());
    }

    IEnumerator SpawnMultipleMeteors()
    {
        for (int i = 0; i < meteorCount; i++)
        {
            // プレイヤーの周囲に半径 playerRadius 内でランダム着地点を決定
            Vector2 randomCircle = Random.insideUnitCircle * playerRadius;
            Vector3 targetPos = player.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // 隕石の出現位置（meteorPoint の高さを基準に）
            Vector3 spawnPos = new Vector3(targetPos.x, meteorPoint.position.y, targetPos.z);

            // 隕石を生成
            GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);

            // Rigidbody がない場合はスクリプトで落とす
            if (meteor.GetComponent<Rigidbody>() == null)
            {
                StartCoroutine(FallMeteor(meteor.transform));
            }

            yield return new WaitForSeconds(spawnDelay); // ちょっとずつ間をあけて降らす
        }
    }

    // Rigidbody を使わない場合の手動落下
    IEnumerator FallMeteor(Transform meteor)
    {
        while (meteor != null && meteor.position.y > 0f)
        {
            meteor.position += Vector3.down * meteorFallSpeed * Time.deltaTime;
            yield return null;
        }

        if (meteor != null)
            Destroy(meteor.gameObject);
    }
    // 外部から呼び出すための公開関数
    public void ShootMeteor()
    {
        if (Time.time - lastMeteorTime >= meteorCooldown)
        {
            DropMeteor();
            lastMeteorTime = Time.time;
            Debug.Log("🪨 隕石攻撃開始！");
        }
        else
        {
            Debug.Log("🪨 隕石クールダウン中...");
        }
    }

    // ★突進の時にも隕石クールタイムを強制するための関数
    public void ForceMeteorCooldown()
    {
        lastMeteorTime = Time.time;
    }

}
