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

    [Header("Alla quests som finns i spelet (dra in QuestData-assets manuellt)")]
    public List<QuestData> allQuests = new List<QuestData>();

    [Header("Referenser (hittas automatiskt)")]
    public PlayerExperience playerExperience;

    [Header("Test (frivilligt)")]
    [Tooltip("Ingen quest-giver-NPC finns än - fyll i ett questId här för att auto-starta den " +
             "questen vid Play, bara för att verifiera att flödet fungerar. Lämna tomt annars.")]
    public string autoStartQuestIdForTesting;

    // Framtida quest-UI (logg/tracker/popup) lyssnar på denna istället för att pollas -
    // samma mönster som PlayerExperience.OnXpGained.
    public event System.Action<QuestData> OnQuestCompleted;

    private List<QuestProgress> activeQuests = new List<QuestProgress>();
    private List<string> completedQuestIds = new List<string>();

    private CombatManager combatManager;

    void Awake()
    {
        Instance = this;
        LoadFromSaveIfPresent();
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
        Debug.Log("Quest startad: " + data.title);
    }

    // Kollar ALLA aktiva quests efter mål som matchar (type, targetId) och tickar dem.
    // Matchar valfritt objective i questen (inte bara "nuvarande") - stödjer quests där
    // flera mål kan göras i valfri ordning, t.ex. "döda 3 Roger OCH prata med Sven".
    public void ReportProgress(ObjectiveType type, string targetId, int amount = 1)
    {
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
            }

            if (!changed) continue;

            AdvanceCurrentObjectiveIndex(data, progress);

            if (AllObjectivesComplete(data, progress))
                CompleteQuest(data, progress);
        }
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
    }

    void HandleEnemyDefeated(Enemy enemy)
    {
        if (enemy == null) return;
        ReportProgress(ObjectiveType.Kill, enemy.enemyName, 1);
    }
}
