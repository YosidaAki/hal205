using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.IO;

public class PlayerMovement : MonoBehaviour
{
    // ----------------------------------------------------
    //   🔧 入力設定（Inspector から変更可能）
    // ----------------------------------------------------

    [Header("0:キーボード 1:マウス")]
    public KeyCode Player_Move_Forward = KeyCode.W;
    public KeyCode Player_Move_Backward = KeyCode.S;
    public KeyCode Player_Move_Left = KeyCode.A;
    public KeyCode Player_Move_Right = KeyCode.D;

    [Header("ダッシュ")]
    [Range(0, 1)] public int Dash_InputType = 0;      // 0:キーボード / 1:マウス
    public KeyCode Player_Dash = KeyCode.LeftShift;

    [Header("攻撃")]
    [Range(0, 1)] public int Atk_InputType = 1;       // 0:キーボード / 1:マウス
    public KeyCode Player_Atk = KeyCode.Mouse0;

    [Header("ガード")]
    [Range(0, 1)] public int Guard_InputType = 0;     // 0:キーボード / 1:マウス
    public KeyCode Player_Guard = KeyCode.G;

    // 外部から ON/OFF されるダッシュ状態
    public bool isDashing = false;

    // Joy-Con 参照
    private Joycon joycoLeft;
    private Joycon joycoRight;
    private WorldDebug worldDebug;
    // ログ出力先
    private string logFilePath;


    // ----------------------------------------------------
    //                 Start()
    // ----------------------------------------------------
    void Start()
    {
        // Joy-Con を JoyconManager から取得
        List<Joycon> joycons = JoyconManager.Instance.j;
        if (joycons.Count >= 2)
        {
            joycoLeft = joycons[0];
            joycoRight = joycons[1];
        }
        
        worldDebug = FindFirstObjectByType<WorldDebug>();
        // ログファイルの初期化
        logFilePath = Application.dataPath + "/InputLog.txt";
        File.WriteAllText(logFilePath, "=== Input Log Start ===\n");
    }

    // 動かさない
    public void SetPlayerDashing(bool isdashing) { isDashing = isdashing; } 
    public bool GetPlayerDashing() { return isDashing; }
    // ----------------------------------------------------
    //                 ログ関数
    // ----------------------------------------------------
    void Log(string msg)
    {
        // ゲーム時間 + メッセージ
        string log = "[" + Time.time.ToString("F2") + "] " + msg;

        // Unityコンソールにも表示
        Debug.Log(log);

        // ファイルにも追記
        File.AppendAllText(logFilePath, log + "\n");
    }


    // ====================================================
    //                 移動（WASD + Joy-Con）
    // ====================================================

    public bool Move_Forward_isPressed()
    {
        // ガード中・ダッシュ中は移動キャンセル
        if (isDashing || Guard_isPressed() || Guard_PressedThisFrame()) return false;

        // Joy-Con スティック or 十字キー（上）
        bool joyconInput = joycoLeft != null &&
                           (joycoLeft.GetButton(Joycon.Button.DPAD_UP) ||
                            joycoLeft.GetStick()[1] > 0.05f);

        // キーボード（InputSystem）
        var key = ToKeyControl(Player_Move_Forward);
        bool pressed = (key != null && key.isPressed) || joyconInput;

        if (pressed&&worldDebug.showDebug()) Log("Move Forward");
        return pressed;
    }

    public bool Move_Backward_isPressed()
    {
        if (isDashing || Guard_isPressed() || Guard_PressedThisFrame()) return false;

        bool joyconInput = joycoLeft != null &&
                           (joycoLeft.GetButton(Joycon.Button.DPAD_DOWN) ||
                            joycoLeft.GetStick()[1] < -0.05f);

        var key = ToKeyControl(Player_Move_Backward);
        bool pressed = (key != null && key.isPressed) || joyconInput;

        if (pressed&&worldDebug.showDebug()) Log("Move Backward");
        return pressed;
    }

    public bool Move_Left_isPressed()
    {
        if (isDashing || Guard_isPressed() || Guard_PressedThisFrame()) return false;

        bool joyconInput = joycoLeft != null &&
                           (joycoLeft.GetButton(Joycon.Button.DPAD_LEFT) ||
                            joycoLeft.GetStick()[0] < -0.2f);

        var key = ToKeyControl(Player_Move_Left);
        bool pressed = (key != null && key.isPressed) || joyconInput;

        if (pressed&&worldDebug.showDebug()) Log("Move Left");
        return pressed;
    }

    public bool Move_Right_isPressed()
    {
        if (isDashing || Guard_isPressed() || Guard_PressedThisFrame()) return false;

        bool joyconInput = joycoLeft != null &&
                           (joycoLeft.GetButton(Joycon.Button.DPAD_RIGHT) ||
                            joycoLeft.GetStick()[0] > 0.2f);

        var key = ToKeyControl(Player_Move_Right);
        bool pressed = (key != null && key.isPressed) || joyconInput;

        if (pressed&&worldDebug.showDebug()) Log("Move Right");
        return pressed;
    }


