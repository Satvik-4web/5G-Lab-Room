using UnityEngine;

public class DisableSimulatorInBuild : MonoBehaviour
{
    void Awake()
    {
        // Safety feature: If this is NOT running in the Unity Editor,
        // we destroy the Simulator so it doesn't block the real VR Headset/Controllers.
        if (!Application.isEditor)
        {
            Destroy(gameObject);
        }
    }
}
