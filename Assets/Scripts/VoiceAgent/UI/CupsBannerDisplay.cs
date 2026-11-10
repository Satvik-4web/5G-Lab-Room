using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Interactive, animated 3D WorldSpace Display for the 5G CUPS Architecture Wall Banner.
    /// Features glowing diagrams, dynamic section highlights, pop-up transitions,
    /// and step-by-step lecture synchronization for Module 1.
    /// </summary>
    public class CupsBannerDisplay : MonoBehaviour
    {
        public static CupsBannerDisplay Instance { get; private set; }

        [Header("Banner Positioning & Transform")]
        [SerializeField] private Vector3 popOffset = new Vector3(-0.15f, 0, 0); // Floats outward from wall into room
        [SerializeField] private float popAnimationDuration = 0.8f;

        [Header("UI Visual Elements")]
        private Canvas worldCanvas;
        private RectTransform canvasRect;
        private GameObject bannerPanel;
        
        // Section Root GameObjects & Outlines
        private Image headerBorder;
        private Image ueCard;
        private Image accessCard;
        private Image controlPlaneCard;
        private Image userPlaneCard;
        private Image cupsConduitCard;

        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subTitleText;
        private TextMeshProUGUI moduleStatusText;

        // Colors
        private readonly Color colIdleBorder = new Color(0.15f, 0.25f, 0.35f, 0.6f);
        private readonly Color colHighlightCyan = new Color(0f, 0.95f, 1f, 1f);     // UEs & Access
        private readonly Color colHighlightGold = new Color(1f, 0.8f, 0.2f, 1f);      // Control Plane
        private readonly Color colHighlightGreen = new Color(0.1f, 1f, 0.45f, 1f);   // User Plane & CUPS
        private readonly Color colBgDark = new Color(0.05f, 0.08f, 0.13f, 0.94f);
        private readonly Color colCardBg = new Color(0.08f, 0.12f, 0.18f, 0.90f);

        private Vector3 initialLocalPos;
        private Vector3 targetLocalPos;
        private bool isPoppedUp = false;
        private Coroutine popRoutine;
        private Coroutine pulseRoutine;

        private string activeSection = "none";

        private void Awake()
        {
            if (Instance == null) Instance = this;
            initialLocalPos = transform.localPosition;
            targetLocalPos = initialLocalPos;

            BuildWorldSpaceBannerUI();
        }

        private void Start()
        {
            SetHighlightSection("none");
        }

        private void BuildWorldSpaceBannerUI()
        {
            // 1. Create or get Canvas
            var canvasGO = new GameObject("CUPS_Banner_Canvas");
            canvasGO.transform.SetParent(transform, false);
            // Face directly into the room (towards -X in room space)
            canvasGO.transform.localRotation = Quaternion.Euler(0, -90f, 0);
            canvasGO.transform.localPosition = new Vector3(-0.02f, 0, 0);

            worldCanvas = canvasGO.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            canvasGO.AddComponent<GraphicRaycaster>();

            canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1600f, 1050f);
            canvasRect.localScale = Vector3.one * 0.0036f; // Fits the 6.0 x 4.0 meter wall banner area

            // 2. Main Background Glassmorphic Panel
            bannerPanel = new GameObject("BannerPanel");
            bannerPanel.transform.SetParent(canvasRect, false);
            var panelRect = bannerPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelBg = bannerPanel.AddComponent<Image>();
            panelBg.color = colBgDark;

            // Top Glowing Accent Line
            var topAcc = CreateImage("TopAccent", bannerPanel.transform, new Vector2(0, 0.99f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            topAcc.color = colHighlightCyan;
            headerBorder = topAcc;

            // 3. Header Text & Institution Crest
            var headerGO = new GameObject("HeaderTitle");
            headerGO.transform.SetParent(bannerPanel.transform, false);
            titleText = headerGO.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 32;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.text = "<color=#E2E8F0><b>DEPARTMENT OF ELECTRONICS & COMMUNICATION ENGINEERING</b></color>\n<size=75%><color=#8AB4F8>THAPAR INSTITUTE OF ENGINEERING & TECHNOLOGY (TIET), PATIALA</color></size>";
            var hRect = headerGO.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.05f, 0.88f);
            hRect.anchorMax = new Vector2(0.95f, 0.98f);
            hRect.offsetMin = Vector2.zero;
            hRect.offsetMax = Vector2.zero;

            var subTitleGO = new GameObject("BannerMainTitle");
            subTitleGO.transform.SetParent(bannerPanel.transform, false);
            subTitleText = subTitleGO.AddComponent<TextMeshProUGUI>();
            subTitleText.fontSize = 34;
            subTitleText.alignment = TextAlignmentOptions.Center;
            subTitleText.text = "<color=#00E5FF><b>5G Architecture based on CUPS</b></color>\n<size=55%><color=#94A3B8>(Control and User Plane Separation — 3GPP TS 23.214 / Rel-15)</color></size>";
            var stRect = subTitleGO.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0.05f, 0.77f);
            stRect.anchorMax = new Vector2(0.95f, 0.88f);
            stRect.offsetMin = Vector2.zero;
            stRect.offsetMax = Vector2.zero;

            // 4. Status Indicator Pill in Banner Header
            var statusGO = new GameObject("ModuleStatusPill");
            statusGO.transform.SetParent(bannerPanel.transform, false);
            moduleStatusText = statusGO.AddComponent<TextMeshProUGUI>();
            moduleStatusText.fontSize = 20;
            moduleStatusText.alignment = TextAlignmentOptions.MidlineRight;
            moduleStatusText.text = "<color=#00E676>● MODULE 1: ACTIVE</color> <color=#64748B>| Click blocks to inspect</color>";
            var msRect = statusGO.GetComponent<RectTransform>();
            msRect.anchorMin = new Vector2(0.60f, 0.92f);
            msRect.anchorMax = new Vector2(0.98f, 0.98f);
            msRect.offsetMin = Vector2.zero;
            msRect.offsetMax = Vector2.zero;

            // 5. ZONE 1: User Equipment (UE) Column (Left)
            ueCard = CreateDiagramCard("Card_UE", bannerPanel.transform,
                new Vector2(0.02f, 0.04f), new Vector2(0.20f, 0.74f),
                "<b>USER EQUIPMENT (UE)</b>",
                "• <b>IoT Devices / Sensors</b> (Smart Meters)\n" +
                "• <b>Connected Vehicles</b> (C-V2X Auto)\n" +
                "• <b>5G Smartphones</b> (Sub-6 & mmWave)\n" +
                "• <b>Fixed Terminals</b> (High-Speed PC)\n\n" +
                "<color=#64748B><size=80%>Generates diverse traffic with latency needs from 1ms to bulk eMBB.</size></color>",
                colHighlightCyan);

            // 6. ZONE 2: Access Network Column
            accessCard = CreateDiagramCard("Card_Access", bannerPanel.transform,
                new Vector2(0.22f, 0.04f), new Vector2(0.42f, 0.74f),
                "<b>ACCESS NETWORK (RAN)</b>",
                "• <b>5G BS (gNodeB):</b>\n  Massive MIMO, Beamforming, NR PHY\n\n" +
                "• <b>4G BS (eNodeB):</b>\n  LTE anchor for NSA Option 3x\n\n" +
                "• <b>Wi-Fi (via N3IWF):</b>\n  Untrusted Non-3GPP IPsec Gateway\n\n" +
                "• <b>Fixed Wireline:</b>\n  5G Residential Gateway (5G-RG)\n\n" +
                "<color=#64748B><size=80%>Connects to AMF via N2 (CP) and UPF via N3 (UP).</size></color>",
                colHighlightCyan);

            // 7. ZONE 3: 5G Control Plane (Top-Right)
            controlPlaneCard = CreateDiagramCard("Card_ControlPlane", bannerPanel.transform,
                new Vector2(0.44f, 0.41f), new Vector2(0.98f, 0.74f),
                "<b>5G CONTROL PLANE (SBA — SERVICE-BASED ARCHITECTURE)</b>",
                "<color=#FFD54F><b>[NSSF]</b> Slice Selection   •   <b>[AUSF]</b> 5G-AKA Auth   •   <b>[UDM]</b> Subscriber Data DB</color>\n" +
                "<size=85%><color=#94A3B8>──────────────────────────────────────────────────────────────────────────</color></size>\n" +
                "<color=#00E5FF><b>[AMF]</b> Access & Mobility</color>   •   <color=#00E5FF><b>[SMF]</b> Session Management</color>   •   <color=#00E5FF><b>[PCF]</b> Policy QoS</color>   •   <b>[AF]</b> App Function\n\n" +
                "<color=#CBD5E1><size=85%>Control plane functions communicate over REST HTTP/2 JSON APIs. Control signaling is fully decoupled from high-throughput user payloads.</size></color>",
                colHighlightGold);

            // 8. ZONE 4: 5G User Plane & CUPS (Bottom-Right)
            userPlaneCard = CreateDiagramCard("Card_UserPlane", bannerPanel.transform,
                new Vector2(0.44f, 0.04f), new Vector2(0.98f, 0.39f),
                "<b>5G USER PLANE & DISTRIBUTED UPFs (CUPS)</b>",
                "• <b>Edge UPF (Local Breakout):</b> Sits at cell site next to MEC rack ➔ <color=#00E676><b>Local DN (< 1ms Latency)</b></color>\n" +
                "• <b>Intermediate UPF:</b> Branching point for session traffic steering\n" +
                "• <b>Central UPF:</b> High-capacity forwarding to Internet & Cloud DN\n\n" +
                "<color=#00E676><b>★ CUPS PRINCIPLE (3GPP TS 23.214):</b> SMF controls forwarding rules over the <b>N4 interface (PFCP)</b> while UPFs scale independently at the edge!</color>",
                colHighlightGreen);

            // 9. Floating Conduit Indicator between SMF and UPF
            var conduitGO = CreateImage("N4_ConduitLine", bannerPanel.transform,
                new Vector2(0.68f, 0.37f), new Vector2(0.74f, 0.43f), Vector2.zero, Vector2.zero);
            conduitGO.color = new Color(0f, 1f, 0.5f, 0.8f);
            cupsConduitCard = conduitGO;
        }

        private Image CreateDiagramCard(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, string title, string content, Color accentColor)
        {
            var cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);
            var rect = cardGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(6, 6);
            rect.offsetMax = new Vector2(-6, -6);

            var bgImg = cardGO.AddComponent<Image>();
            bgImg.color = colCardBg;

            // Outer border image
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(cardGO.transform, false);
            var bRect = borderGO.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            var borderImg = borderGO.AddComponent<Image>();
            borderImg.color = colIdleBorder;

            // Accent Header Tag
            var tagGO = new GameObject("AccentTag");
            tagGO.transform.SetParent(cardGO.transform, false);
            var tagRect = tagGO.AddComponent<RectTransform>();
            tagRect.anchorMin = new Vector2(0, 0.96f);
            tagRect.anchorMax = new Vector2(1, 1f);
            tagRect.offsetMin = Vector2.zero;
            tagRect.offsetMax = Vector2.zero;
            var tagImg = tagGO.AddComponent<Image>();
            tagImg.color = accentColor;

            // Card Text
            var textGO = new GameObject("ContentText");
            textGO.transform.SetParent(cardGO.transform, false);
            var txt = textGO.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 17;
            txt.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            txt.text = $"<size=115%>{title}</size>\n\n{content}";
            var tRect = textGO.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.04f, 0.04f);
            tRect.anchorMax = new Vector2(0.96f, 0.94f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            // Add interactive button
            var btn = cardGO.AddComponent<Button>();
            btn.onClick.AddListener(() => OnCardClicked(name));

            return borderImg;
        }

        private Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return go.AddComponent<Image>();
        }

        private void OnCardClicked(string cardName)
        {
            if (VoiceAgentManager.Instance == null) return;

            string query = cardName switch
            {
                "Card_UE" => "Explain the user equipment layer in the 5G CUPS architecture banner",
                "Card_Access" => "Explain the access networks like gNodeB, 4G, and Wi-Fi in the 5G banner",
                "Card_ControlPlane" => "Explain the 5G Control Plane functions like AMF, SMF, and UDM on the banner",
                "Card_UserPlane" => "Explain Control and User Plane Separation, CUPS, and distributed UPFs on the banner",
                _ => "Explain the 5G CUPS architecture banner"
            };

            _ = VoiceAgentManager.Instance.ProcessTypedInputAsync(query);
        }

        /// <summary>
        /// Smoothly pops the banner forward into the room during inspection or Module 1.
        /// </summary>
        public void SetPopUpState(bool pop)
        {
            isPoppedUp = pop;
            targetLocalPos = pop ? (initialLocalPos + popOffset) : initialLocalPos;

            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(AnimatePosition(targetLocalPos));
        }

        private IEnumerator AnimatePosition(Vector3 dest)
        {
            Vector3 start = transform.localPosition;
            float elapsed = 0;
            while (elapsed < popAnimationDuration)
            {
                float t = elapsed / popAnimationDuration;
                t = Mathf.SmoothStep(0f, 1f, t);
                transform.localPosition = Vector3.Lerp(start, dest, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = dest;
        }

        /// <summary>
        /// Highlights a specific diagram section with animated glowing colors during lecture narration.
        /// </summary>
        public void SetHighlightSection(string section)
        {
            activeSection = section.ToLowerInvariant();

            // Reset all borders to idle
            if (ueCard != null) ueCard.color = colIdleBorder;
            if (accessCard != null) accessCard.color = colIdleBorder;
            if (controlPlaneCard != null) controlPlaneCard.color = colIdleBorder;
            if (userPlaneCard != null) userPlaneCard.color = colIdleBorder;
            if (cupsConduitCard != null) cupsConduitCard.color = new Color(0f, 1f, 0.5f, 0.3f);

            if (pulseRoutine != null) StopCoroutine(pulseRoutine);

            switch (activeSection)
            {
                case "ue":
                case "user_equipment":
                    if (ueCard != null) pulseRoutine = StartCoroutine(PulseBorder(ueCard, colHighlightCyan));
                    break;

                case "access":
                case "ran":
                    if (accessCard != null) pulseRoutine = StartCoroutine(PulseBorder(accessCard, colHighlightCyan));
                    break;

                case "control":
                case "control_plane":
                case "sba":
                    if (controlPlaneCard != null) pulseRoutine = StartCoroutine(PulseBorder(controlPlaneCard, colHighlightGold));
                    break;

                case "user_plane":
                case "upf":
                case "cups":
                    if (userPlaneCard != null) pulseRoutine = StartCoroutine(PulseBorder(userPlaneCard, colHighlightGreen));
                    if (cupsConduitCard != null) cupsConduitCard.color = colHighlightGreen;
                    break;

                case "all":
                    if (ueCard != null) ueCard.color = colHighlightCyan;
                    if (accessCard != null) accessCard.color = colHighlightCyan;
                    if (controlPlaneCard != null) controlPlaneCard.color = colHighlightGold;
                    if (userPlaneCard != null) userPlaneCard.color = colHighlightGreen;
                    break;
            }
        }

        private IEnumerator PulseBorder(Image img, Color targetColor)
        {
            float time = 0;
            while (true)
            {
                float alpha = 0.5f + Mathf.PingPong(time * 2.5f, 0.5f);
                img.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
                time += Time.deltaTime;
                yield return null;
            }
        }
    }
}
