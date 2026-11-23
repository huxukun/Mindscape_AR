using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LogScene
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Mindscape.unity");
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Debug.Log($"CAM name={cam.name} tag={cam.tag} active={cam.gameObject.activeInHierarchy} enabled={cam.enabled} pos={cam.transform.position} rot={cam.transform.rotation.eulerAngles} parent={(cam.transform.parent != null ? cam.transform.parent.name : "root")}");
            foreach (var c in cam.GetComponents<Component>())
            {
                var b = c as Behaviour;
                Debug.Log("  COMP " + c.GetType().FullName + " enabled=" + (b == null || b.enabled));
            }
        }

        var hud = GameObject.Find("Mindscape HUD");
        if (hud != null)
        {
            Debug.Log($"HUD parent={(hud.transform.parent != null ? hud.transform.parent.name : "root")} pos={hud.transform.position} rot={hud.transform.rotation.eulerAngles}");
        }

        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            Debug.Log("ROOT " + root.name);
        }
    }
}
