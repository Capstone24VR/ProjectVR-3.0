using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BuoyancyObject : NetworkBehaviour
{

    public float underWaterDrag = 3f;
    public float underWaterAngularDrag = 1f;
    public float airDrag = 0f;
    public float airAngularDrag = 0.05f;
    public float floatingPower = 15f;
    public float waterHeight = 0.25f;
    public NetworkVariable<bool> underwater = new NetworkVariable<bool>(true);

    public float uprightTorqueStrength = 10f;

    private Rigidbody rb;

    public void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Only the server should manage physics and state updates
    private void FixedUpdate()
    {
        if (!IsOwner) return;

        float difference = transform.position.y - waterHeight;
        if(difference < 0)
        {
            rb.AddForceAtPosition(Vector3.up * floatingPower * Mathf.Abs(difference), transform.position, ForceMode.Force);

            // Rotate to stay upright
            Vector3 upright = Vector3.up;
            Vector3 currentUp = transform.up;
            Vector3 torque = Vector3.Cross(currentUp, upright) * uprightTorqueStrength;
            rb.AddTorque(torque, ForceMode.Force);

            if (!underwater.Value)
            {
                SwitchStateServerRpc(true);
            }
        }
        else if(underwater.Value)
        {
            SwitchStateServerRpc(false);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void SwitchStateServerRpc(bool isUnderwater)
    {
        underwater.Value = isUnderwater;
        if (isUnderwater)
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
}
