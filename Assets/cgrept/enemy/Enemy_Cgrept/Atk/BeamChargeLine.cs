using UnityEngine;

/// <summary>
/// チャージ中に表示する「光の棒オブジェクト」を制御する。
/// 実体のMeshRendererを持つGameObjectを使って発光・伸縮演出。
/// </summary>
public class BeamChargeLine : MonoBehaviour
{
    [Header("=== 見た目設定 ===")]
    public Renderer beamRenderer;          // 光る棒（例: Cylinder, Cube など）
    public float maxLength = 60f;          // 棒の最大長さ
    public float growSpeed = 10f;          // 伸び速度
    public float glowSpeed = 2f;           // 光り方の速度
    public Color baseColor = Color.yellow; // 発光色
    public float maxGlow = 3f;             // 発光の最大強度

    private float currentLength = 0f;
    private float currentGlow = 0f;
    private bool active = false;
    private Material beamMat;

    void Start()
    {
        if (beamRenderer == null)
            beamRenderer = GetComponent<Renderer>();

        // マテリアルをインスタンス化
        beamMat = beamRenderer.material;
        beamMat.EnableKeyword("_EMISSION");
        beamMat.SetColor("_EmissionColor", baseColor * 0f);

        // 最初は非表示
        beamRenderer.enabled = false;
    }

    void Update()
    {
        if (!active) return;

        // 長さを徐々に伸ばす
        currentLength = Mathf.Lerp(currentLength, maxLength, Time.deltaTime * growSpeed);
        transform.localScale = new Vector3(1f, 1f, currentLength);

        // 徐々に光を強める
        currentGlow = Mathf.Lerp(currentGlow, maxGlow, Time.deltaTime * glowSpeed);
        beamMat.SetColor("_EmissionColor", baseColor * currentGlow);
    }

    /// <summary> 光の棒を有効化 </summary>
    public void Activate(Vector3 startPos, Vector3 direction)
    {
        transform.position = startPos;
        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        currentLength = 0f;
        currentGlow = 0f;

        beamRenderer.enabled = true;
        active = true;
    }

    /// <summary> 光の棒を無効化 </summary>
    public void Deactivate()
    {
        active = false;
        beamRenderer.enabled = false;
    }
}

