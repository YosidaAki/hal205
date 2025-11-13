using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Boss/PartData")]
public class BossPartData : ScriptableObject
{
    [Header("部位名（GameObject名と一致）")]
    public string partName = "Body";

    [Header("倍率設定")]
    [Tooltip("基本倍率（弱点なら1.5など）")]
    public float baseMultiplier = 1.0f;

    [System.Serializable]
    public class DamageSetting
    {
        public int attackIndex = 0;
        public float damageMultiplier = 1.0f;
    }

    [Header("攻撃段階ごとの倍率")]
    public List<DamageSetting> damageSettings = new List<DamageSetting>()
    {
        new DamageSetting(){ attackIndex = 0, damageMultiplier = 1.0f },
        new DamageSetting(){ attackIndex = 1, damageMultiplier = 1.3f },
        new DamageSetting(){ attackIndex = 2, damageMultiplier = 1.6f },
    };

    [Header("ヒットエフェクト設定")]
    public GameObject normalHitEffect;
    public GameObject weakPointEffect;

    [Header("演出設定")]
    public bool enableSlowMotion = true;
    public bool enableBlinkEffect = true;
}
