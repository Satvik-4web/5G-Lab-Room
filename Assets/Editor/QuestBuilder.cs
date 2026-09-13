using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public class QuestBuilder
{
    [MenuItem("Antigravity/Build Meta Quest APK")]
    public static void BuildQuestAPK()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            EditorUtility.DisplayDialog("Android Support Missing", "Android Build Support is not installed! Please open Unity Hub, go to Installs, click the gear icon next to your Unity version, select 'Add modules', and check 'Android Build Support'.", "OK");
            return;
        }

        string apkPath = EditorUtility.SaveFilePanel("Save APK", System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "5GLabRoom", "apk");
        if (string.IsNullOrEmpty(apkPath)) return;

        // Switch to Android if not already
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("Switching to Android target... This may take a while.");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        // Configure Settings for Meta Quest
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3, UnityEngine.Rendering.GraphicsDeviceType.Vulkan });
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)32;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        PlayerSettings.companyName = "5G Lab";
        PlayerSettings.productName = "5G Lab VR";
        
        // Ensure scene is saved
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        
        string[] scenes = { "Assets/Scenes/ComputerLab.unity" }; // Ensure this matches their scene path

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        Debug.Log("Starting Build...");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            EditorUtility.DisplayDialog("Build Success", "APK built successfully!\nYou can now use SideQuest to install " + apkPath + " onto your Meta Quest.", "Awesome!");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
            EditorUtility.DisplayDialog("Build Failed", "There were errors during the build. Check the Unity Console.", "OK");
        }
    }
}
