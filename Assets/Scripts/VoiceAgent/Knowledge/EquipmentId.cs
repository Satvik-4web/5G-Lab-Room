using System;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Identifiers for the interactive equipment components in the 5G Lab.
    /// </summary>
    public enum EquipmentId
    {
        None = 0,
        gnodeb = 1,
        mec = 2,
        spectrum = 3
    }

    public static class EquipmentIdExtensions
    {
        public static string ToIdString(this EquipmentId id)
        {
            return id switch
            {
                EquipmentId.gnodeb => "gnodeb",
                EquipmentId.mec => "mec",
                EquipmentId.spectrum => "spectrum",
                _ => "none"
            };
        }

        public static EquipmentId FromIdString(string id)
        {
            if (string.IsNullOrEmpty(id)) return EquipmentId.None;
            string clean = id.Trim().ToLowerInvariant();
            if (clean.Contains("gnodeb") || clean.Contains("antenna") || clean.Contains("base")) return EquipmentId.gnodeb;
            if (clean.Contains("mec") || clean.Contains("rack") || clean.Contains("server")) return EquipmentId.mec;
            if (clean.Contains("spectrum") || clean.Contains("analyzer")) return EquipmentId.spectrum;
            return EquipmentId.None;
        }

        public static string GetDisplayName(this EquipmentId id)
        {
            return id switch
            {
                EquipmentId.gnodeb => "gNodeB (5G Base Station)",
                EquipmentId.mec => "Mobile Edge Computing (MEC-Core)",
                EquipmentId.spectrum => "RF Spectrum Analyzer",
                _ => "Unknown Equipment"
            };
        }
    }
}
