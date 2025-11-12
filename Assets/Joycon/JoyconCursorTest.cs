using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings;

public class JoyconCursortest : MonoBehaviour {
    private List<Joycon> joycons;

    // Values made available via Unity
    public Vector2 stick;
    public Vector3 gyro;
    public Vector3 accel;
    public int jc_ind = 0;

    public float chargeValue;
    public float chargeMultiplier;

    public Vector2 cursorSpeed;
    public Vector2 cursorDeadzone;
    public Vector2 cursorCalibration;
    public int calibrationCount;

    private Vector2 calibrationAcc;

    public Vector2 cursorPos;

    public GameObject cursor;
    public GameObject chargeText;

    void Start()
    {
        chargeValue = 0.0f;
        gyro = new Vector3(0, 0, 0);
        accel = new Vector3(0, 0, 0);
        cursorPos = new Vector2(0.5f, 0.5f);
        calibrationCount = 1000;
        // get the public Joycon array attached to the JoyconManager in scene
        joycons = JoyconManager.Instance.j;
    }

    public void Look(InputAction.CallbackContext context)
    {
        if (joycons.Count == 0)
        {
            Vector2 aaa = context.ReadValue<Vector2>();
            gyro += new Vector3(aaa.y, aaa.x, 0.0f) * Mathf.Rad2Deg * 0.01f;
        }
    }
    private float DeadzoneGyro(float value, float deadzone)
    {
        if (Mathf.Abs(value) > deadzone)
        {
            value = (Mathf.Abs(value) - deadzone) * Mathf.Sign(value);
        }
        else
        {
            value = 0.0f;
        }
        return value;
    }
    // Update is called once per frame
    void Update()
    {
        bool shoot = false;
        bool charge = false;


        // make sure the Joycon only gets checked if attached
        if (joycons.Count > 0)
        {
            Joycon j = joycons[jc_ind];

            float[] stickValue = j.GetStick();
            stick = new Vector2(stickValue[0], stickValue[1]);

            gyro = j.GetGyro();
            accel = j.GetAccel();

            if(calibrationCount < 1000)
            {
                calibrationAcc.x += gyro.z;
                calibrationAcc.y += gyro.y;
                calibrationCount++;

                return;
            }
            if(calibrationCount == 1000)
            {
                cursorCalibration = calibrationAcc / calibrationCount;
            }

            if (j.GetButton(Joycon.Button.SHOULDER_2))
            {
                cursorPos = new Vector2(0.5f, 0.5f);
            }

            cursorPos.x += DeadzoneGyro(gyro.z - cursorCalibration.x, cursorDeadzone.x) * Time.deltaTime * cursorSpeed.x;
            cursorPos.y += DeadzoneGyro(gyro.y - cursorCalibration.y, cursorDeadzone.y) * Time.deltaTime * cursorSpeed.y;

            if (j.GetButtonDown(Joycon.Button.SHOULDER_1))
            {
                shoot = true;
            }

            if (j.GetButtonDown(Joycon.Button.HOME))
            {
                calibrationAcc = new Vector2();
                calibrationCount = 0;
            }

            if (j.GetButton(Joycon.Button.DPAD_UP))
            {
                charge = true;
            }
        }
        else
        {
            cursorPos = Camera.main.ScreenToViewportPoint(Mouse.current.position.ReadValue());

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                shoot = true;
            }
        }

        Vector2 fixedPos = cursorPos;

        fixedPos.x = Mathf.Clamp(fixedPos.x, 0.0f, 1.0f);
        fixedPos.y = Mathf.Clamp(fixedPos.y, 0.0f, 1.0f);

        if (shoot)
        {
            Ray ray = Camera.main.ViewportPointToRay(fixedPos);
            if (Physics.Raycast(ray.origin, ray.direction * 20.0f, out RaycastHit hitInfo))
            {
                Destroy(hitInfo.collider.gameObject);
            }
        }

        if (charge)
        {
            chargeValue += (Mathf.Abs(gyro.x) + Mathf.Abs(gyro.y) + Mathf.Abs(gyro.z)) * Time.deltaTime * chargeMultiplier;
        }

        cursor.GetComponent<RectTransform>().localPosition = new Vector3(
                (fixedPos.x - 0.5f) * 1920.0f,
                (fixedPos.y - 0.5f) * 1080.0f,
                0.0f);
        chargeText.GetComponent<UnityEngine.UI.Text>().text = chargeValue.ToString("0.00");
    }
}
