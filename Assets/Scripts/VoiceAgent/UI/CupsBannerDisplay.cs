using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Professional Holographic Smart-Display & Architecture Callout System for the 5G CUPS Banner (Poster1).
    /// Features:
    /// - 100% Unobstructed Aisle Float: Glides to eye-level in the open aisle during Module 1.
    /// - Precise Element Highlighting: Outlines and adaptive target frames that fit the exact items on the authentic poster.
    /// - Floating Holographic Callouts: World-space TextMeshPro cards labeling the actual UE icons, Access Network boxes, Control Plane functions, and User Plane UPFs.
    /// - Zero Distortion: Maintains authentic 3:2 university poster aspect ratio with high-visibility backlighting.
    /// </summary>
    public class CupsBannerDisplay : MonoBehaviour
    {
        public static CupsBannerDisplay Instance { get; private set; }

        [Header("Banner Presentation Transform")]
        private Vector3 wallRestPosition;
        private Quaternion wallRestRotation;
        private Vector3 poppedPosition;
        private Quaternion poppedRotation;

        [SerializeField] private Vector3 bannerScale = new Vector3(3.30f, 2.20f, 1.0f);
        [SerializeField] private float popDuration = 0.85f;

        [Header("Cybernetic Color Palette")]
        private readonly Color colCyan = new Color(0f, 0.95f, 1f, 1f);        // User Equipment & Access Networks
        private readonly Color colBlue = new Color(0.2f, 0.65f, 1f, 1f);      // Access Network Radios
        private readonly Color colGold = new Color(1f, 0.85f, 0.2f, 1f);      // 5G Control Plane (SBA)
        private readonly Color colGreen = new Color(0.1f, 1f, 0.45f, 1f);     // 5G User Plane & CUPS UPF
        private readonly Color colFrame = new Color(0.35f, 0.75f, 1f, 0.85f); // Border Frame
        private readonly Color colReticle = new Color(1f, 0.25f, 0.85f, 1f);  // Focus Target Bracket

        // Section Outlines
        private LineRenderer lineFrame;
        private LineRenderer lineUE;
        private LineRenderer lineAccess;
        private LineRenderer lineControl;
        private LineRenderer lineUserPlane;

        // Dynamic Element Focus Reticle
        private GameObject targetReticleGO;
        private LineRenderer targetReticleLine;
        private Vector4 currentReticleBounds = Vector4.zero; // (x0, x1, y0, y1)
        private Vector4 targetReticleBounds = Vector4.zero;
        private bool isReticleActive = false;

        // Holographic Callout Badge
        private GameObject calloutRootGO;
        private GameObject calloutBgQuad;
        private TextMeshPro calloutTitleTMP;
        private TextMeshPro calloutBodyTMP;
        private Vector3 targetCalloutPos = Vector3.zero;
        private bool isCalloutActive = false;

        private Coroutine popRoutine;
        private Coroutine pulseRoutine;
        private string activeSection = "none";
        private bool isPopped = false;

        public bool IsPopped => isPopped;

        // Coordinate dictionary mapping authentic banner elements to local quad bounds: (x0, x1, y0, y1)
        private readonly Dictionary<string, Vector4> elementBounds = new Dictionary<string, Vector4>
        {
            // 1. User Equipment (UE) Column & Icons
            { "ue_all",      new Vector4(-0.495f, -0.422f, -0.450f,  0.085f) },
            { "ue_drone",    new Vector4(-0.495f, -0.425f, -0.055f,  0.035f) },
            { "ue_car",      new Vector4(-0.495f, -0.425f, -0.180f, -0.090f) },
            { "ue_phone",    new Vector4(-0.495f, -0.425f, -0.300f, -0.210f) },
            { "ue_pc",       new Vector4(-0.495f, -0.422f, -0.440f, -0.335f) },

            // 2. Heterogeneous Access Network Column & Radios
            { "an_all",      new Vector4(-0.360f, -0.158f, -0.450f,  0.085f) },
            { "an_5g",       new Vector4(-0.360f, -0.160f, -0.070f,  0.030f) },
            { "an_4g",       new Vector4(-0.360f, -0.160f, -0.190f, -0.090f) },
            { "an_wifi",     new Vector4(-0.360f, -0.160f, -0.315f, -0.215f) },
            { "an_fixed",    new Vector4(-0.360f, -0.160f, -0.440f, -0.335f) },

            // 3. 5G Control Plane SBA Functions
            { "cp_all",      new Vector4(-0.125f,  0.445f, -0.055f,  0.230f) },
            { "cp_amf_smf",  new Vector4( 0.020f,  0.250f, -0.010f,  0.075f) },
            { "cp_udm_ausf", new Vector4(-0.040f,  0.265f,  0.115f,  0.200f) },

            // 4. 5G User Plane & Distributed UPFs
            { "up_all",      new Vector4(-0.035f,  0.445f, -0.455f, -0.075f) },
            { "up_upfs",     new Vector4(-0.030f,  0.330f, -0.330f, -0.095f) },
            { "up_localdn",  new Vector4( 0.005f,  0.170f, -0.485f, -0.370f) },
            { "up_centraldn",new Vector4( 0.360f,  0.450f, -0.260f, -0.110f) }
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;

            wallRestPosition = transform.position;
            wallRestRotation = transform.rotation;

            // Quad normal must face into the room (-X direction, Y=90)
            if (wallRestRotation == Quaternion.identity || Mathf.Abs(wallRestRotation.y) < 0.1f)
            {
                wallRestRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // Popped presentation transform: open aisle, eye-level, zero tilt
            poppedPosition = new Vector3(4.80f, 1.95f, -1.80f);
            poppedRotation = Quaternion.Euler(0f, 90f, 0f);

            transform.localScale = bannerScale;
            transform.position = wallRestPosition;
            transform.rotation = wallRestRotation;

            BuildHighlightOutlines();
            BuildTargetReticle();
            BuildCalloutBadge();
        }

        private void Start()
        {
            SetHighlightSection("none");
            HideTargetElement();
            HideCallout();
        }

        private void Update()
        {
            // Smoothly lerp target reticle bounds for slick adaptive framing
            if (isReticleActive && targetReticleLine != null)
            {
                currentReticleBounds = Vector4.Lerp(currentReticleBounds, targetReticleBounds, Time.deltaTime * 9f);
                UpdateReticleGeometry(currentReticleBounds.x, currentReticleBounds.y, currentReticleBounds.z, currentReticleBounds.w);
            }

            // Smooth callout position floating
            if (isCalloutActive && calloutRootGO != null)
            {
                calloutRootGO.transform.localPosition = Vector3.Lerp(
                    calloutRootGO.transform.localPosition,
                    targetCalloutPos,
                    Time.deltaTime * 8f
                );
            }
        }

        private void BuildHighlightOutlines()
        {
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            // 1. Overall Banner Outer Perimeter Frame
            lineFrame = CreateOutlineLine("Outline_Frame",
                -0.495f, 0.495f, -0.495f, 0.495f, -0.012f,
                colFrame, 0.012f, unlitShader);

            // 2. Zone 1A: User Equipment (Far Left Column)
            lineUE = CreateOutlineLine("Outline_UE_Column",
                -0.498f, -0.418f, -0.455f, 0.090f, -0.018f,
                colCyan, 0.014f, unlitShader);

            // 3. Zone 1B: Heterogeneous Access Networks (Middle Left Column)
            lineAccess = CreateOutlineLine("Outline_Access_Column",
                -0.365f, -0.152f, -0.455f, 0.090f, -0.018f,
                colBlue, 0.014f, unlitShader);

            // 4. Zone 2: 5G Control Plane SBA (Top Right Box)
            lineControl = CreateOutlineLine("Outline_ControlPlane_SBA",
                -0.128f, 0.450f, -0.058f, 0.235f, -0.018f,
                colGold, 0.015f, unlitShader);

            // 5. Zone 3: 5G User Plane & Distributed UPFs (Bottom Right Box)
            lineUserPlane = CreateOutlineLine("Outline_UserPlane_CUPS",
                -0.038f, 0.450f, -0.460f, -0.070f, -0.018f,
                colGreen, 0.015f, unlitShader);
        }

        private LineRenderer CreateOutlineLine(string name, float x0, float x1, float y0, float y1, float z, Color color, float width, Shader shader)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 5;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.loop = true;

            lr.SetPosition(0, new Vector3(x0, y0, z));
            lr.SetPosition(1, new Vector3(x1, y0, z));
            lr.SetPosition(2, new Vector3(x1, y1, z));
            lr.SetPosition(3, new Vector3(x0, y1, z));
            lr.SetPosition(4, new Vector3(x0, y0, z));

            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            lr.sharedMaterial = mat;
            lr.enabled = false;

            return lr;
        }

        private void BuildTargetReticle()
        {
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            targetReticleGO = new GameObject("TargetReticle");
            targetReticleGO.transform.SetParent(transform, false);
            targetReticleGO.transform.localPosition = Vector3.zero;
            targetReticleGO.transform.localRotation = Quaternion.identity;
            targetReticleGO.transform.localScale = Vector3.one;

            targetReticleLine = targetReticleGO.AddComponent<LineRenderer>();
            targetReticleLine.useWorldSpace = false;
            targetReticleLine.loop = true;
            targetReticleLine.positionCount = 5;
            targetReticleLine.startWidth = 0.016f;
            targetReticleLine.endWidth = 0.016f;

            var mat = new Material(unlitShader);
            mat.SetColor("_BaseColor", colReticle);
            targetReticleLine.sharedMaterial = mat;

            targetReticleGO.SetActive(false);
        }

        private void UpdateReticleGeometry(float x0, float x1, float y0, float y1)
        {
            if (targetReticleLine == null) return;
            float z = -0.024f; // slightly in front of banner surface
            targetReticleLine.SetPosition(0, new Vector3(x0, y0, z));
            targetReticleLine.SetPosition(1, new Vector3(x1, y0, z));
            targetReticleLine.SetPosition(2, new Vector3(x1, y1, z));
            targetReticleLine.SetPosition(3, new Vector3(x0, y1, z));
            targetReticleLine.SetPosition(4, new Vector3(x0, y0, z));
        }

        private void BuildCalloutBadge()
        {
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            calloutRootGO = new GameObject("CalloutBadgeRoot");
            calloutRootGO.transform.SetParent(transform, false);
            calloutRootGO.transform.localPosition = new Vector3(0f, 0f, -0.035f);
            calloutRootGO.transform.localRotation = Quaternion.identity;
            calloutRootGO.transform.localScale = Vector3.one;

            // Semi-transparent dark glass background backing quad
            calloutBgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            calloutBgQuad.name = "CalloutBg";
            calloutBgQuad.transform.SetParent(calloutRootGO.transform, false);
            calloutBgQuad.transform.localPosition = new Vector3(0f, 0f, 0.005f);
            calloutBgQuad.transform.localRotation = Quaternion.identity;
            calloutBgQuad.transform.localScale = new Vector3(0.36f, 0.22f, 1f);

            var col = calloutBgQuad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = calloutBgQuad.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var bgMat = new Material(unlitShader);
                bgMat.SetColor("_BaseColor", new Color(0.04f, 0.08f, 0.15f, 0.88f)); // Dark Navy Glass
                mr.sharedMaterial = bgMat;
            }

            // Title Text
            var titleGO = new GameObject("CalloutTitle");
            titleGO.transform.SetParent(calloutRootGO.transform, false);
            titleGO.transform.localPosition = new Vector3(0f, 0.07f, -0.008f);
            calloutTitleTMP = titleGO.AddComponent<TextMeshPro>();
            calloutTitleTMP.fontSize = 1.35f;
            calloutTitleTMP.alignment = TextAlignmentOptions.Center;
            calloutTitleTMP.color = Color.white;
            calloutTitleTMP.rectTransform.sizeDelta = new Vector2(0.34f, 0.07f);

            // Body Bullet Points
            var bodyGO = new GameObject("CalloutBody");
            bodyGO.transform.SetParent(calloutRootGO.transform, false);
            bodyGO.transform.localPosition = new Vector3(0f, -0.025f, -0.008f);
            calloutBodyTMP = bodyGO.AddComponent<TextMeshPro>();
            calloutBodyTMP.fontSize = 0.95f;
            calloutBodyTMP.alignment = TextAlignmentOptions.TopLeft;
            calloutBodyTMP.color = new Color(0.92f, 0.95f, 1f, 0.95f);
            calloutBodyTMP.rectTransform.sizeDelta = new Vector2(0.32f, 0.14f);

            calloutRootGO.SetActive(false);
        }

        public void ShowCallout(string title, string content, Vector3 localPos, Color accentColor)
        {
            if (calloutRootGO == null) return;

            targetCalloutPos = new Vector3(localPos.x, localPos.y, -0.035f);
            if (!isCalloutActive)
            {
                calloutRootGO.transform.localPosition = targetCalloutPos;
            }

            if (calloutTitleTMP != null)
            {
                calloutTitleTMP.text = $"<b>{title}</b>";
                calloutTitleTMP.color = accentColor;
            }

            if (calloutBodyTMP != null)
            {
                calloutBodyTMP.text = content;
            }

            isCalloutActive = true;
            calloutRootGO.SetActive(true);
        }

        public void HideCallout()
        {
            isCalloutActive = false;
            if (calloutRootGO != null) calloutRootGO.SetActive(false);
        }

        public void SetTargetElement(string elementName)
        {
            if (string.IsNullOrEmpty(elementName) || elementName == "none")
            {
                HideTargetElement();
                return;
            }

            string key = elementName.ToLowerInvariant();
            if (elementBounds.TryGetValue(key, out var bounds))
            {
                // Add padding around the target element
                float padX = 0.012f;
                float padY = 0.012f;
                targetReticleBounds = new Vector4(bounds.x - padX, bounds.y + padX, bounds.z - padY, bounds.w + padY);

                if (!isReticleActive)
                {
                    currentReticleBounds = targetReticleBounds;
                    UpdateReticleGeometry(currentReticleBounds.x, currentReticleBounds.y, currentReticleBounds.z, currentReticleBounds.w);
                }

                isReticleActive = true;
                if (targetReticleGO != null) targetReticleGO.SetActive(true);
            }
        }

        public void HideTargetElement()
        {
            isReticleActive = false;
            if (targetReticleGO != null) targetReticleGO.SetActive(false);
        }

        // Backward compatibility for point target reticle
        public void SetFocusTarget(Vector3 localTargetPos)
        {
            float halfW = 0.07f;
            float halfH = 0.07f;
            targetReticleBounds = new Vector4(localTargetPos.x - halfW, localTargetPos.x + halfW, localTargetPos.y - halfH, localTargetPos.y + halfH);
            if (!isReticleActive) currentReticleBounds = targetReticleBounds;
            isReticleActive = true;
            if (targetReticleGO != null) targetReticleGO.SetActive(true);
        }

        public void HideFocusTarget()
        {
            HideTargetElement();
        }

        public void SetPopUpState(bool popUp)
        {
            isPopped = popUp;
            if (popRoutine != null) StopCoroutine(popRoutine);

            Vector3 targetPos = popUp ? poppedPosition : wallRestPosition;
            Quaternion targetRot = popUp ? poppedRotation : wallRestRotation;
            popRoutine = StartCoroutine(LerpTransformRoutine(targetPos, targetRot, popDuration));

            if (!popUp)
            {
                SetHighlightSection("none");
                HideTargetElement();
                HideCallout();
            }
        }

        public void SetHighlightSection(string sectionName)
        {
            activeSection = sectionName != null ? sectionName.ToLowerInvariant() : "none";

            if (lineFrame != null) lineFrame.enabled = (activeSection == "all");
            if (lineUE != null) lineUE.enabled = (activeSection == "ue" || activeSection == "all");
            if (lineAccess != null) lineAccess.enabled = (activeSection == "access" || activeSection == "all");
            if (lineControl != null) lineControl.enabled = (activeSection == "control" || activeSection == "all");
            if (lineUserPlane != null) lineUserPlane.enabled = (activeSection == "user_plane" || activeSection == "all");

            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            if (activeSection != "none")
            {
                pulseRoutine = StartCoroutine(PulseActiveHighlights());
            }
        }

        private IEnumerator PulseActiveHighlights()
        {
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                float pulse = 1f + 0.30f * Mathf.Sin(elapsed * 5f);
                float baseWidth = 0.013f;

                if (lineFrame != null && lineFrame.enabled)
                {
                    lineFrame.startWidth = baseWidth * pulse;
                    lineFrame.endWidth = baseWidth * pulse;
                }
                if (lineUE != null && lineUE.enabled)
                {
                    lineUE.startWidth = (baseWidth * 1.15f) * pulse;
                    lineUE.endWidth = (baseWidth * 1.15f) * pulse;
                }
                if (lineAccess != null && lineAccess.enabled)
                {
                    lineAccess.startWidth = (baseWidth * 1.15f) * pulse;
                    lineAccess.endWidth = (baseWidth * 1.15f) * pulse;
                }
                if (lineControl != null && lineControl.enabled)
                {
                    lineControl.startWidth = (baseWidth * 1.15f) * pulse;
                    lineControl.endWidth = (baseWidth * 1.15f) * pulse;
                }
                if (lineUserPlane != null && lineUserPlane.enabled)
                {
                    lineUserPlane.startWidth = (baseWidth * 1.15f) * pulse;
                    lineUserPlane.endWidth = (baseWidth * 1.15f) * pulse;
                }
                if (targetReticleLine != null && isReticleActive)
                {
                    targetReticleLine.startWidth = (baseWidth * 1.25f) * (1f + 0.40f * Mathf.Cos(elapsed * 6.5f));
                    targetReticleLine.endWidth = targetReticleLine.startWidth;
                }

                yield return null;
            }
        }

        private IEnumerator LerpTransformRoutine(Vector3 targetPos, Quaternion targetRot, float duration)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;
            popRoutine = null;
        }

        private void OnDestroy()
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            transform.position = wallRestPosition;
            transform.rotation = wallRestRotation;
        }
    }
}
