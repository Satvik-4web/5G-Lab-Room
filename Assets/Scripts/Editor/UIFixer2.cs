using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class UIFixer2
{
    public static string FixUI()
    {
        GameObject panel = EditorUtility.InstanceIDToObject(58432) as GameObject;
        GameObject nameTextObj = EditorUtility.InstanceIDToObject(57524) as GameObject;
        GameObject descTextObj = EditorUtility.InstanceIDToObject(60350) as GameObject;

        if (panel == null || nameTextObj == null || descTextObj == null) return "Objects missing";

        // Remove bugged layout components
        foreach (var comp in panel.GetComponents<LayoutGroup>()) Object.DestroyImmediate(comp, true);
        foreach (var comp in panel.GetComponents<ContentSizeFitter>()) Object.DestroyImmediate(comp, true);

        // 1. Panel Settings (Fixed wide box, bluish)
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(600, 300);
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.0f, 0.15f, 0.35f, 0.85f); // Bluish dark glass

        // 2. Left Neon Bar (Fixed on left edge)
        Transform leftBarTr = panel.transform.Find("LeftNeonBar");
        if (leftBarTr != null)
        {
            RectTransform lbRect = leftBarTr.GetComponent<RectTransform>();
            foreach (var comp in leftBarTr.GetComponents<LayoutElement>()) Object.DestroyImmediate(comp, true);
            lbRect.anchorMin = new Vector2(0, 0);
            lbRect.anchorMax = new Vector2(0, 1);
            lbRect.pivot = new Vector2(0, 0.5f);
            lbRect.anchoredPosition = Vector2.zero;
            lbRect.sizeDelta = new Vector2(8, 0); // 8px wide, full height
        }

        // 3. Content Container (Fills the rest of the panel)
        Transform contentTr = panel.transform.Find("ContentContainer");
        if (contentTr != null)
        {
            RectTransform contentRect = contentTr.GetComponent<RectTransform>();
            foreach (var comp in contentTr.GetComponents<LayoutGroup>()) Object.DestroyImmediate(comp, true);
            foreach (var comp in contentTr.GetComponents<LayoutElement>()) Object.DestroyImmediate(comp, true);
            
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(30, 20); // Left padding 30, Bottom 20
            contentRect.offsetMax = new Vector2(-20, -20); // Right 20, Top 20
        }

        // 4. Texts
        RectTransform nameRect = nameTextObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 1);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.pivot = new Vector2(0, 1);
        nameRect.anchoredPosition = new Vector2(0, 0);
        nameRect.sizeDelta = new Vector2(0, 50); // Full width, 50 height

        TextMeshProUGUI nameTMP = nameTextObj.GetComponent<TextMeshProUGUI>();
        nameTMP.fontSize = 32;
        nameTMP.characterSpacing = 0; // remove weird spacing
        
        Transform lineTr = panel.transform.Find("ContentContainer/SeparatorLine");
        if (lineTr != null)
        {
            RectTransform lineRect = lineTr.GetComponent<RectTransform>();
            foreach (var comp in lineTr.GetComponents<LayoutElement>()) Object.DestroyImmediate(comp, true);
            lineRect.anchorMin = new Vector2(0, 1);
            lineRect.anchorMax = new Vector2(1, 1);
            lineRect.pivot = new Vector2(0, 1);
            lineRect.anchoredPosition = new Vector2(0, -55);
            lineRect.sizeDelta = new Vector2(0, 2);
        }

        RectTransform descRect = descTextObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0);
        descRect.anchorMax = new Vector2(1, 1);
        descRect.offsetMin = new Vector2(0, 0);
        descRect.offsetMax = new Vector2(0, -70); // Keep top 70px clear for title/line

        TextMeshProUGUI descTMP = descTextObj.GetComponent<TextMeshProUGUI>();
        descTMP.fontSize = 18;
        descTMP.lineSpacing = 0; // reset line spacing

        EditorUtility.SetDirty(panel);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        return "Absolute UI Fixed!";
    }
}
