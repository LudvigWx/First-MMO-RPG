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

// Bygger hela Journal-menyns Canvas/panel-hierarki programmatiskt i den aktiva scenen, samma
// metod som EscapeMenuBuilder.cs. Kör via menyn: Tools > Naxestra > Build Journal Menu
//
// OBS: PlayerStatusRowUI (Currency/Karma-statusraden) byggs INTE av detta verktyg - den är en
// självbyggande runtime-komponent (samma mönster som PlayerHudUI/HotbarUI) som läggs till
// manuellt på ett tomt GameObject i scenen, se rapporten efter körning.
public static class JournalMenuBuilder
{
    private const string CanvasName = "JournalMenuCanvas";

    private class TabBarRefs
    {
        public Outline questsOutline;
        public Outline inventoryOutline;
        public Outline mapOutline;
        public Outline characterOutline;
    }

    [MenuItem("Tools/Naxestra/Build Journal Menu")]
    public static void Build()
    {
        GameObject existing = GameObject.Find(CanvasName);
        if (existing != null)
        {
            bool rebuild = EditorUtility.DisplayDialog(
                "Journal-menyn finns redan",
                $"Ett objekt som heter \"{CanvasName}\" finns redan i scenen. Vill du ta bort det och bygga om från grunden?",
                "Bygg om", "Avbryt");
            if (!rebuild) return;
            Undo.DestroyObjectImmediate(existing);
        }

        EnsureEventSystem();

        GameObject canvasGO = CreateCanvas();

        GameObject journalGO = new GameObject("Journal", typeof(RectTransform));
        journalGO.transform.SetParent(canvasGO.transform, false);
        StretchFull(journalGO.GetComponent<RectTransform>());
        JournalController controller = journalGO.AddComponent<JournalController>();

        GameObject journalRoot = CreateJournalRoot(journalGO.transform);

        CreateTabBar(journalRoot.transform, controller, out TabBarRefs tabRefs);

        GameObject panelContainer = CreatePanelContainer(journalRoot.transform);

        GameObject questsPanel = CreateQuestsPanel(panelContainer.transform);
        GameObject inventoryPanel = CreateInventoryPanel(panelContainer.transform, out RectTransform inventorySoloSlot);
        GameObject mapPanel = CreateMapPanel(panelContainer.transform);
        GameObject characterPanel = CreateCharacterPanel(panelContainer.transform, out RectTransform inventorySplitSlot);

        InventoryPanelView inventoryPanelView = CreateInventoryPanelView(inventorySoloSlot.transform);

        questsPanel.SetActive(true);
        inventoryPanel.SetActive(false);
        mapPanel.SetActive(false);
        characterPanel.SetActive(false);
        journalRoot.SetActive(false);

        WireController(controller, journalRoot, questsPanel, inventoryPanel, mapPanel, characterPanel,
                       inventoryPanelView, inventorySoloSlot, inventorySplitSlot, tabRefs);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGO;

        EditorUtility.DisplayDialog("Klart",
            "Journal-menyn är byggd. Spara scenen med Ctrl+S.\n\n" +
            "OBS: Currency/Karma-statusraden (PlayerStatusRowUI) byggs INTE av detta verktyg - " +
            "lägg till det scriptet manuellt på ett tomt GameObject, samma sätt som PlayerHudUI/HotbarUI.",
            "OK");
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
        Undo.RegisterCreatedObjectUndo(go, "Create Journal Menu Canvas");

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560, 1440);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return go;
    }

    // Helskärms-dimmer bakom Journal-panelerna, samma stil/tema-asset som PauseRoot
    // (FantasyWooden_Panel_Dim) - se EscapeMenuBuilder.CreatePauseRoot för resonemanget.
    private static GameObject CreateJournalRoot(Transform parent)
    {
        GameObject go = new GameObject("JournalRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        StretchFull(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 180f / 255f);

        ThemedPanel themedPanel = go.AddComponent<ThemedPanel>();
        themedPanel.theme = GetFantasyPauseDimTheme();
        themedPanel.Apply();

        return go;
    }

    // ---------- Toppmeny (horisontell flikrad) ----------

    private static GameObject CreateTabBar(Transform parent, JournalController controller, out TabBarRefs refs)
    {
        GameObject bar = new GameObject("TabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        bar.transform.SetParent(parent, false);

        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -20f);

        HorizontalLayoutGroup layout = bar.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.padding = new RectOffset(20, 20, 10, 10);
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = bar.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ButtonTheme theme = GetFantasyButtonTheme();

        refs = new TabBarRefs();
        CreateTabButton(bar.transform, "Quests", controller.OnClickQuestsTab, theme, out refs.questsOutline);
        CreateTabButton(bar.transform, "Inventory", controller.OnClickInventoryTab, theme, out refs.inventoryOutline);
        CreateTabButton(bar.transform, "Map", controller.OnClickMapTab, theme, out refs.mapOutline);
        CreateTabButton(bar.transform, "Character", controller.OnClickCharacterTab, theme, out refs.characterOutline);

        // Stäng-knapp längst till höger om SAMMA rad (X) - inget separat kluster av knappar.
        CreateMenuButton(bar.transform, "X", controller.OnClickClose, theme);

        return bar;
    }

    private static Button CreateTabButton(Transform parent, string label, UnityAction callback, ButtonTheme theme, out Outline outline)
    {
        Button button = CreateMenuButton(parent, label, callback, theme);
        outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);
        outline.enabled = false;
        return button;
    }

    // ---------- Panel-container + de fyra panelerna ----------

    private static GameObject CreatePanelContainer(Transform parent)
    {
        GameObject go = new GameObject("PanelContainer", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.05f);
        rt.anchorMax = new Vector2(0.9f, 0.85f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return go;
    }

    private static GameObject CreateQuestsPanel(Transform parent)
    {
        GameObject panel = new GameObject("QuestsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        StretchFull(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        ThemedPanel themedPanel = panel.AddComponent<ThemedPanel>();
        themedPanel.theme = GetFantasyPanelTheme();
        themedPanel.Apply();

        // QuestsPanelView bygger sin egen ScrollRect/lista i Awake() - körs bara i Play Mode
        // (samma som PlayerHudUI/HotbarUI), så panelen ser tom ut i Editorn tills du trycker Play.
        panel.AddComponent<QuestsPanelView>();

        return panel;
    }

    private static GameObject CreateInventoryPanel(Transform parent, out RectTransform soloSlot)
    {
        GameObject panel = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        StretchFull(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        ThemedPanel themedPanel = panel.AddComponent<ThemedPanel>();
        themedPanel.theme = GetFantasyPanelTheme();
        themedPanel.Apply();

        GameObject slotGO = new GameObject("InventorySoloSlot", typeof(RectTransform));
        slotGO.transform.SetParent(panel.transform, false);
        soloSlot = slotGO.GetComponent<RectTransform>();
        StretchFull(soloSlot);

        return panel;
    }

    private static GameObject CreateMapPanel(Transform parent)
    {
        GameObject panel = new GameObject("MapPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);
        StretchFull(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ThemedPanel themedPanel = panel.AddComponent<ThemedPanel>();
        themedPanel.theme = GetFantasyPanelTheme();
        themedPanel.Apply();

        CreateLabel(panel.transform, "Map — coming soon", 28);

        return panel;
    }

    // Bara skal + inventory-split i denna omgång - INGA stats/equipment-slots (eget senare steg).
    private static GameObject CreateCharacterPanel(Transform parent, out RectTransform splitSlot)
    {
        GameObject panel = new GameObject("CharacterPanel", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        panel.transform.SetParent(parent, false);
        StretchFull(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        HorizontalLayoutGroup hlg = panel.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.padding = new RectOffset(24, 24, 24, 24);
        hlg.childAlignment = TextAnchor.UpperCenter;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        ThemedPanel themedPanel = panel.AddComponent<ThemedPanel>();
        themedPanel.theme = GetFantasyPanelTheme();
        themedPanel.Apply();

        // Vänster halva: platshållare för framtida stats/equipment-slots (egen session senare).
        GameObject leftHalf = new GameObject("CharacterStatsPlaceholder", typeof(RectTransform), typeof(VerticalLayoutGroup));
        leftHalf.transform.SetParent(panel.transform, false);
        VerticalLayoutGroup leftVlg = leftHalf.GetComponent<VerticalLayoutGroup>();
        leftVlg.childAlignment = TextAnchor.UpperCenter;
        leftVlg.childControlWidth = true;
        leftVlg.childControlHeight = false;
        leftVlg.childForceExpandWidth = true;
        leftVlg.childForceExpandHeight = false;
        CreateLabel(leftHalf.transform, "Character", 28);

        // Höger halva: InventoryPanelView monteras hit i split-läge (se JournalController.ShowTab).
        GameObject rightHalf = new GameObject("InventorySplitSlot", typeof(RectTransform));
        rightHalf.transform.SetParent(panel.transform, false);
        splitSlot = rightHalf.GetComponent<RectTransform>();

        return panel;
    }

    private static InventoryPanelView CreateInventoryPanelView(Transform initialParent)
    {
        GameObject go = new GameObject("InventoryPanelViewInstance", typeof(RectTransform));
        go.transform.SetParent(initialParent, false);
        StretchFull(go.GetComponent<RectTransform>());

        InventoryPanelView view = go.AddComponent<InventoryPanelView>();
        view.slotTheme = GetFantasySlotTheme();

        return view;
    }

    // ---------- Wiring ----------

    private static void WireController(JournalController controller, GameObject journalRoot,
        GameObject questsPanel, GameObject inventoryPanel, GameObject mapPanel, GameObject characterPanel,
        InventoryPanelView inventoryPanelView, RectTransform inventorySoloSlot, RectTransform inventorySplitSlot,
        TabBarRefs tabRefs)
    {
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("journalRoot").objectReferenceValue = journalRoot;
        so.FindProperty("questsPanel").objectReferenceValue = questsPanel;
        so.FindProperty("inventoryPanel").objectReferenceValue = inventoryPanel;
        so.FindProperty("mapPanel").objectReferenceValue = mapPanel;
        so.FindProperty("characterPanel").objectReferenceValue = characterPanel;
        so.FindProperty("inventoryPanelView").objectReferenceValue = inventoryPanelView;
        so.FindProperty("inventorySoloSlot").objectReferenceValue = inventorySoloSlot;
        so.FindProperty("inventorySplitSlot").objectReferenceValue = inventorySplitSlot;
        so.FindProperty("questsTabOutline").objectReferenceValue = tabRefs.questsOutline;
        so.FindProperty("inventoryTabOutline").objectReferenceValue = tabRefs.inventoryOutline;
        so.FindProperty("mapTabOutline").objectReferenceValue = tabRefs.mapOutline;
        so.FindProperty("characterTabOutline").objectReferenceValue = tabRefs.characterOutline;

        // Auto-hittar spelarens input/kamera-script i den öppna scenen, samma som EscapeMenuBuilder.
        SerializedProperty disableProp = so.FindProperty("disableWhileOpen");
        List<Behaviour> toDisable = new List<Behaviour>();

        ThirdPersonController tpc = Object.FindFirstObjectByType<ThirdPersonController>();
        if (tpc != null) toDisable.Add(tpc); else Debug.LogWarning("JournalMenuBuilder: Ingen ThirdPersonController hittades i scenen — lades inte till i Disable While Open.");

        CameraZoom camZoom = Object.FindFirstObjectByType<CameraZoom>();
        if (camZoom != null) toDisable.Add(camZoom); else Debug.LogWarning("JournalMenuBuilder: Ingen CameraZoom hittades i scenen — lades inte till i Disable While Open.");

        TargetingController targeting = Object.FindFirstObjectByType<TargetingController>();
        if (targeting != null) toDisable.Add(targeting); else Debug.LogWarning("JournalMenuBuilder: Ingen TargetingController hittades i scenen — lades inte till i Disable While Open.");

        disableProp.arraySize = toDisable.Count;
        for (int i = 0; i < toDisable.Count; i++)
            disableProp.GetArrayElementAtIndex(i).objectReferenceValue = toDisable[i];

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Byggstenar (knapp/label) ----------

    private static TMP_DefaultControls.Resources GetTmpResources() => new TMP_DefaultControls.Resources();

    private static Button CreateMenuButton(Transform parent, string label, UnityAction callback, ButtonTheme theme = null)
    {
        GameObject go = TMP_DefaultControls.CreateButton(GetTmpResources());
        go.name = label + "Button";
        go.transform.SetParent(parent, false);

        RectTransform buttonRt = go.GetComponent<RectTransform>();
        buttonRt.sizeDelta = new Vector2(140, 48);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 140;
        le.preferredHeight = 48;
        le.minHeight = 48;

        TextMeshProUGUI tmpText = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = label;
            tmpText.fontSize = 22;
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

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 12;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return tmp;
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
    // Samma laddningsmönster som EscapeMenuBuilder - via AssetDatabase istället för direkta
    // fältreferenser, så Build() inte kastar fel om NaxestraThemeSetup inte körts än.
    private const string ThemesFolder = "Assets/Themes/FantasyWooden";

    private static ButtonTheme GetFantasyButtonTheme()
    {
        return AssetDatabase.LoadAssetAtPath<ButtonTheme>(ThemesFolder + "/FantasyWooden_Button.asset");
    }

    private static PanelTheme GetFantasyPanelTheme()
    {
        return AssetDatabase.LoadAssetAtPath<PanelTheme>(ThemesFolder + "/FantasyWooden_Panel.asset");
    }

    private static PanelTheme GetFantasyPauseDimTheme()
    {
        return AssetDatabase.LoadAssetAtPath<PanelTheme>(ThemesFolder + "/FantasyWooden_Panel_Dim.asset");
    }

    private static SlotTheme GetFantasySlotTheme()
    {
        return AssetDatabase.LoadAssetAtPath<SlotTheme>(ThemesFolder + "/FantasyWooden_Slot.asset");
    }
}
