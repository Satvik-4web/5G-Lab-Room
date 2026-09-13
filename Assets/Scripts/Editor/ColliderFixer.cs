using UnityEngine;
using UnityEditor;

public class ColliderFixer
{
    public static string Fix()
    {
        GameObject antenna = EditorUtility.InstanceIDToObject(58410) as GameObject;
        GameObject rack = EditorUtility.InstanceIDToObject(56376) as GameObject;
        GameObject spec = EditorUtility.InstanceIDToObject(60432) as GameObject; // Spectrum Analyzer

        void FitCollider(GameObject go)
        {
            if (go == null) return;
            BoxCollider col = go.GetComponent<BoxCollider>();
            if (col == null) return;

            Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                foreach (Renderer r in renderers)
                {
                    bounds.Encapsulate(r.bounds);
                }
                
                // Convert bounds to local space
                col.center = go.transform.InverseTransformPoint(bounds.center);
                // Size needs to account for lossy scale
                Vector3 localSize = bounds.size;
                Vector3 scale = go.transform.lossyScale;
                localSize.x /= scale.x != 0 ? scale.x : 1;
                localSize.y /= scale.y != 0 ? scale.y : 1;
                localSize.z /= scale.z != 0 ? scale.z : 1;
                col.size = localSize;
            }
            EditorUtility.SetDirty(go);
        }

        FitCollider(antenna);
        FitCollider(rack);
        FitCollider(spec);
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        return "Colliders fixed!";
    }
}
