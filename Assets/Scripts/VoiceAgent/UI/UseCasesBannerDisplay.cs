using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Professional Holographic Smart-Display & Highlight Controller for the authentic 5G Use Cases Banner (Poster2).
    /// Features:
    /// - Operates directly on the authentic university poster (2..png) on the wall at X=7.44m, Z=3.10m.
    /// - Mathematically Aligned Laser Highlights: Outlines the 3 authentic ITU-R / 3GPP pillars (eMBB, mMTC, uRLLC).
    /// - Dynamic Adaptive Reticle: Accurately highlights specific applications and specs in the authentic diagram.
    /// - 100% Clean Presentation: Zero floating 3D text cards; all dialogue is handled cleanly by cinematic video subtitles.
    /// </summary>
    public class UseCasesBannerDisplay : MonoBehaviour
    {
        private static UseCasesBannerDisplay _instance;
        public static UseCasesBannerDisplay Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<UseCasesBannerDisplay>();
                return _instance != null ? _instance : null;
            }
            private set => _instance = value;
        }

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
        private Vector3 wallRestPosition = new Vector3(7.44f, 2.29f, 3.10f);
        private Quaternion wallRestRotation = Quaternion.Euler(0f, 90f, 0f);
        private Vector3 poppedPosition = new Vector3(4.80f, 2.15f, 3.10f); // Popped 2.64m forward into open aisle
        private Quaternion poppedRotation = Quaternion.Euler(0f, 90f, 0f);

        [SerializeField] private Vector3 bannerScale = new Vector3(8.0f, 5.0f, 1.0f); // Authentic wall scale
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

        // Local Quad coordinates aligned to the authentic 2..png diagram: Vector4(xMin, xMax, yMin, yMax)
        private readonly Dictionary<string, Vector4> elementBounds = new Dictionary<string, Vector4>
        {
            // 1. eMBB Tier & Sub-elements (Top Row)
            { "embb_all",         new Vector4(-0.1214f,  0.1276f, -0.0074f,  0.0463f) },
            { "embb_label",       new Vector4(-0.1214f, -0.0750f, -0.0074f,  0.0463f) },
            { "embb_vr",          new Vector4(-0.0750f,  0.0550f, -0.0056f,  0.0444f) },
            { "embb_specs",       new Vector4( 0.0550f,  0.1276f, -0.0056f,  0.0444f) },

            // 2. mMTC Tier & Sub-elements (Middle Row)
            { "mmtc_all",         new Vector4(-0.1214f,  0.1276f, -0.0648f, -0.0111f) },
            { "mmtc_label",       new Vector4(-0.1214f, -0.0750f, -0.0648f, -0.0111f) },
            { "mmtc_smartcities", new Vector4(-0.0750f,  0.0550f, -0.0630f, -0.0130f) },
            { "mmtc_specs",       new Vector4( 0.0550f,  0.1276f, -0.0630f, -0.0130f) },

            // 3. uRLLC Tier & Sub-elements (Bottom Row)
            { "urllc_all",        new Vector4(-0.1214f,  0.1276f, -0.1343f, -0.0685f) },
            { "urllc_label",      new Vector4(-0.1214f, -0.0750f, -0.1343f, -0.0685f) },
            { "urllc_surgery",    new Vector4(-0.0750f,  0.0550f, -0.1324f, -0.0704f) },
            { "urllc_specs",      new Vector4( 0.0550f,  0.1276f, -0.1324f, -0.0704f) }
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

            if (transform.localScale != Vector3.zero)
            {
                bannerScale = transform.localScale;
            }

            // Popped presentation transform: open aisle, eye-level (X=4.80m, matching Poster1 pop distance)
            poppedPosition = new Vector3(4.80f, 2.15f, wallRestPosition.z);
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
                UpdateRectLine(targetReticleLine, currentReticleBounds.x, currentReticleBounds.y, currentReticleBounds.z, currentReticleBounds.w, 0.018f);
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
            lineFrame = CreateOutlineLine("Outline_Frame", colFrame, 0.012f);
            lineEMBB = CreateOutlineLine("Outline_eMBB", colEMBB, 0.014f);
            lineMMTC = CreateOutlineLine("Outline_mMTC", colMMTC, 0.014f);
            lineURLLC = CreateOutlineLine("Outline_uRLLC", colURLLC, 0.014f);

            // Frame border around authentic poster diagram
            UpdateRectLine(lineFrame, -0.1224f, 0.1281f, -0.1370f, 0.1435f, 0.012f);

            // 3 Tier Outlines matching authentic poster
            UpdateRectLine(lineEMBB, -0.1214f, 0.1276f, -0.0074f, 0.0463f, 0.015f);
            UpdateRectLine(lineMMTC, -0.1214f, 0.1276f, -0.0648f, -0.0111f, 0.015f);
            UpdateRectLine(lineURLLC, -0.1214f, 0.1276f, -0.1343f, -0.0685f, 0.015f);

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
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            Material mat = new Material(shader);
            mat.SetColor("_BaseColor", colReticle);
            mat.color = colReticle;
            targetReticleLine.material = mat;
            targetReticleLine.useWorldSpace = false;
            targetReticleLine.loop = true;
            targetReticleLine.positionCount = 4;
            targetReticleLine.startWidth = 0.016f;
            targetReticleLine.endWidth = 0.016f;
            targetReticleLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            targetReticleLine.receiveShadows = false;
            targetReticleLine.enabled = false;
        }

        private LineRenderer CreateOutlineLine(string name, Color color, float startW)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            Material mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
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

        public void SetPopUpState(bool pop, bool instant = false)
        {
            isPopped = pop;

            if (popRoutine != null)
            {
                StopCoroutine(popRoutine);
                popRoutine = null;
            }

            if (instant)
            {
                transform.position = pop ? poppedPosition : wallRestPosition;
                transform.rotation = pop ? poppedRotation : wallRestRotation;
                transform.localScale = bannerScale;
            }
            else
            {
                popRoutine = StartCoroutine(AnimatePopUp(pop));
            }
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

        private void OnDestroy()
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            if (_instance == this) _instance = null;
        }
    }
}
