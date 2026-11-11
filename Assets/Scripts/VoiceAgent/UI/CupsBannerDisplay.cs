using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Professional Holographic Pop-Up & Section Highlighter for the 5G CUPS Architecture Wall Banner (Poster1).
    /// Features:
    /// - Professional presentation angle: floats forward to standing presenter eye-level with an ergonomic 6-degree backward tilt.
    /// - Subtle holographic energy field micro-bobbing.
    /// - Cybernetic laser outlines for UE/Access, Control Plane SBA, and User Plane/UPF.
    /// - Dynamic holographic focus reticle that glides smoothly to the component Aria is explaining.
    /// </summary>
    public class CupsBannerDisplay : MonoBehaviour
    {
        public static CupsBannerDisplay Instance { get; private set; }

        [Header("Banner Positioning & Professional Presentation Angle")]
        private Vector3 wallRestPosition;
        private Quaternion wallRestRotation;
        private Vector3 poppedPosition;
        private Quaternion poppedRotation;

        [SerializeField] private Vector3 bannerScale = new Vector3(3.30f, 2.20f, 1.0f);
        [SerializeField] private float popDuration = 0.85f;

        [Header("Cybernetic Laser Colors")]
        private readonly Color colCyan = new Color(0f, 0.95f, 1f, 1f);        // Zone 1: UE & Access Network
        private readonly Color colGold = new Color(1f, 0.85f, 0.2f, 1f);      // Zone 2: 5G Control Plane SBA
        private readonly Color colGreen = new Color(0.1f, 1f, 0.45f, 1f);     // Zone 3: 5G User Plane & CUPS UPF
        private readonly Color colFrame = new Color(0.25f, 0.65f, 1f, 0.85f); // Overall border

        // Outline LineRenderers
        private LineRenderer lineFrame;
        private LineRenderer lineUE;
        private LineRenderer lineControl;
        private LineRenderer lineUserPlane;

        // Dynamic Holographic Focus Reticle
        private GameObject focusMarkerGO;
        private LineRenderer focusMarkerLine;
        private Vector3 targetFocusLocalPos = Vector3.zero;
        private bool isFocusActive = false;

        private Coroutine popRoutine;
        private Coroutine pulseRoutine;
        private string activeSection = "none";
        private bool isPopped = false;
        private float hoverTime = 0f;

        public bool IsPopped => isPopped;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            // Preserve natural wall transform from scene
            wallRestPosition = transform.position;
            wallRestRotation = transform.rotation;

            // Ensure proper rotation: quad normal faces into room (-X direction, Y=90)
            // Never allow zero or identity rotation to invert/mirror the Quad!
            if (wallRestRotation == Quaternion.identity || Mathf.Abs(wallRestRotation.y) < 0.1f)
            {
                wallRestRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // Pops cleanly forward into the open room aisle (X = 4.80m), completely in front of all desks and equipment!
            poppedPosition = new Vector3(4.80f, 1.95f, -1.80f);
            // Parallel presentation angle: perfectly facing the student, zero distortion
            poppedRotation = Quaternion.Euler(0f, 90f, 0f);

            transform.localScale = bannerScale;
            transform.position = wallRestPosition;
            transform.rotation = wallRestRotation;

            BuildHighlightOutlines();
            BuildFocusReticle();
        }

        private void Start()
        {
            SetHighlightSection("none");
            HideFocusTarget();
        }

        private void Update()
        {
            // Smooth focus marker gliding
            if (focusMarkerGO != null && isFocusActive)
            {
                focusMarkerGO.transform.localPosition = Vector3.Lerp(
                    focusMarkerGO.transform.localPosition,
                    targetFocusLocalPos,
                    Time.deltaTime * 8f
                );
            }
        }

        private void BuildHighlightOutlines()
        {
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            // 1. Overall Banner Border Frame
            lineFrame = CreateOutlineLine("Outline_Frame",
                -0.495f, 0.495f, -0.495f, 0.495f, -0.012f,
                colFrame, 0.012f, unlitShader);

            // 2. Zone 1: UE & Heterogeneous Access Network (Left Side)
            lineUE = CreateOutlineLine("Outline_UE_Access",
                -0.485f, -0.155f, -0.450f, 0.330f, -0.018f,
                colCyan, 0.015f, unlitShader);

            // 3. Zone 2: 5G Control Plane SBA (Top Right)
            lineControl = CreateOutlineLine("Outline_ControlPlane",
                -0.130f, 0.465f, 0.015f, 0.330f, -0.018f,
                colGold, 0.015f, unlitShader);

            // 4. Zone 3: 5G User Plane & Distributed UPFs (Bottom Right)
            lineUserPlane = CreateOutlineLine("Outline_UserPlane",
                -0.075f, 0.465f, -0.450f, -0.015f, -0.018f,
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

        private void BuildFocusReticle()
        {
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            focusMarkerGO = new GameObject("HoloFocusReticle");
            focusMarkerGO.transform.SetParent(transform, false);
            focusMarkerGO.transform.localPosition = new Vector3(0f, 0f, -0.025f);
            focusMarkerGO.transform.localRotation = Quaternion.identity;
            focusMarkerGO.transform.localScale = Vector3.one;

            focusMarkerLine = focusMarkerGO.AddComponent<LineRenderer>();
            focusMarkerLine.useWorldSpace = false;
            focusMarkerLine.loop = true;
            focusMarkerLine.startWidth = 0.012f;
            focusMarkerLine.endWidth = 0.012f;

            // 16-point circle reticle
            int segments = 16;
            focusMarkerLine.positionCount = segments + 1;
            float radius = 0.065f;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                focusMarkerLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }

            var mat = new Material(unlitShader);
            mat.SetColor("_BaseColor", new Color(1f, 0.3f, 0.8f, 1f)); // Neon Magenta Target
            focusMarkerLine.sharedMaterial = mat;
            focusMarkerGO.SetActive(false);
        }

        public void SetFocusTarget(Vector3 localTargetPos)
        {
            targetFocusLocalPos = localTargetPos;
            targetFocusLocalPos.z = -0.025f; // ensure slightly in front of banner surface
            isFocusActive = true;
            if (focusMarkerGO != null) focusMarkerGO.SetActive(true);
        }

        public void HideFocusTarget()
        {
            isFocusActive = false;
            if (focusMarkerGO != null) focusMarkerGO.SetActive(false);
        }

        public void SetPopUpState(bool popUp)
        {
            isPopped = popUp;
            hoverTime = 0f;
            if (popRoutine != null) StopCoroutine(popRoutine);

            Vector3 targetPos = popUp ? poppedPosition : wallRestPosition;
            Quaternion targetRot = popUp ? poppedRotation : wallRestRotation;
            popRoutine = StartCoroutine(LerpTransformRoutine(targetPos, targetRot, popDuration));

            if (!popUp)
            {
                SetHighlightSection("none");
                HideFocusTarget();
            }
        }

        public void SetHighlightSection(string sectionName)
        {
            activeSection = sectionName != null ? sectionName.ToLowerInvariant() : "none";
            if (activeSection == "access" || activeSection == "ran") activeSection = "ue";

            if (lineFrame != null) lineFrame.enabled = (activeSection == "all");
            if (lineUE != null) lineUE.enabled = (activeSection == "ue" || activeSection == "all");
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
                float pulse = 1f + 0.35f * Mathf.Sin(elapsed * 5f);
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
                if (focusMarkerLine != null && isFocusActive)
                {
                    focusMarkerLine.startWidth = (baseWidth * 1.1f) * (1f + 0.4f * Mathf.Cos(elapsed * 7f));
                    focusMarkerLine.endWidth = focusMarkerLine.startWidth;
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
                // Ease out cubic
                t = 1f - Mathf.Pow(1f - t, 3f);

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
