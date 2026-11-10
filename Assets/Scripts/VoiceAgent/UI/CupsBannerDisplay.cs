using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Holographic Pop-Up & Section Highlighter for the 5G CUPS Architecture Wall Banner (Poster1).
    /// Smoothly floats the banner forward from the wall during Module 1 and draws
    /// pulsing cybernetic laser outlines over the UE/Access, Control Plane SBA, and User Plane/UPF blocks.
    /// </summary>
    public class CupsBannerDisplay : MonoBehaviour
    {
        public static CupsBannerDisplay Instance { get; private set; }

        [Header("Banner Positioning & Pop-Up")]
        [SerializeField] private Vector3 wallRestPosition = new Vector3(7.42f, 2.34f, -1.49f);
        [SerializeField] private Vector3 poppedPosition = new Vector3(5.40f, 2.40f, -1.49f);
        [SerializeField] private float popDuration = 0.85f;

        [Header("Colors")]
        private readonly Color colCyan = new Color(0f, 0.95f, 1f, 1f);        // UE & Access Network
        private readonly Color colGold = new Color(1f, 0.85f, 0.2f, 1f);      // 5G Control Plane SBA
        private readonly Color colGreen = new Color(0.1f, 1f, 0.45f, 1f);     // 5G User Plane & CUPS UPF
        private readonly Color colFrame = new Color(0.3f, 0.7f, 1f, 0.75f);   // Overall border

        // Outline LineRenderers
        private LineRenderer lineFrame;
        private LineRenderer lineUE;
        private LineRenderer lineControl;
        private LineRenderer lineUserPlane;

        private Coroutine popRoutine;
        private Coroutine pulseRoutine;
        private string activeSection = "none";
        private bool isPopped = false;

        public bool IsPopped => isPopped;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            transform.position = wallRestPosition;
            BuildHighlightOutlines();
        }

        private void Start()
        {
            SetHighlightSection("none");
        }

        private void BuildHighlightOutlines()
        {
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            // 1. Overall Banner Border Frame
            lineFrame = CreateOutlineLine("Outline_Frame",
                -0.495f, 0.495f, -0.495f, 0.495f, -0.015f,
                colFrame, 0.02f, unlitShader);

            // 2. Zone 1: UE & Heterogeneous Access Network (Left Side)
            lineUE = CreateOutlineLine("Outline_UE_Access",
                -0.485f, -0.05f, -0.47f, 0.42f, -0.02f,
                colCyan, 0.022f, unlitShader);

            // 3. Zone 2: 5G Control Plane SBA (Top Right)
            lineControl = CreateOutlineLine("Outline_ControlPlane",
                -0.05f, 0.485f, 0.02f, 0.42f, -0.02f,
                colGold, 0.022f, unlitShader);

            // 4. Zone 3: 5G User Plane & Distributed UPFs (Bottom Right)
            lineUserPlane = CreateOutlineLine("Outline_UserPlane",
                -0.05f, 0.485f, -0.47f, 0.02f, -0.02f,
                colGreen, 0.022f, unlitShader);
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

        public void SetPopUpState(bool popUp)
        {
            isPopped = popUp;
            if (popRoutine != null) StopCoroutine(popRoutine);
            Vector3 targetPos = popUp ? poppedPosition : wallRestPosition;
            popRoutine = StartCoroutine(LerpPosition(targetPos, popDuration));

            if (!popUp)
            {
                SetHighlightSection("none");
            }
        }

        public void SetHighlightSection(string sectionName)
        {
            activeSection = sectionName ?? "none";
            if (activeSection == "access") activeSection = "ue";

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
                float baseWidth = 0.02f;

                if (lineFrame != null && lineFrame.enabled)
                {
                    lineFrame.startWidth = baseWidth * pulse;
                    lineFrame.endWidth = baseWidth * pulse;
                }
                if (lineUE != null && lineUE.enabled)
                {
                    lineUE.startWidth = (baseWidth * 1.1f) * pulse;
                    lineUE.endWidth = (baseWidth * 1.1f) * pulse;
                }
                if (lineControl != null && lineControl.enabled)
                {
                    lineControl.startWidth = (baseWidth * 1.1f) * pulse;
                    lineControl.endWidth = (baseWidth * 1.1f) * pulse;
                }
                if (lineUserPlane != null && lineUserPlane.enabled)
                {
                    lineUserPlane.startWidth = (baseWidth * 1.1f) * pulse;
                    lineUserPlane.endWidth = (baseWidth * 1.1f) * pulse;
                }

                yield return null;
            }
        }

        private IEnumerator LerpPosition(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out cubic
                t = 1f - Mathf.Pow(1f - t, 3f);
                transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }

            transform.position = target;
        }

        private void OnDestroy()
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            transform.position = wallRestPosition;
        }
    }
}
