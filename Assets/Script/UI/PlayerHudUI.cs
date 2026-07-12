using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using TMPro;

// Bygger HELA spelar-HUD:et (namn/level/porträtt/health/rage) i kod vid Start(),
// samma mönster som HotbarUI. Lägg detta script på ETT tomt GameObject i scenen.
// Placeras permanent i NEDRE VÄNSTRA hörnet, separat från Hotbar och Enemy HUD.
// Tryck H för att visa/dölja (påverkar bara UI:t, ingen spellogik).
public class PlayerHudUI : MonoBehaviour
{
    [Header("Referenser (hittas automatiskt)")]
    public PlayerHealth health;
    public RageResource rage;

    [Header("Placeholder-data (koppla till riktig speldata senare)")]
    public string playerName = "Player";
    public int playerLevel = 1;

    [Header("Placering (nedre vänstra hörnet)")]
    public Vector2 hudPosition = new Vector2(24f, 24f);

    [Header("Utseende – porträtt")]
    public int portraitSize = 96;
    public Color portraitPlaceholderColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Utseende – bars (avlånga)")]
    public int barWidth = 170;
    public int healthBarHeight = 16;
    public int rageBarWidth = 150;
    public int rageBarHeight = 9;
    public int barSpacing = 4;
    public Color healthColor = Color.red;
    public Color rageColor = new Color(1f, 0.55f, 0f); // orange
    public Color barBackground = new Color(0.08f, 0.08f, 0.08f, 0.9f);

    [Header("Utseende – text (lämna tomt för TextMeshPros standardfont)")]
    public TMP_FontAsset nameFont;
    public TMP_FontAsset levelFont;
    public TMP_FontAsset healthValueFont;
    public TMP_FontAsset rageValueFont;
    public float nameFontSize = 18f;
    public float levelFontSize = 22f;
    public float barValueFontSize = 11f;
    public bool showBarValues = true;

    [Header("Toggle")]
    public Key toggleKey = Key.H;

    private GameObject root;
    private Image healthFill;
    private Image rageFill;
    private TMP_Text nameText;
    private TMP_Text levelText;
    private TMP_Text healthValueText;
    private TMP_Text rageValueText;
    private bool visible = true;
    private Sprite fillSprite;

    void Start()
    {
        if (health == null) health = FindFirstObjectByType<PlayerHealth>();
        if (rage == null) rage = FindFirstObjectByType<RageResource>();
        if (health == null)
        {
            Debug.LogWarning("PlayerHudUI: Ingen PlayerHealth hittades i scenen — spelar-HUD:et byggs inte.");
            return;
        }

        Canvas canvas = EnsureCanvas();
        EnsureEventSystem();

        BuildLayout(canvas.transform);
        RefreshBars();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            visible = !visible;
            if (root != null)
            {
                root.SetActive(visible);
                if (visible) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
            }
        }

        if (root == null || !visible) return;

        RefreshBars();

