using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class UIBluishProfessional
{
    public static string FixUI()
    {
        GameObject panel = EditorUtility.InstanceIDToObject(58432) as GameObject;
        GameObject nameTextObj = EditorUtility.InstanceIDToObject(57524) as GameObject;
        GameObject descTextObj = EditorUtility.InstanceIDToObject(60350) as GameObject;

        if (panel == null || nameTextObj == null || descTextObj == null) return "Objects missing";

        // 1. Remove Neon Bar
        Transform leftBarTr = panel.transform.Find("LeftNeonBar");
        if (leftBarTr != null) Object.DestroyImmediate(leftBarTr.gameObject);

        // 2. Adjust Panel Content Padding (Since left bar is gone)
        Transform contentTr = panel.transform.Find("ContentContainer");
        if (contentTr != null)
        {
            RectTransform contentRect = contentTr.GetComponent<RectTransform>();
            contentRect.offsetMin = new Vector2(30, 20); // Balanced 30px padding on left
            contentRect.offsetMax = new Vector2(-30, -20); // Balanced 30px padding on right
        }

        // 3. Panel Background (Deep Cool Navy Blue)
        Image bg = panel.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0.02f, 0.12f, 0.25f, 0.85f);
        
        // Give the panel a subtle elegant light-blue border
        Outline outline = panel.GetComponent<Outline>();
        if (outline == null) outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.6f, 0.9f, 0.4f); // soft ice blue border
        outline.effectDistance = new Vector2(1, -1);

        // Load Roboto font
        TMP_FontAsset robotoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset");
        TMP_FontAsset robotoRegFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Regular SDF.asset");
        
        // 4. Professional Title Text
        TextMeshProUGUI nameTMP = nameTextObj.GetComponent<TextMeshProUGUI>();
        if (robotoFont != null) nameTMP.font = robotoFont;
        nameTMP.color = new Color(0.6f, 0.85f, 1.0f, 1.0f); // Ice Blue
        nameTMP.fontSize = 28;
        nameTMP.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        nameTMP.characterSpacing = 3; // Professional letter spacing
        
        Shadow titleShadow = nameTextObj.GetComponent<Shadow>();
        if (titleShadow != null) Object.DestroyImmediate(titleShadow); // Remove cheap shadow for cleaner look

        // 5. Professional Separator Line
        Transform lineTr = panel.transform.Find("ContentContainer/SeparatorLine");
        if (lineTr != null)
        {
            Image lineImg = lineTr.GetComponent<Image>();
            if (lineImg != null) lineImg.color = new Color(0.3f, 0.6f, 0.9f, 0.6f); // Cool blue separator
            
            RectTransform lineRect = lineTr.GetComponent<RectTransform>();
            lineRect.anchoredPosition = new Vector2(0, -45);
            lineRect.sizeDelta = new Vector2(0, 1); // 1px thin, elegant line
        }

        // 6. Professional Description Text
        TextMeshProUGUI descTMP = descTextObj.GetComponent<TextMeshProUGUI>();
        // Fallback to bold if regular not found, but regular is much better for body text
        if (robotoRegFont != null) descTMP.font = robotoRegFont; 
        else if (robotoFont != null) descTMP.font = robotoFont;
        
        descTMP.color = new Color(0.9f, 0.95f, 1.0f, 1.0f); // Bright ice white
        descTMP.fontSize = 18;
        descTMP.fontStyle = FontStyles.Normal; // Normal weight for readability
        descTMP.lineSpacing = 15; // Elegant spacing between lines
        
        RectTransform descRect = descTextObj.GetComponent<RectTransform>();
        descRect.offsetMax = new Vector2(0, -60);

        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(nameTextObj);
        EditorUtility.SetDirty(descTextObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        return "Bluish Professional UI Built!";
    }
}
