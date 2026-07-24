using UnityEngine;
using TMPro;
using System.Collections;

// "1 / 2"-popup ovanför fienden när ett Kill-mål tickar framåt - exakt samma popup-mönster
// (världsposition -> skärmposition via Camera.WorldToScreenPoint, glid uppåt + tona) som
// PlayerHudUI:s XP-popup, fast lyssnar på QuestManager.OnObjectiveProgressTicked istället.
//
// Lägg detta script på SAMMA tomma GameObject som QuestTrackerUI (eller valfritt annat
// GameObject i scenen) - hittar PlayerHudCanvas automatiskt precis som QuestTrackerUI.
public class QuestProgressPopupUI : MonoBehaviour
{
    [Header("Utseende")]
    public Color popupColor = new Color(1f, 0.85f, 0.3f, 1f);
    public float fontSize = 26f;
    public float duration = 1.1f;
    public float riseDistance = 50f;
    public float worldHeightOffset = 2.4f;
    [Range(0f, 0.5f)] public float fadeInFraction = 0.15f;

    private QuestManager questManager;
    private Transform canvasTransform;

    void Start()
    {
        EnsureCanvasTransform();
    }

    void OnEnable()
    {
        FindQuestManager();
        if (questManager != null) questManager.OnObjectiveProgressTicked += HandleObjectiveProgressTicked;
    }

    void OnDisable()
    {
        if (questManager != null) questManager.OnObjectiveProgressTicked -= HandleObjectiveProgressTicked;
    }

    void FindQuestManager()
    {
        if (questManager == null) questManager = QuestManager.Instance;
        if (questManager == null) questManager = FindFirstObjectByType<QuestManager>();
    }

    void EnsureCanvasTransform()
    {
        if (canvasTransform != null) return;
        GameObject canvasGO = GameObject.Find("PlayerHudCanvas");
        if (canvasGO != null) canvasTransform = canvasGO.transform;
    }

    void HandleObjectiveProgressTicked(QuestObjective objective, int current, int required, Vector3 worldPosition)
    {
        // Bara Kill-mål skickar en position idag (se QuestManager.HandleEnemyDefeated) - andra
        // måltyper (Collect/Interact/ReachZone/TalkTo) tickar utan position och visar ingen popup.
        if (objective.type != ObjectiveType.Kill) return;
        SpawnPopup(current, required, worldPosition);
    }

    void SpawnPopup(int current, int required, Vector3 worldPosition)
    {
        EnsureCanvasTransform();
        if (canvasTransform == null) return;

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null) return;

        Vector3 worldPos = worldPosition + Vector3.up * worldHeightOffset;
        Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
        if (screenPoint.z < 0f) return; // fienden bakom kameran, hoppa över

        GameObject popupGO = new GameObject("QuestProgressPopup", typeof(RectTransform));
        RectTransform popupRT = popupGO.GetComponent<RectTransform>();
        popupRT.SetParent(canvasTransform, false);
        popupRT.anchorMin = new Vector2(0.5f, 0.5f);
        popupRT.anchorMax = new Vector2(0.5f, 0.5f);
        popupRT.pivot = new Vector2(0.5f, 0.5f);
        popupRT.sizeDelta = new Vector2(200f, 40f);
        popupRT.position = new Vector3(screenPoint.x, screenPoint.y, 0f);

        TMP_Text text = popupGO.AddComponent<TextMeshProUGUI>();
        text.text = current + " / " + required;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = popupColor;
        text.raycastTarget = false;

        StartCoroutine(AnimatePopup(popupGO, text));
    }

    IEnumerator AnimatePopup(GameObject popupGO, TMP_Text text)
    {
        RectTransform popupRT = (RectTransform)popupGO.transform;
        Vector3 startPos = popupRT.position;
        Color c = text.color;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            popupRT.position = startPos + new Vector3(0f, Mathf.Lerp(0f, riseDistance, p), 0f);

            float alpha;
            if (fadeInFraction > 0f && p < fadeInFraction)
                alpha = Mathf.Lerp(0f, 1f, p / fadeInFraction);
            else
                alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01((p - 0.3f) / 0.7f));
            c.a = alpha;
            text.color = c;

            yield return null;
        }

        Destroy(popupGO);
    }
}
