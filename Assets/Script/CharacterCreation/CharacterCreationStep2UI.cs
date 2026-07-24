using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Steg 2 av Character Creation: Edit Appearance.
// Spawnar "Modular Character"-prefaben långt bort från resten av scenen (så den inte
// krockar med annat), riggar en egen förhandsvisningskamera som renderar till en
// RenderTexture, och visar den i en RawImage i UI:t. Sex rader med "<" / namn / ">"
// låter spelaren bläddra HAIRS/EYEBROWS/EYES/FACE HAIRS/EARS/NOSES en variant i taget
// (samma barn-hierarki som ModularHeroController använder för Randomize).
//
// OBS: hudfärg sparas (CharacterCreationSelection.chosenSkinColor) men tonar INTE
// huden - paketet delar ETT material för hela kroppen, så vi hade riskerat att färga
// hela karaktären fel. Det behöver undersökas separat innan det kopplas på riktigt.
// OBS: Man/Kvinna har ingen visuell effekt - paketet har bara en könsneutral kropp.
//
// Lägg detta script på ett GameObject i samma scen som CharacterCreationStep1UI,
// dra in denna komponent i Step1:ans "Step 2 UI"-fält, och dra in "Modular Character"-
// prefaben (Prefabs/Modular Character/GanzSe Free Modular Character Update 1_1) i
// "Character Prefab"-fältet här.
public class CharacterCreationStep2UI : MonoBehaviour
{
    [Header("Karaktär")]
    public GameObject characterPrefab;
    public Vector3 previewSpawnPosition = new Vector3(2000f, 0f, 2000f);

