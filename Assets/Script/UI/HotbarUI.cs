using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

// Bygger HELA hotbar-UI:t i kod vid Start() — inget manuellt Canvas/prefab-arbete krävs.
// Lägg detta script på ETT tomt GameObject i scenen så sköter det resten:
// - 8 ability-slots (rad), kopplade mot befintliga AbilityCaster.slots
// - Ett 2x2-grid med quick-use item-platshållare bredvid
// - Tangent 1-8 hanteras redan av AbilityCaster.Update() — detta script ritar bara cooldown/ikon.
public class HotbarUI : MonoBehaviour
{
    [Header("Referenser (hittas automatiskt)")]
    public AbilityCaster caster;

    [Header("Utseende – ability-slots")]
    public int slotSize = 64;
    public int slotSpacing = 8;
    public Color slotBackground = new Color(0.08f, 0.08f, 0.08f, 0.9f);
    public Color lockedOutlineColor = new Color(0.85f, 0.65f, 0.15f, 1f);
    public Color cooldownColor = new Color(0.15f, 0.15f, 0.15f, 0.75f);

    [Header("Utseende – quick items")]
    public int quickSlotSize = 64;
    public int quickSlotSpacing = 8;
    public int groupSpacing = 24;

    [Header("Utseende – slot-teman (valfritt)")]
    public SlotTheme abilitySlotTheme;
    public SlotTheme quickSlotTheme;

    // Layout-presets (kolumn-antal) för Edit Mode-panelen (HudEditModeController).
    // Index i *ColumnOptions/*ColumnLabels måste matcha varandra (parallella arrayer).
    public static readonly int[] AbilityColumnOptions = { 8, 4, 2, 1 };
    public static readonly string[] AbilityColumnLabels = { "1 row (1x8)", "2 rows (2x4)", "4 rows (4x2)", "8 rows (8x1)" };
    public static readonly int[] QuickColumnOptions = { 2, 4, 1 };
    public static readonly string[] QuickColumnLabels = { "2x2", "1 row (1x4)", "4 rows (4x1)" };

    const string K_ABILITY_COLUMNS = "hud_hotbar_ability_columns";
    const string K_QUICK_COLUMNS = "hud_hotbar_quick_columns";
    const string K_ABILITY_SCALE = "hud_hotbar_ability_scale";
    const string K_QUICK_SCALE = "hud_hotbar_quick_scale";
    const string K_ABILITY_POS = "hud_hotbar_abilities";
    const string K_QUICK_POS = "hud_hotbar_quickitems";
    // Delar samma PlayerPrefs-nyckel som PlayerHudUI (en gemensam Full Edit Mode-kryssruta
    // styr både HUD-barerna och hotbaren, se HudEditModeController.OnFullEditModeChanged).
    const string K_FULL_EDIT = "hud_full_edit_mode";
    const int DefaultAbilityColumns = 8;
    const int DefaultQuickColumns = 2;

    GridLayoutGroup abilityGrid;
    RectTransform abilityRootRT;
    GridLayoutGroup quickGrid;
    RectTransform quickRootRT;
    Transform canvasTransform;
    Transform hotbarRootTransform;
    int abilitySiblingIndex;
    int quickSiblingIndex;
    bool fullEditModeOn;

    void Start()
    {
        if (caster == null) caster = FindFirstObjectByType<AbilityCaster>();
        if (caster == null)
        {
            Debug.LogWarning("HotbarUI: Ingen AbilityCaster hittades i scenen — hotbaren byggs inte.");
            return;
        }

        Canvas canvas = EnsureCanvas();
        EnsureEventSystem();
        canvasTransform = canvas.transform;

        // HotbarRoot håller Ability- och Quick Item-gruppen ihop sida vid sida som EN enhet
        // (HorizontalLayoutGroup) i normalläge — de bryts bara loss till individuellt
        // dragbara/skalbara block när Full Edit Mode slås på, se SetFullEditMode.
        RectTransform root = BuildRoot(canvas.transform);
        hotbarRootTransform = root;
        abilityRootRT = BuildAbilityGroup(root);
        quickRootRT = BuildQuickItemGroup(root);
        abilitySiblingIndex = abilityRootRT.GetSiblingIndex();
        quickSiblingIndex = quickRootRT.GetSiblingIndex();

        // Gör hela Hotbar-blocket flyttbart som EN enhet (döljs i Full Edit Mode eftersom
        // rooten då är tom — barnen är utflyttade till canvasen, se hideWhenFullEditModeActive).
        HudDragHandle rootHandle = root.gameObject.AddComponent<HudDragHandle>();
        rootHandle.saveKey = "hud_hotbar";
        rootHandle.hideWhenFullEditModeActive = true;

        if (PlayerPrefs.GetInt(K_FULL_EDIT, 0) == 1) SetFullEditMode(true);
    }

