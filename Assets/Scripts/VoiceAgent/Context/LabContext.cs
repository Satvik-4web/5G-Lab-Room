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
            if (name.Contains("gnodeb") || name.Contains("antenna") || name.Contains("base station")) return EquipmentId.gnodeb;
            if (name.Contains("mec") || name.Contains("rack") || name.Contains("cabinet") || name.Contains("computing")) return EquipmentId.mec;
            if (name.Contains("spectrum") || name.Contains("analyzer")) return EquipmentId.spectrum;
            return EquipmentId.None;
        }
    }
}
