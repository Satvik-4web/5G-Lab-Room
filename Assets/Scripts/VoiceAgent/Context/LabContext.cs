using System;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Static tracking hub for the user's current spatial attention in the 5G lab.
    /// Decoupled and non-breaking for existing interaction scripts.
    /// </summary>
    public static class LabContext
    {
        public static EquipmentId CurrentHovered { get; private set; } = EquipmentId.None;
        public static EquipmentId CurrentInspected { get; private set; } = EquipmentId.None;

        public static event Action<EquipmentId> OnHoverChanged;
        public static event Action<EquipmentId> OnInspectChanged;

        public static void SetHovered(EquipmentId id)
        {
            if (CurrentHovered != id)
            {
                CurrentHovered = id;
                OnHoverChanged?.Invoke(id);
            }
        }

        public static void SetInspected(EquipmentId id)
        {
            if (CurrentInspected != id)
            {
                CurrentInspected = id;
                OnInspectChanged?.Invoke(id);
            }
        }

        public static EquipmentId ResolveEquipment(GameObject go)
        {
            if (go == null) return EquipmentId.None;
            string name = go.name.ToLowerInvariant();

            var label = go.GetComponent<InteractableLabel>() 
                     ?? go.GetComponentInChildren<InteractableLabel>() 
                     ?? go.GetComponentInParent<InteractableLabel>();
            if (label != null && !string.IsNullOrEmpty(label.objectName))
            {
                name += " " + label.objectName.ToLowerInvariant();
            }

            if (go.transform.parent != null)
            {
                name += " " + go.transform.parent.name.ToLowerInvariant();
            }

            if (name.Contains("gnodeb") || name.Contains("antenna") || name.Contains("antena") || name.Contains("base station") || name.Contains("tower"))
            {
                return EquipmentId.gnodeb;
            }

            if (name.Contains("mec") || name.Contains("rack") || name.Contains("cabinet") || name.Contains("computing") || name.Contains("server"))
            {
                return EquipmentId.mec;
            }

            if (name.Contains("spectrum") || name.Contains("analyzer") || name.Contains("analyser") || name.Contains("soectrum") || name.Contains("spectram"))
            {
                return EquipmentId.spectrum;
            }

            return EquipmentId.None;
        }
    }
}