        if (nameText != null) nameText.text = playerName;
        if (levelText != null) levelText.text = playerLevel.ToString();
    }

    void RefreshBars()
    {
        // Rage-komponenten kan tillkomma i scenen efter att HUD:et byggts (t.ex. om spelaren
        // spawnas senare) — leta vidare tills den hittas istället för att ge upp permanent.
        if (rage == null) rage = FindFirstObjectByType<RageResource>();
        if (health == null) health = FindFirstObjectByType<PlayerHealth>();

        if (healthFill != null && health != null)
        {
            healthFill.fillAmount = (float)health.currentHealth / health.maxHealth;
            if (healthValueText != null) healthValueText.text = health.currentHealth + " / " + health.maxHealth;
        }

        if (rageFill != null && rage != null)
        {
            rageFill.fillAmount = (float)rage.currentRage / rage.maxRage;
            if (rageValueText != null) rageValueText.text = rage.currentRage + " / " + rage.maxRage;
        }
    }

    // ---------- Canvas / EventSystem (återanvänder befintlig Canvas om den finns) ----------

    Canvas EnsureCanvas()
    {
        Canvas existing = FindFirstObjectByType<Canvas>();
        if (existing != null) return existing;

        GameObject canvasGO = new GameObject("PlayerHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // ---------- Layout-uppbyggnad ----------

    void BuildLayout(Transform parent)
    {
        root = new GameObject("PlayerHudRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.SetParent(parent, false);
        rootRT.anchorMin = new Vector2(0f, 0f);
        rootRT.anchorMax = new Vector2(0f, 0f);
        rootRT.pivot = new Vector2(0f, 0f);
        rootRT.anchoredPosition = hudPosition;

        VerticalLayoutGroup rootLayout = root.GetComponent<VerticalLayoutGroup>();
        rootLayout.childAlignment = TextAnchor.LowerLeft;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = false;
        rootLayout.childControlWidth = false;
        rootLayout.childControlHeight = false;

        ContentSizeFitter fitter = root.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject frameGO = new GameObject("Frame", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform frameRT = frameGO.GetComponent<RectTransform>();
        frameRT.SetParent(rootRT.transform, false);

        HorizontalLayoutGroup frameLayout = frameGO.GetComponent<HorizontalLayoutGroup>();
        frameLayout.spacing = 8;
        frameLayout.childAlignment = TextAnchor.MiddleLeft;
        frameLayout.childForceExpandWidth = false;
        frameLayout.childForceExpandHeight = false;
        frameLayout.childControlWidth = false;
        frameLayout.childControlHeight = false;

        ContentSizeFitter frameFitter = frameGO.AddComponent<ContentSizeFitter>();
        frameFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        frameFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildPortrait(frameRT.transform);
        BuildBarColumn(frameRT.transform);

        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRT);
    }

    void BuildPortrait(Transform parent)
    {
        GameObject portraitGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        RectTransform rt = portraitGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(portraitSize, portraitSize);

        LayoutElement le = portraitGO.GetComponent<LayoutElement>();
        le.preferredWidth = portraitSize;
        le.preferredHeight = portraitSize;

        // Platshållar-fyrkant. Byt ut .sprite senare mot en riktig porträttbild.
        Image img = portraitGO.GetComponent<Image>();
        img.color = portraitPlaceholderColor;

        // Level-text som overlay i nedre vänstra hörnet av porträttet
        GameObject levelGO = new GameObject("LevelText", typeof(RectTransform));
        RectTransform levelRT = levelGO.GetComponent<RectTransform>();
        levelRT.SetParent(rt, false);
        levelRT.anchorMin = new Vector2(0f, 0f);
        levelRT.anchorMax = new Vector2(0f, 0f);
        levelRT.pivot = new Vector2(0f, 0f);
        levelRT.anchoredPosition = new Vector2(2f, 2f);
        levelRT.sizeDelta = new Vector2(32f, 18f);

        levelText = levelGO.AddComponent<TextMeshProUGUI>();
        levelText.text = playerLevel.ToString();
        if (levelFont != null) levelText.font = levelFont;
        levelText.fontSize = levelFontSize;
        levelText.fontStyle = FontStyles.Bold;
        levelText.alignment = TextAlignmentOptions.BottomLeft;
        levelText.color = Color.white;
        levelText.raycastTarget = false;
    }

    void BuildBarColumn(Transform parent)
    {
        GameObject columnGO = new GameObject("BarColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform rt = columnGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        VerticalLayoutGroup layout = columnGO.GetComponent<VerticalLayoutGroup>();
        layout.spacing = barSpacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = columnGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        nameText = BuildText(rt.transform, "NameText", playerName, nameFontSize, nameFont, TextAlignmentOptions.MidlineLeft, barWidth);
        healthFill = BuildBar(rt.transform, "HealthBar", healthColor, barWidth, healthBarHeight, healthValueFont, out healthValueText);
        rageFill = BuildBar(rt.transform, "RageBar", rageColor, rageBarWidth, rageBarHeight, rageValueFont, out rageValueText);
    }

    TMP_Text BuildText(Transform parent, string goName, string initialText, float fontSize, TMP_FontAsset customFont, TextAlignmentOptions alignment, float width)
    {
        GameObject textGO = new GameObject(goName, typeof(RectTransform), typeof(LayoutElement));
        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(width, fontSize + 4f);

        LayoutElement le = textGO.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = fontSize + 4f;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = initialText;
        if (customFont != null) text.font = customFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    Image BuildBar(Transform parent, string goName, Color fillColor, int width, int height, TMP_FontAsset customFont, out TMP_Text valueText)
    {
        GameObject barGO = new GameObject(goName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        RectTransform rt = barGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(width, height);

        LayoutElement le = barGO.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;

        Image background = barGO.GetComponent<Image>();
        background.color = barBackground;

        GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.SetParent(rt, false);
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(2f, 2f);
        fillRT.offsetMax = new Vector2(-2f, -2f);

        Image fillImage = fillGO.GetComponent<Image>();
        fillImage.color = fillColor;
        fillImage.sprite = GetFillSprite(); // Filled-typen kräver en sprite för att fillAmount ska maskera korrekt
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        fillImage.raycastTarget = false;

        valueText = null;
        if (showBarValues)
        {
            GameObject textGO = new GameObject("ValueText", typeof(RectTransform));
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.SetParent(rt, false);
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            valueText = textGO.AddComponent<TextMeshProUGUI>();
            if (customFont != null) valueText.font = customFont;
            valueText.fontSize = barValueFontSize;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = Color.white;
            valueText.raycastTarget = false;
            valueText.enableAutoSizing = false;
        }

        return fillImage;
    }

    // Enkel vit 1x1-sprite som fill-bilderna kan maskera mot (Image.Type.Filled fungerar
    // annars inte tillförlitligt utan en tilldelad sprite).
    Sprite GetFillSprite()
    {
        if (fillSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            fillSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        return fillSprite;
    }
}
