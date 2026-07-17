using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;

// Alltid synlig statusrad (Currency + Karma) i övre vänstra hörnet av skärmen - separat GameObject
// från Journal-canvasen, syns även när Journal är stängd. Bygger sin egen layout i kod vid Start(),
// samma mönster som PlayerHudUI/HotbarUI. Lägg detta script på ETT tomt GameObject i scenen
// (manuell placering, precis som PlayerHudUI/HotbarUI/TargetFrameUI redan kräver).
//
// Delar samma HudDragHandle-komponent som PlayerHudRoot/HotbarRoot använder för att flytta HELA
// blocket i Edit Mode (whole-block drag, inte per-element som Portrait/Health/Rage/XP) - rör
// alltså INTE PlayerHudUI.cs/HudEditModeController.cs för att uppnå detta.
public class PlayerStatusRowUI : MonoBehaviour
{
    [Header("Placering (övre vänstra hörnet)")]
    public Vector2 rowPosition = new Vector2(24f, -24f);
    public int spacing = 20;

    [Header("Currency (platshållarvärden - riktig data kopplas in när valuta-systemet finns)")]
    public List<CurrencyDisplayEntry> currencies = new List<CurrencyDisplayEntry>
    {
        new CurrencyDisplayEntry { label = "Gold", iconColor = new Color(0.95f, 0.8f, 0.2f), amount = 0 }
    };

    [Header("Karma (platshållarvärde - riktig data kopplas in när karma-systemet finns)")]
    public int startingKarma = 0;

    public CurrencyStatusUI CurrencyUI { get; private set; }
    public KarmaDisplayUI KarmaUI { get; private set; }

    void Start()
    {
        Canvas canvas = EnsureCanvas();
        EnsureEventSystem();
        BuildLayout(canvas.transform);
    }

    void BuildLayout(Transform parent)
    {
        GameObject rootGO = new GameObject("PlayerStatusRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform root = rootGO.GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = rowPosition;

        HorizontalLayoutGroup layout = rootGO.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = rootGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject currencyGO = new GameObject("CurrencyStatus", typeof(RectTransform));
        currencyGO.transform.SetParent(root, false);
        CurrencyUI = currencyGO.AddComponent<CurrencyStatusUI>();
        CurrencyUI.entries = currencies;
        CurrencyUI.BuildLayout();

        GameObject karmaGO = new GameObject("KarmaStatus", typeof(RectTransform));
        karmaGO.transform.SetParent(root, false);
        KarmaUI = karmaGO.AddComponent<KarmaDisplayUI>();
        KarmaUI.BuildLayout();
        KarmaUI.SetKarma(startingKarma);

        // Gör hela statusraden flyttbar som EN enhet i Edit Mode - samma mönster som
        // PlayerHudRoot/HotbarRoot (whole-block drag i normalt Edit Mode, inte gated bakom
        // Full Edit Mode-kryssrutan). Ingen egen skal-slider i Edit Mode-panelen i denna omgång -
        // det hade krävt att röra HudEditModeController.cs/EscapeMenuBuilder.cs.
        HudDragHandle handle = rootGO.AddComponent<HudDragHandle>();
        handle.saveKey = "hud_statusrow";
    }

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
}
