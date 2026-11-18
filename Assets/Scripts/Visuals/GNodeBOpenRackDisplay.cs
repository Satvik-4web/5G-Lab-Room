using System.Collections.Generic;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Builds a compact, open-front 5G radio cabinet for the module 5 cutaway view.
    /// It is deliberately made from ordinary meshes so the internal DU, CU, RU and
    /// power units remain visible without extra overlays or imported assets.
    /// </summary>
    public sealed class GNodeBOpenRackDisplay : MonoBehaviour
    {
        private static GNodeBOpenRackDisplay instance;
        private readonly List<Material> ownedMaterials = new List<Material>();
        private Material cabinetMaterial;
        private Material edgeMaterial;
        private Material equipmentMaterial;
        private Material panelMaterial;
        private Material ventMaterial;
        private Material accentMaterial;
        private Material greenLedMaterial;
        private Material cyanLedMaterial;
        private Material amberLedMaterial;

        public static GNodeBOpenRackDisplay EnsureInstance()
        {
            if (instance != null) return instance;

            var existing = GameObject.Find("gNodeB_OpenRack_Display");
            if (existing != null)
            {
                instance = existing.GetComponent<GNodeBOpenRackDisplay>();
                if (instance != null) return instance;
            }

            var root = new GameObject("gNodeB_OpenRack_Display");
            instance = root.AddComponent<GNodeBOpenRackDisplay>();
            instance.BuildCabinet();
            return instance;
        }

        private void BuildCabinet()
        {
            transform.SetPositionAndRotation(new Vector3(4.15f, 0f, -6.55f), Quaternion.Euler(0f, 6f, 0f));

            cabinetMaterial = MakeMaterial(new Color(0.055f, 0.075f, 0.095f), 0.72f, 0.24f);
            edgeMaterial = MakeMaterial(new Color(0.16f, 0.21f, 0.25f), 0.62f, 0.28f);
            equipmentMaterial = MakeMaterial(new Color(0.10f, 0.14f, 0.18f), 0.42f, 0.30f);
            panelMaterial = MakeMaterial(new Color(0.025f, 0.035f, 0.048f), 0.15f, 0.28f);
            ventMaterial = MakeMaterial(new Color(0.012f, 0.018f, 0.024f), 0.05f, 0.18f);
            accentMaterial = MakeMaterial(new Color(0.05f, 0.62f, 0.76f), 0.25f, 0.30f, true);
            greenLedMaterial = MakeMaterial(new Color(0.12f, 0.82f, 0.48f), 0.05f, 0.1f, true);
            cyanLedMaterial = MakeMaterial(new Color(0.12f, 0.72f, 0.96f), 0.05f, 0.1f, true);
            amberLedMaterial = MakeMaterial(new Color(0.98f, 0.56f, 0.13f), 0.05f, 0.1f, true);

            // Feet, plinth, side walls and rear shell leave the front open like a real rack.
            Cube("Foot_L", new Vector3(-0.34f, 0.035f, -0.20f), new Vector3(0.12f, 0.07f, 0.30f), cabinetMaterial);
            Cube("Foot_R", new Vector3(0.34f, 0.035f, -0.20f), new Vector3(0.12f, 0.07f, 0.30f), cabinetMaterial);
            Cube("Base_Plinth", new Vector3(0f, 0.12f, -0.02f), new Vector3(0.94f, 0.16f, 0.70f), edgeMaterial);
            Cube("Rear_Panel", new Vector3(0f, 1.08f, -0.31f), new Vector3(0.82f, 1.78f, 0.055f), cabinetMaterial);
            Cube("Left_Side", new Vector3(-0.435f, 1.08f, -0.01f), new Vector3(0.07f, 1.78f, 0.66f), cabinetMaterial);
            Cube("Right_Side", new Vector3(0.435f, 1.08f, -0.01f), new Vector3(0.07f, 1.78f, 0.66f), cabinetMaterial);
            Cube("Top_Cap", new Vector3(0f, 2.02f, -0.01f), new Vector3(0.96f, 0.12f, 0.72f), edgeMaterial);
            Cube("Front_Bottom_Rail", new Vector3(0f, 0.22f, 0.335f), new Vector3(0.84f, 0.055f, 0.055f), edgeMaterial);
            Cube("Front_Top_Rail", new Vector3(0f, 1.94f, 0.335f), new Vector3(0.84f, 0.055f, 0.055f), edgeMaterial);
            Cube("Front_Left_Post", new Vector3(-0.40f, 1.08f, 0.335f), new Vector3(0.055f, 1.70f, 0.065f), edgeMaterial);
            Cube("Front_Right_Post", new Vector3(0.40f, 1.08f, 0.335f), new Vector3(0.055f, 1.70f, 0.065f), edgeMaterial);

            // Rack mounting rails with repeated square-hole detail.
            Cube("Mount_Rail_L", new Vector3(-0.335f, 1.08f, 0.285f), new Vector3(0.025f, 1.55f, 0.035f), panelMaterial);
            Cube("Mount_Rail_R", new Vector3(0.335f, 1.08f, 0.285f), new Vector3(0.025f, 1.55f, 0.035f), panelMaterial);
            for (int i = 0; i < 14; i++)
            {
                float y = 0.37f + i * 0.105f;
                Cube($"RailHole_L_{i}", new Vector3(-0.335f, y, 0.307f), new Vector3(0.012f, 0.018f, 0.006f), ventMaterial);
                Cube($"RailHole_R_{i}", new Vector3(0.335f, y, 0.307f), new Vector3(0.012f, 0.018f, 0.006f), ventMaterial);
            }

            // Five distinct radio/baseband units: exposed faceplates, vents and live indicators.
            string[] units = { "POWER", "RADIO UNIT", "DISTRIBUTED UNIT", "CENTRAL UNIT", "SYNC + FIBER" };
            float[] heights = { 0.42f, 0.72f, 1.02f, 1.32f, 1.62f };
            for (int i = 0; i < units.Length; i++)
            {
                float y = heights[i];
                Cube($"{units[i]}_Chassis", new Vector3(0f, y, 0.02f), new Vector3(0.62f, 0.25f, 0.49f), equipmentMaterial);
                Cube($"{units[i]}_Faceplate", new Vector3(0f, y, 0.278f), new Vector3(0.62f, 0.22f, 0.035f), panelMaterial);
                Cube($"{units[i]}_StatusStrip", new Vector3(-0.245f, y + 0.071f, 0.301f), new Vector3(0.06f, 0.018f, 0.006f), accentMaterial);

                for (int vent = 0; vent < 5; vent++)
                {
                    Cube($"{units[i]}_Vent_{vent}", new Vector3(-0.13f + vent * 0.048f, y - 0.055f, 0.301f),
                        new Vector3(0.032f, 0.008f, 0.006f), ventMaterial);
                }

                Cube($"{units[i]}_Led_G", new Vector3(0.205f, y + 0.05f, 0.303f), new Vector3(0.026f, 0.026f, 0.012f), greenLedMaterial);
                Cube($"{units[i]}_Led_C", new Vector3(0.255f, y + 0.05f, 0.303f), new Vector3(0.026f, 0.026f, 0.012f), cyanLedMaterial);
                Cube($"{units[i]}_Led_A", new Vector3(0.205f, y - 0.005f, 0.303f), new Vector3(0.026f, 0.026f, 0.012f), amberLedMaterial);
            }

            // Side cooling slots and a fiber/power harness visible through the open frame.
            for (int i = 0; i < 9; i++)
            {
                float y = 0.48f + i * 0.15f;
                Cube($"Cooling_Slot_L_{i}", new Vector3(-0.472f, y, 0.05f), new Vector3(0.012f, 0.035f, 0.36f), ventMaterial);
                Cube($"Cooling_Slot_R_{i}", new Vector3(0.472f, y, 0.05f), new Vector3(0.012f, 0.035f, 0.36f), ventMaterial);
            }
            Cable("Fiber_Harness", new Vector3(-0.22f, 0.35f, -0.19f), new Vector3(-0.22f, 1.78f, -0.19f), 0.018f, accentMaterial);
            Cable("Power_Harness", new Vector3(0.23f, 0.35f, -0.20f), new Vector3(0.23f, 1.78f, -0.20f), 0.022f, amberLedMaterial);

            // Subtle cabinet identification strip, physically attached to the top rail.
            Cube("Identity_Plate", new Vector3(0f, 1.91f, 0.373f), new Vector3(0.29f, 0.035f, 0.012f), accentMaterial);
            var nameplate = new GameObject("gNodeB_Nameplate", typeof(TextMesh));
            nameplate.transform.SetParent(transform, false);
            nameplate.transform.localPosition = new Vector3(0f, 1.91f, 0.382f);
            var nameText = nameplate.GetComponent<TextMesh>();
            nameText.text = "gNB  /  5G NR";
            nameText.anchor = TextAnchor.MiddleCenter;
            nameText.alignment = TextAlignment.Center;
            nameText.fontSize = 48;
            nameText.characterSize = 0.003f;
            nameText.color = new Color(0.88f, 0.96f, 1f);
            var hitbox = gameObject.AddComponent<BoxCollider>();
            hitbox.center = new Vector3(0f, 1.05f, 0f);
            hitbox.size = new Vector3(1.05f, 2.15f, 0.82f);
        }

        private Material MakeMaterial(Color color, float metallic, float smoothness, bool emissive = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 1.5f);
            }
            ownedMaterials.Add(material);
            return material;
        }

        private void Cube(string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            var collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private void Cable(string name, Vector3 start, Vector3 end, float radius, Material material)
        {
            Vector3 delta = end - start;
            var cable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cable.name = name;
            cable.transform.SetParent(transform, false);
            cable.transform.localPosition = (start + end) * 0.5f;
            cable.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            cable.transform.localScale = new Vector3(radius * 2f, delta.magnitude * 0.5f, radius * 2f);
            var collider = cable.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            cable.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            foreach (var material in ownedMaterials)
            {
                if (material != null) Destroy(material);
            }
        }
    }
}