    // ====================================================
    //                      ダッシュ
    // ====================================================
    public bool Dash_isPressed()
    {
        if (isDashing || Guard_isPressed() || Guard_PressedThisFrame()) return false;

        // Joy-Con R トリガー (ZL/ZR なら SHOULDER_2)
        bool joyconInput = joycoLeft != null &&
                           joycoLeft.GetButton(Joycon.Button.SHOULDER_2);

        bool pressed;

        if (Dash_InputType == 0)
        {
            var key = ToKeyControl(Player_Dash);
            pressed = (key != null && key.isPressed) || joyconInput;
        }
        else
        {
            var key = ToMouseControl(Player_Dash);
            pressed = (key != null && key.isPressed) || joyconInput;
        }

        if (pressed&&worldDebug.showDebug()) Log("Dash");
        return pressed;
    }


    // ====================================================
    //                      攻撃
    // ====================================================

    public bool Atk_PressedThisFrame()
    {
        if (isDashing || Guard_isPressed() || Guard_PressedThisFrame()) return false;

        // Joy-Con 右 DPAD を攻撃ボタンとして扱う
        bool joyconInput = joycoRight != null &&
                           (joycoRight.GetButtonDown(Joycon.Button.DPAD_LEFT) ||
                            joycoRight.GetButtonDown(Joycon.Button.DPAD_RIGHT) ||
                            joycoRight.GetButtonDown(Joycon.Button.DPAD_UP) ||
                            joycoRight.GetButtonDown(Joycon.Button.DPAD_DOWN));

        bool pressed;

        if (Atk_InputType == 0)
        {
            var key = ToKeyControl(Player_Atk);
            pressed = (key != null && key.wasPressedThisFrame) || joyconInput;
        }
        else
        {
            var key = ToMouseControl(Player_Atk);
            pressed = (key != null && key.wasPressedThisFrame) || joyconInput;
        }

        if (pressed&&worldDebug.showDebug()) Log("Attack Pressed");
        return pressed;
    }

    public bool Atk_isPressed()
    {
        if (isDashing || Guard_isPressed() || Guard_PressedThisFrame()) return false;

        bool joyconInput = joycoRight != null &&
                           (joycoRight.GetButton(Joycon.Button.DPAD_LEFT) ||
                            joycoRight.GetButton(Joycon.Button.DPAD_RIGHT) ||
                            joycoRight.GetButton(Joycon.Button.DPAD_UP) ||
                            joycoRight.GetButton(Joycon.Button.DPAD_DOWN));

        var key = (Atk_InputType == 0)
            ? ToKeyControl(Player_Atk)
            : ToMouseControl(Player_Atk);

        bool pressed = (key != null && key.isPressed) || joyconInput;

        if (pressed&&worldDebug.showDebug()) Log("Attack Holding");
        return pressed;
    }


    // ====================================================
    //                    ガード
    // ====================================================

    public bool Guard_PressedThisFrame()
    {
        if (isDashing) return false;

        bool joyconInput = joycoRight != null &&
                           joycoRight.GetButtonDown(Joycon.Button.SHOULDER_2);

        var key = (Guard_InputType == 0)
            ? ToKeyControl(Player_Guard)
            : ToMouseControl(Player_Guard);

        bool pressed = (key != null && key.wasPressedThisFrame) || joyconInput;

        if (pressed&&worldDebug.showDebug()) Log("Guard Pressed");
        return pressed;
    }

    public bool Guard_isPressed()
    {
        if (isDashing) return false;

        bool joyconInput = joycoRight != null &&
                           joycoRight.GetButton(Joycon.Button.SHOULDER_2);

        var key = (Guard_InputType == 0)
            ? ToKeyControl(Player_Guard)
            : ToMouseControl(Player_Guard);

        bool pressed = (key != null && key.isPressed) || joyconInput;

        if (pressed&&worldDebug.showDebug()) Log("Guard Holding");
        return pressed;
    }


    // ====================================================
    //         KeyCode → Input System の実オブジェクト
    // ====================================================

