using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

// Bygger HELA Character Creation Steg 1-UI:t i kod vid Start() - samma mönster
// som HotbarUI/PlayerHudUI (se Assets/Script/UI). Lägg detta script på ETT tomt
// GameObject i en scen, dra in exempel-RaceData/ClassData-assets i listorna nedan.
//
// Flöde: välj ras -> välj klass (filtrerad efter rasens excludedClasses) ->
// välj subklass (uppdateras efter vald klass) -> välj kön -> "Nästa".
// "Nästa" sparar valet i CharacterCreationSelection och visar CharacterCreationStep2UI
// (Edit Appearance) - koppla den komponenten i "Step 2 UI"-fältet i Inspector.
public class CharacterCreationStep1UI : MonoBehaviour
{
    [Header("Data (dra in exempel-assets här, valfritt antal)")]
    public RaceData[] races;
    public ClassData[] classes;

    [Header("Koppling")]
    [Tooltip("Komponenten CharacterCreationStep2UI (kan sitta på samma eller ett annat GameObject).")]
    public CharacterCreationStep2UI step2UI;

    [Header("Utseende - knappar")]
    public int buttonWidth = 180;
    public int buttonHeight = 44;
    public int buttonSpacing = 12;
    public int sectionSpacing = 26;
    public Color normalButtonColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
    public Color selectedButtonColor = new Color(0.20f, 0.55f, 0.25f, 1f);
    public Color panelBackground = new Color(0.05f, 0.05f, 0.05f, 0.92f);

    // --- Vad spelaren har valt just nu ---
    private RaceData selectedRace;
    private ClassData selectedClass;
    private string selectedSubclass;
    private bool isMale = true;

    // --- Paneler ---
    private GameObject creationPanel;

    // --- Rader som byggs om dynamiskt ---
    private RectTransform classRow;
    private RectTransform subclassRow;

    private readonly List<Button> raceButtons = new List<Button>();
    private readonly List<Button> classButtons = new List<Button>();
    private readonly List<Button> subclassButtons = new List<Button>();
    private Button maleButton;
    private Button femaleButton;
    private Button nextButton;

    void Start()
    {
        Canvas canvas = EnsureCanvas();
        EnsureEventSystem();

        BuildCreationPanel(canvas.transform);

        RefreshClassRow();
        RefreshSubclassRow();
        RefreshNextInteractable();
    }

    // ---------- Canvas / EventSystem (samma mönster som HotbarUI) ----------

