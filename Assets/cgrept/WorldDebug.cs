using UnityEngine;

public class WorldDebug : MonoBehaviour
{

    [Header("デバッグ")]
    public bool showDebugLog = false;

    [Header("ボスの攻撃ボタン操作")]
    public bool bossAttackInput = false;
    //[Header("ビーム")]
    //KeyCode bossAttackKey_Beam = KeyCode.Space;
    //[Header("メテオ")]
    //KeyCode bossAttackKey_Meteor = KeyCode.Space;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public bool showDebug()//デバッグ機能
    {
        return showDebugLog;
    }

}
