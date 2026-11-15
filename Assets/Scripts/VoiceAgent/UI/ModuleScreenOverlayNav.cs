using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Screen-Space Overlay Navigation HUD for the 5G Guided Tour.
    /// Provides an always-clickable, responsive, cybernetic "NEXT: MODULE 2 ➔" button
    /// in desktop mode and simulator view, matching the obsidian/cyan aesthetic.
    /// Supports mouse click, hover glow, breathing pulse, and keyboard shortcuts [N], [Space], [Enter].
    /// </summary>
    public class ModuleScreenOverlayNav : MonoBehaviour
    {
        public static ModuleScreenOverlayNav Instance { get; private set; }

        public static ModuleScreenOverlayNav EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("ModuleScreenOverlayNav");
            Instance = go.AddComponent<ModuleScreenOverlayNav>();
            DontDestroyOnLoad(go);
            return Instance;
        }

        private Canvas canvas;
        private GameObject buttonRoot;
        private Button button;
        private Image bgImage;
        private Outline outline;
        private TextMeshProUGUI buttonText;

        private Action currentCallback;
        private bool isButtonVisible = false;
        private bool isHovered = false;
        private Coroutine clickCoroutine;

        public bool IsActive => isButtonVisible;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            EnsureUIStructure();
        }

        private void EnsureUIStructure()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
                if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;

                var scaler = GetComponent<CanvasScaler>();
                if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                if (GetComponent<GraphicRaycaster>() == null)
                {
                    gameObject.AddComponent<GraphicRaycaster>();
                }
            }

            if (buttonRoot == null)
            {
                buttonRoot = new GameObject("ScreenNav_NextButton");
                buttonRoot.transform.SetParent(transform, false);

                var rect = buttonRoot.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(300f, 48f);
                rect.anchoredPosition = new Vector2(0f, 114f); // Sits comfortably above the bottom subtitle pill

                // Obsidian glass background
                bgImage = buttonRoot.AddComponent<Image>();
                bgImage.color = new Color(0.03f, 0.06f, 0.12f, 0.94f);

                // Glowing Cyan Outline
                outline = buttonRoot.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0.90f, 1f, 0.92f);
                outline.effectDistance = new Vector2(2f, 2f);

                // Soft shadow for depth
                var shadow = buttonRoot.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.70f);
                shadow.effectDistance = new Vector2(0f, -3f);

                // Button interaction
                button = buttonRoot.AddComponent<Button>();
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.5f, 1f, 1f, 1f);
                colors.pressedColor = new Color(0f, 0.85f, 1f, 1f);
                colors.selectedColor = Color.white;
                button.colors = colors;
                button.onClick.AddListener(OnButtonClicked);

                // EventTrigger for pointer hover
                var trigger = buttonRoot.AddComponent<EventTrigger>();
                var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener((data) => OnPointerEnter());
                trigger.triggers.Add(enterEntry);

                var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exitEntry.callback.AddListener((data) => OnPointerExit());
                trigger.triggers.Add(exitEntry);

                // Typography
                var textGO = new GameObject("ButtonText");
                textGO.transform.SetParent(buttonRoot.transform, false);
                var tRect = textGO.AddComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.offsetMin = new Vector2(12, 0);
                tRect.offsetMax = new Vector2(-12, 0);

                buttonText = textGO.AddComponent<TextMeshProUGUI>();
                buttonText.fontSize = 15;
                buttonText.fontStyle = FontStyles.Bold;
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.color = new Color(0f, 0.95f, 1f, 1f);
                buttonText.text = "NEXT: MODULE 2 ➔  <size=75%><color=#80D8FF>[N / Click]</color></size>";

                buttonRoot.SetActive(false);
            }
        }

        public void ShowNextButton(string label, Action onClick)
        {
            EnsureUIStructure();
            currentCallback = onClick;
            isButtonVisible = true;

            if (buttonRoot != null)
            {
                buttonRoot.SetActive(true);
                buttonRoot.transform.localScale = Vector3.one;
            }

            if (buttonText != null)
            {
                buttonText.text = label;
            }
        }

        public void HideNextButton()
        {
            isButtonVisible = false;
            currentCallback = null;
            isHovered = false;

            if (buttonRoot != null)
            {
                buttonRoot.SetActive(false);
            }
        }

        private void OnPointerEnter()
        {
            isHovered = true;
            if (bgImage != null) bgImage.color = new Color(0.06f, 0.22f, 0.35f, 0.96f);
            if (buttonText != null) buttonText.color = Color.white;
        }

        private void OnPointerExit()
        {
            isHovered = false;
            if (bgImage != null) bgImage.color = new Color(0.03f, 0.06f, 0.12f, 0.94f);
            if (buttonText != null) buttonText.color = new Color(0f, 0.95f, 1f, 1f);
        }

        private void OnButtonClicked()
        {
            TriggerNext();
        }

        public void TriggerNext()
        {
            if (!isButtonVisible) return;

            if (clickCoroutine != null) StopCoroutine(clickCoroutine);
            clickCoroutine = StartCoroutine(ClickFeedbackRoutine());

            var cb = currentCallback;
            cb?.Invoke();
        }

        private IEnumerator ClickFeedbackRoutine()
        {
            if (buttonRoot != null) buttonRoot.transform.localScale = Vector3.one * 0.94f;
            if (bgImage != null) bgImage.color = new Color(0f, 0.85f, 1f, 0.85f);
            yield return new WaitForSecondsRealtime(0.12f);
            if (buttonRoot != null) buttonRoot.transform.localScale = Vector3.one;
            if (bgImage != null) bgImage.color = new Color(0.03f, 0.06f, 0.12f, 0.94f);
        }

        private void Update()
        {
            if (!isButtonVisible) return;

            // Keyboard shortcut detection: [N], [Space], [Enter]
            bool hotkeyPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.nKey.wasPressedThisFrame ||
                    Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    hotkeyPressed = true;
                }
            }
            try
            {
                if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    hotkeyPressed = true;
                }
            }
            catch { }

            if (hotkeyPressed)
            {
                TriggerNext();
                return;
            }

            // Gentle breathing animation when waiting
            if (!isHovered && buttonRoot != null)
            {
                float pulse = 1.0f + 0.03f * Mathf.Sin(Time.unscaledTime * 4.5f);
                buttonRoot.transform.localScale = Vector3.one * pulse;
                if (outline != null)
                {
                    float glow = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 4.5f);
                    outline.effectColor = new Color(0f, 0.90f, 1f, glow);
                }
            }
        }
    }
}