    Canvas EnsureCanvas()
    {
        Canvas existing = FindFirstObjectByType<Canvas>();
        if (existing != null) return existing;

        GameObject canvasGO = new GameObject("CharacterCreationCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        return canvas;
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        // Projektet kör "Input System (New)", så EventSystem måste använda
        // InputSystemUIInputModule, inte gamla StandaloneInputModule.
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // ---------- Steg 1-panel ----------

    void BuildCreationPanel(Transform canvasParent)
    {
        creationPanel = new GameObject("CreationPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform panelRT = creationPanel.GetComponent<RectTransform>();
        panelRT.SetParent(canvasParent, false);
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;

        Image bg = creationPanel.GetComponent<Image>();
        bg.color = panelBackground;

        VerticalLayoutGroup layout = creationPanel.GetComponent<VerticalLayoutGroup>();
        layout.spacing = sectionSpacing;
        layout.padding = new RectOffset(30, 30, 30, 30);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = creationPanel.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateTitle("Create Character - Step 1", panelRT);

        CreateSectionLabel("Choose Race", panelRT);
        RectTransform raceRow = CreateButtonRow(panelRT, "RaceRow");
        for (int i = 0; i < races.Length; i++)
        {
            RaceData race = races[i];
            Button b = CreateButton(race != null ? race.raceName : "(missing)", raceRow, () => OnRaceSelected(race));
            raceButtons.Add(b);
        }

        CreateSectionLabel("Choose Class", panelRT);
        classRow = CreateButtonRow(panelRT, "ClassRow");

        CreateSectionLabel("Choose Subclass", panelRT);
        subclassRow = CreateButtonRow(panelRT, "SubclassRow");

        CreateSectionLabel("Gender", panelRT);
        RectTransform genderRow = CreateButtonRow(panelRT, "GenderRow");
        maleButton = CreateButton("Male", genderRow, () => OnGenderSelected(true));
        femaleButton = CreateButton("Female", genderRow, () => OnGenderSelected(false));

        nextButton = CreateButton("Next", panelRT, OnNextClicked, buttonWidth, buttonHeight + 6);

        RefreshRaceButtonColors();
        RefreshGenderButtonColors();
    }

    // ---------- Val-hantering ----------

    void OnRaceSelected(RaceData race)
    {
        selectedRace = race;
        selectedClass = null;
        selectedSubclass = null;

        RefreshRaceButtonColors();
        RefreshClassRow();
        RefreshSubclassRow();
        RefreshNextInteractable();
    }

    void OnClassSelected(ClassData classData)
    {
        selectedClass = classData;
        selectedSubclass = null;

        RefreshClassButtonColors();
        RefreshSubclassRow();
        RefreshNextInteractable();
    }

    void OnSubclassSelected(string subclassName)
    {
        selectedSubclass = subclassName;

        RefreshSubclassButtonColors();
        RefreshNextInteractable();
    }

    void OnGenderSelected(bool male)
    {
        isMale = male;
        RefreshGenderButtonColors();
    }

    void OnNextClicked()
    {
        if (selectedRace == null || selectedClass == null || selectedSubclass == null) return;

        CharacterCreationSelection.chosenRace = selectedRace;
        CharacterCreationSelection.chosenClass = selectedClass;
        CharacterCreationSelection.chosenSubclassName = selectedSubclass;
        CharacterCreationSelection.isMale = isMale;

        creationPanel.SetActive(false);

        if (step2UI != null)
        {
            step2UI.Show(selectedRace);
        }
        else
        {
            Debug.LogWarning("CharacterCreationStep1UI: \"Step 2 UI\" är inte ikopplad i Inspector - kan inte visa Edit Appearance.");
            creationPanel.SetActive(true);
        }
    }

    // Anropas av CharacterCreationStep2UI när spelaren klickar "Tillbaka".
    public void ShowCreationPanel()
    {
        creationPanel.SetActive(true);
    }

    // ---------- Rebuild av klass/subklass-rader ----------

    void RefreshClassRow()
    {
        ClearRow(classRow, classButtons);

        if (selectedRace == null)
        {
            CreateBodyText("Choose a race first", classRow);
            return;
        }

        for (int i = 0; i < classes.Length; i++)
        {
            ClassData classData = classes[i];
            if (classData == null || !selectedRace.AllowsClass(classData)) continue;

            Button b = CreateButton(classData.className, classRow, () => OnClassSelected(classData));
            classButtons.Add(b);
        }

        RefreshClassButtonColors();
    }

    void RefreshSubclassRow()
    {
        ClearRow(subclassRow, subclassButtons);

        if (selectedClass == null)
        {
            CreateBodyText("Choose a class first", subclassRow);
            return;
        }

        foreach (string subclassName in selectedClass.subclassNames)
        {
            string capturedName = subclassName;
            Button b = CreateButton(capturedName, subclassRow, () => OnSubclassSelected(capturedName));
            subclassButtons.Add(b);
        }

        RefreshSubclassButtonColors();
    }

    void ClearRow(RectTransform row, List<Button> buttonList)
    {
        buttonList.Clear();
        for (int i = row.childCount - 1; i >= 0; i--)
        {
            Destroy(row.GetChild(i).gameObject);
        }
    }

    // ---------- Färg-uppdatering (visar vad som är valt) ----------

    void RefreshRaceButtonColors()
    {
        for (int i = 0; i < raceButtons.Count; i++)
        {
            bool selected = races[i] == selectedRace;
            SetButtonSelected(raceButtons[i], selected);
        }
    }

    void RefreshClassButtonColors()
    {
        int buttonIndex = 0;
        for (int i = 0; i < classes.Length; i++)
        {
            ClassData classData = classes[i];
            if (classData == null || selectedRace == null || !selectedRace.AllowsClass(classData)) continue;

            bool selected = classData == selectedClass;
            SetButtonSelected(classButtons[buttonIndex], selected);
            buttonIndex++;
        }
    }

    void RefreshSubclassButtonColors()
    {
        if (selectedClass == null) return;

        for (int i = 0; i < selectedClass.subclassNames.Count; i++)
        {
            bool selected = selectedClass.subclassNames[i] == selectedSubclass;
            SetButtonSelected(subclassButtons[i], selected);
        }
    }

    void RefreshGenderButtonColors()
    {
        SetButtonSelected(maleButton, isMale);
        SetButtonSelected(femaleButton, !isMale);
    }

    void RefreshNextInteractable()
    {
        bool ready = selectedRace != null && selectedClass != null && selectedSubclass != null;
        nextButton.interactable = ready;
        // Transition=None ger ingen automatisk gråtoning vid interactable=false,
        // så vi målar Nästa-knappen manuellt: grön = redo, grå = saknar val.
        nextButton.GetComponent<Image>().color = ready ? selectedButtonColor : normalButtonColor;
    }

    void SetButtonSelected(Button button, bool selected)
    {
        button.GetComponent<Image>().color = selected ? selectedButtonColor : normalButtonColor;
    }

    // ---------- Bygg-hjälpare (knappar/text/rader) ----------

    RectTransform CreateButtonRow(Transform parent, string name)
    {
        GameObject rowGO = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.SetParent(parent, false);

        HorizontalLayoutGroup layout = rowGO.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = buttonSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = rowGO.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rowRT;
    }

    Button CreateButton(string label, Transform parent, System.Action onClick)
    {
        return CreateButton(label, parent, onClick, buttonWidth, buttonHeight);
    }

    Button CreateButton(string label, Transform parent, System.Action onClick, int width, int height)
    {
        GameObject buttonGO = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        // OBS: childControlWidth/Height=false i layout-grupperna ovan gör att
        // LayoutElement INTE styr faktisk storlek - sizeDelta måste sättas direkt.
        rt.sizeDelta = new Vector2(width, height);

        LayoutElement le = buttonGO.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;

        Image background = buttonGO.GetComponent<Image>();
        background.color = normalButtonColor;

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.SetParent(rt, false);
        StretchFull(textRT, 4f);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 20f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        Button button = buttonGO.GetComponent<Button>();
        button.targetGraphic = background;
        // Transition=None: vi tintar bakgrunden helt själva (SetButtonSelected),
        // annars slåss Buttons inbyggda ColorTint-övergång mot vår färg vid hover/klick.
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => onClick());

        return button;
    }

    void CreateTitle(string label, Transform parent)
    {
        GameObject textGO = new GameObject("Title", typeof(RectTransform), typeof(LayoutElement));
        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(buttonWidth * 3 + buttonSpacing * 2, 40f);

        LayoutElement le = textGO.GetComponent<LayoutElement>();
        le.preferredWidth = rt.sizeDelta.x;
        le.preferredHeight = rt.sizeDelta.y;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    void CreateSectionLabel(string label, Transform parent)
    {
        GameObject textGO = new GameObject("Label_" + label, typeof(RectTransform), typeof(LayoutElement));
        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(buttonWidth * 3 + buttonSpacing * 2, 26f);

        LayoutElement le = textGO.GetComponent<LayoutElement>();
        le.preferredWidth = rt.sizeDelta.x;
        le.preferredHeight = rt.sizeDelta.y;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Left;
        text.color = new Color(0.85f, 0.85f, 0.85f);
        text.raycastTarget = false;
    }

    TextMeshProUGUI CreateBodyText(string label, Transform parent)
    {
        GameObject textGO = new GameObject("Body_" + label, typeof(RectTransform), typeof(LayoutElement));
        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(buttonWidth * 3 + buttonSpacing * 2, 28f);

        LayoutElement le = textGO.GetComponent<LayoutElement>();
        le.preferredWidth = rt.sizeDelta.x;
        le.preferredHeight = rt.sizeDelta.y;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.7f, 0.7f, 0.7f);
        text.raycastTarget = false;
        return text;
    }

    static void StretchFull(RectTransform rt, float margin)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }
}
