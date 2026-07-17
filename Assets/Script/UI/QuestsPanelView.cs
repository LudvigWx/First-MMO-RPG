using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using TMPro;

// Fristående, självbyggande quest-lista (samma mönster som InventoryPanelView/PlayerHudUI) -
// visar spelarens activeQuests med titel + per-objective "X / Y"-progress + en enkel lokal
// Track-toggle (bara UI-status, ingen HUD-tracker-overlay än, se STEG 3 i uppdraget).
//
// Rör ALDRIG quest-datamodellen härifrån - läser bara från QuestManager/QuestData/QuestProgress.
public class QuestsPanelView : MonoBehaviour
{
    [Header("Utseende")]
    public int entrySpacing = 10;
    public Color entryBackground = new Color(0.12f, 0.12f, 0.12f, 0.85f);
    public Color objectiveDoneColor = new Color(0.4f, 0.9f, 0.5f);

    private RectTransform contentRoot;
    private QuestManager questManager;
    private readonly HashSet<string> trackedQuestIds = new HashSet<string>();
    private readonly List<GameObject> spawnedEntries = new List<GameObject>();
    private bool built;

    void Awake()
    {
        BuildChrome();
    }

    void OnEnable()
    {
        Refresh();
        FindQuestManager();
        if (questManager != null) questManager.OnQuestCompleted += HandleQuestCompleted;
    }

    void OnDisable()
    {
        if (questManager != null) questManager.OnQuestCompleted -= HandleQuestCompleted;
    }

    void HandleQuestCompleted(QuestData data)
    {
        // Panelen kan vara inaktiv när eventet triggas (spelaren dödade fienden med Journal
        // stängd) - bygg inte om en dold lista i onödan, Refresh() körs ändå nästa OnEnable.
        if (isActiveAndEnabled) Refresh();
    }

    void FindQuestManager()
    {
        if (questManager == null) questManager = QuestManager.Instance;
        if (questManager == null) questManager = FindFirstObjectByType<QuestManager>();
    }

    void BuildChrome()
    {
        if (built) return;
        built = true;

        GameObject scrollGO = new GameObject("QuestScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.SetParent(transform, false);
        StretchFull(scrollRT);
        scrollGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.SetParent(scrollRT, false);
        StretchFull(viewportRT);
        viewportGO.GetComponent<Image>().color = Color.white;
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentRoot = contentGO.GetComponent<RectTransform>();
        contentRoot.SetParent(viewportRT, false);
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = entrySpacing;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.content = contentRoot;
        scrollRect.viewport = viewportRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    public void Refresh()
    {
        FindQuestManager();
        if (questManager == null || contentRoot == null) return;

        foreach (GameObject go in spawnedEntries) if (go != null) Destroy(go);
        spawnedEntries.Clear();

        List<QuestProgress> active = questManager.GetActiveQuestsForSave();
        foreach (QuestProgress progress in active)
        {
            QuestData data = questManager.allQuests.FirstOrDefault(q => q != null && q.questId == progress.questId);
            if (data == null) continue;

            spawnedEntries.Add(BuildEntry(data, progress));
        }

        if (spawnedEntries.Count == 0)
            spawnedEntries.Add(BuildEmptyLabel());
    }

    GameObject BuildEmptyLabel()
    {
        GameObject go = new GameObject("NoActiveQuests", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(contentRoot, false);

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredHeight = 32f;

        TMP_Text text = go.AddComponent<TextMeshProUGUI>();
        text.text = "Inga aktiva quests.";
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.gray;
        text.raycastTarget = false;

        return go;
    }

    GameObject BuildEntry(QuestData data, QuestProgress progress)
    {
        GameObject entryGO = new GameObject("Quest_" + data.questId, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        entryGO.transform.SetParent(contentRoot, false);

        entryGO.GetComponent<Image>().color = entryBackground;

        VerticalLayoutGroup vlg = entryGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 10, 10);
        vlg.spacing = 6;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter entryFitter = entryGO.GetComponent<ContentSizeFitter>();
        entryFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateChildLabel(entryGO.transform, data.title, 22f, Color.white);

        for (int i = 0; i < data.objectives.Count; i++)
        {
            QuestObjective objective = data.objectives[i];
            int current = (i < progress.objectiveProgress.Count) ? progress.objectiveProgress[i] : 0;
            bool done = current >= objective.requiredAmount;

            string label = (string.IsNullOrEmpty(objective.description) ? objective.targetId : objective.description)
                           + ": " + current + " / " + objective.requiredAmount + (done ? "  ✓" : "");

            CreateChildLabel(entryGO.transform, label, 16f, done ? objectiveDoneColor : Color.white);
        }

        BuildTrackButton(entryGO.transform, data.questId);

        return entryGO;
    }

    void CreateChildLabel(Transform parent, string text, float fontSize, Color color)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredHeight = fontSize + 8f;

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = color;
        tmp.raycastTarget = false;
    }

    void BuildTrackButton(Transform parent, string questId)
    {
        GameObject buttonGO = new GameObject("TrackButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonGO.transform.SetParent(parent, false);

        LayoutElement le = buttonGO.GetComponent<LayoutElement>();
        le.preferredHeight = 32f;
        le.preferredWidth = 130f;

        Image bg = buttonGO.GetComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TMP_Text label = textGO.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 15f;
        label.color = Color.white;
        label.raycastTarget = false;
        RefreshTrackLabel(label, questId);

        Button button = buttonGO.GetComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() =>
        {
            if (!trackedQuestIds.Add(questId)) trackedQuestIds.Remove(questId);
            RefreshTrackLabel(label, questId);
        });
    }

    void RefreshTrackLabel(TMP_Text label, string questId)
    {
        label.text = trackedQuestIds.Contains(questId) ? "Tracking ✓" : "Track";
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
