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

        RectTransform root = BuildRoot(canvas.transform);
        BuildAbilityRow(root);
        BuildQuickItemGrid(root);
    }

    // ---------- Canvas / EventSystem ----------

    Canvas EnsureCanvas()
    {
        Canvas existing = FindFirstObjectByType<Canvas>();
        if (existing != null) return existing;

        GameObject canvasGO = new GameObject("HotbarCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

    void BuildAbilityRow(RectTransform root)
    {
        GameObject rowGO = new GameObject("AbilityRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.SetParent(root, false);

        HorizontalLayoutGroup layout = rowGO.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = slotSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = rowGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < 8; i++)
        {
            BuildAbilitySlot(rowRT, i);
        }
    }

    void BuildAbilitySlot(Transform parent, int index)
    {
        GameObject slotGO = new GameObject("AbilitySlot_" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rt = slotGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        LayoutElement le = slotGO.GetComponent<LayoutElement>();
        le.preferredWidth = slotSize;
        le.preferredHeight = slotSize;

        Image background = slotGO.GetComponent<Image>();
        background.color = slotBackground;

        bool locked = caster.IsSlotLocked(index);
        if (locked)
        {
            Outline outline = slotGO.AddComponent<Outline>();
            outline.effectColor = lockedOutlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
        }

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

    void BuildQuickItemGrid(RectTransform root)
    {
        GameObject gridGO = new GameObject("QuickItemGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        RectTransform gridRT = gridGO.GetComponent<RectTransform>();
        gridRT.SetParent(root, false);

        GridLayoutGroup grid = gridGO.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(quickSlotSize, quickSlotSize);
        grid.spacing = new Vector2(quickSlotSpacing, quickSlotSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.MiddleCenter;

        LayoutElement gridLE = gridGO.AddComponent<LayoutElement>();
        gridLE.preferredWidth = quickSlotSize * 2 + quickSlotSpacing;
        gridLE.preferredHeight = quickSlotSize * 2 + quickSlotSpacing;

        for (int i = 0; i < 4; i++)
        {
            BuildQuickItemSlot(gridRT, i);
        }
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
