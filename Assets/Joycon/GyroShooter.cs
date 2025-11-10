using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GyroShooter : MonoBehaviour
{
    private List<Joycon> joycons;

    public Vector3 gyro;
    public int jc_ind = 0;

    public float chargeValue;
    public float chargeMultiplier;

    public Vector2 cursorSpeed;
    public Vector2 cursorCalibration;
    public Vector2 cursorPos;

    public GameObject cursor;
    public GameObject bulletPrefab;

    private Vector2 calibrationAcc;
    private int calibrationCount;
    private RailMover railMover;
    private bool active;

    void Start()
    {
        cursor.SetActive(false);
        railMover = GetComponent<RailMover>();
        chargeValue = 0.0f;
        gyro = new Vector3(0, 0, 0);
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

    // Update is called once per frame
    void Update()
    {
        bool shoot = false;
        bool charge = false;

        if (railMover.onRail)
        {
            if (!cursor.activeSelf)
            {
                cursor.SetActive(true);
                //cursorPos = new Vector2(0.5f, 0.5f);
            }
        }
        else
        {
            if (cursor.activeSelf)
            {
                cursor.SetActive(false);
            }
            return;
        }

        // make sure the Joycon only gets checked if attached
        if (joycons.Count > 0)
        {
            Joycon j = joycons[jc_ind];

            gyro = j.GetGyro();

            if (calibrationCount < 1000)
            {
                calibrationAcc.x += gyro.z;
                calibrationAcc.y += gyro.y;
                calibrationCount++;

                return;
            }
            if (calibrationCount == 1000)
            {
                cursorCalibration = calibrationAcc / calibrationCount;
            }

            if (j.GetButton(Joycon.Button.SHOULDER_2))
            {
                cursorPos = new Vector2(0.5f, 0.5f);
            }

            cursorPos.x += (gyro.z - cursorCalibration.x) * Time.deltaTime * cursorSpeed.x;
            cursorPos.y += (gyro.y - cursorCalibration.y) * Time.deltaTime * cursorSpeed.y;

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

        Ray ray = Camera.main.ViewportPointToRay(fixedPos);
        Vector3 shootDir = ray.origin + ray.direction * 50.0f - (transform.position + new Vector3(0.0f, 1.0f, 0.0f));
        shootDir.Normalize();
        Debug.DrawLine(transform.position + new Vector3(0.0f, 1.0f, 0.0f), ray.origin + ray.direction * 100.0f);

        if (shoot)
        {
            GameObject bullet = GameObject.Instantiate(bulletPrefab);
            bullet.transform.position = transform.position + new Vector3(0.0f, 1.0f, 0.0f) + shootDir * 0.5f;
            bullet.GetComponent<GyroBullet>().shootDir = shootDir;
        }

        if (charge)
        {
            //chargeValue += (Mathf.Abs(gyro.x) + Mathf.Abs(gyro.y) + Mathf.Abs(gyro.z)) * Time.deltaTime * chargeMultiplier;
        }

        cursor.GetComponent<RectTransform>().localPosition = new Vector3(
                (fixedPos.x - 0.5f) * 1920.0f,
                (fixedPos.y - 0.5f) * 1080.0f,
                0.0f);

        //chargeText.GetComponent<UnityEngine.UI.Text>().text = chargeValue.ToString("0.00");
    }
}
