using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class UIUltraFixer
{
    public static string FixUI()
    {
        GameObject panel = EditorUtility.InstanceIDToObject(58432) as GameObject;
        GameObject nameTextObj = EditorUtility.InstanceIDToObject(57524) as GameObject;
        GameObject descTextObj = EditorUtility.InstanceIDToObject(60350) as GameObject;

        if (panel == null || nameTextObj == null || descTextObj == null) return "UI Objects not found";

        // 1. Panel Background - Horizontal Layout
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.02f, 0.05f, 0.1f, 0.95f); // Very dark navy blue
        
        // Switch Panel to HorizontalLayoutGroup
        VerticalLayoutGroup oldVlg = panel.GetComponent<VerticalLayoutGroup>();
        if (oldVlg != null) Object.DestroyImmediate(oldVlg, true);
        
        HorizontalLayoutGroup hlg = panel.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = panel.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 30, 0, 0); // padding on right
        hlg.spacing = 20;
        hlg.childControlHeight = true;
        hlg.childControlWidth = false;
        hlg.childForceExpandHeight = true;

        // 2. Add Left Neon Bar
        Transform leftBarTr = panel.transform.Find("LeftNeonBar");
        GameObject leftBar;
        if (leftBarTr == null)
        {
            leftBar = new GameObject("LeftNeonBar");
            leftBar.transform.SetParent(panel.transform, false);
            leftBar.transform.SetSiblingIndex(0); // Very first child
        }
        else { leftBar = leftBarTr.gameObject; }

        Image leftBarImg = leftBar.GetComponent<Image>();
        if (leftBarImg == null) leftBarImg = leftBar.AddComponent<Image>();
        leftBarImg.color = new Color(0.0f, 1.0f, 0.8f, 1.0f); // Bright Cyan/Teal
        
        LayoutElement leLeft = leftBar.GetComponent<LayoutElement>();
        if (leLeft == null) leLeft = leftBar.AddComponent<LayoutElement>();
        leLeft.minWidth = 8; // 8px thick vertical line
        
        // Add glow (Outline) to Left Bar
        Outline outline = leftBar.GetComponent<Outline>();
        if (outline == null) outline = leftBar.AddComponent<Outline>();
        outline.effectColor = new Color(0.0f, 1.0f, 0.8f, 0.5f);
        outline.effectDistance = new Vector2(4, 0);

        // 3. Create Content Container
        Transform contentTr = panel.transform.Find("ContentContainer");
        GameObject contentContainer;
        if (contentTr == null)
        {
            contentContainer = new GameObject("ContentContainer");
            contentContainer.transform.SetParent(panel.transform, false);
            contentContainer.transform.SetSiblingIndex(1); // Second child
            
            // Move texts into content container
            nameTextObj.transform.SetParent(contentContainer.transform, false);
            Transform lineTr = panel.transform.Find("SeparatorLine");
            if (lineTr != null) lineTr.SetParent(contentContainer.transform, false);
            descTextObj.transform.SetParent(contentContainer.transform, false);
        }
        else { contentContainer = contentTr.gameObject; }

        VerticalLayoutGroup contentVlg = contentContainer.GetComponent<VerticalLayoutGroup>();
        if (contentVlg == null) contentVlg = contentContainer.AddComponent<VerticalLayoutGroup>();
        contentVlg.padding = new RectOffset(0, 0, 25, 25);
        contentVlg.spacing = 10;
        contentVlg.childAlignment = TextAnchor.UpperLeft;
        contentVlg.childControlHeight = true;
        contentVlg.childControlWidth = true;
        contentVlg.childForceExpandHeight = false;
        
        LayoutElement leContent = contentContainer.GetComponent<LayoutElement>();
        if (leContent == null) leContent = contentContainer.AddComponent<LayoutElement>();
        leContent.flexibleWidth = 1;

        // 4. Enhance Texts
        TextMeshProUGUI nameTMP = nameTextObj.GetComponent<TextMeshProUGUI>();
        nameTMP.color = new Color(0.0f, 1.0f, 0.8f, 1.0f); // Match left bar
        nameTMP.characterSpacing = 2; // slightly spaced out for tech feel
        
        // Add drop shadow to texts
        Shadow titleShadow = nameTextObj.GetComponent<Shadow>();
        if (titleShadow == null) titleShadow = nameTextObj.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0, 0.5f, 0.5f, 0.5f);
        titleShadow.effectDistance = new Vector2(2, -2);
        
        TextMeshProUGUI descTMP = descTextObj.GetComponent<TextMeshProUGUI>();
        descTMP.color = new Color(0.85f, 0.95f, 1.0f, 1.0f); // very light blue-white
        descTMP.lineSpacing = 10; // More breathing room between lines
        
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(nameTextObj);
        EditorUtility.SetDirty(descTextObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        return "Ultra Premium UI Built!";
    }
}
