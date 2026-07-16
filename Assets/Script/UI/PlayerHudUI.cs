using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Bygger HELA spelar-HUD:et (namn/level/porträtt/health/rage) i kod vid Start(),
// samma mönster som HotbarUI. Lägg detta script på ETT tomt GameObject i scenen.
// Placeras permanent i NEDRE VÄNSTRA hörnet, separat från Hotbar och Enemy HUD.
// Tryck H för att visa/dölja (påverkar bara UI:t, ingen spellogik).
public class PlayerHudUI : MonoBehaviour
{
    [Header("Referenser (hittas automatiskt)")]
    public PlayerHealth health;
    public RageResource rage;
    public PlayerExperience experience;

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
    public int xpBarWidth = 150;
    public int xpBarHeight = 7;
    public int barSpacing = 4;
    public Color healthColor = Color.red;
    public Color rageColor = new Color(1f, 0.55f, 0f); // orange
    public Color xpColor = new Color(0.4f, 0.7f, 1f);  // ljusblå
    public Color barBackground = new Color(0.08f, 0.08f, 0.08f, 0.9f);

    [Header("Utseende – text (lämna tomt för TextMeshPros standardfont)")]
    public TMP_FontAsset nameFont;
    public TMP_FontAsset levelFont;
    public TMP_FontAsset healthValueFont;
    public TMP_FontAsset rageValueFont;
    public TMP_FontAsset xpValueFont;
    public float nameFontSize = 18f;
    public float levelFontSize = 22f;
    public float barValueFontSize = 11f;
    public bool showBarValues = true;

    [Header("Toggle")]
    public Key toggleKey = Key.H;

    [Header("Animation")]
    // Hur snabbt XP-baren "hinner ikapp" verkliga värdet (fraction av baren per sekund).
    // Siffertexten uppdateras direkt — bara den visuella fyllningen animeras.
    public float xpBarFillSpeed = 0.2f;
    public Color levelUpPopupColor = new Color(1f, 0.85f, 0.2f, 1f);
    public float levelUpPopupDuration = 1.1f;

    [Header("Animation – \"XP: xxx\"-popup ovanför spelaren vid intjänad XP")]
    public Color xpGainPopupColor = new Color(0.4f, 0.9f, 1f, 1f);
    public float xpGainPopupFontSize = 30f;
    public float xpGainPopupDuration = 1.3f;
    public float xpGainPopupRiseDistance = 60f;
    public float xpGainPopupWorldHeightOffset = 2.2f;
    // Hur stor andel av xpGainPopupDuration som används till att tona in (0 = poppar direkt
    // som idag, högre = mjukare/långsammare intoning). Håll under ~0.3 så den hinner tona
    // in innan uttoningen börjar.
    [Range(0f, 0.5f)] public float xpGainPopupFadeInFraction = 0.15f;

    private GameObject root;
    private Image healthFill;
    private Image rageFill;
    private Image xpFill;
    private TMP_Text nameText;
    private TMP_Text levelText;
    private TMP_Text healthValueText;
    private TMP_Text rageValueText;
    private TMP_Text xpValueText;
    private bool visible = true;
    private Sprite fillSprite;
    private float xpDisplayedFraction = -1f; // -1 = ej initierad, sätts direkt utan animation första gången
    private int lastKnownLevel = -1;
    private bool xpLevelUpAnimating = false;
    private TMP_Text levelUpText;
    private bool subscribedXpGainedEvent = false;

    // ---------- Full Edit Mode (Del 5: individuellt flyttbara Portrait/Name/Health/Rage/XP) ----------

    const string K_FULL_EDIT = "hud_full_edit_mode";
    const string K_PORTRAIT_SCALE = "hud_portrait_scale";
    const string K_SHOW_HEALTH_NUM = "hud_show_health_numbers";
    const string K_SHOW_RAGE_NUM = "hud_show_rage_numbers";
    const string K_SHOW_XP_NUM = "hud_show_xp_numbers";

