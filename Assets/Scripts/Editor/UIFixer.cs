using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class UIFixer
{
    public static string FixUI()
    {
        GameObject panel = EditorUtility.InstanceIDToObject(58432) as GameObject;
        GameObject nameTextObj = EditorUtility.InstanceIDToObject(57524) as GameObject;
        GameObject descTextObj = EditorUtility.InstanceIDToObject(60350) as GameObject;

        if (panel == null || nameTextObj == null || descTextObj == null) return "UI Objects not found";

        // 1. Configure the Background Panel
        Image bg = panel.GetComponent<Image>();
        if (bg == null) bg = panel.AddComponent<Image>();
        bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f); // Dark translucent

        // Add Layout Group for perfect padding
        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(30, 30, 20, 30);
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        // Add Content Size Fitter to hug the text
        ContentSizeFitter csf = panel.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = panel.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(500, panelRect.sizeDelta.y);

        // Load Roboto font
        TMP_FontAsset robotoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset");

        // 2. Configure Title Text
        TextMeshProUGUI nameTMP = nameTextObj.GetComponent<TextMeshProUGUI>();
        if (robotoFont != null) nameTMP.font = robotoFont;
        nameTMP.color = new Color(0.2f, 0.9f, 1.0f, 1.0f); // Sleek Cyan
        nameTMP.fontSize = 28;
        nameTMP.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        nameTMP.alignment = TextAlignmentOptions.TopLeft;
        nameTMP.enableAutoSizing = false;

        // 3. Configure Description Text
        TextMeshProUGUI descTMP = descTextObj.GetComponent<TextMeshProUGUI>();
        descTMP.color = new Color(0.9f, 0.9f, 0.9f, 1.0f); // Off-white
        descTMP.fontSize = 18;
        descTMP.fontStyle = FontStyles.Normal;
        descTMP.alignment = TextAlignmentOptions.TopLeft;
        descTMP.enableAutoSizing = false;
        
        // Add a nice visual separator line between Title and Description
        Transform lineTr = panel.transform.Find("SeparatorLine");
        GameObject lineObj;
        if (lineTr == null)
        {
            lineObj = new GameObject("SeparatorLine");
            lineObj.transform.SetParent(panel.transform, false);
            lineObj.transform.SetSiblingIndex(1); // Between Name and Description
        }
        else
        {
            lineObj = lineTr.gameObject;
        }
        
        Image lineImg = lineObj.GetComponent<Image>();
        if (lineImg == null) lineImg = lineObj.AddComponent<Image>();
        lineImg.color = new Color(0.2f, 0.9f, 1.0f, 0.5f); // Semi-transparent cyan line
        LayoutElement le = lineObj.GetComponent<LayoutElement>();
        if (le == null) le = lineObj.AddComponent<LayoutElement>();
        le.minHeight = 2; // 2 pixels thick

        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(nameTextObj);
        EditorUtility.SetDirty(descTextObj);
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        return "UI Made Premium!";
    }
}
