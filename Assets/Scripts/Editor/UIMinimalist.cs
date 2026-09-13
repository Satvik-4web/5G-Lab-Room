using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class UIMinimalist
{
    public static string FixUI()
    {
        GameObject panel = EditorUtility.InstanceIDToObject(58432) as GameObject;
        GameObject nameTextObj = EditorUtility.InstanceIDToObject(57524) as GameObject;
        GameObject descTextObj = EditorUtility.InstanceIDToObject(60350) as GameObject;

        if (panel == null || nameTextObj == null || descTextObj == null) return "Objects missing";

        // 1. Panel Background (Sleek Minimalist Dark Glass)
        Image bg = panel.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0.05f, 0.05f, 0.06f, 0.75f); // Very elegant dark gray/black glass
        
        // Remove cheap looking borders/outlines
        Outline outline = panel.GetComponent<Outline>();
        if (outline != null) Object.DestroyImmediate(outline);
        Shadow shadow = panel.GetComponent<Shadow>();
        if (shadow != null) Object.DestroyImmediate(shadow);

        // Resize to a more elegant widescreen ratio
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(650, 260);

        // Load Roboto fonts
        TMP_FontAsset robotoBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset");
        TMP_FontAsset robotoReg = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Regular SDF.asset");
        
        // 2. Minimalist Title Text
        TextMeshProUGUI nameTMP = nameTextObj.GetComponent<TextMeshProUGUI>();
        if (robotoBold != null) nameTMP.font = robotoBold;
        nameTMP.color = new Color(1f, 1f, 1f, 0.95f); // Crisp white
        nameTMP.fontSize = 30;
        nameTMP.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        nameTMP.characterSpacing = 4; // premium tracking
        
        RectTransform nameRect = nameTextObj.GetComponent<RectTransform>();
        nameRect.anchoredPosition = new Vector2(0, 0);
        nameRect.sizeDelta = new Vector2(0, 50);

        // 3. Minimalist Separator Line
        Transform lineTr = panel.transform.Find("ContentContainer/SeparatorLine");
        if (lineTr != null)
        {
            Image lineImg = lineTr.GetComponent<Image>();
            if (lineImg != null) lineImg.color = new Color(1f, 1f, 1f, 0.2f); // subtle white line
            
            RectTransform lineRect = lineTr.GetComponent<RectTransform>();
            lineRect.anchoredPosition = new Vector2(0, -50);
            lineRect.sizeDelta = new Vector2(0, 1); 
        }

        // 4. Clean Description Text
        TextMeshProUGUI descTMP = descTextObj.GetComponent<TextMeshProUGUI>();
        if (robotoReg != null) descTMP.font = robotoReg; 
        
        descTMP.color = new Color(1f, 1f, 1f, 0.75f); // Soft white for readability
        descTMP.fontSize = 20;
        descTMP.fontStyle = FontStyles.Normal; 
        descTMP.lineSpacing = 10; 
        
        RectTransform descRect = descTextObj.GetComponent<RectTransform>();
        descRect.offsetMax = new Vector2(0, -70);

        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(nameTextObj);
        EditorUtility.SetDirty(descTextObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        return "Minimalist UI Built!";
    }
}
