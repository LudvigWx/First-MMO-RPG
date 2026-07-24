using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using Unity.Cinemachine;
using StarterAssets;
using Naxestra.UI;

// Skyrim-liknande dialogruta: kameran zoomar in mot NPC:ns ansikte, spelarkontrollerna fryses,
// och en dialogpanel med titel/text + två knappar (Accept/Decline eller Turn In/Not now) visas.
// Ersätter det gamla "tryck E -> questen startar direkt"-flödet i QuestGiverNPC (som fortfarande
// fungerar oförändrat om det här scriptet INTE finns i scenen - se QuestGiverNPC.HandleInteract).
//
// Kamera-tricket: Cinemachine styr kameran varje frame via en CinemachineBrain på Camera.main.
// Vi stänger AV den brainen tillfälligt och lerpar Camera.main.transform manuellt fram till en
// punkt vid NPC:ns ansikte (se ZoomToFaceRoutine) - och likadant tillbaka igen vid stängning
// (se ZoomOutRoutine). Cinemachine blendar BARA mellan två virtuella kameror, inte från manuell
// kontroll tillbaka till en vcam, så brainen kan inte lita på att glida tillbaka själv - vi lerpar
// därför själva mot vcam-objektets egen (kontinuerligt uträknade) transform och slår first på
// brainen igen när kameran redan står exakt där den ska.
//
// Lägg detta script på VILKET tomt GameObject som helst i scenen (en gång, singleton via Instance).
public class NpcDialogueUI : MonoBehaviour
{
    public static NpcDialogueUI Instance { get; private set; }

    [Header("Kamera-zoom")]
    public float cameraTransitionDuration = 0.5f;
    public float faceDistance = 1.5f;

    [Header("Utseende")]
    public float panelWidth = 900f;
    public float panelHeight = 260f;
    public Color panelBackground = new Color(0.05f, 0.05f, 0.05f, 0.92f);
    public Color dimBackground = new Color(0f, 0f, 0f, 0.35f);
    public TMP_FontAsset font;
    public float titleFontSize = 30f;
    public float bodyFontSize = 22f;
    public float buttonFontSize = 22f;

    public bool IsOpen { get; private set; }

    private GameObject canvasRoot;
    private GameObject dimRoot;
    private GameObject panelRoot;
    private TMP_Text titleText;
    private TMP_Text bodyText;
    private Button primaryButton;
    private TMP_Text primaryButtonLabel;
    private Button secondaryButton;
    private TMP_Text secondaryButtonLabel;

    private Camera mainCam;
    private CinemachineBrain brain;
    private Coroutine cameraRoutine;

    private ThirdPersonController tpc;
    private CameraZoom camZoom;
    private MmoCameraControl camControl;
    private TargetingController targeting;

    private System.Action pendingSecondaryAction;
    // Körs INTE direkt vid knapptryck - väntar tills zoom-ut-glidningen är klar och kameran är
    // tillbaka på spelaren (se ZoomOutRoutine). Annars hinner t.ex. AddXP/OnXpGained (vid Turn In)
    // trigga PlayerHudUI:s XP-popup MEDAN kameran fortfarande tittar på NPC:ns ansikte - popupen
    // spawnas ovanför spelaren via WorldToScreenPoint, som då räknar ut att spelaren ligger bakom
    // kameran (screenPoint.z < 0) och hoppar tyst över den. Genom att vänta ser Turn In-XP:n likadan
    // ut som XP från en kill.
    private System.Action pendingPrimaryAction;

    void Awake()
    {
        Instance = this;
        BuildUI();
    }

    void Update()
    {
        if (!IsOpen) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            InvokeSecondaryAndClose();
    }

    // ---------- Publikt API ----------

    public void ShowQuestOffer(QuestData data, Transform npc, Vector3 faceOffset, System.Action onAccept, System.Action onDecline = null)
    {
        Open(npc, faceOffset);
        titleText.text = data.title;
        bodyText.text = data.description;
        SetButtons("Accept", () => onAccept?.Invoke(), "Decline", onDecline);
    }

    public void ShowTurnIn(QuestData data, Transform npc, Vector3 faceOffset, System.Action onTurnIn)
    {
        Open(npc, faceOffset);
        titleText.text = data.title;
        bodyText.text = "All objectives complete. Turn in the quest?";
        SetButtons("Turn In", () => onTurnIn?.Invoke(), "Not now", null);
    }

    // ---------- Öppna/stäng ----------

