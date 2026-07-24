using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using StarterAssets;

// Fäst på VILKEN GameObject som helst i scenen (en NPC-figur, eller en tom platshållare om du
// inte har någon modell än) och fyll i "Quest Id" i Inspector - startar den questen med E när
// spelaren är i närheten. Bygger sin egen "[E] Prata"-prompt i kod ovanför sig själv (samma
// självbyggande UI-mönster som PlayerHudUI/HotbarUI), synlig bara inom räckhåll.
//
// Avstånd mäts med enkel Vector3.Distance i Update(), samma stil som PlayerAttack.cs använder
// för attack-räckvidd - ingen trigger-collider krävs.
public class QuestGiverNPC : MonoBehaviour
{
    [Header("Quest")]
    [Tooltip("Måste matcha questId på en QuestData-asset (se QuestManager.allQuests). Lämna tomt " +
             "om NPC:n bara ska ta emot inlämning (se Turn In Quest Id) utan att själv erbjuda quest.")]
    public string questId;

    [Header("Inlämning")]
    [Tooltip("Vilken quest den här NPC:n tar emot inlämning för när alla mål är klara. Lämna tomt " +
             "för att använda SAMMA quest som Quest Id ovan (vanligast - spelaren lämnar in hos " +
             "samma NPC som gav questen). Fyll bara i om inlämningen ska ske hos en ANNAN NPC än " +
             "den som gav questen (t.ex. \"prata med Sven för att avsluta\").")]
    public string turnInQuestId;

    [Header("Interaktion")]
    public float interactRange = 3.5f;
    public Vector3 promptWorldOffset = new Vector3(0f, 2.2f, 0f);

    [Tooltip("Ungefärlig ansiktshöjd (lokal offset från fötterna) - kameran zoomar in mot denna " +
             "punkt när dialogen öppnas (se NpcDialogueUI). Vanligtvis lite lägre än Prompt World " +
             "Offset ovan, som ligger högre för att sväva OVANFÖR huvudet.")]
    public Vector3 faceOffset = new Vector3(0f, 1.65f, 0f);

    private Transform player;
    private GameObject promptRoot;
    private TMP_Text promptText;
    private Camera cam;
    private bool playerInRange;

    void Start()
    {
        ThirdPersonController tpc = FindFirstObjectByType<ThirdPersonController>();
        if (tpc != null) player = tpc.transform;
        else Debug.LogWarning("QuestGiverNPC: Ingen ThirdPersonController hittades i scenen - kan inte avgöra avstånd till spelaren.");

        cam = Camera.main;
        BuildPrompt();
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool inRangeNow = dist <= interactRange;
        if (inRangeNow != playerInRange)
        {
            playerInRange = inRangeNow;
            if (playerInRange) SetPromptText(PromptForCurrentState());
            SetPromptVisible(playerInRange);
        }

        if (promptRoot != null)
        {
            promptRoot.transform.position = transform.position + promptWorldOffset;
            if (cam != null) promptRoot.transform.rotation = cam.transform.rotation;
        }

        bool dialogueOpen = NpcDialogueUI.Instance != null && NpcDialogueUI.Instance.IsOpen;
        if (!dialogueOpen && playerInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            HandleInteract();
    }

    string EffectiveTurnInQuestId => string.IsNullOrEmpty(turnInQuestId) ? questId : turnInQuestId;

    // Kollar FÖRST om det finns en klar quest att lämna in här - annars faller den tillbaka
    // till att erbjuda Quest Id (om ifyllt), samma prioritering som prompt-texten visar. Öppnar
    // NpcDialogueUI (kamera-zoom + Accept/Decline-ruta) om den finns i scenen - annars faller
    // båda grenarna tillbaka till det gamla direkta beteendet (ingen dialog krävs för att testa).
    void HandleInteract()
    {
        string turnIn = EffectiveTurnInQuestId;
        if (!string.IsNullOrEmpty(turnIn) && QuestManager.Instance != null && QuestManager.Instance.IsQuestReadyToTurnIn(turnIn))
        {
            QuestData turnInData = QuestManager.Instance.GetQuestData(turnIn);
            if (turnInData != null && NpcDialogueUI.Instance != null)
            {
                NpcDialogueUI.Instance.ShowTurnIn(turnInData, transform, faceOffset,
                    onTurnIn: () => { QuestManager.Instance.TryTurnInQuest(turnIn); SetPromptText(PromptForCurrentState()); });
                return;
            }

            QuestManager.Instance.TryTurnInQuest(turnIn);
            SetPromptText(PromptForCurrentState());
            return;
        }

        TryStartQuest();
    }

    void TryStartQuest()
    {
        if (string.IsNullOrEmpty(questId))
        {
            Debug.LogWarning("QuestGiverNPC: Inget Quest Id ifyllt på \"" + name + "\".");
            return;
        }
        if (QuestManager.Instance == null) return;

        if (QuestManager.Instance.IsQuestCompleted(questId) || QuestManager.Instance.IsQuestActive(questId))
        {
            SetPromptText(PromptForCurrentState());
            return;
        }

        QuestData data = QuestManager.Instance.GetQuestData(questId);
        if (data != null && NpcDialogueUI.Instance != null)
        {
            NpcDialogueUI.Instance.ShowQuestOffer(data, transform, faceOffset,
                onAccept: () => { QuestManager.Instance.StartQuest(questId); SetPromptText(PromptForCurrentState()); });
            return;
        }

        QuestManager.Instance.StartQuest(questId);
        SetPromptText(PromptForCurrentState());
    }

    string PromptForCurrentState()
    {
        if (QuestManager.Instance == null) return "[E] Prata";

        string turnIn = EffectiveTurnInQuestId;
        if (!string.IsNullOrEmpty(turnIn) && QuestManager.Instance.IsQuestReadyToTurnIn(turnIn))
            return "[E] Turn in quest";

        if (string.IsNullOrEmpty(questId)) return ""; // ren inlämnings-NPC, inget att erbjuda just nu

        if (QuestManager.Instance.IsQuestCompleted(questId)) return "Completed";
        if (QuestManager.Instance.IsQuestActive(questId)) return "Quest in progress";
        return "[E] Talk";
    }

    void BuildPrompt()
    {
        GameObject canvasGO = new GameObject("QuestPromptCanvas", typeof(Canvas));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.transform.localScale = Vector3.one * 0.02f;

        RectTransform canvasRT = (RectTransform)canvasGO.transform;
        canvasRT.sizeDelta = new Vector2(300f, 60f);

        promptRoot = canvasGO;

        GameObject textGO = new GameObject("PromptText", typeof(RectTransform));
        textGO.transform.SetParent(canvasGO.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        promptText = textGO.AddComponent<TextMeshProUGUI>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 28f;
        promptText.color = Color.white;
        promptText.raycastTarget = false;
        promptText.text = "[E] Talk";

        promptRoot.SetActive(false);
    }

    void SetPromptVisible(bool visible)
    {
        if (promptRoot != null) promptRoot.SetActive(visible);
    }

    void SetPromptText(string text)
    {
        if (promptText != null) promptText.text = text;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