    public static KeyControl ToKeyControl(KeyCode code)
    {
        var kb = Keyboard.current;
        if (kb == null) return null;

        return code switch
        {
            // アルファベット
            KeyCode.A => kb.aKey,
            KeyCode.B => kb.bKey,
            KeyCode.C => kb.cKey,
            KeyCode.D => kb.dKey,
            KeyCode.E => kb.eKey,
            KeyCode.F => kb.fKey,
            KeyCode.G => kb.gKey,
            KeyCode.H => kb.hKey,
            KeyCode.I => kb.iKey,
            KeyCode.J => kb.jKey,
            KeyCode.K => kb.kKey,
            KeyCode.L => kb.lKey,
            KeyCode.M => kb.mKey,
            KeyCode.N => kb.nKey,
            KeyCode.O => kb.oKey,
            KeyCode.P => kb.pKey,
            KeyCode.Q => kb.qKey,
            KeyCode.R => kb.rKey,
            KeyCode.S => kb.sKey,
            KeyCode.T => kb.tKey,
            KeyCode.U => kb.uKey,
            KeyCode.V => kb.vKey,
            KeyCode.W => kb.wKey,
            KeyCode.X => kb.xKey,
            KeyCode.Y => kb.yKey,
            KeyCode.Z => kb.zKey,

            // 数字列（上段）
            KeyCode.Alpha0 => kb.digit0Key,
            KeyCode.Alpha1 => kb.digit1Key,
            KeyCode.Alpha2 => kb.digit2Key,
            KeyCode.Alpha3 => kb.digit3Key,
            KeyCode.Alpha4 => kb.digit4Key,
            KeyCode.Alpha5 => kb.digit5Key,
            KeyCode.Alpha6 => kb.digit6Key,
            KeyCode.Alpha7 => kb.digit7Key,
            KeyCode.Alpha8 => kb.digit8Key,
            KeyCode.Alpha9 => kb.digit9Key,

            // テンキー
            KeyCode.Keypad0 => kb.numpad0Key,
            KeyCode.Keypad1 => kb.numpad1Key,
            KeyCode.Keypad2 => kb.numpad2Key,
            KeyCode.Keypad3 => kb.numpad3Key,
            KeyCode.Keypad4 => kb.numpad4Key,
            KeyCode.Keypad5 => kb.numpad5Key,
            KeyCode.Keypad6 => kb.numpad6Key,
            KeyCode.Keypad7 => kb.numpad7Key,
            KeyCode.Keypad8 => kb.numpad8Key,
            KeyCode.Keypad9 => kb.numpad9Key,
            KeyCode.KeypadPlus => kb.numpadPlusKey,
            KeyCode.KeypadMinus => kb.numpadMinusKey,
            KeyCode.KeypadMultiply => kb.numpadMultiplyKey,
            KeyCode.KeypadDivide => kb.numpadDivideKey,
            KeyCode.KeypadEnter => kb.numpadEnterKey,
            KeyCode.KeypadPeriod => kb.numpadPeriodKey,

            // ファンクションキー
            KeyCode.F1 => kb.f1Key,
            KeyCode.F2 => kb.f2Key,
            KeyCode.F3 => kb.f3Key,
            KeyCode.F4 => kb.f4Key,
            KeyCode.F5 => kb.f5Key,
            KeyCode.F6 => kb.f6Key,
            KeyCode.F7 => kb.f7Key,
            KeyCode.F8 => kb.f8Key,
            KeyCode.F9 => kb.f9Key,
            KeyCode.F10 => kb.f10Key,
            KeyCode.F11 => kb.f11Key,
            KeyCode.F12 => kb.f12Key,

            // 矢印キー
            KeyCode.UpArrow => kb.upArrowKey,
            KeyCode.DownArrow => kb.downArrowKey,
            KeyCode.LeftArrow => kb.leftArrowKey,
            KeyCode.RightArrow => kb.rightArrowKey,

            // 修飾キー
            KeyCode.LeftShift => kb.leftShiftKey,
            KeyCode.RightShift => kb.rightShiftKey,
            KeyCode.LeftControl => kb.leftCtrlKey,
            KeyCode.RightControl => kb.rightCtrlKey,
            KeyCode.LeftAlt => kb.leftAltKey,
            KeyCode.RightAlt => kb.rightAltKey,
            KeyCode.LeftCommand => kb.leftMetaKey,
            KeyCode.RightCommand => kb.rightMetaKey,

            // その他
            KeyCode.Space => kb.spaceKey,
            KeyCode.Tab => kb.tabKey,
            KeyCode.Return => kb.enterKey,
            KeyCode.Backspace => kb.backspaceKey,
            KeyCode.Escape => kb.escapeKey,

            KeyCode.Insert => kb.insertKey,
            KeyCode.Delete => kb.deleteKey,
            KeyCode.Home => kb.homeKey,
            KeyCode.End => kb.endKey,
            KeyCode.PageUp => kb.pageUpKey,
            KeyCode.PageDown => kb.pageDownKey,

            // 記号
            KeyCode.Minus => kb.minusKey,
            KeyCode.Equals => kb.equalsKey,
            KeyCode.LeftBracket => kb.leftBracketKey,
            KeyCode.RightBracket => kb.rightBracketKey,
            KeyCode.Semicolon => kb.semicolonKey,
            KeyCode.Quote => kb.quoteKey,
            KeyCode.Comma => kb.commaKey,
            KeyCode.Period => kb.periodKey,
            KeyCode.Slash => kb.slashKey,
            KeyCode.Backslash => kb.backslashKey,
            KeyCode.BackQuote => kb.backquoteKey,

            _ => null
        };
    }

    // ----------------------------------------------------
    //           マウスの KeyCode を InputSystem に変換
    // ----------------------------------------------------
    public static ButtonControl ToMouseControl(KeyCode code)
    {
        var mouse = Mouse.current;
        if (mouse == null) return null;

        return code switch
        {
            KeyCode.Mouse0 => mouse.leftButton,
            KeyCode.Mouse1 => mouse.rightButton,
            KeyCode.Mouse2 => mouse.middleButton,
            KeyCode.Mouse3 => mouse.forwardButton,
            KeyCode.Mouse4 => mouse.backButton,
            _ => null
        };
    }
}
