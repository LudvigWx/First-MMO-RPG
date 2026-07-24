using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Håller koll på spelarens aktiva/avklarade quests och tickar progress. Lägg på PlayerArmature
// (bredvid PlayerExperience/CombatManager) - EN per scen, precis som CombatManager/PlayerExperience,
// inte DontDestroyOnLoad (inget annat system i projektet persisterar mellan scener - progress
// sparas/laddas istället via SaveSystem i varje scen, samma mönster som PlayerExperience.Awake()).
//
// Fångar kills via CombatManager.EnemyDefeated (samma event PlayerExperience redan lyssnar på för
// XP) istället för att lägga ett anrop i Enemy.cs/EnemyAI.cs - undviker att röra combat-koden alls.
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Alla quests som finns i spelet")]
    [Tooltip("Fylls i automatiskt vid Awake med alla QuestData-assets som ligger i " +
             "Assets/Resources/Quests (se LoadQuestsFromResources) - manuell drag-och-släpp " +
             "behövs inte längre, men fungerar fortfarande om du vill lägga till en asset som " +
             "ligger utanför den mappen.")]
    public List<QuestData> allQuests = new List<QuestData>();

    [Header("Referenser (hittas automatiskt)")]
    public PlayerExperience playerExperience;

    [Header("Test (frivilligt)")]
    [Tooltip("Fyll i ett questId här för att auto-starta den questen vid Play, utan att behöva " +
             "en QuestGiverNPC i scenen. Lämna tomt annars.")]
    public string autoStartQuestIdForTesting;

    // Framtida quest-UI (logg/tracker/popup) lyssnar på denna istället för att pollas -
    // samma mönster som PlayerExperience.OnXpGained.
    public event System.Action<QuestData> OnQuestCompleted;

    // Fångar ALLT som kan ändra vad en tracker-UI ska visa (ny quest, progress, klar quest,
    // track på/av) - QuestsPanelView och QuestTrackerUI lyssnar båda på denna istället för
    // att ha varsin egen kopia av track-statusen.
    public event System.Action OnQuestsChanged;

    // Ett enda mål tickade framåt PÅ EN KÄND VÄRLDSPOSITION (bara kills skickar en position
    // idag, se HandleEnemyDefeated) - QuestProgressPopupUI lyssnar på denna för att visa
    // "1 / 2" ovanför fienden, samma mönster som PlayerExperience.OnXpGained -> XP-popupen.
    public event System.Action<QuestObjective, int, int, Vector3> OnObjectiveProgressTicked;

    // Alla mål på en quest är klara men den väntar på att lämnas in (se QuestProgress.objectivesComplete
    // och TryTurnInQuest) - UI kan använda denna för att t.ex. visa "Lämna in!" i tracker/Journal.
    public event System.Action<QuestData> OnQuestReadyToTurnIn;

    private List<QuestProgress> activeQuests = new List<QuestProgress>();
    private List<string> completedQuestIds = new List<string>();

    // Vilka aktiva quests som ska visas i QuestTrackerUI (HUD-overlayen). Nya quests trackas
    // automatiskt vid StartQuest - spelaren behöver INTE klicka Track manuellt för varje quest,
    // Track-knappen i Journal är bara en visa/dölj-toggle för HUD-rutan.
    private readonly HashSet<string> trackedQuestIds = new HashSet<string>();

    private CombatManager combatManager;

    void Awake()
    {
        Instance = this;
        LoadQuestsFromResources();
        LoadFromSaveIfPresent();
    }

    // Slår ihop alla QuestData-assets i Resources/Quests med den manuellt ifyllda listan (om
    // någon redan ligger där, t.ex. en asset som medvetet placerats utanför Resources-mappen).
    // Resources fungerar likadant i Editorn och i ett bygge, till skillnad från AssetDatabase.
    void LoadQuestsFromResources()
    {
        QuestData[] found = Resources.LoadAll<QuestData>("Quests");
        foreach (QuestData data in found)
        {
            if (data == null || allQuests.Contains(data)) continue;
            allQuests.Add(data);
        }
    }

    void Start()
    {
        if (playerExperience == null) playerExperience = FindFirstObjectByType<PlayerExperience>();

        combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager != null)
            combatManager.EnemyDefeated += HandleEnemyDefeated;
        else
            Debug.LogWarning("QuestManager: Ingen CombatManager hittades - Kill-mål tickar inte.");

        if (!string.IsNullOrEmpty(autoStartQuestIdForTesting))
            StartQuest(autoStartQuestIdForTesting);
    }

    void OnDestroy()
    {
        if (combatManager != null)
            combatManager.EnemyDefeated -= HandleEnemyDefeated;
    }

    // Läser samma save-fil som PlayerExperience (se SaveSystem) - varje system laddar sin egen
    // del av datan i Awake, ingen delad laddningskod.
    void LoadFromSaveIfPresent()
    {
        if (!SaveSystem.HasSaveFile()) return;

        SaveData data = SaveSystem.LoadGame();
        if (data == null) return;

        activeQuests = data.activeQuests != null ? new List<QuestProgress>(data.activeQuests) : new List<QuestProgress>();
        completedQuestIds = data.completedQuestIds != null ? new List<string>(data.completedQuestIds) : new List<string>();
    }

    // Kallas av PauseMenuController.SaveCurrentCharacter() när karaktären sparas.
    public List<QuestProgress> GetActiveQuestsForSave() => activeQuests;
    public List<string> GetCompletedQuestIdsForSave() => completedQuestIds;

    QuestData FindQuestData(string questId)
    {
        return allQuests.FirstOrDefault(q => q != null && q.questId == questId);
    }

    // Publik variant åt NpcDialogueUI/QuestGiverNPC - de behöver titel/beskrivning för att
    // bygga dialogtexten, men ska inte behöva egen LINQ-sökning över allQuests.
    public QuestData GetQuestData(string questId) => FindQuestData(questId);

    public bool IsQuestActive(string questId)
    {
        return activeQuests.Any(p => p.questId == questId);
    }

    public bool IsQuestCompleted(string questId)
    {
        return completedQuestIds.Contains(questId);
    }

    public void StartQuest(string questId)
    {
        if (IsQuestActive(questId) || IsQuestCompleted(questId)) return;

        QuestData data = FindQuestData(questId);
        if (data == null)
        {
            Debug.LogWarning("QuestManager: Hittade ingen QuestData med questId '" + questId + "'.");
            return;
        }

        QuestProgress progress = new QuestProgress
        {
            questId = questId,
            currentObjectiveIndex = 0,
            isCompleted = false,
            objectiveProgress = new List<int>(new int[data.objectives.Count])
        };

        activeQuests.Add(progress);
        trackedQuestIds.Add(questId);
        Debug.Log("Quest startad: " + data.title);

        OnQuestsChanged?.Invoke();
    }

    public bool IsQuestTracked(string questId) => trackedQuestIds.Contains(questId);

    public void SetQuestTracked(string questId, bool tracked)
    {
        if (tracked) trackedQuestIds.Add(questId);
        else trackedQuestIds.Remove(questId);

        OnQuestsChanged?.Invoke();
    }

    public void ToggleQuestTracked(string questId) => SetQuestTracked(questId, !IsQuestTracked(questId));

    // Kollar ALLA aktiva quests efter mål som matchar (type, targetId) och tickar dem.
    // Matchar valfritt objective i questen (inte bara "nuvarande") - stödjer quests där
    // flera mål kan göras i valfri ordning, t.ex. "döda 3 Roger OCH prata med Sven".
    public void ReportProgress(ObjectiveType type, string targetId, int amount = 1, Vector3? worldPosition = null)
    {
        bool anyChanged = false;

        // Itererar över en kopia - CompleteQuest tar bort ur activeQuests, vilket annars
        // kraschar ett pågående foreach över samma lista (InvalidOperationException).
        foreach (QuestProgress progress in new List<QuestProgress>(activeQuests))
        {
            if (progress.isCompleted) continue;

            QuestData data = FindQuestData(progress.questId);
            if (data == null) continue;

            bool changed = false;
            for (int i = 0; i < data.objectives.Count; i++)
            {
                QuestObjective objective = data.objectives[i];
                if (objective.type != type || objective.targetId != targetId) continue;
                if (progress.objectiveProgress[i] >= objective.requiredAmount) continue;

                progress.objectiveProgress[i] = Mathf.Min(progress.objectiveProgress[i] + amount, objective.requiredAmount);
                changed = true;

                if (worldPosition.HasValue)
                    OnObjectiveProgressTicked?.Invoke(objective, progress.objectiveProgress[i], objective.requiredAmount, worldPosition.Value);
            }

            if (!changed) continue;
            anyChanged = true;

            AdvanceCurrentObjectiveIndex(data, progress);

            // Blir INTE klar automatiskt längre - måste lämnas in hos rätt NPC (se TryTurnInQuest).
            // !objectivesComplete-kollen förhindrar att eventet triggas om igen på en efterföljande
            // (irrelevant) ReportProgress-anrop innan spelaren hunnit lämna in.
            if (!progress.objectivesComplete && AllObjectivesComplete(data, progress))
            {
                progress.objectivesComplete = true;
                OnQuestReadyToTurnIn?.Invoke(data);
            }
        }

        if (anyChanged) OnQuestsChanged?.Invoke();
    }

    public bool IsQuestReadyToTurnIn(string questId)
    {
        return activeQuests.Any(p => p.questId == questId && p.objectivesComplete && !p.isCompleted);
    }

    // Kallas av QuestGiverNPC (eller vilken NPC som är satt som turn-in-mål för questen) när
    // spelaren interagerar. Returnerar false om questen inte är redo (fel NPC-flöde/dubbelklick
    // hanteras tyst av anroparen - se QuestGiverNPC.PromptForCurrentState).
    public bool TryTurnInQuest(string questId)
    {
        QuestProgress progress = activeQuests.FirstOrDefault(p => p.questId == questId && p.objectivesComplete && !p.isCompleted);
        if (progress == null) return false;

        QuestData data = FindQuestData(questId);
        if (data == null) return false;

        CompleteQuest(data, progress);
        return true;
    }

    // Uppdaterar pekaren till första ej klara målet - används av framtida tracker-UI, påverkar
    // inte vilka mål ReportProgress får ticka (se ovan).
    void AdvanceCurrentObjectiveIndex(QuestData data, QuestProgress progress)
    {
        for (int i = 0; i < data.objectives.Count; i++)
        {
            if (progress.objectiveProgress[i] < data.objectives[i].requiredAmount)
            {
                progress.currentObjectiveIndex = i;
                return;
            }
        }
        progress.currentObjectiveIndex = data.objectives.Count;
    }

    bool AllObjectivesComplete(QuestData data, QuestProgress progress)
    {
        for (int i = 0; i < data.objectives.Count; i++)
        {
            if (progress.objectiveProgress[i] < data.objectives[i].requiredAmount) return false;
        }
        return true;
    }

    void CompleteQuest(QuestData data, QuestProgress progress)
    {
        progress.isCompleted = true;
        activeQuests.Remove(progress);
        completedQuestIds.Add(data.questId);

        Debug.Log("Quest avklarad: " + data.title);

        if (playerExperience != null) playerExperience.AddXP(data.rewardXp);

        OnQuestCompleted?.Invoke(data);
        OnQuestsChanged?.Invoke();
    }

    void HandleEnemyDefeated(Enemy enemy)
    {
        if (enemy == null) return;
        ReportProgress(ObjectiveType.Kill, enemy.enemyName, 1, enemy.transform.position);
    }
}