    // Ett element som kan kopplas loss från den vanliga kolumn-layouten och bli fritt
    // placerbart (och ev. storleksbart/skalbart) när Full Edit Mode är på.
    private class HudElement
    {
        public RectTransform rt;
        public LayoutElement layoutElement;
        public Transform simpleParent;
        public int simpleSiblingIndex;
        public Vector2 defaultSizeDelta;
        public bool resizable;
        public bool scalable;
        public string posKey;
        public string sizeKey;
        public string scaleKey;
        public HudResizeHandle resizeHandle;
        public float hitPadding;
    }

    private readonly List<HudElement> fullModeElements = new List<HudElement>();
    private Transform canvasTransform;
    private bool fullEditModeOn;
    private RectTransform portraitRT;
    private LayoutElement portraitLE;
    private RectTransform healthBarRT;
    private LayoutElement healthBarLE;
    private RectTransform rageBarRT;
    private LayoutElement rageBarLE;
    private RectTransform xpBarRT;
    private LayoutElement xpBarLE;

    void Start()
    {
        if (health == null) health = FindFirstObjectByType<PlayerHealth>();
        if (rage == null) rage = FindFirstObjectByType<RageResource>();
        if (experience == null) experience = FindFirstObjectByType<PlayerExperience>();
        if (health == null)
        {
            Debug.LogWarning("PlayerHudUI: Ingen PlayerHealth hittades i scenen — spelar-HUD:et byggs inte.");
            return;
        }

        Canvas canvas = EnsureCanvas();
        EnsureEventSystem();

        canvasTransform = canvas.transform;
        BuildLayout(canvasTransform);
        RefreshBars();

        if (healthValueText != null) healthValueText.gameObject.SetActive(GetHealthNumbersVisible());
        if (rageValueText != null) rageValueText.gameObject.SetActive(GetRageNumbersVisible());
        if (xpValueText != null) xpValueText.gameObject.SetActive(GetXpNumbersVisible());

        if (PlayerPrefs.GetInt(K_FULL_EDIT, 0) == 1) SetFullEditMode(true);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            visible = !visible;
            SetHudVisible(visible);
        }

        if (root == null || !visible) return;

        RefreshBars();

