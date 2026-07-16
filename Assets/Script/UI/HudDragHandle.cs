using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Gör GameObjectet den sitter på flyttbart med musen, men bara medan EditModeActive är true.
// Läggs på PlayerHudRoot (PlayerHudUI), HotbarRoot (HotbarUI) och TargetFrameUI.panel
// direkt av respektive script efter att de byggt/hittat sin layout — se HudEditModeController
// för hur EditModeActive slås på/av.
public class HudDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Globalt PÅ/AV-läge, sätts av HudEditModeController när Edit Mode-panelen öppnas/stängs.
    public static bool EditModeActive = false;

    // Globalt PÅ/AV-läge för Full Edit Mode-kryssrutan (Del 5: individuellt flyttbara
    // Portrait/Name/Health/Rage/XP-element). Sätts av PlayerHudUI.SetFullEditMode().
    public static bool FullEditModeActive = false;

    [Header("Full Edit Mode-koppling")]
    // Sätts av PlayerHudUI på de fem individuella elementen (Portrait/Name/Health/Rage/XP) —
    // de får bara dras när BÅDE EditModeActive och FullEditModeActive är sanna.
    public bool requiresFullEditMode = false;
    // Sätts på PlayerHudRoots egen handle — den ska döljas/inaktiveras medan Full Edit Mode
    // är på, eftersom rooten då är tom (dess barn är utflyttade till egna element).
    public bool hideWhenFullEditModeActive = false;

    // Extra osynlig klick-yta runt elementet (i pixlar), för tunna element som XP-baren
    // (7px hög) där den riktiga rutan annars är svår att träffa med musen.
    public float extraHitPadding = 0f;

    [Header("Sparning (unik nyckel per element)")]
    public string saveKey;

    [Header("Utseende i redigeringsläge")]
    public Color highlightColor = new Color(0.3f, 0.7f, 1f, 0.28f);

    private RectTransform rt;
    private Canvas canvas;
    private Image image;
    private Outline outline;
    private bool hadExistingImage;
    private Vector2 defaultAnchoredPosition;
    private bool defaultCaptured;

    void Awake()
    {
        rt = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        EnsureVisualComponents();
    }

    void Start()
    {
        CaptureDefaultIfNeeded();
        ApplySavedPositionIfAny();
        ApplyEditVisual();
    }

    void EnsureVisualComponents()
    {
        image = GetComponent<Image>();
        hadExistingImage = image != null;
        if (!hadExistingImage)
        {
            image = gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
        }

        outline = GetComponent<Outline>();
        if (outline == null) outline = gameObject.AddComponent<Outline>();
        outline.effectColor = highlightColor;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.enabled = false;

        ApplyHitPadding();
    }

    // Negativ raycastPadding VÄXER klick-ytan utåt (positiv krymper den) — se extraHitPadding.
    // Public så PlayerHudUI kan anropa den igen efter att ha satt extraHitPadding på ett
    // element som fick sin HudDragHandle skapad (via AddComponent) i samma anrop — Awake()
    // hinner köra med extraHitPadding fortfarande på sitt default-värde annars.
    public void ApplyHitPadding()
    {
        if (image == null) return;
        if (extraHitPadding > 0f)
        {
            image.raycastPadding = new Vector4(-extraHitPadding, -extraHitPadding, -extraHitPadding, -extraHitPadding);
        }
    }

    void CaptureDefaultIfNeeded()
    {
        if (defaultCaptured) return;
        defaultAnchoredPosition = rt.anchoredPosition;
        defaultCaptured = true;
    }

    void ApplySavedPositionIfAny()
    {
        if (string.IsNullOrEmpty(saveKey)) return;
        if (!PlayerPrefs.HasKey(saveKey + "_x")) return;

        float x = PlayerPrefs.GetFloat(saveKey + "_x");
        float y = PlayerPrefs.GetFloat(saveKey + "_y");
        rt.anchoredPosition = new Vector2(x, y);
    }

    public void ResetToDefault()
    {
        CaptureDefaultIfNeeded();
        rt.anchoredPosition = defaultAnchoredPosition;

        if (!string.IsNullOrEmpty(saveKey))
        {
            PlayerPrefs.DeleteKey(saveKey + "_x");
            PlayerPrefs.DeleteKey(saveKey + "_y");
        }
    }

    // Kallas av HudEditModeController när redigeringsläget slås av/på, så att outline/tint
    // och raycastTarget uppdateras på alla element samtidigt.
    public void ApplyEditVisual()
    {
        bool on = EditModeActive;
        if (requiresFullEditMode && !FullEditModeActive) on = false;
        if (hideWhenFullEditModeActive && FullEditModeActive) on = false;
        if (outline != null) outline.enabled = on;

        if (image == null) return;

        if (hadExistingImage)
        {
            // Rör inte färgen på en redan designad bakgrund (t.ex. Enemy HUD-panelen) —
            // se bara till att raycasten går att träffa medan man drar.
            image.raycastTarget = on || image.raycastTarget;
        }
        else
        {
            Color c = highlightColor;
            c.a = on ? highlightColor.a : 0f;
            image.color = c;
            image.raycastTarget = on;
        }
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (!EditModeActive) return;
        if (requiresFullEditMode && !FullEditModeActive) return;
        if (hideWhenFullEditModeActive && FullEditModeActive) return;
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        rt.anchoredPosition += eventData.delta / scale;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!EditModeActive || string.IsNullOrEmpty(saveKey)) return;
        if (requiresFullEditMode && !FullEditModeActive) return;
        if (hideWhenFullEditModeActive && FullEditModeActive) return;
        PlayerPrefs.SetFloat(saveKey + "_x", rt.anchoredPosition.x);
        PlayerPrefs.SetFloat(saveKey + "_y", rt.anchoredPosition.y);
        PlayerPrefs.Save();
    }
}
