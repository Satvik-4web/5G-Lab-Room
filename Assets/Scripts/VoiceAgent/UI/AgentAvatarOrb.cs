using System;
using System.Collections;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Futuristic 3D Holographic AI Companion Avatar for Aria.
    /// Features floating gyroscopic orbital rings, glowing core sphere, outer energy field,
    /// dynamic point light, and audio-reactive animations for Idle, Listening, Thinking, and Speaking.
    /// </summary>
    public class AgentAvatarOrb : MonoBehaviour
    {
        [Header("Appearance & Colors")]
        [SerializeField] private Color idleColor = new Color(0f, 0.9f, 1f, 1f);        // Cyan #00E5FF
        [SerializeField] private Color listeningColor = new Color(0f, 1f, 0.5f, 1f);   // Bright Neon Green #00E676
        [SerializeField] private Color thinkingColor = new Color(1f, 0.82f, 0.25f, 1f); // Amber Gold #FFD54F
        [SerializeField] private Color speakingColor = new Color(0.25f, 0.85f, 1f, 1f); // Electric Azure #40C4FF

        [Header("Orb Hierarchy")]
        private GameObject coreSphere;
        private GameObject outerShield;
        private GameObject ringX;
        private GameObject ringY;
        private Light orbLight;

        private Material coreMat;
        private Material shieldMat;
        private Material ringMat;

        private VoiceAgentManager manager;
        private MicrophoneCapture micCapture;
        private AgentSpeaker speaker;
        private AudioSource speakerAudio;

        private float[] audioSamples = new float[256];
        private float smoothedRms = 0f;
        // In local UI space (parent hudRoot scale is 0.0016), baseScale = 38f -> ~6.1 cm in world space
        private float baseScale = 38f;
        private Vector3 initialLocalPos = new Vector3(0, 120f, -10f);

        private Color targetColor;
        private Color currentColor;

        private void Awake()
        {
            transform.localPosition = initialLocalPos;
            BuildHologramHierarchy();
        }

        private void Start()
        {
            manager = VoiceAgentManager.Instance;
            if (manager != null)
            {
                micCapture = manager.GetComponentInChildren<MicrophoneCapture>();
                speaker = manager.GetComponentInChildren<AgentSpeaker>();
                if (speaker != null)
                {
                    speakerAudio = speaker.GetComponent<AudioSource>();
                }
            }

            targetColor = idleColor;
            currentColor = idleColor;
            ApplyColor(currentColor, 1.2f);
        }

        private void BuildHologramHierarchy()
        {
            Shader uLit = Shader.Find("Universal Render Pipeline/Unlit") 
                       ?? Shader.Find("Universal Render Pipeline/Lit") 
                       ?? Shader.Find("Unlit/Color")
                       ?? Shader.Find("Standard");

            // 1. Glowing Core Sphere
            coreSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreSphere.name = "Aria_CoreSphere";
            coreSphere.transform.SetParent(transform, false);
            coreSphere.transform.localScale = Vector3.one * baseScale;
            RemoveCollider(coreSphere);

            coreMat = new Material(uLit);
            coreMat.name = "Aria_CoreMat";
            SetMaterialColor(coreMat, idleColor, 2.5f);
            coreSphere.GetComponent<MeshRenderer>().material = coreMat;

            // 2. Outer Hologram Energy Shield
            outerShield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            outerShield.name = "Aria_OuterShield";
            outerShield.transform.SetParent(transform, false);
            outerShield.transform.localScale = Vector3.one * (baseScale * 1.35f);
            RemoveCollider(outerShield);

            shieldMat = new Material(uLit);
            shieldMat.name = "Aria_ShieldMat";
            SetMaterialTransparent(shieldMat, new Color(idleColor.r, idleColor.g, idleColor.b, 0.25f));
            outerShield.GetComponent<MeshRenderer>().material = shieldMat;

            // 3. Gyroscopic Orbital Ring X
            ringX = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringX.name = "Aria_OrbitalRingX";
            ringX.transform.SetParent(transform, false);
            ringX.transform.localScale = new Vector3(baseScale * 1.85f, 1.2f, baseScale * 1.85f);
            RemoveCollider(ringX);

            ringMat = new Material(uLit);
            ringMat.name = "Aria_RingMat";
            SetMaterialColor(ringMat, idleColor, 1.8f);
            ringX.GetComponent<MeshRenderer>().material = ringMat;

            // 4. Gyroscopic Orbital Ring Y
            ringY = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringY.name = "Aria_OrbitalRingY";
            ringY.transform.SetParent(transform, false);
            ringY.transform.localScale = new Vector3(baseScale * 2.15f, 1.2f, baseScale * 2.15f);
            ringY.transform.localRotation = Quaternion.Euler(45f, 0, 45f);
            RemoveCollider(ringY);
            ringY.GetComponent<MeshRenderer>().material = ringMat;

            // 5. Point Light for environmental glow (in world units)
            var lightGO = new GameObject("Aria_GlowLight");
            lightGO.transform.SetParent(transform, false);
            orbLight = lightGO.AddComponent<Light>();
            orbLight.type = LightType.Point;
            orbLight.range = 0.85f;
            orbLight.intensity = 1.2f;
            orbLight.color = idleColor;
        }

        private void RemoveCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private void SetMaterialColor(Material mat, Color col, float emission)
        {
            if (mat == null) return;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", col * emission);
            }
        }

        private void SetMaterialTransparent(Material mat, Color col)
        {
            if (mat == null) return;
            mat.SetFloat("_Surface", 1); // URP Transparent
            mat.SetFloat("_Blend", 0);   // Alpha blend
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", col * 1.2f);
            }
        }

        private void Update()
        {
            AgentState state = manager != null ? manager.CurrentState : AgentState.Idle;

            // Update Target Color based on Agent State
            switch (state)
            {
                case AgentState.Idle:
                case AgentState.Touring:
                    targetColor = idleColor;
                    break;
                case AgentState.Listening:
                    targetColor = listeningColor;
                    break;
                case AgentState.Transcribing:
                case AgentState.Thinking:
                    targetColor = thinkingColor;
                    break;
                case AgentState.Speaking:
                    targetColor = speakingColor;
                    break;
            }

            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * 6f);

            // Compute Voice & Audio RMS for Reactive Pulsing
            float voiceRms = 0f;
            if (state == AgentState.Listening && micCapture != null && micCapture.IsRecording)
            {
                voiceRms = Mathf.Clamp01(micCapture.CurrentRms * 14f);
            }
            else if (state == AgentState.Speaking && speakerAudio != null && speakerAudio.isPlaying)
            {
                speakerAudio.GetOutputData(audioSamples, 0);
                float sum = 0f;
                for (int i = 0; i < audioSamples.Length; i++)
                {
                    sum += audioSamples[i] * audioSamples[i];
                }
                voiceRms = Mathf.Clamp01(Mathf.Sqrt(sum / audioSamples.Length) * 8f);
            }

            smoothedRms = Mathf.Lerp(smoothedRms, voiceRms, Time.deltaTime * 18f);

            // Animate Core, Rings, Floating Bob & Glow
            AnimateAvatar(state, smoothedRms);
        }

        private void AnimateAvatar(AgentState state, float rms)
        {
            float time = Time.time;

            // 1. Idle / Ambient Breathing Floating Bob
            float bob = Mathf.Sin(time * 2.4f) * 4f; // 4 units bobbing in local space
            transform.localPosition = new Vector3(initialLocalPos.x, initialLocalPos.y + bob, initialLocalPos.z);

            float pulse = Mathf.Sin(time * 3f) * 0.04f;

            // 2. Gyroscopic Ring Rotation
            float ringSpeedMultiplier = 1f;
            if (state == AgentState.Thinking || state == AgentState.Transcribing)
            {
                ringSpeedMultiplier = 5f; // Fast neural spin during computation
            }
            else if (state == AgentState.Speaking)
            {
                ringSpeedMultiplier = 2.4f;
            }

            if (ringX != null)
            {
                ringX.transform.Rotate(Vector3.up, 32f * ringSpeedMultiplier * Time.deltaTime, Space.Self);
                ringX.transform.Rotate(Vector3.right, 22f * ringSpeedMultiplier * Time.deltaTime, Space.Self);
            }

            if (ringY != null)
            {
                ringY.transform.Rotate(Vector3.forward, -42f * ringSpeedMultiplier * Time.deltaTime, Space.Self);
                ringY.transform.Rotate(Vector3.up, -28f * ringSpeedMultiplier * Time.deltaTime, Space.Self);
            }

            // 3. Audio-Reactive Core Pulsing
            float reactiveScale = baseScale * (1f + pulse + rms * 0.40f);
            if (coreSphere != null)
            {
                coreSphere.transform.localScale = Vector3.one * reactiveScale;
            }

            if (outerShield != null)
            {
                float shieldScale = (baseScale * 1.35f) * (1f - pulse * 0.5f + rms * 0.22f);
                outerShield.transform.localScale = Vector3.one * shieldScale;
                outerShield.transform.Rotate(Vector3.up, 12f * Time.deltaTime, Space.Self);
            }

            // 4. Update Emissive Colors & Light Intensity
            float lightPulse = 1.0f + rms * 1.8f;
            ApplyColor(currentColor, lightPulse);

            if (orbLight != null)
            {
                orbLight.color = currentColor;
                orbLight.intensity = Mathf.Lerp(orbLight.intensity, 1.0f + rms * 1.6f, Time.deltaTime * 10f);
            }
        }

        private void ApplyColor(Color col, float intensityMultiplier)
        {
            if (coreMat != null)
            {
                SetMaterialColor(coreMat, col, 2.2f * intensityMultiplier);
            }
            if (shieldMat != null)
            {
                SetMaterialTransparent(shieldMat, new Color(col.r, col.g, col.b, 0.22f));
            }
            if (ringMat != null)
            {
                SetMaterialColor(ringMat, col, 1.8f * intensityMultiplier);
            }
        }
    }
}
