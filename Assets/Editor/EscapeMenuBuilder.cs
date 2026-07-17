using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using TMPro.EditorUtilities;
using System.Collections.Generic;
using StarterAssets;
using Naxestra.UI;

// Bygger hela Escape-menyns Canvas/panel-hierarki programmatiskt i den aktiva scenen.
// Kör via menyn: Tools > Naxestra > Build Escape Menu
public static class EscapeMenuBuilder
{
    private const string CanvasName = "EscapeMenuCanvas";

    [MenuItem("Tools/Naxestra/Build Escape Menu")]
    public static void Build()
    {
        GameObject existing = GameObject.Find(CanvasName);
        if (existing != null)
        {
            bool rebuild = EditorUtility.DisplayDialog(
                "Escape-menyn finns redan",
                $"Ett objekt som heter \"{CanvasName}\" finns redan i scenen. Vill du ta bort det och bygga om från grunden?",
                "Bygg om", "Avbryt");
            if (!rebuild) return;
            Undo.DestroyObjectImmediate(existing);
        }

        EnsureEventSystem();

        GameObject canvasGO = CreateCanvas();

        GameObject pauseMenuGO = new GameObject("PauseMenu", typeof(RectTransform));
        pauseMenuGO.transform.SetParent(canvasGO.transform, false);
        StretchFull(pauseMenuGO.GetComponent<RectTransform>());
        PauseMenuController controller = pauseMenuGO.AddComponent<PauseMenuController>();

        GameObject pauseRoot = CreatePauseRoot(pauseMenuGO.transform);
        GameObject container = CreateContainer(pauseRoot.transform);

        GameObject mainPanel = CreateMainPanel(container.transform, controller);
        GameObject optionsPanel = CreateOptionsPanel(container.transform, controller);
        GameObject gameplayPanel = CreateGameplayPanel(container.transform, controller);

        OptionsMenuController optionsController = optionsPanel.GetComponent<OptionsMenuController>();
        GameObject editModePanel = CreateEditModePanel(pauseRoot.transform, controller, optionsController);

        optionsPanel.SetActive(false);
        gameplayPanel.SetActive(false);
        editModePanel.SetActive(false);
        pauseRoot.SetActive(false);

        WireController(controller, pauseRoot, mainPanel, optionsPanel, gameplayPanel, editModePanel);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGO;

        EditorUtility.DisplayDialog("Klart", "Escape-menyn är byggd. Spara scenen med Ctrl+S.", "OK");
    }

