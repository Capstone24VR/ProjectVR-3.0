#if UNITY_EDITOR
using UnityEngine;
using ParrelSync;
using System.Collections;
using UnityEngine.XR.Management;
using UnityEngine.UI;

public class VRUtilities : MonoBehaviour
{
    public bool enabledXR = false;

    void Awake()
    {
        if (ClonesManager.IsClone())
        {
            DisableXR();
        }
        else
        {
            EnableXR();
        }
    }

    private void OnValidate()
    {
        if (enabledXR)
        {
            EnableXR();
        }
        else
        {
            DisableXR();
        }
    }

    public void EnableXR()
    {
        StartCoroutine(StartXRCoroutine());
        enabledXR = true;
    }

    public void DisableXR()
    {
        Debug.Log("Stopping XR...");
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        Debug.Log("XR stopped completely.");
        enabledXR = false;
    }


    public IEnumerator StartXRCoroutine()
    {
        Debug.Log("Initializing XR...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
        }
        else
        {
            Debug.Log("Starting XR...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
    }
}
#endif