    void Open(Transform npc, Vector3 faceOffset)
    {
        IsOpen = true;
        UITopLevelTracker.NotifyOpened(UITopLevelTracker.Layer.Dialogue);

        FindPlayerControlRefs();
        SetPlayerControlsEnabled(false);
        SetCursorFree(true);

        canvasRoot.SetActive(true);

        if (cameraRoutine != null) StopCoroutine(cameraRoutine);
        cameraRoutine = StartCoroutine(ZoomToFaceRoutine(npc, faceOffset));
    }

    void Close()
    {
        IsOpen = false;
        UITopLevelTracker.NotifyClosed(UITopLevelTracker.Layer.Dialogue);

        canvasRoot.SetActive(false);
        SetCursorFree(false);

        // Spelarrörelse/kamera-input hålls frysta ända tills zoom-ut-glidningen är klar (annars
        // hinner spelaren röra sig/snurra kameran innan bilden ens är tillbaka), se ZoomOutRoutine.
        if (cameraRoutine != null) StopCoroutine(cameraRoutine);
        cameraRoutine = StartCoroutine(ZoomOutRoutine());
    }

    void InvokeSecondaryAndClose()
    {
        pendingSecondaryAction?.Invoke();
        pendingSecondaryAction = null;
        Close();
    }

    void SetButtons(string primaryLabel, System.Action primaryAction, string secondaryLabel, System.Action secondaryAction)
    {
        pendingSecondaryAction = secondaryAction;
        pendingPrimaryAction = null;

        primaryButtonLabel.text = primaryLabel;
        primaryButton.onClick.RemoveAllListeners();
        primaryButton.onClick.AddListener(() =>
        {
            pendingSecondaryAction = null; // primärval gjordes - kör inte sekundär-callbacken också
            pendingPrimaryAction = primaryAction; // körs i slutet av ZoomOutRoutine, se fältkommentaren
            Close();
        });

        secondaryButtonLabel.text = secondaryLabel;
        secondaryButton.onClick.RemoveAllListeners();
        secondaryButton.onClick.AddListener(InvokeSecondaryAndClose);
    }

    // ---------- Spelarkontroller frysta medan dialogen är öppen ----------

    void FindPlayerControlRefs()
    {
        if (tpc == null) tpc = FindFirstObjectByType<ThirdPersonController>();
        if (camZoom == null) camZoom = FindFirstObjectByType<CameraZoom>();
        if (camControl == null) camControl = FindFirstObjectByType<MmoCameraControl>();
        if (targeting == null) targeting = FindFirstObjectByType<TargetingController>();
    }

    void SetPlayerControlsEnabled(bool enabled)
    {
        if (tpc != null) tpc.enabled = enabled;
        if (camZoom != null) camZoom.enabled = enabled;
        if (camControl != null) camControl.enabled = enabled;
        if (targeting != null) targeting.enabled = enabled;
    }

    void SetCursorFree(bool free)
    {
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = free;
    }

    // ---------- Kamera-zoom ----------

