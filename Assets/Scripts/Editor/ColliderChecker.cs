using UnityEngine;
using UnityEditor;

public class ColliderChecker
{
    public static string Check()
    {
        GameObject antenna = EditorUtility.InstanceIDToObject(58410) as GameObject;
        BoxCollider col = antenna.GetComponent<BoxCollider>();
        if (col != null)
        {
            return $"Antenna Collider: Center={col.center}, Size={col.size}";
        }
        return "No collider on antenna";
    }
}