    [Header("Förhandsvisning")]
    public int previewTextureSize = 512;
    public Color previewBackgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);

    [Header("Utseende - knappar (samma stil som Steg 1)")]
    public int buttonWidth = 180;
    public int rowLabelWidth = 130;
    public int arrowButtonWidth = 44;
    public int buttonHeight = 44;
    public int buttonSpacing = 12;
    public int sectionSpacing = 18;
    public Color normalButtonColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
    public Color selectedButtonColor = new Color(0.20f, 0.55f, 0.25f, 1f);
    public Color panelBackground = new Color(0.05f, 0.05f, 0.05f, 0.92f);

    [Header("Koppling")]
    public CharacterCreationStep1UI step1UI;

    // Kategorinamn = exakt namn i prefabens hierarki under "FACE DETAILS PARTS".
    private static readonly string[] CategoryKeys = { "HAIRS", "EYEBROWS", "EYES", "FACE HAIRS", "EARS", "NOSES" };
    private static readonly string[] CategoryLabels = { "Hair", "Eyebrows", "Eyes", "Face Hair", "Ears", "Nose" };

    private GameObject panelRoot;
    private GameObject previewRig; // förälder till spawnad karaktär + kamera, döljs ihop med panelen
    private GameObject spawnedCharacter;
    private ModularCharacterAppearance appearance;
    private Camera previewCamera;
    private RenderTexture previewTexture;

    private TextMeshProUGUI summaryText;
    private readonly Dictionary<string, TextMeshProUGUI> categoryValueTexts = new Dictionary<string, TextMeshProUGUI>();
    private readonly List<Button> skinButtons = new List<Button>();
    private bool built;

    public void Show(RaceData race)
    {
        BuildIfNeeded();
        SpawnCharacterIfNeeded();

        summaryText.text = BuildSummaryText();
        RefreshAllCategoryTexts();
        RefreshSkinRow(race);

        panelRoot.SetActive(true);
        previewRig.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (previewRig != null) previewRig.SetActive(false);
    }

    void OnBackClicked()
    {
        Hide();
        if (step1UI != null) step1UI.ShowCreationPanel();
    }

    string BuildSummaryText()
    {
        string race = CharacterCreationSelection.chosenRace != null ? CharacterCreationSelection.chosenRace.raceName : "-";
        string cls = CharacterCreationSelection.chosenClass != null ? CharacterCreationSelection.chosenClass.className : "-";
        string sub = CharacterCreationSelection.chosenSubclassName ?? "-";
        string gender = CharacterCreationSelection.isMale ? "Male" : "Female";
        return race + " - " + cls + " (" + sub + ") - " + gender;
    }

    // ---------- Spawn av karaktär + förhandsvisningskamera ----------

    void SpawnCharacterIfNeeded()
    {
        if (spawnedCharacter != null) return;

        if (characterPrefab == null)
        {
            Debug.LogWarning("CharacterCreationStep2UI: Character Prefab är inte ikopplad i Inspector - ingen karaktär att visa.");
            return;
        }

        spawnedCharacter = Instantiate(characterPrefab, previewSpawnPosition, Quaternion.identity, previewRig.transform);
        appearance = new ModularCharacterAppearance(spawnedCharacter.transform);
        HideArmorSoFaceIsVisible();

        FramePreviewCamera();
    }

    // Prefaben spawnas med full rustning aktiv som standard, vilket skymmer ansiktet.
    // Edit Appearance handlar bara om ansikte/hår - vi stänger av hela "ARMOR PARTS"
    // (samma root som ModularHeroController.armorPartsRoot pekar på) och tvingar på
    // "FACE DETAILS PARTS" så delarna alltid syns här, oavsett prefabens sparade läge.
    void HideArmorSoFaceIsVisible()
    {
        Transform armorRoot = spawnedCharacter.transform.Find("ARMOR PARTS");
        if (armorRoot != null) armorRoot.gameObject.SetActive(false);

        Transform faceRoot = spawnedCharacter.transform.Find("FACE DETAILS PARTS");
        if (faceRoot != null) faceRoot.gameObject.SetActive(true);
    }

    void FramePreviewCamera()
    {
        Bounds bounds = CalculateBounds(spawnedCharacter);
        Vector3 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.magnitude, 0.5f);

        // Kameran placeras i karaktärens EGEN forward-riktning (spawnedCharacter.transform.forward),
        // inte världens +Z/-Z - annars ser vi fel sida beroende på hur modellen är riggad.
        Vector3 direction = (spawnedCharacter.transform.forward + Vector3.up * 0.15f).normalized;
        previewCamera.transform.position = center + direction * (radius * 2.2f + 0.5f);
        previewCamera.transform.LookAt(center + Vector3.up * (bounds.extents.y * 0.15f));
    }

    static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    // ---------- Bläddring ----------

    void OnCycleClicked(string categoryKey, int direction)
    {
        if (appearance == null || !appearance.IsValid) return;

        appearance.Cycle(categoryKey, direction);
        categoryValueTexts[categoryKey].text = appearance.GetActiveVariantName(categoryKey);
    }

    void RefreshAllCategoryTexts()
    {
        if (appearance == null || !appearance.IsValid) return;

        foreach (string key in CategoryKeys)
        {
            categoryValueTexts[key].text = appearance.GetActiveVariantName(key);
        }
    }

    // ---------- Hudfärg (sparas, tonar inte huden än - se klasskommentar) ----------

    void RefreshSkinRow(RaceData race)
    {
        Transform row = skinRow;
        for (int i = row.childCount - 1; i >= 0; i--) Destroy(row.GetChild(i).gameObject);
        skinButtons.Clear();

        if (race == null || race.availableSkinColors == null) return;

        foreach (Color color in race.availableSkinColors)
        {
            Color capturedColor = color;
            GameObject swatchGO = new GameObject("Skin_" + ColorUtility.ToHtmlStringRGB(color), typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rt = swatchGO.GetComponent<RectTransform>();
            rt.SetParent(row, false);
            rt.sizeDelta = new Vector2(buttonHeight, buttonHeight);

            LayoutElement le = swatchGO.GetComponent<LayoutElement>();
            le.preferredWidth = buttonHeight;
            le.preferredHeight = buttonHeight;

            Image swatchImage = swatchGO.GetComponent<Image>();
            swatchImage.color = capturedColor;

            Button button = swatchGO.GetComponent<Button>();
            button.targetGraphic = swatchImage;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OnSkinColorSelected(capturedColor));

            skinButtons.Add(button);
        }

        if (skinButtons.Count > 0) OnSkinColorSelected(race.availableSkinColors[0]);
    }

    void OnSkinColorSelected(Color color)
    {
        CharacterCreationSelection.chosenSkinColor = color;

        for (int i = 0; i < skinButtons.Count; i++)
        {
            Outline outline = skinButtons[i].GetComponent<Outline>();
            bool selected = skinButtons[i].GetComponent<Image>().color == color;
            if (selected && outline == null)
            {
                outline = skinButtons[i].gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(3f, -3f);
            }
            else if (!selected && outline != null)
            {
                Destroy(outline);
            }
        }
    }

    // ---------- Bygg UI (körs en gång, lazy) ----------

    private RectTransform skinRow;

    void BuildIfNeeded()
    {
        if (built) return;
        built = true;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("CharacterCreationStep2UI: ingen Canvas hittades - se till att CharacterCreationStep1UI har körts (Start) före Show().");
            return;
        }

        previewRig = new GameObject("Step2PreviewRig");
        BuildPreviewCamera();

        panelRoot = new GameObject("EditAppearancePanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform panelRT = panelRoot.GetComponent<RectTransform>();
        panelRT.SetParent(canvas.transform, false);
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;

        panelRoot.GetComponent<Image>().color = panelBackground;

        VerticalLayoutGroup layout = panelRoot.GetComponent<VerticalLayoutGroup>();
        layout.spacing = sectionSpacing;
        layout.padding = new RectOffset(30, 30, 30, 30);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = panelRoot.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateTitle("Edit Appearance - Step 2", panelRT);
        summaryText = CreateBodyText("", panelRT);

        BuildPreviewImage(panelRT);

        foreach (string key in CategoryKeys)
        {
            string label = CategoryLabels[System.Array.IndexOf(CategoryKeys, key)];
            RectTransform row = CreateRow(panelRT, "Row_" + key);

            CreateFixedText(label, row, rowLabelWidth, buttonHeight);
            CreateButton("<", row, () => OnCycleClicked(key, -1), arrowButtonWidth, buttonHeight);
            categoryValueTexts[key] = CreateFixedText("-", row, buttonWidth, buttonHeight);
            CreateButton(">", row, () => OnCycleClicked(key, 1), arrowButtonWidth, buttonHeight);
        }

        CreateSectionLabel("Skin Color (saved, not visually applied yet)", panelRT);
        skinRow = CreateRow(panelRT, "SkinRow");

        CreateButton("Back", panelRT, OnBackClicked, buttonWidth, buttonHeight);
    }

    void BuildPreviewCamera()
    {
        GameObject camGO = new GameObject("Step2PreviewCamera", typeof(Camera));
        camGO.transform.SetParent(previewRig.transform, false);

        previewCamera = camGO.GetComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = previewBackgroundColor;
        previewCamera.fieldOfView = 30f;
        previewCamera.nearClipPlane = 0.05f;
        previewCamera.farClipPlane = 100f;

        previewTexture = new RenderTexture(previewTextureSize, previewTextureSize, 16);
        previewTexture.Create();
        previewCamera.targetTexture = previewTexture;
    }

    void BuildPreviewImage(Transform parent)
    {
        GameObject imgGO = new GameObject("PreviewImage", typeof(RectTransform), typeof(RawImage), typeof(LayoutElement));
        RectTransform rt = imgGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(320f, 320f);

        LayoutElement le = imgGO.GetComponent<LayoutElement>();
        le.preferredWidth = 320f;
        le.preferredHeight = 320f;

        RawImage rawImage = imgGO.GetComponent<RawImage>();
        rawImage.texture = previewTexture;
    }

    // ---------- Bygg-hjälpare (samma mönster som Steg 1) ----------

    RectTransform CreateRow(Transform parent, string name)
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

    Button CreateButton(string label, Transform parent, System.Action onClick, int width, int height)
    {
        GameObject buttonGO = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
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
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => onClick());

        return button;
    }

    TextMeshProUGUI CreateFixedText(string label, Transform parent, int width, int height)
    {
        GameObject textGO = new GameObject("Text_" + label, typeof(RectTransform), typeof(LayoutElement));
        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(width, height);

        LayoutElement le = textGO.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    void CreateTitle(string label, Transform parent)
    {
        GameObject textGO = new GameObject("Title", typeof(RectTransform), typeof(LayoutElement));
        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(400f, 40f);

        LayoutElement le = textGO.GetComponent<LayoutElement>();
        le.preferredWidth = rt.sizeDelta.x;
        le.preferredHeight = rt.sizeDelta.y;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 26f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    void CreateSectionLabel(string label, Transform parent)
    {
        GameObject textGO = new GameObject("Label_" + label, typeof(RectTransform), typeof(LayoutElement));
        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(400f, 26f);

        LayoutElement le = textGO.GetComponent<LayoutElement>();
        le.preferredWidth = rt.sizeDelta.x;
        le.preferredHeight = rt.sizeDelta.y;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 16f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.85f, 0.85f, 0.85f);
        text.raycastTarget = false;
    }

    TextMeshProUGUI CreateBodyText(string label, Transform parent)
    {
        GameObject textGO = new GameObject("Body", typeof(RectTransform), typeof(LayoutElement));
        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(400f, 24f);

        LayoutElement le = textGO.GetComponent<LayoutElement>();
        le.preferredWidth = rt.sizeDelta.x;
        le.preferredHeight = rt.sizeDelta.y;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 16f;
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
