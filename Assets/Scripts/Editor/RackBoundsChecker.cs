using UnityEngine;
using UnityEditor;

public class RackBoundsChecker
{
    public static string Check()
    {
        GameObject rack = EditorUtility.InstanceIDToObject(56376) as GameObject;
        BoxCollider col = rack.GetComponent<BoxCollider>();
        if (col != null)
        {
            Vector3 size = col.size;
            Vector3 lossy = rack.transform.lossyScale;
            float maxDim = Mathf.Max(size.x * lossy.x, size.y * lossy.y, size.z * lossy.z);
            return $"Rack Bounds: Size={size}, LossyScale={lossy}, MaxDim={maxDim}";
        }
        return "No collider on rack";
    }
}
