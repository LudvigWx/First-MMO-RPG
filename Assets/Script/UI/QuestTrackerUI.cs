using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using TMPro;

// STEG 3 av quest-tracking-uppdraget (se QuestsPanelView.cs) - en egen, flyttbar HUD-ruta
// (samma självbyggande mönster som PlayerHudUI/HotbarUI) som visar titel + progress för varje
// quest som är "trackad" (QuestManager.IsQuestTracked). Nya quests trackas automatiskt vid
// mottagande (QuestManager.StartQuest) - Track-knappen i Journal slår bara av/på om just den
// questen syns HÄR, den påverkar aldrig om questen faktiskt progressar (alla aktiva quests
// progressar alltid, oavsett trackning - se QuestManager.ReportProgress).
//
// Lägg detta script på ETT tomt GameObject i scenen, precis som PlayerHudUI/HotbarUI.
public class QuestTrackerUI : MonoBehaviour
{
    [Header("Placering (övre högra hörnet som standard, dra själv i Edit HUD Layout-läge)")]
    public Vector2 hudPosition = new Vector2(-24f, -24f);

    [Header("Utseende")]
    public float panelWidth = 280f;
    public int entrySpacing = 14;
    public Color panelBackground = new Color(0.05f, 0.05f, 0.05f, 0.55f);
    public Color entryBackground = new Color(0f, 0f, 0f, 0.35f);
    public Color objectiveDoneColor = new Color(0.4f, 0.9f, 0.5f);

    [Header("Text")]
    public TMP_FontAsset font;
    public float titleFontSize = 22f;
    public float objectiveFontSize = 18f;
    public Color titleColor = Color.white;
    public Color objectiveColor = new Color(0.85f, 0.85f, 0.85f);

    [Header("Redo att lämna in")]
    public Color turnInColor = new Color(1f, 0.85f, 0.2f);
    public string turnInLabel = "  Turn in the quest!";

    private GameObject root;
    private RectTransform contentRoot;
    private Transform canvasTransform;
    private QuestManager questManager;
    private readonly List<GameObject> spawnedEntries = new List<GameObject>();

    void Start()
    {
        Canvas canvas = EnsureCanvas();
        canvasTransform = canvas.transform;
        BuildLayout(canvasTransform);
        Refresh();
    }

    void OnEnable()
    {
        FindQuestManager();
        if (questManager != null) questManager.OnQuestsChanged += HandleQuestsChanged;
    }

    void OnDisable()
    {
        if (questManager != null) questManager.OnQuestsChanged -= HandleQuestsChanged;
    }

    void HandleQuestsChanged()
    {
        if (isActiveAndEnabled) Refresh();
    }

    void FindQuestManager()
    {
        if (questManager == null) questManager = QuestManager.Instance;
        if (questManager == null) questManager = FindFirstObjectByType<QuestManager>();
    }

    // Återanvänder samma Canvas som PlayerHudUI/HotbarUI bygger/hittar - då döljs Quest
    // Trackern automatiskt med resten av HUD:et av JournalController/PauseMenuController
    // (som stänger av HELA PlayerHudCanvas), ingen egen döljningslogik behövs här.
    Canvas EnsureCanvas()
    {
        GameObject existingGO = GameObject.Find("PlayerHudCanvas");
        Canvas existing = existingGO != null ? existingGO.GetComponent<Canvas>() : null;
        if (existing != null) return existing;

        GameObject canvasGO = new GameObject("PlayerHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        return canvas;
    }

    void BuildLayout(Transform parent)
    {
        root = new GameObject("QuestTrackerRoot", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.SetParent(parent, false);
        rootRT.anchorMin = new Vector2(1f, 1f);
        rootRT.anchorMax = new Vector2(1f, 1f);
        rootRT.pivot = new Vector2(1f, 1f);
        rootRT.anchoredPosition = hudPosition;
        rootRT.sizeDelta = new Vector2(panelWidth, 0f);
        contentRoot = rootRT;

        root.GetComponent<Image>().color = panelBackground;

        VerticalLayoutGroup vlg = root.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = entrySpacing;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Dragbar precis som PlayerHudRoot/HotbarRoot - se HudDragHandle. Fungerar så fort
        // Edit HUD Layout-panelen är öppen (HudDragHandle.EditModeActive), ingen extra koppling
        // krävs här, men HudEditModeController hittar/registrerar den ändå på namn för att
        // Standard-knappen ska kunna nollställa positionen.
        HudDragHandle handle = root.AddComponent<HudDragHandle>();
        handle.saveKey = "hud_questtracker";
    }

    public void Refresh()
    {
        FindQuestManager();
        if (questManager == null || contentRoot == null) return;

        foreach (GameObject go in spawnedEntries) if (go != null) Destroy(go);
        spawnedEntries.Clear();

        List<QuestProgress> tracked = questManager.GetActiveQuestsForSave()
            .Where(p => questManager.IsQuestTracked(p.questId))
            .ToList();

        foreach (QuestProgress progress in tracked)
        {
            QuestData data = questManager.allQuests.FirstOrDefault(q => q != null && q.questId == progress.questId);
            if (data == null) continue;

            spawnedEntries.Add(BuildEntry(data, progress));
        }

        root.SetActive(tracked.Count > 0);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
    }

    GameObject BuildEntry(QuestData data, QuestProgress progress)
    {
        GameObject entryGO = new GameObject("Quest_" + data.questId, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        entryGO.transform.SetParent(contentRoot, false);

        entryGO.GetComponent<Image>().color = entryBackground;

        VerticalLayoutGroup vlg = entryGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 6, 6);
        vlg.spacing = 2;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter entryFitter = entryGO.GetComponent<ContentSizeFitter>();
        entryFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateChildLabel(entryGO.transform, data.title, titleFontSize, titleColor, FontStyles.Bold);

        for (int i = 0; i < data.objectives.Count; i++)
        {
            QuestObjective objective = data.objectives[i];
            int current = (i < progress.objectiveProgress.Count) ? progress.objectiveProgress[i] : 0;
            bool done = current >= objective.requiredAmount;

            string label = "  " + (string.IsNullOrEmpty(objective.description) ? objective.targetId : objective.description)
                           + ": " + current + " / " + objective.requiredAmount;

            CreateChildLabel(entryGO.transform, label, objectiveFontSize, done ? objectiveDoneColor : objectiveColor, FontStyles.Normal);
        }

        if (progress.objectivesComplete)
            CreateChildLabel(entryGO.transform, turnInLabel, objectiveFontSize, turnInColor, FontStyles.Bold);

        return entryGO;
    }

    void CreateChildLabel(Transform parent, string text, float fontSize, Color color, FontStyles style)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredHeight = fontSize + 6f;

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        if (font != null) tmp.font = font;
    }
}
