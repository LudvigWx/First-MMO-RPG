using System.Collections.Generic;
using UnityEngine;

// Vilken typ av mål ett QuestObjective beskriver. targetId tolkas olika beroende på typ:
// Kill -> Enemy.enemyName, Collect/Interact/TalkTo -> ett item-/NPC-id, ReachZone -> ett scennamn.
public enum ObjectiveType { Kill, Collect, Interact, ReachZone, TalkTo }

// Ett enda mål i en quest. Medvetet EN klass för alla typer (inte en subklass per ObjectiveType) -
// samma mönster som RaceData/ClassData: ingen logik här, bara data som QuestManager tolkar.
[System.Serializable]
public class QuestObjective
{
    public ObjectiveType type;
    public string targetId;
    public int requiredAmount = 1;
    public string description;
}

// Datamall för EN quest - delad "mall" som återanvänds av alla spelare som har questen.
// Spelar-specifik data (vilket mål man kommit till, om den är klar) ligger i QuestProgress
// istället, se den filen.
//
// Skapa nya quests via menyn Quests -> Ny Quest... (QuestCreatorWindow, ett enkelt formulär) -
// eller manuellt via Project-fönstret -> Create -> Quests -> Quest Data. Assets som hamnar i
// Assets/Resources/Quests hittas automatiskt av QuestManager, ingen manuell koppling behövs.
[CreateAssetMenu(fileName = "NewQuestData", menuName = "Quests/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Grundinfo")]
    public string questId;
    public string title;
    [TextArea]
    public string description;
    public string questGiverId;

    [Header("Mål")]
    public List<QuestObjective> objectives = new List<QuestObjective>();

    [Header("Belöning")]
    public int rewardXp;

    [Tooltip("Currency-systemet är inte byggt än - fältet ligger på plats för framtida bruk.")]
    public int rewardCurrency;

    [Tooltip("Item-systemet är inte byggt än - fältet ligger på plats för framtida bruk.")]
    public List<string> rewardItemIds = new List<string>();

    [Header("Krav (framtida questkedjor)")]
    public List<string> prerequisiteQuestIds = new List<string>();

    [Header("Utseende i Journal (frivilligt)")]
    [Tooltip("Valfri ikon bredvid titeln i quest-listan. Lämna tom för ingen ikon.")]
    public Sprite icon;
    [Tooltip("Alpha 0 (osynlig) som standard - höj alpha för att ge questen en färgad kant i listan.")]
    public Color accentColor = new Color(1f, 1f, 1f, 0f);

    [Header("Karma-gate (EJ implementerad - fälten ligger bara på plats för framtiden)")]
    [Tooltip("Lämna null/oanvänd om questen inte ska karma-gatas.")]
    public int? minKarma;
    public int? maxKarma;
}
