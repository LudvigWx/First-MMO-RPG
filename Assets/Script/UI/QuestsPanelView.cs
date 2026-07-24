using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using TMPro;

// Fristående, självbyggande quest-lista (samma mönster som InventoryPanelView/PlayerHudUI) -
// visar spelarens activeQuests med titel + per-objective "X / Y"-progress + en Track-toggle.
// Track-statusen ägs av QuestManager (delas med QuestTrackerUI, se den filen för STEG 3-HUD:et) -
// den här panelen är bara en visuell knapp mot den, ingen egen lista längre.
//
// Rör ALDRIG quest-datamodellen härifrån - läser bara från QuestManager/QuestData/QuestProgress.
public class QuestsPanelView : MonoBehaviour
{
    [Header("Utseende")]
    public int entrySpacing = 10;
    public Color entryBackground = new Color(0f, 0f, 0f, 0f);
    public Color objectiveDoneColor = new Color(0.4f, 0.9f, 0.5f);

    [Header("Text")]
    public TMP_FontAsset font;
    public float titleFontSize = 30f;
    public float objectiveFontSize = 24f;
    public float trackButtonFontSize = 22f;
    public Color titleColor = Color.white;
    public Color objectiveColor = Color.white;

    [Header("Redo att lämna in")]
    public Color turnInColor = new Color(1f, 0.85f, 0.2f);
    public string turnInLabel = "Turn in the quest at the right NPC!";

    private RectTransform contentRoot;
    private QuestManager questManager;
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
        if (questManager != null) questManager.OnQuestsChanged += HandleQuestsChanged;
    }

    void OnDisable()
    {
        if (questManager != null) questManager.OnQuestsChanged -= HandleQuestsChanged;
    }

    void HandleQuestsChanged()
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

    // Ingen ScrollRect/Viewport/Mask längre - quest-raderna läggs direkt på QuestsPanel via en
    // VerticalLayoutGroup på panelen själv. Detta är en medveten förenkling efter att den nästlade
    // Scroll/Viewport/Content-strukturen visade sig rendera raderna fel positionerade (ovanför
    // pergamentet) trots att alla RectTransform-värden mätt upp korrekt i Editorn - se om detta
    // löser det. Scrollning kan läggas till igen senare när grundorsaken är förstådd.
    void BuildChrome()
    {
        if (built) return;
        built = true;

        contentRoot = (RectTransform)transform;

        VerticalLayoutGroup vlg = gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = entrySpacing;
        // Stor toppmarginal - pergamentbildens översta remsa är mörkare/skuggad innan den ljusa
        // sidytan börjar, så innehållet flyttas ner förbi den för att tydligt se ut att ligga PÅ
        // sidan istället för i den skuggade kanten.
        vlg.padding = new RectOffset(70, 70, 110, 30);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
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

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
    }

    GameObject BuildEmptyLabel()
    {
        GameObject go = new GameObject("NoActiveQuests", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(contentRoot, false);

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredHeight = objectiveFontSize + 8f;

        TMP_Text text = go.AddComponent<TextMeshProUGUI>();
        text.text = "No active quests.";
        text.fontSize = objectiveFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.gray;
        text.raycastTarget = false;
        if (font != null) text.font = font;

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
        // false, INTE true - Unity tvingar annars flexibleWidth till minst 1 för ALLA barn
        // (även TrackButtons explicita flexibleWidth=0), vilket sträckte ut knappen till hela
        // radens bredd oavsett dess egen preferredWidth.
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter entryFitter = entryGO.GetComponent<ContentSizeFitter>();
        entryFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildAccentStripe(entryGO.transform, data.accentColor);
        BuildTitleRow(entryGO.transform, data);

        for (int i = 0; i < data.objectives.Count; i++)
        {
            QuestObjective objective = data.objectives[i];
            int current = (i < progress.objectiveProgress.Count) ? progress.objectiveProgress[i] : 0;
            bool done = current >= objective.requiredAmount;

            string label = (string.IsNullOrEmpty(objective.description) ? objective.targetId : objective.description)
                           + ": " + current + " / " + objective.requiredAmount + (done ? "  (done)" : "");

            CreateChildLabel(entryGO.transform, label, objectiveFontSize, done ? objectiveDoneColor : objectiveColor);
        }

        if (progress.objectivesComplete)
            CreateChildLabel(entryGO.transform, turnInLabel, objectiveFontSize, turnInColor);

        BuildTrackButton(entryGO.transform, data.questId);

        return entryGO;
    }

    // Tunn färgad kant längs vänsterkanten av raden - ignoreLayout så VerticalLayoutGroup inte
    // radbryter den som ett eget "barn", stretchar istället full höjd oavsett radens dynamiska
    // höjd (ContentSizeFitter). Osynlig som standard (QuestData.accentColor alpha 0).
    void BuildAccentStripe(Transform parent, Color color)
    {
        if (color.a <= 0f) return;

        GameObject go = new GameObject("AccentStripe", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(6f, 0f);
        rt.anchoredPosition = Vector2.zero;

        go.GetComponent<Image>().color = color;
        go.GetComponent<LayoutElement>().ignoreLayout = true;
    }

    // Titelraden - bara text om questen saknar QuestData.icon, annars ikon + text sida vid sida.
    void BuildTitleRow(Transform parent, QuestData data)
    {
        if (data.icon == null)
        {
            CreateChildLabel(parent, data.title, titleFontSize, titleColor);
            return;
        }

        GameObject rowGO = new GameObject("TitleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGO.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlg = rowGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        rowGO.GetComponent<LayoutElement>().preferredHeight = titleFontSize + 8f;

        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconGO.transform.SetParent(rowGO.transform, false);
        LayoutElement iconLE = iconGO.GetComponent<LayoutElement>();
        iconLE.preferredWidth = titleFontSize;
        iconLE.preferredHeight = titleFontSize;
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.sprite = data.icon;
        iconImg.preserveAspect = true;

        CreateChildLabel(rowGO.transform, data.title, titleFontSize, titleColor);
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
        if (font != null) tmp.font = font;
    }

    void BuildTrackButton(Transform parent, string questId)
    {
        GameObject buttonGO = new GameObject("TrackButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonGO.transform.SetParent(parent, false);

        LayoutElement le = buttonGO.GetComponent<LayoutElement>();
        le.preferredHeight = trackButtonFontSize + 22f;
        le.preferredWidth = trackButtonFontSize * 8f;
        // Utan detta tvingar entryns VerticalLayoutGroup (childForceExpandWidth) knappen att
        // fylla hela radens bredd istället för att hålla sin egna kompakta bredd.
        le.flexibleWidth = 0f;

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
        label.fontSize = trackButtonFontSize;
        label.color = Color.white;
        label.raycastTarget = false;
        if (font != null) label.font = font;
        RefreshTrackLabel(label, questId);

        Button button = buttonGO.GetComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() =>
        {
            FindQuestManager();
            if (questManager == null) return;
            questManager.ToggleQuestTracked(questId);
            RefreshTrackLabel(label, questId);
        });
    }

    void RefreshTrackLabel(TMP_Text label, string questId)
    {
        FindQuestManager();
        bool tracked = questManager != null && questManager.IsQuestTracked(questId);
        label.text = tracked ? "Tracking (on)" : "Track";
    }
}