    IEnumerator ZoomToFaceRoutine(Transform npc, Vector3 faceOffset)
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) yield break;
        if (brain == null) brain = mainCam.GetComponent<CinemachineBrain>();
        if (brain != null) brain.enabled = false;

        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;

        Vector3 facePoint = npc.position + faceOffset;

        // Zoomar in FRÅN samma horisontella riktning kameran redan stod i (mot NPC:ns ansikte) -
        // fungerar oavsett vilket håll NPC:n själv råkar vara vänd, och oavsett varifrån spelaren
        // gick fram. Fallback på NPC:ns egen -forward om kameran råkar stå typ rakt ovanför.
        Vector3 dir = mainCam.transform.position - facePoint;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = -npc.forward;
        dir.Normalize();

        Vector3 targetPos = facePoint + dir * faceDistance;
        Quaternion targetRot = Quaternion.LookRotation((facePoint - targetPos).normalized, Vector3.up);

        float t = 0f;
        while (t < cameraTransitionDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / cameraTransitionDuration));
            mainCam.transform.position = Vector3.Lerp(startPos, targetPos, p);
            mainCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, p);
            yield return null;
        }

        mainCam.transform.position = targetPos;
        mainCam.transform.rotation = targetRot;
        cameraRoutine = null;
    }

    // Speglar ZoomToFaceRoutine exakt, fast åt andra hållet. camZoom.transform (vcam-objektet med
    // CinemachineThirdPersonFollow) räknar ut sin egen kamerapose kontinuerligt HELA TIDEN, oavsett
    // om brainen är av eller på - vcam:en och brainen är separata i Cinemachine, brainen bara
    // KOPIERAR den aktiva vcam:ens redan uträknade pose till den riktiga kameran. Genom att lerpa
    // manuellt dit (istället för att bara slå på brainen och hoppas att den glider) och först sätta
    // brainen på igen när vi redan STÅR där, blir övergången lika mjuk som zoom-in, ingen "hopp".
    IEnumerator ZoomOutRoutine()
    {
        if (mainCam == null || camZoom == null)
        {
            if (brain != null) brain.enabled = true;
            SetPlayerControlsEnabled(true);
            cameraRoutine = null;
            InvokePendingPrimaryAction();
            yield break;
        }

        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;
        Vector3 targetPos = camZoom.transform.position;
        Quaternion targetRot = camZoom.transform.rotation;

        float t = 0f;
        while (t < cameraTransitionDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / cameraTransitionDuration));
            mainCam.transform.position = Vector3.Lerp(startPos, targetPos, p);
            mainCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, p);
            yield return null;
        }

        mainCam.transform.position = targetPos;
        mainCam.transform.rotation = targetRot;

        if (brain != null) brain.enabled = true; // kameran står redan exakt där Cinemachine vill ha den
        SetPlayerControlsEnabled(true);
        cameraRoutine = null;
        InvokePendingPrimaryAction();
    }

    void InvokePendingPrimaryAction()
    {
        System.Action action = pendingPrimaryAction;
        pendingPrimaryAction = null;
        action?.Invoke();
    }

    // ---------- UI-uppbyggnad (samma självbyggande mönster som övriga HUD-script) ----------

    void BuildUI()
    {
        GameObject canvasGO = new GameObject("NpcDialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // ovanpå PlayerHudCanvas, under Journal/Pause om de råkar vara öppna samtidigt

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560, 1440);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasRoot = canvasGO;

        EnsureEventSystem();

        dimRoot = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        RectTransform dimRT = dimRoot.GetComponent<RectTransform>();
        dimRT.SetParent(canvasGO.transform, false);
        dimRT.anchorMin = Vector2.zero;
        dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = Vector2.zero;
        dimRT.offsetMax = Vector2.zero;
        dimRoot.GetComponent<Image>().color = dimBackground;

        panelRoot = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        RectTransform panelRT = panelRoot.GetComponent<RectTransform>();
        panelRT.SetParent(canvasGO.transform, false);
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = new Vector2(0f, 60f);
        panelRT.sizeDelta = new Vector2(panelWidth, panelHeight);

        panelRoot.GetComponent<Image>().color = panelBackground;

        VerticalLayoutGroup vlg = panelRoot.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(36, 36, 26, 26);
        vlg.spacing = 14;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        titleText = CreateLabel(panelRoot.transform, titleFontSize, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        titleText.gameObject.GetComponent<LayoutElement>().preferredHeight = titleFontSize + 10f;

        bodyText = CreateLabel(panelRoot.transform, bodyFontSize, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        LayoutElement bodyLE = bodyText.gameObject.GetComponent<LayoutElement>();
        bodyLE.preferredHeight = panelHeight - titleFontSize - 120f;
        bodyLE.flexibleHeight = 1f;
        bodyText.enableWordWrapping = true;

        GameObject buttonRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        buttonRow.transform.SetParent(panelRoot.transform, false);
        HorizontalLayoutGroup hlg = buttonRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        buttonRow.GetComponent<LayoutElement>().preferredHeight = 48f;

        secondaryButton = CreateButton(buttonRow.transform, out secondaryButtonLabel);
        primaryButton = CreateButton(buttonRow.transform, out primaryButtonLabel);

        canvasRoot.SetActive(false);
    }

    TMP_Text CreateLabel(Transform parent, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;

        return tmp;
    }

    Button CreateButton(Transform parent, out TMP_Text label)
    {
        GameObject buttonGO = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonGO.transform.SetParent(parent, false);

        LayoutElement le = buttonGO.GetComponent<LayoutElement>();
        le.preferredWidth = 180f;
        le.preferredHeight = 48f;

        Image bg = buttonGO.GetComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        label = textGO.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = buttonFontSize;
        label.color = Color.white;
        label.raycastTarget = false;
        if (font != null) label.font = font;

        Button button = buttonGO.GetComponent<Button>();
        button.targetGraphic = bg;

        return button;
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
    }
}
