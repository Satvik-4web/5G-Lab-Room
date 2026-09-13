using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class UIIndustrialLab
{
    public static string BuildUI()
    {
        GameObject panel = EditorUtility.InstanceIDToObject(58432) as GameObject;
        GameObject nameTextObj = EditorUtility.InstanceIDToObject(57524) as GameObject;
        GameObject descTextObj = EditorUtility.InstanceIDToObject(60350) as GameObject;

        if (panel == null || nameTextObj == null || descTextObj == null) return "Objects missing";

        // Remove old custom wrappers/content containers if they exist
        Transform oldContent = panel.transform.Find("ContentContainer");
        if (oldContent != null)
        {
            // Move text objects back to panel root
            nameTextObj.transform.SetParent(panel.transform, false);
            Transform oldLine = oldContent.Find("SeparatorLine");
            if (oldLine != null) oldLine.SetParent(panel.transform, false);
            descTextObj.transform.SetParent(panel.transform, false);
            Object.DestroyImmediate(oldContent.gameObject);
        }

        // 1. Main Panel (Background)
        Image bg = panel.GetComponent<Image>();
        if (bg == null) bg = panel.AddComponent<Image>();
        bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bg.type = Image.Type.Sliced;
        
        Color utilityCharcoal;
        ColorUtility.TryParseHtmlString("#1A1A1A", out utilityCharcoal);
        utilityCharcoal.a = 0.85f;
        bg.color = utilityCharcoal;
        
        // Remove old components
        foreach (var comp in panel.GetComponents<Outline>()) Object.DestroyImmediate(comp, true);
        foreach (var comp in panel.GetComponents<Shadow>()) Object.DestroyImmediate(comp, true);

        // Layout Setup
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(450, panelRect.sizeDelta.y); // Set fixed width, height dynamic

        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(15, 15, 15, 15);
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        ContentSizeFitter csf = panel.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = panel.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Fonts
        TMP_FontAsset robotoBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset");
        TMP_FontAsset robotoReg = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Regular SDF.asset");
        
        // 2. Title Text
        TextMeshProUGUI nameTMP = nameTextObj.GetComponent<TextMeshProUGUI>();
        if (robotoBold != null) nameTMP.font = robotoBold;
        nameTMP.text = "SPECTRUM ANALYZER";
        nameTMP.color = Color.white; 
        nameTMP.fontSize = 24;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.alignment = TextAlignmentOptions.TopLeft;
        nameTMP.characterSpacing = 0;
        
        nameTextObj.transform.SetSiblingIndex(0); // Top

        // 3. Divider Line
        Transform lineTr = panel.transform.Find("SeparatorLine");
        GameObject lineObj = lineTr != null ? lineTr.gameObject : new GameObject("SeparatorLine");
        lineObj.transform.SetParent(panel.transform, false);
        lineObj.transform.SetSiblingIndex(1); // 2nd
        
        Image lineImg = lineObj.GetComponent<Image>();
        if (lineImg == null) lineImg = lineObj.AddComponent<Image>();
        Color utilityGray;
        ColorUtility.TryParseHtmlString("#555555", out utilityGray);
        lineImg.color = utilityGray;
        
        LayoutElement lineLE = lineObj.GetComponent<LayoutElement>();
        if (lineLE == null) lineLE = lineObj.AddComponent<LayoutElement>();
        lineLE.minHeight = 2; // 2 pixels
        lineLE.flexibleHeight = 0;

        // 4. Description and Specs (using Rich Text for formatting within one block for elegance)
        TextMeshProUGUI descTMP = descTextObj.GetComponent<TextMeshProUGUI>();
        if (robotoReg != null) descTMP.font = robotoReg; 
        
        string specsText = "<color=#8AB4F8>• Freq Range: 2 Hz - 85 GHz\n• Res BW: 1 Hz to 10 MHz\n• Status: ONLINE</color>";
        string descText = "<color=#E0E0E0><size=80%>Diagnostic tool used to visualize and measure radio frequency (RF) signals, monitor 5G strength, and detect interference.</size></color>";
        
        descTMP.text = specsText + "\n\n" + descText;
        descTMP.color = Color.white; // Base color overridden by rich text
        descTMP.fontSize = 20;
        descTMP.fontStyle = FontStyles.Normal; 
        descTMP.lineSpacing = 0; 
        descTMP.alignment = TextAlignmentOptions.TopLeft;
        
        descTextObj.transform.SetSiblingIndex(2); // Bottom

        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(nameTextObj);
        EditorUtility.SetDirty(descTextObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        return "Industrial UI Built!";
    }
}
