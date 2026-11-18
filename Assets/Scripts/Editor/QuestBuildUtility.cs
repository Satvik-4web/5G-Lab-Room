using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

/// <summary>Repeatable headset setup and APK build from the Unity 6 editor.</summary>
public static class QuestBuildUtility
{
    private const string Scene = "Assets/Scenes/ComputerLab.unity";
    private const string XrSettings = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
    private const string Output = "Builds/5GLabRoom-Quest.apk";

    [MenuItem("5G Lab/Prepare Meta Quest")]
    public static void Prepare()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play mode before changing XR loaders.");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Install Android Build Support, Android SDK & NDK Tools, and OpenJDK for this exact Unity editor in Unity Hub.");

        var xr = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(XrSettings);
        if (xr == null) throw new InvalidOperationException("XR settings asset is missing: " + XrSettings);
        if (!xr.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            xr.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

        XRManagerSettings manager = xr.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        if (!XRPackageMetadataStore.AssignLoader(manager, typeof(OpenXRLoader).FullName, BuildTargetGroup.Android)
            && manager.activeLoaders.Count == 0)
            throw new InvalidOperationException("Could not assign the Android OpenXR loader.");
        manager.automaticLoading = true;
        manager.automaticRunning = true;
        EditorUtility.SetDirty(manager);

        // RefreshFeatures creates the Android feature instances, then enable the two
        // features required for a Meta headset and its Touch controllers.
        FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
        OpenXRSettings openXr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (openXr == null) throw new InvalidOperationException("Android OpenXR settings could not be created.");
        var quest = openXr.GetFeature<MetaQuestFeature>();
        var touch = openXr.GetFeature<OculusTouchControllerProfile>();
        if (quest == null || touch == null)
            throw new InvalidOperationException("Meta Quest Support or Oculus Touch Controller Profile is missing from OpenXR.");
        quest.enabled = true;
        touch.enabled = true;
        EditorUtility.SetDirty(quest);
        EditorUtility.SetDirty(touch);
        EditorUtility.SetDirty(openXr);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.satvik.fiveglabroom");
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        AssetDatabase.SaveAssets();
        Debug.Log("Meta Quest project setup complete. Use 5G Lab > Build Meta Quest APK.");
    }

    [MenuItem("5G Lab/Build Meta Quest APK")]
    public static void Build()
    {
        Prepare();
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Could not switch to the Android build target.");
        Directory.CreateDirectory("Builds");
        EditorUserBuildSettings.buildAppBundle = false;
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = Output,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new InvalidOperationException("Quest APK build failed. Check the Unity Console for the first error.");
        Debug.Log("Quest APK ready: " + Path.GetFullPath(Output));
    }
}
