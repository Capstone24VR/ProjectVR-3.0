using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoyouncyObject : MonoBehaviour
{

    public float underWaterDrag = 3f;
    public float underWaterAngularDrag = 1f;
    public float airDrag = 0f;
    public float airAngularDrag = 0.05f;
    public float floatingPower = 15f;
    public float waterHeight = 0.25f;
    public bool underwater;

    public float uprightTorqueStrength = 10f;

    private Rigidbody rb;

    public void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        float difference = transform.position.y - waterHeight;
        if(difference < 0)
        {
            rb.AddForceAtPosition(Vector3.up * floatingPower * Mathf.Abs(difference), transform.position, ForceMode.Force);

            // Rotate to stay upright
            Vector3 upright = Vector3.up;
            Vector3 currentUp = transform.up;
            Vector3 torque = Vector3.Cross(currentUp, upright) * uprightTorqueStrength;
            rb.AddTorque(torque, ForceMode.Force);

            if (!underwater)
            {
                underwater = true;
                SwitchState(underwater);
            }
        }
        else if(underwater)
        {
            underwater = false;
            SwitchState(underwater);
        }
    }

    void SwitchState(bool isUnderwater)
    {
        if(isUnderwater)
        {
            rb.drag = underWaterDrag;
            rb.angularDrag = underWaterAngularDrag;
        }
        else
        {
            rb.drag = airDrag;
            rb.angularDrag= airAngularDrag;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
