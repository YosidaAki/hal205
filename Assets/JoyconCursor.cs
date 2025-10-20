using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoyconCursor : MonoBehaviour {
    
    private List<Joycon> joycons;

    // Values made available via Unity
    public Vector2 stick;
    public Vector3 gyro;
    public Vector3 accel;
    public int jc_ind = 0;
    public Quaternion orientation;

    public Vector3 debug_v3;
    public Vector3 debug_v3_2;
    public Vector3 debug_sspos;

    public GameObject cursor;

    private Vector3 gyroAcc;

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

        gyroAcc = Vector3.zero;
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
            gyro = Mathf.Rad2Deg * new Vector3(-gyroValue.y, gyroValue.z, -gyroValue.x);
            //gyro = Mathf.Rad2Deg * new Vector3(-gyroValue.y, gyroValue.z, 0.0f);

            accel = j.GetAccel();

            orientation *= Quaternion.Euler(
                gyro * Time.deltaTime * 2.0f);

            if(j.GetButtonDown(Joycon.Button.SHOULDER_1))
            {
                gyroAcc = Vector3.zero;
            }

            //gameObject.transform.rotation = orientation;

            Vector3 v3 = orientation * new Vector3(0.0f, 0.0f, 1.0f);

            float yRot = Mathf.Atan2(v3.z, v3.x);
            float xRot = Mathf.Atan2(v3.z, v3.y);

            gameObject.transform.position = Camera.main.cameraToWorldMatrix.MultiplyPoint(new Vector3(v3.x, v3.y, -v3.z) * 10.0f);
            //gameObject.transform.rotation = orientation;

            {
                Vector4 tempVec2 = Camera.main.projectionMatrix * new Vector4(v3.x, v3.y, -v3.z, 1.0f);

                v3 = new Vector3(tempVec2.x, tempVec2.y, tempVec2.z) / tempVec2.w;

                Debug.Log(v3);

                cursor.GetComponent<RectTransform>().localPosition = new Vector3(
                    v3.x * 0.5f * 1920.0f,
                    v3.y * 0.5f * 1080.0f,
                    0.0f);
            }
        }
    }
}