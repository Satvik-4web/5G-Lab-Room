using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Professional Holographic Smart-Display & Highlight Controller for the 5G Use Cases Banner (Poster2).
    /// Features:
    /// - Unobstructed Aisle Pop-Up: Glides to presenter eye-level in the open aisle during Module 2.
    /// - Mathematically Aligned Laser Highlights: Frames the 3 ITU-R / 3GPP pillars (eMBB, mMTC, uRLLC).
    /// - Dynamic Adaptive Reticle: Accurately highlights specific applications (VR, Smart Cities, Remote Surgery, V2X, Specs).
    /// - 100% Clean Presentation: Zero floating 3D text cards; all dialogue is handled cleanly by cinematic video subtitles.
    /// </summary>
    public class UseCasesBannerDisplay : MonoBehaviour
    {
        public static UseCasesBannerDisplay Instance { get; private set; }

        public static UseCasesBannerDisplay EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = GameObject.Find("Poster2");
            if (go != null)
            {
                var comp = go.GetComponent<UseCasesBannerDisplay>();
                if (comp == null) comp = go.AddComponent<UseCasesBannerDisplay>();
                return comp;
            }
            return null;
        }

        [Header("Banner Presentation Transform")]
        private Vector3 wallRestPosition;
        private Quaternion wallRestRotation;
        private Vector3 poppedPosition;
        private Quaternion poppedRotation;

        [SerializeField] private Vector3 bannerScale = new Vector3(3.56f, 2.00f, 1.0f); // 16:9 ratio
        [SerializeField] private float popDuration = 0.85f;

        [Header("Cybernetic Color Palette")]
        private readonly Color colEMBB = new Color(0.0f, 0.90f, 1.0f, 1.0f);     // Electric Cyan (High Speed / Bandwidth)
        private readonly Color colMMTC = new Color(0.1f, 1.00f, 0.4f, 1.0f);     // Emerald Green (Massive IoT / Sensors)
        private readonly Color colURLLC = new Color(1.0f, 0.25f, 0.25f, 1.0f);   // Vivid Coral Red (Ultra-Reliable Mission Critical)
        private readonly Color colFrame = new Color(0.35f, 0.75f, 1.0f, 0.85f);  // Outer Border Frame
        private readonly Color colReticle = new Color(1.0f, 0.85f, 0.2f, 1.0f);  // Focus Target Gold

        // Section Outlines
        private LineRenderer lineFrame;
        private LineRenderer lineEMBB;
        private LineRenderer lineMMTC;
        private LineRenderer lineURLLC;

        // Dynamic Element Focus Reticle
        private GameObject targetReticleGO;
        private LineRenderer targetReticleLine;
        private Vector4 currentReticleBounds = Vector4.zero;
        private Vector4 targetReticleBounds = Vector4.zero;
        private bool isReticleActive = false;

        private Coroutine popRoutine;
        private Coroutine pulseRoutine;
        private string activeSection = "none";
        private bool isPopped = false;

        public bool IsPopped => isPopped;

        // Local Quad coordinates mapping: Vector4(xMin, xMax, yMin, yMax)
        private readonly Dictionary<string, Vector4> elementBounds = new Dictionary<string, Vector4>
        {
            // 1. eMBB Tier & Sub-elements (Top Row)
            { "embb_all",       new Vector4(-0.4896f, 0.4896f,  0.0648f, 0.3102f) },
            { "embb_label",     new Vector4(-0.4857f, -0.3659f, 0.0741f, 0.3009f) },
            { "embb_vr",        new Vector4(-0.3646f, -0.1354f, 0.2454f, 0.2940f) },
            { "embb_video",     new Vector4(-0.1354f,  0.0990f, 0.2454f, 0.2940f) },
            { "embb_fwa",       new Vector4( 0.0990f,  0.3021f, 0.2454f, 0.2940f) },
            { "embb_specs",     new Vector4( 0.3021f,  0.4857f, 0.0741f, 0.3009f) },

            // 2. mMTC Tier & Sub-elements (Middle Row)
            { "mmtc_all",       new Vector4(-0.4896f, 0.4896f, -0.1898f, 0.0556f) },
            { "mmtc_label",     new Vector4(-0.4857f, -0.3659f, -0.1806f, 0.0463f) },
            { "mmtc_wearables", new Vector4(-0.3646f, -0.1354f, -0.0093f, 0.0394f) },
            { "mmtc_smartcities", new Vector4(0.0990f,  0.3021f, -0.0093f, 0.0394f) },
            { "mmtc_health",    new Vector4(-0.3646f, -0.1354f, -0.0741f, -0.0150f) },
            { "mmtc_industrial", new Vector4(0.0990f,  0.3021f, -0.0741f, -0.0150f) },
            { "mmtc_specs",     new Vector4( 0.3021f,  0.4857f, -0.1806f, 0.0463f) },

            // 3. uRLLC Tier & Sub-elements (Bottom Row)
            { "urllc_all",      new Vector4(-0.4896f, 0.4896f, -0.4444f, -0.1991f) },
            { "urllc_label",    new Vector4(-0.4857f, -0.3659f, -0.4352f, -0.2083f) },
            { "urllc_safety",   new Vector4(-0.3646f, -0.1354f, -0.2639f, -0.2153f) },
            { "urllc_surgery",  new Vector4(-0.1354f,  0.0990f, -0.2639f, -0.2153f) },
            { "urllc_v2x",      new Vector4(-0.3646f,  0.0990f, -0.3241f, -0.2755f) },
            { "urllc_robotics", new Vector4(-0.1354f,  0.0990f, -0.3241f, -0.2755f) },
            { "urllc_specs",    new Vector4( 0.3021f,  0.4857f, -0.4352f, -0.2083f) }
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;

            wallRestPosition = transform.position;
            wallRestRotation = transform.rotation;

            if (wallRestRotation == Quaternion.identity || Mathf.Abs(wallRestRotation.y) < 0.1f)
            {
                wallRestRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // Popped presentation transform: open aisle, eye-level, Z=3.10f
            poppedPosition = new Vector3(4.80f, 1.95f, 3.10f);
            poppedRotation = Quaternion.Euler(0f, 90f, 0f);

            transform.localScale = bannerScale;
            transform.position = wallRestPosition;
            transform.rotation = wallRestRotation;

            EnsureInteractionComponents();
            BuildHighlightOutlines();
            BuildTargetReticle();
        }

        private void Start()
        {
            SetHighlightSection("none");
            HideTargetElement();
        }

        private void Update()
        {
            if (isReticleActive && targetReticleLine != null)
            {
                currentReticleBounds = Vector4.Lerp(currentReticleBounds, targetReticleBounds, Time.deltaTime * 12f);
                UpdateRectLine(targetReticleLine, currentReticleBounds.x, currentReticleBounds.y, currentReticleBounds.z, currentReticleBounds.w, 0.022f);
            }
        }

        private void EnsureInteractionComponents()
        {
            var label = GetComponent<InteractableLabel>();
            if (label == null)
            {
                label = gameObject.AddComponent<InteractableLabel>();
                label.objectName = "5G USE CASES (eMBB, mMTC, uRLLC)";
                label.objectDescription = "<color=#8AB4F8>• eMBB: 20 Gbps Peak, 4ms Latency, 8K/VR Streaming\n• mMTC: 1 Million Devices/km², 10+ Year Battery, Smart Cities\n• uRLLC: < 1ms Latency, 99.999% Reliability, Remote Surgery & Autonomous Vehicles</color>";
            }

            var tester = GetComponent<PCInteractionTester>();
            if (tester == null)
            {
                tester = gameObject.AddComponent<PCInteractionTester>();
                tester.distanceInFront = 2.8f;
            }
        }

        private void BuildHighlightOutlines()
        {
            lineFrame = CreateOutlineLine("Outline_Frame", colFrame, 0.018f, 0.016f);
            lineEMBB = CreateOutlineLine("Outline_eMBB", colEMBB, 0.020f, 0.018f);
            lineMMTC = CreateOutlineLine("Outline_mMTC", colMMTC, 0.020f, 0.018f);
            lineURLLC = CreateOutlineLine("Outline_uRLLC", colURLLC, 0.020f, 0.018f);

            // Frame border
            UpdateRectLine(lineFrame, -0.490f, 0.490f, -0.485f, 0.485f, 0.012f);

            // 3 Tier Outlines
            UpdateRectLine(lineEMBB, -0.4896f, 0.4896f, 0.0648f, 0.3102f, 0.015f);
            UpdateRectLine(lineMMTC, -0.4896f, 0.4896f, -0.1898f, 0.0556f, 0.015f);
            UpdateRectLine(lineURLLC, -0.4896f, 0.4896f, -0.4444f, -0.1991f, 0.015f);

            lineFrame.enabled = false;
            lineEMBB.enabled = false;
            lineMMTC.enabled = false;
            lineURLLC.enabled = false;
        }

        private void BuildTargetReticle()
        {
            targetReticleGO = new GameObject("Target_Reticle_UseCases");
            targetReticleGO.transform.SetParent(transform, false);

            targetReticleLine = targetReticleGO.AddComponent<LineRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
            mat.color = colReticle;
            targetReticleLine.material = mat;
            targetReticleLine.useWorldSpace = false;
            targetReticleLine.loop = true;
            targetReticleLine.positionCount = 4;
            targetReticleLine.startWidth = 0.022f;
            targetReticleLine.endWidth = 0.022f;
            targetReticleLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            targetReticleLine.receiveShadows = false;
            targetReticleLine.enabled = false;
        }

        private LineRenderer CreateOutlineLine(string name, Color color, float startW, float zOffset)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
            mat.color = color;
            lr.material = mat;
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 4;
            lr.startWidth = startW;
            lr.endWidth = startW;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        private void UpdateRectLine(LineRenderer lr, float xMin, float xMax, float yMin, float yMax, float zOffset)
        {
            if (lr == null) return;
            // Local Quad face points in +Z or -Z depending on mesh. For Quad, -Z is facing front when flipped, or +Z.
            // In CupsBannerDisplay, zOffset is -0.015f towards the viewer.
            float z = -Mathf.Abs(zOffset);
            lr.SetPosition(0, new Vector3(xMin, yMin, z));
            lr.SetPosition(1, new Vector3(xMin, yMax, z));
            lr.SetPosition(2, new Vector3(xMax, yMax, z));
            lr.SetPosition(3, new Vector3(xMax, yMin, z));
        }

        public void SetHighlightSection(string section)
        {
            activeSection = section.ToLowerInvariant();

            if (lineFrame != null) lineFrame.enabled = (activeSection != "none");
            if (lineEMBB != null) lineEMBB.enabled = (activeSection == "all" || activeSection == "embb");
            if (lineMMTC != null) lineMMTC.enabled = (activeSection == "all" || activeSection == "mmtc");
            if (lineURLLC != null) lineURLLC.enabled = (activeSection == "all" || activeSection == "urllc");

            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            if (activeSection != "none")
            {
                pulseRoutine = StartCoroutine(AnimateOutlinePulse());
            }
        }

        public void SetTargetElement(string elementName)
        {
            if (string.IsNullOrEmpty(elementName))
            {
                HideTargetElement();
                return;
            }

            string key = elementName.ToLowerInvariant();
            if (elementBounds.TryGetValue(key, out Vector4 bounds))
            {
                targetReticleBounds = bounds;
                if (!isReticleActive)
                {
                    currentReticleBounds = bounds;
                    isReticleActive = true;
                    if (targetReticleLine != null) targetReticleLine.enabled = true;
                }
            }
        }

        public void HideTargetElement()
        {
            isReticleActive = false;
            if (targetReticleLine != null) targetReticleLine.enabled = false;
        }

        private IEnumerator AnimateOutlinePulse()
        {
            while (activeSection != "none")
            {
                float t = (Mathf.Sin(Time.time * 4.5f) + 1f) * 0.5f;
                float widthMultiplier = Mathf.Lerp(0.85f, 1.45f, t);

                if (lineFrame != null && lineFrame.enabled) lineFrame.widthMultiplier = 1.0f;
                if (lineEMBB != null && lineEMBB.enabled) lineEMBB.widthMultiplier = (activeSection == "embb") ? widthMultiplier : 0.8f;
                if (lineMMTC != null && lineMMTC.enabled) lineMMTC.widthMultiplier = (activeSection == "mmtc") ? widthMultiplier : 0.8f;
                if (lineURLLC != null && lineURLLC.enabled) lineURLLC.widthMultiplier = (activeSection == "urllc") ? widthMultiplier : 0.8f;

                if (targetReticleLine != null && targetReticleLine.enabled)
                {
                    targetReticleLine.widthMultiplier = Mathf.Lerp(1.0f, 1.5f, t);
                }

                yield return null;
            }
        }

        public void SetPopUpState(bool pop)
        {
            if (isPopped == pop) return;
            isPopped = pop;

            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(AnimatePopUp(pop));
        }

        private IEnumerator AnimatePopUp(bool pop)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Vector3 targetPos = pop ? poppedPosition : wallRestPosition;
            Quaternion targetRot = pop ? poppedRotation : wallRestRotation;

            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                transform.localScale = bannerScale;
                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;
            transform.localScale = bannerScale;
            popRoutine = null;
        }
    }
}