    // Skapar en permanent "PlayerHudCanvas" i Edit mode så att:
    // 1) Du kan koppla dess Canvas Scaler till OptionsPanel > Options Menu Controller > Hud Canvas Scaler redan nu.
    // 2) PlayerHudUI.cs / HotbarUI.cs (efter namn-fixen i EnsureCanvas()) hittar och återanvänder
    //    just detta Canvas i Play mode istället för att skapa ett eget.
    [MenuItem("Tools/Naxestra/Create PlayerHudCanvas Placeholder")]
    public static void CreateHudCanvasPlaceholder()
    {
        GameObject existing = GameObject.Find("PlayerHudCanvas");
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Finns redan", "Ett GameObject som heter \"PlayerHudCanvas\" finns redan i scenen. Inget nytt skapat.", "OK");
            Selection.activeGameObject = existing;
            return;
        }

        GameObject go = new GameObject("PlayerHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create PlayerHudCanvas Placeholder");

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = go;

        EditorUtility.DisplayDialog("Klart", "PlayerHudCanvas skapad. Spara scenen med Ctrl+S.", "OK");
    }

    // ---------- Toppnivå ----------

    private static void EnsureEventSystem()
    {
        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        if (es != null) return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
    }

    private static GameObject CreateCanvas()
    {
        GameObject go = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create Escape Menu Canvas");

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560, 1440);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return go;
    }

    private static GameObject CreatePauseRoot(Transform parent)
    {
        GameObject go = new GameObject("PauseRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        StretchFull(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 180f / 255f);

        // Egen dimmer-tema-asset (INTE samma som GetFantasyPanelTheme()) med flit — PauseRoot
        // ska mörklägga hela skärmen, inte visa samma pergament-panel som MainPanel/OptionsPanel/
        // GameplayPanel. Genom att peka på en separat, namngiven asset (som du duplicerar och gör
        // svart/halvgenomskinlig en gång) överlever din anpassning varje ombyggnad av menyn —
        // .asset-filen ligger kvar på disk oavsett hur många gånger Build() river/skapar om scenen.
        ThemedPanel themedPanel = go.AddComponent<ThemedPanel>();
        themedPanel.theme = GetFantasyPauseDimTheme();
        themedPanel.Apply();

        return go;
    }

    private static GameObject CreateContainer(Transform parent)
    {
        GameObject go = new GameObject("PanelContainer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(560, 820);
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    // ---------- Paneler ----------

    private static GameObject CreateMainPanel(Transform parent, PauseMenuController controller)
    {
        GameObject panel = CreatePanelBase("MainPanel", parent, TextAnchor.MiddleCenter);
        ButtonTheme buttonTheme = GetFantasyButtonTheme();

        CreateMenuButton(panel.transform, "Resume", controller.OnResume, buttonTheme);
        CreateMenuButton(panel.transform, "Logout", controller.OnLogout, buttonTheme);
        CreateMenuButton(panel.transform, "Options", controller.ShowOptions, buttonTheme);
        CreateMenuButton(panel.transform, "Gameplay", controller.ShowGameplay, buttonTheme);
        CreateMenuButton(panel.transform, "Exit", controller.OnExit, buttonTheme);

        return panel;
    }

    private static GameObject CreateOptionsPanel(Transform parent, PauseMenuController controller)
    {
        GameObject panel = CreatePanelBase("OptionsPanel", parent, TextAnchor.UpperCenter);

        CreateLabel(panel.transform, "Options", 32);

        Slider volumeSlider = CreateLabeledSlider(panel.transform, "Master Volume", 0f, 1f, 1f);
        Toggle fullscreenToggle = CreateLabeledToggle(panel.transform, "Fullscreen", Screen.fullScreen);
        TMP_Dropdown qualityDropdown = CreateLabeledDropdown(panel.transform, "Quality");
        TMP_Dropdown resolutionDropdown = CreateLabeledDropdown(panel.transform, "Resolution");

        CreateMenuButton(panel.transform, "Edit Mode", controller.ShowEditMode);
        CreateMenuButton(panel.transform, "Back", controller.ShowMain, GetFantasyButtonTheme());

        OptionsMenuController optionsController = panel.AddComponent<OptionsMenuController>();

        SerializedObject so = new SerializedObject(optionsController);
        so.FindProperty("masterVolumeSlider").objectReferenceValue = volumeSlider;
        so.FindProperty("qualityDropdown").objectReferenceValue = qualityDropdown;
        so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreenToggle;
        so.FindProperty("resolutionDropdown").objectReferenceValue = resolutionDropdown;
        so.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    // Edit Mode-panelen ligger direkt under pauseRoot (INTE i det centrerade PanelContainer)
    // så den kan ankras till ett hörn av skärmen och lämna resten av skärmbilden fri —
    // det är där det riktiga HUD:et/Hotbaren/Enemy HUD:et (på ett annat Canvas) syns och går att dra.
    private static GameObject CreateEditModePanel(Transform parent, PauseMenuController controller, OptionsMenuController optionsController)
    {
        GameObject panel = new GameObject("EditModePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(380, 780);
        rt.anchoredPosition = new Vector2(-24, -24);

        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(24, 24, 20, 20);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        CreateLabel(panel.transform, "Edit HUD Layout", 26);
        CreateLabel(panel.transform, "Drag the HUD, Hotbar and Enemy HUD to move them", 14);

        Slider hudScaleSlider = CreateLabeledSlider(panel.transform, "HUD Scale", 0.8f, 1.4f, 1f);
        Slider hotbarScaleSlider = CreateLabeledSlider(panel.transform, "Hotbar Scale", 0.8f, 1.4f, 1f);
        Slider enemyHudScaleSlider = CreateLabeledSlider(panel.transform, "Enemy HUD Scale", 0.8f, 1.4f, 1f);

        // Kolumn-layout för hotbaren (t.ex. 1x8/2x4/4x2/8x1) — inte gated bakom Full Edit Mode,
        // eftersom Ability- och Quick Item-grupperna redan går att dra fritt utan den.
        TMP_Dropdown abilityLayoutDropdown = CreateLabeledDropdown(panel.transform, "Ability Slot Layout");
        TMP_Dropdown quickItemLayoutDropdown = CreateLabeledDropdown(panel.transform, "Quick Item Layout");

        Toggle fullEditModeToggle = CreateLabeledToggle(panel.transform, "Full Edit Mode", false);

        GameObject subPanel = new GameObject("FullEditModeSubPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
        subPanel.transform.SetParent(panel.transform, false);
        VerticalLayoutGroup subVlg = subPanel.GetComponent<VerticalLayoutGroup>();
        subVlg.childAlignment = TextAnchor.UpperCenter;
        subVlg.spacing = 10;
        subVlg.childControlWidth = true;
        subVlg.childControlHeight = false;
        subVlg.childForceExpandWidth = true;
        subVlg.childForceExpandHeight = false;
        ContentSizeFitter subFitter = subPanel.AddComponent<ContentSizeFitter>();
        subFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Slider portraitScaleSlider = CreateLabeledSlider(subPanel.transform, "Portrait Scale", 0.5f, 2.5f, 1f);
        Slider abilityScaleSlider = CreateLabeledSlider(subPanel.transform, "Ability Scale", 0.5f, 2.5f, 1f);
        Slider quickScaleSlider = CreateLabeledSlider(subPanel.transform, "Quick Item Scale", 0.5f, 2.5f, 1f);
        Toggle healthNumbersToggle = CreateLabeledToggle(subPanel.transform, "Show Health Numbers", true);
        Toggle rageNumbersToggle = CreateLabeledToggle(subPanel.transform, "Show Rage Numbers", true);
        Toggle xpNumbersToggle = CreateLabeledToggle(subPanel.transform, "Show XP Numbers", true);

        subPanel.SetActive(false);

        HudEditModeController editController = panel.AddComponent<HudEditModeController>();

        CreateMenuButton(panel.transform, "Standard", editController.OnStandardClick);
        CreateMenuButton(panel.transform, "Close", controller.ShowOptions);

        SerializedObject editSo = new SerializedObject(editController);
        editSo.FindProperty("optionsController").objectReferenceValue = optionsController;
        editSo.FindProperty("fullEditModeToggle").objectReferenceValue = fullEditModeToggle;
        editSo.FindProperty("fullEditModeSubPanel").objectReferenceValue = subPanel;
        editSo.FindProperty("portraitScaleSlider").objectReferenceValue = portraitScaleSlider;
        editSo.FindProperty("abilityScaleSlider").objectReferenceValue = abilityScaleSlider;
        editSo.FindProperty("quickScaleSlider").objectReferenceValue = quickScaleSlider;
        editSo.FindProperty("healthNumbersToggle").objectReferenceValue = healthNumbersToggle;
        editSo.FindProperty("rageNumbersToggle").objectReferenceValue = rageNumbersToggle;
        editSo.FindProperty("xpNumbersToggle").objectReferenceValue = xpNumbersToggle;
        editSo.FindProperty("abilityLayoutDropdown").objectReferenceValue = abilityLayoutDropdown;
        editSo.FindProperty("quickItemLayoutDropdown").objectReferenceValue = quickItemLayoutDropdown;
        editSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject optSo = new SerializedObject(optionsController);
        optSo.FindProperty("hudScaleSlider").objectReferenceValue = hudScaleSlider;
        optSo.FindProperty("hotbarScaleSlider").objectReferenceValue = hotbarScaleSlider;
        optSo.FindProperty("enemyHudScaleSlider").objectReferenceValue = enemyHudScaleSlider;
        optSo.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    private static GameObject CreateGameplayPanel(Transform parent, PauseMenuController controller)
    {
        GameObject panel = CreatePanelBase("GameplayPanel", parent, TextAnchor.MiddleCenter);

        CreateLabel(panel.transform, "Gameplay (kommer snart)", 26);
        CreateMenuButton(panel.transform, "Back", controller.ShowMain);

        return panel;
    }

    private static GameObject CreatePanelBase(string name, Transform parent, TextAnchor alignment)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);
        StretchFull(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = alignment;
        vlg.spacing = 14;
        vlg.padding = new RectOffset(40, 40, 30, 30);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ThemedPanel themedPanel = panel.AddComponent<ThemedPanel>();
        themedPanel.theme = GetFantasyPanelTheme();
        themedPanel.Apply();

        return panel;
    }

    private static void WireController(PauseMenuController controller, GameObject pauseRoot, GameObject mainPanel, GameObject optionsPanel, GameObject gameplayPanel, GameObject editModePanel)
    {
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("pauseRoot").objectReferenceValue = pauseRoot;
        so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        so.FindProperty("optionsPanel").objectReferenceValue = optionsPanel;
        so.FindProperty("gameplayPanel").objectReferenceValue = gameplayPanel;
        so.FindProperty("editModePanel").objectReferenceValue = editModePanel;
        so.FindProperty("pauseBackgroundImage").objectReferenceValue = pauseRoot.GetComponent<Image>();

        // Auto-hittar spelarens input/kamera-script i den öppna scenen istället för att
        // kräva manuell Inspector-koppling efter varje ombyggnad (tidigare känd fälla).
        SerializedProperty disableProp = so.FindProperty("disableWhileOpen");
        List<Behaviour> toDisable = new List<Behaviour>();

        ThirdPersonController tpc = Object.FindFirstObjectByType<ThirdPersonController>();
        if (tpc != null) toDisable.Add(tpc); else Debug.LogWarning("EscapeMenuBuilder: Ingen ThirdPersonController hittades i scenen — lades inte till i Disable While Open.");

        CameraZoom camZoom = Object.FindFirstObjectByType<CameraZoom>();
        if (camZoom != null) toDisable.Add(camZoom); else Debug.LogWarning("EscapeMenuBuilder: Ingen CameraZoom hittades i scenen — lades inte till i Disable While Open.");

        TargetingController targeting = Object.FindFirstObjectByType<TargetingController>();
        if (targeting != null) toDisable.Add(targeting); else Debug.LogWarning("EscapeMenuBuilder: Ingen TargetingController hittades i scenen — lades inte till i Disable While Open.");

        disableProp.arraySize = toDisable.Count;
        for (int i = 0; i < toDisable.Count; i++)
            disableProp.GetArrayElementAtIndex(i).objectReferenceValue = toDisable[i];

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Byggstenar (knapp/label/slider/toggle/dropdown) ----------

    private static TMP_DefaultControls.Resources GetTmpResources()
    {
        return new TMP_DefaultControls.Resources();
    }

    private static Button CreateMenuButton(Transform parent, string label, UnityAction callback, ButtonTheme theme = null)
    {
        GameObject go = TMP_DefaultControls.CreateButton(GetTmpResources());
        go.name = label + "Button";
        go.transform.SetParent(parent, false);

        RectTransform buttonRt = go.GetComponent<RectTransform>();
        buttonRt.sizeDelta = new Vector2(buttonRt.sizeDelta.x, 56);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 56;
        le.minHeight = 56;

        TextMeshProUGUI tmpText = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = label;
            tmpText.fontSize = 28;
        }

        Button button = go.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, callback);
        int idx = button.onClick.GetPersistentEventCount() - 1;
        button.onClick.SetPersistentListenerState(idx, UnityEventCallState.RuntimeOnly);

        if (theme != null)
        {
            ThemedButton themedButton = go.AddComponent<ThemedButton>();
            themedButton.theme = theme;
            themedButton.Apply();
        }

        return button;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string text, int fontSize)
    {
        GameObject go = new GameObject(text + "Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform labelRt = go.GetComponent<RectTransform>();
        labelRt.sizeDelta = new Vector2(labelRt.sizeDelta.x, fontSize + 12);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 12;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return tmp;
    }

    private static Slider CreateLabeledSlider(Transform parent, string label, float min, float max, float value)
    {
        CreateLabel(parent, label, 20);

        GameObject sliderGO = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderGO.name = label + "Slider";
        sliderGO.transform.SetParent(parent, false);

        RectTransform sliderRt = sliderGO.GetComponent<RectTransform>();
        sliderRt.sizeDelta = new Vector2(sliderRt.sizeDelta.x, 24);

        LayoutElement le = sliderGO.AddComponent<LayoutElement>();
        le.preferredHeight = 24;

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        return slider;
    }

    // Byggd från grunden (inte DefaultControls.CreateToggle) — den varianten använder tomma
    // Sprite-referenser i det här projektet, vilket gjorde bocken i kryssrutan osynlig och
    // hela kontrollen väldigt liten. Den här ger en garanterat synlig, tydligt större ruta.
    private static Toggle CreateLabeledToggle(Transform parent, string label, bool value)
    {
        const float boxSize = 30f;

        GameObject toggleGO = new GameObject(label + "Toggle", typeof(RectTransform));
        toggleGO.transform.SetParent(parent, false);

        RectTransform toggleRt = toggleGO.GetComponent<RectTransform>();
        toggleRt.sizeDelta = new Vector2(toggleRt.sizeDelta.x, boxSize);

        LayoutElement le = toggleGO.AddComponent<LayoutElement>();
        le.preferredHeight = boxSize;

        GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        RectTransform bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.SetParent(toggleRt, false);
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(0f, 0.5f);
        bgRt.pivot = new Vector2(0f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(boxSize, boxSize);
        Image bgImage = bgGO.GetComponent<Image>();
        bgImage.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        GameObject checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        RectTransform checkRt = checkGO.GetComponent<RectTransform>();
        checkRt.SetParent(bgRt, false);
        checkRt.anchorMin = Vector2.zero;
        checkRt.anchorMax = Vector2.one;
        checkRt.offsetMin = new Vector2(5f, 5f);
        checkRt.offsetMax = new Vector2(-5f, -5f);
        Image checkImage = checkGO.GetComponent<Image>();
        checkImage.color = new Color(0.3f, 0.8f, 1f, 1f);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        RectTransform labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.SetParent(toggleRt, false);
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.offsetMin = new Vector2(boxSize + 12f, 0f);
        labelRt.offsetMax = Vector2.zero;
        TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 20;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        Toggle toggle = toggleGO.AddComponent<Toggle>();
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        toggle.isOn = value;

        return toggle;
    }

    private static TMP_Dropdown CreateLabeledDropdown(Transform parent, string label)
    {
        CreateLabel(parent, label, 20);

        GameObject ddGO = TMP_DefaultControls.CreateDropdown(GetTmpResources());
        ddGO.name = label + "Dropdown";
        ddGO.transform.SetParent(parent, false);

        RectTransform ddRt = ddGO.GetComponent<RectTransform>();
        ddRt.sizeDelta = new Vector2(ddRt.sizeDelta.x, 36);

        LayoutElement le = ddGO.AddComponent<LayoutElement>();
        le.preferredHeight = 36;

        return ddGO.GetComponent<TMP_Dropdown>();
    }

    // ---------- Hjälpare ----------

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ---------- UI-teman (Fantasy Wooden GUI) ----------
    // Laddas via AssetDatabase istället för direkta fältreferenser, så Build() inte kastar
    // fel om NaxestraThemeSetup (Tools > Naxestra > Setup Fantasy Wooden Theme) inte körts än
    // — knapparna/panelerna får då bara ingen ThemedButton/ThemedPanel-koppling denna gång.
    private const string ThemesFolder = "Assets/Themes/FantasyWooden";

    private static ButtonTheme GetFantasyButtonTheme()
    {
        return AssetDatabase.LoadAssetAtPath<ButtonTheme>(ThemesFolder + "/FantasyWooden_Button.asset");
    }

    private static PanelTheme GetFantasyPanelTheme()
    {
        return AssetDatabase.LoadAssetAtPath<PanelTheme>(ThemesFolder + "/FantasyWooden_Panel.asset");
    }

    // Separat asset för PauseRoots helskärms-dimmer (se CreatePauseRoot). Skapa den genom att
    // duplicera FantasyWooden_Panel.asset, döpa kopian till exakt "FantasyWooden_Panel_Dim",
    // rensa Background Sprite (None) och sätta Tint till svart med låg alpha (~180/255).
    // Finns den inte än (fil saknas) blir themedPanel.theme null — PauseRoot behåller då bara
    // sin vanliga hårdkodade svarta färg, precis som innan temasystemet fanns.
    private static PanelTheme GetFantasyPauseDimTheme()
    {
        return AssetDatabase.LoadAssetAtPath<PanelTheme>(ThemesFolder + "/FantasyWooden_Panel_Dim.asset");
    }
}
