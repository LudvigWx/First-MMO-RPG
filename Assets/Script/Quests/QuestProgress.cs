using System.Collections.Generic;

// Spelar-specifik runtime-data för EN påbörjad quest - skild från QuestData eftersom detta
// är olika för varje spelare (två spelare med samma QuestData kan ha kommit olika långt).
// Sparas i SaveData (se SaveSystem) tillsammans med resten av karaktärsdatan.
[System.Serializable]
public class QuestProgress
{
    public string questId;
    public int currentObjectiveIndex;
    public List<int> objectiveProgress = new List<int>();
    public bool isCompleted;

    // Alla mål klara men INTE inlämnad än - måste lämnas in hos rätt NPC (QuestGiverNPC.TryTurnIn)
    // innan CompleteQuest/belöning triggas. Skild från isCompleted, som bara sätts EFTER inlämning.
    public bool objectivesComplete;
}
