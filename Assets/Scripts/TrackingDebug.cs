using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class TrackingDebug : MonoBehaviour
{
    private float m_Timer;

    private void Update()
    {
        m_Timer += Time.unscaledDeltaTime;
        if (m_Timer < 1f) return;
        m_Timer = 0f;

        var hmd = InputSystem.devices.OfType<XRHMD>().FirstOrDefault();
        var cam = Camera.main;
        var hmdPos = hmd != null ? hmd.centerEyePosition.ReadValue().ToString("F3") : "-";
        var hmdRot = hmd != null ? hmd.centerEyeRotation.ReadValue().eulerAngles.ToString("F1") : "-";
        var camPos = cam != null ? cam.transform.position.ToString("F3") : "-";
        var camRot = cam != null ? cam.transform.rotation.eulerAngles.ToString("F1") : "-";
        var devs = string.Join("|", InputSystem.devices.Select(d => d.name));
        Debug.Log($"[TRACKDBG] hmd={(hmd != null ? hmd.name : "null")} hmdPos={hmdPos} hmdRot={hmdRot} camPos={camPos} camRot={camRot} devs={devs}");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        var go = new GameObject("TrackingDebug");
        go.AddComponent<TrackingDebug>();
        Object.DontDestroyOnLoad(go);
    }
}
