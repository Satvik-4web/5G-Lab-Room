using UnityEngine;
using UnityEditor;

public class AndroidChecker
{
    public static string Check()
    {
        return BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android).ToString();
    }
}
