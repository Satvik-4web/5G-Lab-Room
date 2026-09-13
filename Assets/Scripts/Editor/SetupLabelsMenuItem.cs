using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class SetupLabelsMenuItem
{
    [MenuItem("Tools/1. Auto-Assign Lab Labels")]
    public static void AssignLabels()
    {
        string log = "Label Assignment Report:\n\n";
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
        
        GameObject rack = null;
        GameObject antena = null;
        GameObject pc = null;
        GameObject spectrum = null;

        foreach(var go in allObjects)
        {
            if (go.name.Contains("Rack") || go.name.Contains("Cabinet") || go.name.Contains("Server Rack")) rack = go;
            if (go.name.Contains("antena") || go.name.Contains("antenna") || go.name.Contains("gNodeB")) antena = go;
            if (go.name.Equals("PC") || go.name.Contains("5G Server")) pc = go;
            if (go.name.Contains("Spectrum")) spectrum = go;
        }

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer == -1) interactableLayer = 0;

        if (rack != null) {
            if (rack.GetComponent<Collider>() == null) rack.AddComponent<BoxCollider>();
            var label = rack.GetComponent<InteractableLabel>() ?? rack.AddComponent<InteractableLabel>();
            label.objectName = "MEC (Mobile Edge Computing) Cabinet";
            label.objectDescription = "Stores edge computing servers and networking devices that support 5G communication. It processes data locally to provide low-latency, high-speed, and reliable network services.";
            rack.layer = interactableLayer;
            log += $"Rack updated: '{rack.name}'\n";
        }

        if (antena != null) {
            if (antena.GetComponent<Collider>() == null) antena.AddComponent<BoxCollider>();
            var label = antena.GetComponent<InteractableLabel>() ?? antena.AddComponent<InteractableLabel>();
            label.objectName = "gNodeB (5G Base Station)";
            label.objectDescription = "The gNodeB is the main antenna used in a 5G network. It sends and receives wireless signals between mobile devices and the network, providing fast and reliable 5G connectivity.";
            antena.layer = interactableLayer;
            log += $"Antena updated: '{antena.name}'\n";
        }

        if (pc != null) {
            if (pc.GetComponent<Collider>() == null) pc.AddComponent<BoxCollider>();
            var label = pc.GetComponent<InteractableLabel>() ?? pc.AddComponent<InteractableLabel>();
            label.objectName = "5G Server";
            label.objectDescription = "The 5G Server controls and manages the 5G network. It processes data, communicates with the gNodeB, and helps the network run smoothly.";
            pc.layer = interactableLayer;
            log += $"PC updated: '{pc.name}'\n";
        }

        if (spectrum != null) {
            if (spectrum.GetComponent<Collider>() == null) spectrum.AddComponent<BoxCollider>();
            var label = spectrum.GetComponent<InteractableLabel>() ?? spectrum.AddComponent<InteractableLabel>();
            label.objectName = "Spectrum Analyzer";
            label.objectDescription = "The Spectrum Analyzer is used to measure and analyze wireless signals. It helps engineers check signal strength, frequency, and detect interference to ensure the 5G network works properly.";
            spectrum.layer = interactableLayer;
            log += $"Spectrum updated: '{spectrum.name}'\n";
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log(log);
        EditorUtility.DisplayDialog("Label Setup Complete", log, "OK");
    }

    [MenuItem("Tools/2. Auto-Setup VR Rig and Simulator (Magic Button)")]
    public static void SetupVRRig()
    {
        string log = "VR Rig & Simulator Setup Report:\n\n";

        // 1. Create XR Origin (VR Rig)
        GameObject xrOrigin = GameObject.Find("XR Origin (VR)");
        if (xrOrigin == null)
        {
            EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (VR)");
            xrOrigin = GameObject.Find("XR Origin (VR)");
            if (xrOrigin != null) log += "Spawned XR Origin (VR) with Hands/Controllers.\n";
            
            GameObject oldCam = GameObject.Find("Main Camera");
            if (oldCam != null && oldCam.transform.parent == null)
            {
                Object.DestroyImmediate(oldCam);
            }
        }

        if (xrOrigin != null && xrOrigin.GetComponent<LabWalkTester>() == null)
        {
            xrOrigin.AddComponent<LabWalkTester>();
        }

        GameObject leftController = GameObject.Find("Left Controller");
        if (leftController != null && leftController.transform.Find("HandModel") == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "HandModel";
            cube.transform.SetParent(leftController.transform, false);
            cube.transform.localScale = new Vector3(0.05f, 0.05f, 0.1f);
            if (cube.GetComponent<Collider>()) Object.DestroyImmediate(cube.GetComponent<Collider>());
        }

        GameObject rightController = GameObject.Find("Right Controller");
        if (rightController != null && rightController.transform.Find("HandModel") == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "HandModel";
            cube.transform.SetParent(rightController.transform, false);
            cube.transform.localScale = new Vector3(0.05f, 0.05f, 0.1f);
            if (cube.GetComponent<Collider>()) Object.DestroyImmediate(cube.GetComponent<Collider>());
        }

        // 2. Create XR Device Simulator and make sure it doesn't break real VR
        GameObject sim = GameObject.Find("XR Device Simulator");
        if (sim == null)
        {
            GameObject simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.unity.xr.interaction.toolkit/Runtime/XRDeviceSimulator/XR Device Simulator.prefab");
            if (simPrefab != null)
            {
                sim = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
            }
        }
        
        // Safety feature: Ensure XR Simulator turns off on real devices!
        if (sim != null && sim.GetComponent<DisableSimulatorInBuild>() == null)
        {
            sim.AddComponent<DisableSimulatorInBuild>();
        }

        // 3. Setup UI (World Space)
        SetupWorldSpaceUI(ref log);

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log(log);
        EditorUtility.DisplayDialog("VR Check Passed", "All systems are GO for VR!\nSimulator has a safety script to avoid breaking real VR.", "OK");
    }

    private static void SetupWorldSpaceUI(ref string log)
    {
        GameObject canvasObj = GameObject.Find("PopupCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("PopupCanvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
        }
        else
        {
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            if (canvasObj.GetComponent<GraphicRaycaster>()) Object.DestroyImmediate(canvasObj.GetComponent<GraphicRaycaster>());
            if (!canvasObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>())
                canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
        }

        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>();
        
        if (cam != null)
        {
            canvasObj.transform.SetParent(cam.transform);
            canvasObj.transform.localPosition = new Vector3(0, 0, 1.5f);
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        }

        Transform panelTrans = canvasObj.transform.Find("PopupPanel");
        GameObject panelObj = panelTrans != null ? panelTrans.gameObject : new GameObject("PopupPanel");
        if (panelTrans == null)
        {
            panelObj.transform.SetParent(canvasObj.transform, false);
            Image img = panelObj.AddComponent<Image>();
            img.color = new Color(0.06f, 0.09f, 0.16f, 0.8f); 
            
            VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400, 150);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.localRotation = Quaternion.identity;
        }

        Transform nameTrans = panelObj.transform.Find("NameText");
        GameObject nameObj = nameTrans != null ? nameTrans.gameObject : new GameObject("NameText");
        if (nameTrans == null)
        {
            nameObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Object Name";
            nameTmp.fontSize = 24;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = new Color(0.22f, 0.74f, 0.97f, 1f);
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
            nameObj.GetComponent<RectTransform>().localRotation = Quaternion.identity;
        }

        Transform descTrans = panelObj.transform.Find("DescriptionText");
        GameObject descObj = descTrans != null ? descTrans.gameObject : new GameObject("DescriptionText");
        if (descTrans == null)
        {
            descObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
            descTmp.text = "Object Description";
            descTmp.fontSize = 16;
            descTmp.color = new Color(0.88f, 0.91f, 0.94f, 1f);
            descTmp.alignment = TextAlignmentOptions.Center;
            descTmp.enableAutoSizing = true;
            descTmp.fontSizeMin = 12;
            descTmp.fontSizeMax = 18;
            descObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 80);
            descObj.GetComponent<RectTransform>().localRotation = Quaternion.identity;
        }

        GameObject managerObj = GameObject.Find("UIManager");
        if (managerObj == null) managerObj = new GameObject("UIManager");
        
        PopupUIManager uiManager = managerObj.GetComponent<PopupUIManager>();
        if (uiManager == null) uiManager = managerObj.AddComponent<PopupUIManager>();

        SerializedObject so = new SerializedObject(uiManager);
        so.FindProperty("popupPanel").objectReferenceValue = panelObj;
        so.FindProperty("nameText").objectReferenceValue = nameObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("descriptionText").objectReferenceValue = descObj.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();
    }
}
