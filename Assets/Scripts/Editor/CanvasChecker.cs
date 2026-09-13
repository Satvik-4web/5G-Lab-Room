using UnityEngine;
using UnityEditor;

public class CanvasChecker
{
    public static string Check()
    {
        GameObject uiManager = EditorUtility.InstanceIDToObject(57228) as GameObject;
        Canvas canvas = uiManager.GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            return $"Canvas RenderMode: {canvas.renderMode}, PlaneDist: {canvas.planeDistance}";
        }
        return "No canvas found";
    }
}
