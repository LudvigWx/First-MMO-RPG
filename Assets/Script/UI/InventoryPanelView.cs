using UnityEngine;
using UnityEngine.UI;

// Fristående, återanvändbar inventory-grid - EN instans, monteras om mellan två layout-slots
// beroende på vilken Journal-flik som är aktiv (se JournalController.ShowTab): solo (fyller
// hela Inventory-fliken) eller split (halv bredd bredvid CharacterPanel). Självbyggande i kod,
// samma mönster som PlayerHudUI/HotbarUI. Bara strukturell platshållare (tomma slots) - ingen
// item-data eller drag-drop-logik än.
public class InventoryPanelView : MonoBehaviour
{
    [Header("Grid")]
    public int slotCount = 30;
    public int slotSize = 64;
    public int slotSpacing = 8;
    public SlotTheme slotTheme;

    private bool built;
    private RectTransform contentRT;
    private ScrollRect scrollRect;

    void Awake()
    {
        BuildIfNeeded();
    }

    public void BuildIfNeeded()
    {
        if (built) return;
        built = true;
        BuildLayout();
    }

    void BuildLayout()
    {
        // ScrollRect + Viewport + Content, samma grundstruktur som en vanlig UGUI scroll-lista.
        GameObject scrollGO = new GameObject("InventoryScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.SetParent(transform, false);
        StretchFull(scrollRT);
        scrollGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f); // nästan osynlig, ger ScrollRect en Graphic att fånga scroll-input på

        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.SetParent(scrollRT, false);
        StretchFull(viewportRT);
        viewportGO.GetComponent<Image>().color = Color.white;
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.SetParent(viewportRT, false);
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        // Unitys default sizeDelta för en ny RectTransform är (100,100) - för en horisontellt
        // stretchad rect blir det 50 enheter överhäng på VARJE sida om vi inte nollställer X här.
        contentRT.sizeDelta = new Vector2(0f, contentRT.sizeDelta.y);

        GridLayoutGroup grid = contentGO.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(slotSize, slotSize);
        grid.spacing = new Vector2(slotSpacing, slotSpacing);
        // Flexible istället för FixedColumnCount: räknar ut hur många kolumner som får plats i
        // containerns bredd (olika i solo- vs split-läge, se InventoryPanelView.MoveToSlot) och
        // radbryter resten nedåt, så griden aldrig sticker ut utanför sidan horisontellt.
        grid.constraint = GridLayoutGroup.Constraint.Flexible;
        // Stor topp- och sidmarginal - pergamentbildens kanter är mörkare/skuggade innan den ljusa
        // sidytan börjar (samma orsak/fix som QuestsPanelView), så griden flyttas in förbi dem.
        grid.padding = new RectOffset(70, 70, 110, 30);
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter contentFitter = contentGO.GetComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.content = contentRT;
        scrollRect.viewport = viewportRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        for (int i = 0; i < slotCount; i++)
        {
            BuildSlot(contentRT, i);
        }

        RefreshLayout();
    }

    // Tvingar fram en layout-omräkning + återställer scrollposition till toppen. Utan detta kan
    // ScrollRect råka klämma fast Content på en Y-position som hörde till en tidigare (tom eller
    // annorlunda bred) storlek, så griden verkar sväva utanför synligt fönster - se
    // QuestsPanelView.Refresh för samma mönster.
    void RefreshLayout()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    void BuildSlot(Transform parent, int index)
    {
        GameObject slotGO = new GameObject("InventorySlot_" + (index + 1), typeof(RectTransform), typeof(Image));
        slotGO.transform.SetParent(parent, false);

        Image background = slotGO.GetComponent<Image>();
        background.color = new Color(0.35f, 0.24f, 0.14f, 0.3f);

        ThemedSlot themedSlot = slotGO.AddComponent<ThemedSlot>();
        themedSlot.slotBackground = background;
        themedSlot.theme = slotTheme;
    }

    // Kallas av JournalController när fliken byts. Stretchar till att fylla vilken container den
    // hamnar i (helt olika bredd solo vs split) - medvetet INTE en världsposition-bevarande flytt
    // (till skillnad från HudDragHandles Detach/Reattach), eftersom vi VILL att griden byter
    // storlek/layout beroende på vilken av de två containrarna den sitter i just nu.
    public void MoveToSlot(RectTransform targetSlot)
    {
        if (targetSlot == null) return;
        BuildIfNeeded();

        RectTransform rt = (RectTransform)transform;
        rt.SetParent(targetSlot, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        RefreshLayout();
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
