using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
//ここで操作変更できるようにした
public class PlayerMovement : MonoBehaviour
{
    [Header("0:キーボード 1:マウス")]
    [Header("プレイヤー操作(移動)キーボードのみ")]
    public KeyCode Player_Move_Forward = KeyCode.W;
    public KeyCode Player_Move_Backward = KeyCode.S;
    public KeyCode Player_Move_Left = KeyCode.A;
    public KeyCode Player_Move_Right = KeyCode.D;
    [Header("ダッシュ")]
    [Range(0, 1)] int Dash_InputType = 0; //0:キーボード 1:マウス
    public KeyCode Player_Dash = KeyCode.LeftShift;
    [Header("攻撃")]
    [Range(0, 1)] int Atk_InputType = 1; //0:キーボード 1:マウス
    public KeyCode Player_Atk = KeyCode.Mouse0;
    [Header("ガード")]
    [Range(0, 1)] int Guard_InputType = 0; //0:キーボード 1:マウス
    public KeyCode Player_Guard = KeyCode.G;

    public bool isDashing = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // 動かさない
    public void SetPlayerDashing(bool isdashing) 
    {
        isDashing = isdashing;
    }
    public bool GetPlayerDashing()
    {
        return isDashing;
    }

    //移動関連
    public bool Move_Forward_isPressed()//wキー
    {
        if (isDashing) return false;

        var forwardKey = ToKeyControl(Player_Move_Forward);
        return forwardKey != null && forwardKey.isPressed;
    }
    public bool Move_Backward_isPressed()//sキー
    {
        if (isDashing) return false;

        var backwardKey = ToKeyControl(Player_Move_Backward);
        return backwardKey != null && backwardKey.isPressed;
    }
    public bool Move_Left_isPressed()//aキー
    {
        if (isDashing) return false;

        var leftKey = ToKeyControl(Player_Move_Left);
        return leftKey != null && leftKey.isPressed;
    }
    public bool Move_Right_isPressed()//dキー
    {
        if (isDashing) return false;

        var rightKey = ToKeyControl(Player_Move_Right);
        return rightKey != null && rightKey.isPressed;
    }
    public bool Dash_isPressed()
    {
        if (isDashing) return false;

        if (Dash_InputType == 0)
        {
            var dashKey = ToKeyControl(Player_Dash);
            return dashKey != null && dashKey.isPressed;
        }
        else
        {
            var dashKey = ToMouseControl(Player_Dash);
            return dashKey != null && dashKey.isPressed;
        }
    }
    //攻撃関連と確定
    public bool Atk_PressedThisFrame()
    {
        if (isDashing) return false;
        
        if (Atk_InputType == 0)
        {
            var atkKey = ToKeyControl(Player_Atk);
            return atkKey != null && atkKey.wasPressedThisFrame;
        }
        else
        {
            var atkKey = ToMouseControl(Player_Atk);
            return atkKey != null && atkKey.wasPressedThisFrame;
        }
    }
    public bool Atk_isPressed()
    {
        if (isDashing) return false;
        
        if (Atk_InputType == 0)
        {
            var atkKey = ToKeyControl(Player_Atk);
            return atkKey != null && atkKey.isPressed;
        }
        else
        {
            var atkKey = ToMouseControl(Player_Atk);
            return atkKey != null && atkKey.isPressed;
        }
    }


    //ガード関連
    public bool Guard_PressedThisFrame()
    {
        if (isDashing) return false;

        if (Guard_InputType == 0)
        {
            var guardKey = ToKeyControl(Player_Guard);
            return guardKey != null && guardKey.wasPressedThisFrame;
        }
        else
        {
            var guardKey = ToMouseControl(Player_Guard);
            return guardKey != null && guardKey.wasPressedThisFrame;
        }
    }
    public bool Guard_isPressed()
    {
        if (isDashing)return false;

        if (Guard_InputType == 0)
        {
            var guardKey = ToKeyControl(Player_Guard);
            return guardKey != null && guardKey.isPressed;
        }
        else
        {
            var guardKey = ToMouseControl(Player_Guard);
            return guardKey != null && guardKey.isPressed;
        }
    }

    public static KeyControl ToKeyControl(KeyCode code)
    {

        var kb = Keyboard.current;
        if (kb == null) return null;

        return code switch
        {
            // Alphabet
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

            // Number row (top)
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

            // Keypad (テンキー)
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

            // Function keys
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

            // Arrow keys
            KeyCode.UpArrow => kb.upArrowKey,
            KeyCode.DownArrow => kb.downArrowKey,
            KeyCode.LeftArrow => kb.leftArrowKey,
            KeyCode.RightArrow => kb.rightArrowKey,

            // Modifier keys
            KeyCode.LeftShift => kb.leftShiftKey,
            KeyCode.RightShift => kb.rightShiftKey,
            KeyCode.LeftControl => kb.leftCtrlKey,
            KeyCode.RightControl => kb.rightCtrlKey,
            KeyCode.LeftAlt => kb.leftAltKey,
            KeyCode.RightAlt => kb.rightAltKey,
            KeyCode.LeftCommand => kb.leftMetaKey,
            KeyCode.RightCommand => kb.rightMetaKey,

            // Common keys
            KeyCode.Space => kb.spaceKey,
            KeyCode.Tab => kb.tabKey,
            KeyCode.Return => kb.enterKey,
            KeyCode.Backspace => kb.backspaceKey,
            KeyCode.Escape => kb.escapeKey,

            // Navigation keys
            KeyCode.Insert => kb.insertKey,
            KeyCode.Delete => kb.deleteKey,
            KeyCode.Home => kb.homeKey,
            KeyCode.End => kb.endKey,
            KeyCode.PageUp => kb.pageUpKey,
            KeyCode.PageDown => kb.pageDownKey,

            // Symbols / punctuation
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