        if (experience != null) playerLevel = experience.level;
        if (nameText != null) nameText.text = playerName;
        if (levelText != null) levelText.text = playerLevel.ToString();
    }

    void OnDestroy()
    {
        if (experience != null) experience.OnXpGained -= HandleXpGained;
    }

    // Döljer/visar hela HUD:et. Går igenom root + alla full-mode-element för sig,
    // eftersom Portrait/Name/Health/Rage/XP kan ligga utanför root när Full Edit Mode är på.
    void SetHudVisible(bool show)
    {
        if (root != null)
        {
            root.SetActive(show);
            if (show) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
        }

        foreach (HudElement e in fullModeElements)
        {
            if (e.rt != null && e.rt.parent == canvasTransform) e.rt.gameObject.SetActive(show);
        }
    }

    void RefreshBars()
    {
        // Rage/Experience-komponenterna kan tillkomma i scenen efter att HUD:et byggts (t.ex.
        // om spelaren spawnas senare) — leta vidare tills de hittas istället för att ge upp permanent.
        if (rage == null) rage = FindFirstObjectByType<RageResource>();
        if (health == null) health = FindFirstObjectByType<PlayerHealth>();
        if (experience == null) experience = FindFirstObjectByType<PlayerExperience>();

        if (experience != null && !subscribedXpGainedEvent)
        {
            experience.OnXpGained += HandleXpGained;
            subscribedXpGainedEvent = true;
        }

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

        if (xpFill != null && experience != null)
        {
            if (lastKnownLevel < 0) lastKnownLevel = experience.level;
            if (experience.level > lastKnownLevel)
            {
                lastKnownLevel = experience.level;
                if (!xpLevelUpAnimating) StartCoroutine(PlayLevelUpAnimation());
            }

            if (!xpLevelUpAnimating)
            {
                float targetFraction = experience.IsMaxLevel ? 1f : (float)experience.currentXP / experience.xpToNextLevel;
                if (xpDisplayedFraction < 0f) xpDisplayedFraction = targetFraction;
                xpDisplayedFraction = Mathf.MoveTowards(xpDisplayedFraction, targetFraction, xpBarFillSpeed * Time.deltaTime);
                xpFill.fillAmount = xpDisplayedFraction;
            }

            if (xpValueText != null)
            {
                xpValueText.text = experience.IsMaxLevel
                    ? "MAX"
                    : experience.currentXP + " / " + experience.xpToNextLevel;
            }
        }
    }

    // ---------- Canvas / EventSystem (återanvänder befintlig Canvas om den finns) ----------

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

        // Gör hela HUD-blocket flyttbart i Edit Mode (se HudEditModeController). Döljs
        // automatiskt medan Full Edit Mode är på (rooten är då tom, se hideWhenFullEditModeActive).
        HudDragHandle handle = root.AddComponent<HudDragHandle>();
        handle.saveKey = "hud_playerhud";
        handle.hideWhenFullEditModeActive = true;
        // NameText renderas ovanför Portrait-rutan (utanför dess LayoutElement-storlek), så
        // rootens klick-yta räcker annars inte upp dit — extra padding täcker in namntexten.
        handle.extraHitPadding = 30f;
        handle.ApplyHitPadding();

        RegisterFullModeElement(portraitRT, portraitLE, "hud_portrait", false, null, true, K_PORTRAIT_SCALE, 0f);

        HudElement healthEl = RegisterFullModeElement(healthBarRT, healthBarLE, "hud_healthbar", true, "hud_healthbar_size", false, null, 4f);
        healthEl.resizeHandle = AddResizeHandle(healthBarRT, healthBarLE, "hud_healthbar_size");

        HudElement rageEl = RegisterFullModeElement(rageBarRT, rageBarLE, "hud_ragebar", true, "hud_ragebar_size", false, null, 8f);
        rageEl.resizeHandle = AddResizeHandle(rageBarRT, rageBarLE, "hud_ragebar_size");

        HudElement xpEl = RegisterFullModeElement(xpBarRT, xpBarLE, "hud_xpbar", true, "hud_xpbar_size", false, null, 10f);
        xpEl.resizeHandle = AddResizeHandle(xpBarRT, xpBarLE, "hud_xpbar_size");
    }

    // Litet dra-handtag i nedre högra hörnet av en bar, för att ändra bredd+höjd i Full Edit
    // Mode. Ligger kvar som barn till baren alltid (även utanpå simple-mode-kolumnen) — det
    // syns bara när HudResizeHandle.ApplyVisibility() slår på det (se RefreshEditVisuals).
    HudResizeHandle AddResizeHandle(RectTransform barTarget, LayoutElement barLE, string sizeKey)
    {
        GameObject handleGO = new GameObject("ResizeHandle", typeof(RectTransform), typeof(Image));
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.SetParent(barTarget, false);
        handleRT.anchorMin = new Vector2(1f, 0f);
        handleRT.anchorMax = new Vector2(1f, 0f);
        handleRT.pivot = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(14f, 14f);
        handleRT.anchoredPosition = new Vector2(5f, -5f);

        Image img = handleGO.GetComponent<Image>();
        img.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        img.raycastTarget = true;

        HudResizeHandle resize = handleGO.AddComponent<HudResizeHandle>();
        resize.target = barTarget;
        resize.targetLayoutElement = barLE;
        resize.sizeKey = sizeKey;

        handleGO.SetActive(false);
        return resize;
    }

    void BuildPortrait(Transform parent)
    {
        GameObject portraitGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        RectTransform rt = portraitGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(portraitSize, portraitSize);
        portraitRT = rt;

        LayoutElement le = portraitGO.GetComponent<LayoutElement>();
        le.preferredWidth = portraitSize;
        le.preferredHeight = portraitSize;
        portraitLE = le;

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

        // Namntext ovanför porträttet. Ligger ihop med Portrait/Level som EN grupp (flyttas,
        // skalas — inte separat storleksbar) eftersom en fristående TMP-text inte kan bära en
        // egen Image (HudDragHandle kräver en Graphic den äger, TMP_Text är redan en Graphic).
        GameObject nameGO = new GameObject("NameText", typeof(RectTransform), typeof(LayoutElement));
        RectTransform nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.SetParent(rt, false);
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(0f, 1f);
        nameRT.pivot = new Vector2(0f, 0f);
        nameRT.anchoredPosition = new Vector2(0f, 4f);
        nameRT.sizeDelta = new Vector2(barWidth, nameFontSize + 4f);

        nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = playerName;
        if (nameFont != null) nameText.font = nameFont;
        nameText.fontSize = nameFontSize;
        nameText.alignment = TextAlignmentOptions.BottomLeft;
        nameText.color = Color.white;
        nameText.raycastTarget = false;
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

        healthFill = BuildBar(rt.transform, "HealthBar", healthColor, barWidth, healthBarHeight, healthValueFont, out healthValueText, out healthBarRT, out healthBarLE);
        rageFill = BuildBar(rt.transform, "RageBar", rageColor, rageBarWidth, rageBarHeight, rageValueFont, out rageValueText, out rageBarRT, out rageBarLE);
        xpFill = BuildBar(rt.transform, "XPBar", xpColor, xpBarWidth, xpBarHeight, xpValueFont, out xpValueText, out xpBarRT, out xpBarLE);

        BuildLevelUpPopup(xpBarRT);
    }

    // "LEVEL UP!"-text som sitter som barn till XP-baren (följer med den även om baren
    // flyttas/kopplas loss i Full Edit Mode), dold tills PlayLevelUpAnimation() visar den.
    void BuildLevelUpPopup(Transform parent)
    {
        GameObject popupGO = new GameObject("LevelUpPopup", typeof(RectTransform));
        RectTransform popupRT = popupGO.GetComponent<RectTransform>();
        popupRT.SetParent(parent, false);
        popupRT.anchorMin = new Vector2(0.5f, 1f);
        popupRT.anchorMax = new Vector2(0.5f, 1f);
        popupRT.pivot = new Vector2(0.5f, 0f);
        popupRT.anchoredPosition = new Vector2(0f, 6f);
        popupRT.sizeDelta = new Vector2(200f, 30f);

        levelUpText = popupGO.AddComponent<TextMeshProUGUI>();
        levelUpText.text = "LEVEL UP!";
        levelUpText.fontSize = 20f;
        levelUpText.fontStyle = FontStyles.Bold;
        levelUpText.alignment = TextAlignmentOptions.Center;
        levelUpText.color = levelUpPopupColor;
        levelUpText.raycastTarget = false;

        popupGO.SetActive(false);
    }

    // Fas 1: fyll XP-baren till 100% (den gamla nivåns stapel), visa "LEVEL UP!"-poppen,
    // nollställ sedan baren och släpp taget — RefreshBars() fyller vidare mot nya målet.
    IEnumerator PlayLevelUpAnimation()
    {
        xpLevelUpAnimating = true;

        while (xpDisplayedFraction < 1f)
        {
            xpDisplayedFraction = Mathf.MoveTowards(xpDisplayedFraction, 1f, xpBarFillSpeed * Time.deltaTime);
            if (xpFill != null) xpFill.fillAmount = xpDisplayedFraction;
            yield return null;
        }

        if (levelUpText != null) StartCoroutine(AnimateLevelUpPopup());

        xpDisplayedFraction = 0f;
        if (xpFill != null) xpFill.fillAmount = 0f;

        yield return new WaitForSeconds(0.15f);

        xpLevelUpAnimating = false;
    }

    // Poppar upp, glider sakta uppåt och tonar bort — allt via enkel Lerp, ingen extern tween-lib.
    IEnumerator AnimateLevelUpPopup()
    {
        RectTransform popupRT = (RectTransform)levelUpText.transform;
        Vector2 startPos = new Vector2(0f, 6f);
        popupRT.anchoredPosition = startPos;
        popupRT.localScale = Vector3.one * 1.3f;

        Color c = levelUpPopupColor;
        levelUpText.color = c;
        levelUpText.gameObject.SetActive(true);

        float t = 0f;
        while (t < levelUpPopupDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / levelUpPopupDuration);

            popupRT.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, Mathf.Clamp01(p * 3f));
            popupRT.anchoredPosition = startPos + new Vector2(0f, Mathf.Lerp(0f, 22f, p));
            c.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01((p - 0.4f) / 0.6f));
            levelUpText.color = c;

            yield return null;
        }

        levelUpText.gameObject.SetActive(false);
    }

    // ---------- "XP: xxx"-popup ovanför spelaren (kills nu, quests senare — se PlayerExperience.OnXpGained) ----------

    void HandleXpGained(int amount)
    {
        SpawnXpGainPopup(amount);
    }

    // Skapar ett eget kortlivat GameObject per popup (istället för att återanvända ett enda)
    // så flera XP-vinster i snabb följd (t.ex. AoE-kills) kan synas samtidigt utan att krocka.
    // Positioneras via Camera.WorldToScreenPoint + RectTransform.position (world space) —
    // fungerar oavsett vilken pivot/anchor PlayerHudCanvas själv råkar ha.
    void SpawnXpGainPopup(int amount)
    {
        if (experience == null || canvasTransform == null) return;

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null) return;

        Vector3 worldPos = experience.transform.position + Vector3.up * xpGainPopupWorldHeightOffset;
        Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
        if (screenPoint.z < 0f) return; // spelaren bakom kameran, hoppa över

        GameObject popupGO = new GameObject("XpGainPopup", typeof(RectTransform));
        RectTransform popupRT = popupGO.GetComponent<RectTransform>();
        popupRT.SetParent(canvasTransform, false);
        popupRT.anchorMin = new Vector2(0.5f, 0.5f);
        popupRT.anchorMax = new Vector2(0.5f, 0.5f);
        popupRT.pivot = new Vector2(0.5f, 0.5f);
        popupRT.sizeDelta = new Vector2(240f, 44f);
        popupRT.position = new Vector3(screenPoint.x, screenPoint.y, 0f);

        TMP_Text text = popupGO.AddComponent<TextMeshProUGUI>();
        text.text = "XP: " + amount;
        text.fontSize = xpGainPopupFontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = xpGainPopupColor;
        text.raycastTarget = false;

        StartCoroutine(AnimateXpGainPopup(popupGO, text));
    }

    IEnumerator AnimateXpGainPopup(GameObject popupGO, TMP_Text text)
    {
        RectTransform popupRT = (RectTransform)popupGO.transform;
        Vector3 startPos = popupRT.position;
        Color c = text.color;

        float t = 0f;
        while (t < xpGainPopupDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / xpGainPopupDuration);

            popupRT.position = startPos + new Vector3(0f, Mathf.Lerp(0f, xpGainPopupRiseDistance, p), 0f);

            float alpha;
            if (xpGainPopupFadeInFraction > 0f && p < xpGainPopupFadeInFraction)
                alpha = Mathf.Lerp(0f, 1f, p / xpGainPopupFadeInFraction);
            else
                alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01((p - 0.3f) / 0.7f));
            c.a = alpha;
            text.color = c;

            yield return null;
        }

        Destroy(popupGO);
    }

    Image BuildBar(Transform parent, string goName, Color fillColor, int width, int height, TMP_FontAsset customFont, out TMP_Text valueText, out RectTransform barRT, out LayoutElement barLE)
    {
        GameObject barGO = new GameObject(goName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        RectTransform rt = barGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(width, height);
        barRT = rt;

        LayoutElement le = barGO.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;
        barLE = le;

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

    // ---------- Full Edit Mode ----------

    HudElement RegisterFullModeElement(RectTransform rt, LayoutElement le, string posKey, bool resizable, string sizeKey, bool scalable, string scaleKey, float hitPadding)
    {
        HudElement e = new HudElement
        {
            rt = rt,
            layoutElement = le,
            simpleParent = rt.parent,
            simpleSiblingIndex = rt.GetSiblingIndex(),
            defaultSizeDelta = rt.sizeDelta,
            resizable = resizable,
            scalable = scalable,
            posKey = posKey,
            sizeKey = sizeKey,
            scaleKey = scaleKey,
            hitPadding = hitPadding
        };
        fullModeElements.Add(e);
        return e;
    }

    // Kallas av HudEditModeController (Full Edit Mode-kryssrutan) och av Start() om
    // spelaren hade läget påslaget sist. Kopplar loss/på Portrait, Name och de tre
    // barerna från kolumn-layouten så de går att flytta/storleksändra individuellt.
    public void SetFullEditMode(bool on)
    {
        if (fullEditModeOn == on) return;
        fullEditModeOn = on;
        HudDragHandle.FullEditModeActive = on;

        foreach (HudElement e in fullModeElements)
        {
            if (on) DetachElement(e);
            else ReattachElement(e);
        }

        if (!on && root != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
        }

        foreach (HudElement e in fullModeElements)
        {
            HudDragHandle h = e.rt.GetComponent<HudDragHandle>();
            if (h != null) h.ApplyEditVisual();
        }

        HudDragHandle rootHandle = root != null ? root.GetComponent<HudDragHandle>() : null;
        if (rootHandle != null) rootHandle.ApplyEditVisual();

        PlayerPrefs.SetInt(K_FULL_EDIT, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsFullEditModeOn => fullEditModeOn;

    // ---------- Portrait-skala (bara verksam medan Portrait är utflyttat, dvs Full Edit Mode på) ----------

    public float GetPortraitScale() => PlayerPrefs.GetFloat(K_PORTRAIT_SCALE, 1f);

    public void SetPortraitScale(float scale)
    {
        PlayerPrefs.SetFloat(K_PORTRAIT_SCALE, scale);
        PlayerPrefs.Save();
        if (fullEditModeOn && portraitRT != null) portraitRT.localScale = Vector3.one * scale;
    }

    // ---------- Av/på för siffertext på barerna (gäller oavsett Full Edit Mode) ----------

    public bool GetHealthNumbersVisible() => PlayerPrefs.GetInt(K_SHOW_HEALTH_NUM, 1) == 1;
    public bool GetRageNumbersVisible() => PlayerPrefs.GetInt(K_SHOW_RAGE_NUM, 1) == 1;
    public bool GetXpNumbersVisible() => PlayerPrefs.GetInt(K_SHOW_XP_NUM, 1) == 1;

    public void SetHealthNumbersVisible(bool on) => SetNumbersVisible(healthValueText, K_SHOW_HEALTH_NUM, on);
    public void SetRageNumbersVisible(bool on) => SetNumbersVisible(rageValueText, K_SHOW_RAGE_NUM, on);
    public void SetXpNumbersVisible(bool on) => SetNumbersVisible(xpValueText, K_SHOW_XP_NUM, on);

    void SetNumbersVisible(TMP_Text text, string key, bool on)
    {
        if (text != null) text.gameObject.SetActive(on);
        PlayerPrefs.SetInt(key, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Kallas av HudEditModeControllers "Standard"-knapp. Raderar alla sparade positioner/
    // storlekar/skala för Portrait/Health/Rage/XP och stänger av Full Edit Mode — vilket
    // (via SetFullEditMode/ReattachElement) hoppar tillbaka till standardkolumnen på köpet.
    public void ResetFullModeElements()
    {
        foreach (HudElement e in fullModeElements)
        {
            if (!string.IsNullOrEmpty(e.posKey))
            {
                PlayerPrefs.DeleteKey(e.posKey + "_x");
                PlayerPrefs.DeleteKey(e.posKey + "_y");
            }
            if (e.resizable && !string.IsNullOrEmpty(e.sizeKey))
            {
                PlayerPrefs.DeleteKey(e.sizeKey + "_w");
                PlayerPrefs.DeleteKey(e.sizeKey + "_h");
            }
            if (e.scalable && !string.IsNullOrEmpty(e.scaleKey))
            {
                PlayerPrefs.DeleteKey(e.scaleKey);
            }
        }
        PlayerPrefs.DeleteKey(K_SHOW_HEALTH_NUM);
        PlayerPrefs.DeleteKey(K_SHOW_RAGE_NUM);
        PlayerPrefs.DeleteKey(K_SHOW_XP_NUM);
        if (healthValueText != null) healthValueText.gameObject.SetActive(true);
        if (rageValueText != null) rageValueText.gameObject.SetActive(true);
        if (xpValueText != null) xpValueText.gameObject.SetActive(true);

        PlayerPrefs.Save();

        SetFullEditMode(false);
    }

    // Kallas av HudEditModeController varje gång Edit Mode-panelen öppnas/stängs, så att
    // outline/raycast på Portrait/Name/Health/Rage/XP uppdateras precis som för hudRoot.
    public void RefreshEditVisuals()
    {
        foreach (HudElement e in fullModeElements)
        {
            HudDragHandle h = e.rt.GetComponent<HudDragHandle>();
            if (h != null) h.ApplyEditVisual();
            if (e.resizeHandle != null) e.resizeHandle.ApplyVisibility();
        }
    }

    void DetachElement(HudElement e)
    {
        Vector3 worldPos = e.rt.position;
        e.rt.SetParent(canvasTransform, false);
        e.rt.anchorMin = new Vector2(0f, 0f);
        e.rt.anchorMax = new Vector2(0f, 0f);
        e.rt.pivot = new Vector2(0f, 0f);
        e.rt.position = worldPos;

        if (!string.IsNullOrEmpty(e.posKey) && PlayerPrefs.HasKey(e.posKey + "_x"))
        {
            e.rt.anchoredPosition = new Vector2(
                PlayerPrefs.GetFloat(e.posKey + "_x"),
                PlayerPrefs.GetFloat(e.posKey + "_y"));
        }

        if (e.resizable && !string.IsNullOrEmpty(e.sizeKey) && PlayerPrefs.HasKey(e.sizeKey + "_w"))
        {
            Vector2 size = new Vector2(
                PlayerPrefs.GetFloat(e.sizeKey + "_w"),
                PlayerPrefs.GetFloat(e.sizeKey + "_h"));
            e.rt.sizeDelta = size;
            if (e.layoutElement != null)
            {
                e.layoutElement.preferredWidth = size.x;
                e.layoutElement.preferredHeight = size.y;
            }
        }

        if (e.scalable && !string.IsNullOrEmpty(e.scaleKey) && PlayerPrefs.HasKey(e.scaleKey))
        {
            float s = PlayerPrefs.GetFloat(e.scaleKey);
            e.rt.localScale = Vector3.one * s;
        }

        HudDragHandle handle = e.rt.GetComponent<HudDragHandle>();
        if (handle == null) handle = e.rt.gameObject.AddComponent<HudDragHandle>();
        handle.saveKey = e.posKey;
        handle.requiresFullEditMode = true;
        handle.extraHitPadding = e.hitPadding;
        handle.ApplyHitPadding();

        if (e.resizeHandle != null) e.resizeHandle.ApplyVisibility();
    }

    void ReattachElement(HudElement e)
    {
        e.rt.SetParent(e.simpleParent, false);
        e.rt.SetSiblingIndex(e.simpleSiblingIndex);
        e.rt.sizeDelta = e.defaultSizeDelta;
        e.rt.localScale = Vector3.one;

        if (e.layoutElement != null)
        {
            e.layoutElement.preferredWidth = e.defaultSizeDelta.x;
            e.layoutElement.preferredHeight = e.defaultSizeDelta.y;
        }

        if (e.resizeHandle != null) e.resizeHandle.ApplyVisibility();
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