    // ---------- Full Edit Mode (Ability/Quick Item individuellt, annars ihop som en enhet) ----------

    // Kallas av HudEditModeController (samma Full Edit Mode-kryssruta som PlayerHudUI).
    public void SetFullEditMode(bool on)
    {
        if (fullEditModeOn == on) return;
        fullEditModeOn = on;
        HudDragHandle.FullEditModeActive = on;

        if (on)
        {
            DetachGroup(abilityRootRT, K_ABILITY_POS, GetAbilityScale());
            DetachGroup(quickRootRT, K_QUICK_POS, GetQuickScale());
        }
        else
        {
            ReattachGroup(abilityRootRT, abilitySiblingIndex);
            ReattachGroup(quickRootRT, quickSiblingIndex);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)hotbarRootTransform);
        }

        RefreshEditVisuals();

        HudDragHandle rootHandle = hotbarRootTransform.GetComponent<HudDragHandle>();
        if (rootHandle != null) rootHandle.ApplyEditVisual();

        PlayerPrefs.SetInt(K_FULL_EDIT, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsFullEditModeOn => fullEditModeOn;

    // Kallas av HudEditModeController när Edit Mode-panelen öppnas/stängs, så att
    // outline/raycast på Ability-/Quick Item-gruppen uppdateras precis som för hudRoot.
    public void RefreshEditVisuals()
    {
        HudDragHandle abilityHandle = abilityRootRT.GetComponent<HudDragHandle>();
        if (abilityHandle != null) abilityHandle.ApplyEditVisual();
        HudDragHandle quickHandle = quickRootRT.GetComponent<HudDragHandle>();
        if (quickHandle != null) quickHandle.ApplyEditVisual();
    }

    void DetachGroup(RectTransform rt, string posKey, float scale)
    {
        Vector3 worldPos = rt.position;
        rt.SetParent(canvasTransform, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.position = worldPos;

        if (PlayerPrefs.HasKey(posKey + "_x"))
        {
            rt.anchoredPosition = new Vector2(
                PlayerPrefs.GetFloat(posKey + "_x"),
                PlayerPrefs.GetFloat(posKey + "_y"));
        }

        rt.localScale = Vector3.one * scale;

        HudDragHandle handle = rt.GetComponent<HudDragHandle>();
        if (handle == null) handle = rt.gameObject.AddComponent<HudDragHandle>();
        handle.saveKey = posKey;
        handle.requiresFullEditMode = true;
    }

    void ReattachGroup(RectTransform rt, int siblingIndex)
    {
        rt.SetParent(hotbarRootTransform, false);
        rt.SetSiblingIndex(siblingIndex);
        rt.localScale = Vector3.one;
    }

    // Kallas av HudEditModeControllers "Standard"-knapp.
    public void ResetLayoutToDefault()
    {
        SetAbilityColumns(DefaultAbilityColumns);
        SetQuickColumns(DefaultQuickColumns);
        SetAbilityScale(1f);
        SetQuickScale(1f);

        HudDragHandle abilityHandle = abilityRootRT.GetComponent<HudDragHandle>();
        if (abilityHandle != null) abilityHandle.ResetToDefault();
        HudDragHandle quickHandle = quickRootRT.GetComponent<HudDragHandle>();
        if (quickHandle != null) quickHandle.ResetToDefault();
    }

    // ---------- Skala (var för sig, syns bara som slidrar i Full Edit Mode och är bara
    // verksam medan gruppen är utflyttad — precis som PlayerHudUI:s Portrait Scale) ----------

    public float GetAbilityScale() => PlayerPrefs.GetFloat(K_ABILITY_SCALE, 1f);
    public float GetQuickScale() => PlayerPrefs.GetFloat(K_QUICK_SCALE, 1f);

    public void SetAbilityScale(float scale)
    {
        PlayerPrefs.SetFloat(K_ABILITY_SCALE, scale);
        PlayerPrefs.Save();
        if (fullEditModeOn && abilityRootRT != null) abilityRootRT.localScale = Vector3.one * scale;
    }

    public void SetQuickScale(float scale)
    {
        PlayerPrefs.SetFloat(K_QUICK_SCALE, scale);
        PlayerPrefs.Save();
        if (fullEditModeOn && quickRootRT != null) quickRootRT.localScale = Vector3.one * scale;
    }

    // ---------- Layout-presets ----------

    public int GetAbilityColumns() => abilityGrid != null ? abilityGrid.constraintCount : DefaultAbilityColumns;
    public int GetQuickColumns() => quickGrid != null ? quickGrid.constraintCount : DefaultQuickColumns;

    public void SetAbilityColumns(int columns)
    {
        if (abilityGrid == null) return;
        abilityGrid.constraintCount = Mathf.Clamp(columns, 1, 8);
        PlayerPrefs.SetInt(K_ABILITY_COLUMNS, abilityGrid.constraintCount);
        PlayerPrefs.Save();
        LayoutRebuilder.ForceRebuildLayoutImmediate(abilityRootRT);
    }

    public void SetQuickColumns(int columns)
    {
        if (quickGrid == null) return;
        quickGrid.constraintCount = Mathf.Clamp(columns, 1, 4);
        PlayerPrefs.SetInt(K_QUICK_COLUMNS, quickGrid.constraintCount);
        PlayerPrefs.Save();
        LayoutRebuilder.ForceRebuildLayoutImmediate(quickRootRT);
    }

    // ---------- Canvas / EventSystem ----------

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

        // Projektet kör "Input System (New)" (se ProjectSettings), så EventSystem
        // måste använda InputSystemUIInputModule, inte gamla StandaloneInputModule.
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // ---------- Layout-uppbyggnad ----------

    RectTransform BuildRoot(Transform parent)
    {
        GameObject rootGO = new GameObject("HotbarRoot", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rt = rootGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 24f);

        HorizontalLayoutGroup layout = rootGO.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = groupSpacing;
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = rootGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rt;
    }

    RectTransform BuildAbilityGroup(Transform parent)
    {
        GameObject rootGO = new GameObject("HotbarAbilityRoot", typeof(RectTransform), typeof(GridLayoutGroup));
        RectTransform rt = rootGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        abilityGrid = rootGO.GetComponent<GridLayoutGroup>();
        abilityGrid.cellSize = new Vector2(slotSize, slotSize);
        abilityGrid.spacing = new Vector2(slotSpacing, slotSpacing);
        abilityGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        abilityGrid.constraintCount = Mathf.Clamp(PlayerPrefs.GetInt(K_ABILITY_COLUMNS, DefaultAbilityColumns), 1, 8);

        ContentSizeFitter fitter = rootGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < 8; i++)
        {
            BuildAbilitySlot(rt, i);
        }

        return rt;
    }

    void BuildAbilitySlot(Transform parent, int index)
    {
        GameObject slotGO = new GameObject("AbilitySlot_" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = slotGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        Image background = slotGO.GetComponent<Image>();
        background.color = slotBackground;

        bool locked = caster.IsSlotLocked(index);
        if (locked)
        {
            Outline outline = slotGO.AddComponent<Outline>();
            outline.effectColor = lockedOutlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        ThemedSlot themedSlot = slotGO.AddComponent<ThemedSlot>();
        themedSlot.slotBackground = background;
        themedSlot.theme = abilitySlotTheme;
        themedSlot.SetHighlighted(locked);

        // Ikon (fyller hela slotten, ligger under cooldown-overlayn)
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.SetParent(rt, false);
        StretchFull(iconRT, 4f);
        Image iconImage = iconGO.GetComponent<Image>();

        // Cooldown-overlay: gråtonad "fyllning" som krymper radiellt medan cooldown räknar ner
        GameObject cdGO = new GameObject("CooldownOverlay", typeof(RectTransform), typeof(Image));
        RectTransform cdRT = cdGO.GetComponent<RectTransform>();
        cdRT.SetParent(rt, false);
        StretchFull(cdRT, 4f);
        Image cooldownImage = cdGO.GetComponent<Image>();
        cooldownImage.color = cooldownColor;
        cooldownImage.type = Image.Type.Filled;
        cooldownImage.fillMethod = Image.FillMethod.Radial360;
        cooldownImage.fillOrigin = (int)Image.Origin360.Top;
        cooldownImage.fillClockwise = true;
        cooldownImage.fillAmount = 0f;
        cooldownImage.raycastTarget = false;

        // Kortkommando-siffra i hörnet
        GameObject keyGO = new GameObject("KeybindText", typeof(RectTransform));
        RectTransform keyRT = keyGO.GetComponent<RectTransform>();
        keyRT.SetParent(rt, false);
        keyRT.anchorMin = new Vector2(1f, 0f);
        keyRT.anchorMax = new Vector2(1f, 0f);
        keyRT.pivot = new Vector2(1f, 0f);
        keyRT.anchoredPosition = new Vector2(-2f, 2f);
        keyRT.sizeDelta = new Vector2(16f, 14f);
        TextMeshProUGUI keyText = keyGO.AddComponent<TextMeshProUGUI>();
        keyText.text = (index + 1).ToString();
        keyText.fontSize = 12f;
        keyText.alignment = TextAlignmentOptions.BottomRight;
        keyText.color = Color.white;
        keyText.raycastTarget = false;

        HotbarSlotUI slotUI = slotGO.AddComponent<HotbarSlotUI>();
        slotUI.slotIndex = index;
        slotUI.caster = caster;
        slotUI.iconImage = iconImage;
        slotUI.cooldownOverlay = cooldownImage;
        slotUI.keybindText = keyText;

        Button button = slotGO.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(slotUI.OnClickCast);
    }

    RectTransform BuildQuickItemGroup(Transform parent)
    {
        GameObject rootGO = new GameObject("HotbarQuickItemRoot", typeof(RectTransform), typeof(GridLayoutGroup));
        RectTransform rt = rootGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        quickGrid = rootGO.GetComponent<GridLayoutGroup>();
        quickGrid.cellSize = new Vector2(quickSlotSize, quickSlotSize);
        quickGrid.spacing = new Vector2(quickSlotSpacing, quickSlotSpacing);
        quickGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        quickGrid.constraintCount = Mathf.Clamp(PlayerPrefs.GetInt(K_QUICK_COLUMNS, DefaultQuickColumns), 1, 4);

        ContentSizeFitter fitter = rootGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < 4; i++)
        {
            BuildQuickItemSlot(rt, i);
        }

        return rt;
    }

    void BuildQuickItemSlot(Transform parent, int index)
    {
        GameObject slotGO = new GameObject("QuickItemSlot_" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = slotGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        Image background = slotGO.GetComponent<Image>();
        background.color = slotBackground;

        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.SetParent(rt, false);
        StretchFull(iconRT, 4f);
        Image iconImage = iconGO.GetComponent<Image>();

        QuickItemSlotUI slotUI = slotGO.AddComponent<QuickItemSlotUI>();
        slotUI.slotIndex = index;
        slotUI.iconImage = iconImage;

        ThemedSlot themedSlot = slotGO.AddComponent<ThemedSlot>();
        themedSlot.slotBackground = background;
        themedSlot.theme = quickSlotTheme;

        Button button = slotGO.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(slotUI.OnClickUse);
    }

    static void StretchFull(RectTransform rt, float margin)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }
}
