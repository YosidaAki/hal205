using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class JoyconCursor : MonoBehaviour {
    private List<Joycon> joycons;

    // Values made available via Unity
    public Vector2 stick;
    public Vector3 gyro;
    public Vector3 accel;
    public int jc_ind = 0;
    public Quaternion orientation;

    public float cursorSpeed;

    private Vector3 i_b;
    private Vector3 j_b;
    private Vector3 k_b;

    public GameObject cursor;

    void Start()
    {
        gyro = new Vector3(0, 0, 0);
        accel = new Vector3(0, 0, 0);
        orientation = Quaternion.identity;
        // get the public Joycon array attached to the JoyconManager in scene
        joycons = JoyconManager.Instance.j;
        if(joycons.Count < jc_ind+1)
        {
            Destroy(gameObject);
        }
        i_b = new Vector3(1, 0, 0);
        j_b = new Vector3(0, 1, 0);
        k_b = new Vector3(0, 0, 1);
    }

    private float DeadZoneGyro(float value, float deadzone)
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
        // make sure the Joycon only gets checked if attached
        if(joycons.Count > 0)
        {
            Joycon j = joycons[jc_ind];

            float[] stickValue = j.GetStick();
            stick = new Vector2(stickValue[0], stickValue[1]);

            Vector3 gyroValue = j.GetGyro();
            gyro = new Vector3(-gyroValue.y, gyroValue.z, -gyroValue.x) * Mathf.Rad2Deg;

            Vector3 accelValue = j.GetAccel();
            accel = new Vector3(accelValue.y, -accelValue.z, accelValue.x);

            orientation *= Quaternion.Euler(Time.deltaTime * cursorSpeed * gyro);

            if (j.GetButtonDown(Joycon.Button.SHOULDER_2))
            {
                orientation = Quaternion.identity;
            }

            gameObject.transform.rotation = orientation;

            Vector3 v3 = orientation * new Vector3(0.0f, 0.0f, 1.0f);

            if(Mathf.Abs(v3.x) < 0.9f && Mathf.Abs(v3.y) < 0.9f && v3.z > 0.0f)
            {
                Vector4 tempVec2 = Camera.main.projectionMatrix * new Vector4(v3.x, v3.y, -v3.z, 1.0f);

                v3 = new Vector3(tempVec2.x, tempVec2.y, tempVec2.z) / tempVec2.w;

                v3.x = Mathf.Clamp(v3.x, -1.0f, 1.0f);
                v3.y = Mathf.Clamp(v3.y, -1.0f, 1.0f);

                cursor.GetComponent<RectTransform>().localPosition = new Vector3(
                    v3.x * 0.5f * 1920.0f,
                    v3.y * 0.5f * 1080.0f,
                    0.0f);

                tempVec2 = Camera.main.projectionMatrix.inverse * new Vector4(v3.x, v3.y, v3.z, 1.0f);

                v3 = new Vector3(tempVec2.x, tempVec2.y, tempVec2.z) / tempVec2.w;
                v3.Normalize();

                //gameObject.transform.position = Camera.main.cameraToWorldMatrix.MultiplyPoint(v3 * 10.0f);
            }


        }
    }
}