using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Management;
using Viture.XR;

/// <summary>
/// 无界面初始化 VITURE XR 工程：配置 XR Plug-in、Android 参数、输入系统，并生成最小场景。
/// 由 Unity -executeMethod VitureAutoSetup.Run 调用。
/// </summary>
public static class VitureAutoSetup
{
    private const string k_SampleRoot = "Assets/Samples/VITURE XR Plugin/0.9.0/Starter Assets";
    private const string k_ScenePath = "Assets/Scenes/Mindscape.unity";

    public static void Run()
    {
        try
        {
            ConfigureAndroidTools();
            ConfigureVitureSettings();
            ConfigureXrPlugin();
            ConfigurePlayer();
            ConfigureInput();
            CreateScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("VITURE_AUTO_SETUP_OK");
        }
        catch (Exception e)
        {
            Debug.LogError("VITURE_AUTO_SETUP_FAILED: " + e);
            throw;
        }
    }

    private static void ConfigureAndroidTools()
    {
        AndroidExternalToolsSettings.sdkRootPath = "D:/Android/Sdk";
        AndroidExternalToolsSettings.ndkRootPath = "D:/Android/Sdk/ndk/27.2.12479018";
        AndroidExternalToolsSettings.jdkRootPath = "D:/Android/jdk17";
        Debug.Log("Android tools configured: SDK=D:/Android/Sdk, NDK=r27c, JDK=17");
    }

    private static void ConfigureVitureSettings()
    {
        const string path = "Assets/XR/VitureSettings.asset";
        var settings = AssetDatabase.LoadAssetAtPath<VitureSettings>(path);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<VitureSettings>();
            AssetDatabase.CreateAsset(settings, path);
        }

        settings.AppGlassesSupport = VitureAppGlassesSupport.SixDoFOnly;
        settings.ActivateHandTrackingOnStartup = false;
        settings.CameraPermission = false;
        settings.RecordAudioPermission = false;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        EditorBuildSettings.AddConfigObject(VitureConstants.k_SettingsKey, settings, true);
        Debug.Log("Viture settings configured: " + path);
    }

    private static void ConfigureXrPlugin()
    {
        var buildTargetSettings = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget")
            .Select(guid => AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(guid)))
            .FirstOrDefault();

        if (buildTargetSettings == null)
        {
            buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(buildTargetSettings, "Assets/XRGeneralSettingsPerBuildTarget.asset");
        }

        var generalSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
        if (generalSettings == null)
        {
            generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            AssetDatabase.AddObjectToAsset(generalSettings, buildTargetSettings);
            buildTargetSettings.SetSettingsForBuildTarget(BuildTargetGroup.Android, generalSettings);

            var managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
            AssetDatabase.AddObjectToAsset(managerSettings, buildTargetSettings);
            generalSettings.Manager = managerSettings;

            EditorUtility.SetDirty(buildTargetSettings);
            AssetDatabase.SaveAssets();
        }

        if (generalSettings.Manager == null)
        {
            throw new Exception("XRManagerSettings is null");
        }

        while (generalSettings.Manager.activeLoaders.Count > 0)
        {
            var loaderName = generalSettings.Manager.activeLoaders[0].GetType().FullName;
            XRPackageMetadataStore.RemoveLoader(generalSettings.Manager, loaderName, BuildTargetGroup.Android);
        }

        if (!XRPackageMetadataStore.AssignLoader(generalSettings.Manager, "VitureLoader", BuildTargetGroup.Android))
        {
            throw new Exception("Failed to assign VitureLoader. Check that com.viture.xr compiled successfully.");
        }
    }

    private static void ConfigurePlayer()
    {
        PlayerSettings.companyName = "Mindscape";
        PlayerSettings.productName = "Mindscape AR";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.huxukun.mindscape");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.resizeableActivity = true;

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetMobileMTRendering(NamedBuildTarget.Android, true);
        PlayerSettings.graphicsJobs = false;
        PlayerSettings.gpuSkinning = true;

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;

        // Active Input Handling = Input System Package (New)
        var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (settings != null && settings.Length > 0)
        {
            var so = new SerializedObject(settings[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null)
            {
                prop.intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void ConfigureInput()
    {
        var guids = AssetDatabase.FindAssets("t:InputActionAsset Viture Input Actions");
        if (guids.Length == 0)
        {
            throw new Exception("Viture Input Actions asset not found inside com.viture.xr");
        }

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        if (asset == null)
        {
            throw new Exception("Failed to load input actions at " + path);
        }

        InputSystem.actions = asset;
        EditorUtility.SetDirty(InputSystem.settings);
    }

    private static void CreateScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var originPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_SampleRoot + "/Prefabs/XR Origin (Viture).prefab");
        if (originPrefab == null)
        {
            throw new Exception("XR Origin prefab not found at " + k_SampleRoot);
        }
        PrefabUtility.InstantiatePrefab(originPrefab);

        var quickPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_SampleRoot + "/Prefabs/Viture Quick Actions.prefab");
        if (quickPrefab != null)
        {
            PrefabUtility.InstantiatePrefab(quickPrefab);
        }

        var cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            var camPos = cam.transform.position;
            var canvasPos = camPos + cam.transform.forward * 2.0f + cam.transform.up * -0.2f;
            var canvasGo = new GameObject("Mindscape HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.position = canvasPos;
            canvasGo.transform.rotation = Quaternion.LookRotation(canvasPos - camPos, Vector3.up);
            canvasGo.transform.localScale = Vector3.one * 0.001f;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 260f);

            var textGo = new GameObject("Status", typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);

            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 64;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "MINDSCAPE AR\nMEMORY SPACE READY";

            var rt = textGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1000f, 260f);
            rt.anchoredPosition = Vector2.zero;
        }

        EditorSceneManager.SaveScene(scene, k_ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(k_ScenePath, true) };
    }
}
