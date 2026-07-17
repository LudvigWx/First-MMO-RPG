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
    public int columns = 6;
    public int slotSize = 64;
    public int slotSpacing = 8;
    public SlotTheme slotTheme;

    private bool built;

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
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.SetParent(viewportRT, false);
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;

        GridLayoutGroup grid = contentGO.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(slotSize, slotSize);
        grid.spacing = new Vector2(slotSpacing, slotSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.padding = new RectOffset(8, 8, 8, 8);

        ContentSizeFitter contentFitter = contentGO.GetComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.content = contentRT;
        scrollRect.viewport = viewportRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        for (int i = 0; i < slotCount; i++)
        {
            BuildSlot(contentRT, i);
        }
    }

    void BuildSlot(Transform parent, int index)
    {
        GameObject slotGO = new GameObject("InventorySlot_" + (index + 1), typeof(RectTransform), typeof(Image));
        slotGO.transform.SetParent(parent, false);

        Image background = slotGO.GetComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

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
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
