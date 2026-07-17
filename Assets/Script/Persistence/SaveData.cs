using System.Collections.Generic;
using UnityEngine;

// Enkel data-container för EN sparad karaktär. Fältnamnen matchar det som
// faktiskt finns i CharacterCreationSelection/PlayerExperience idag:
// raceName/className/subclassName är strängar (RaceData/ClassData har inga
// ID-fält), gender heter isMale. Utseende (hår/ögon/hudfärg) och upplåsta
// abilities är medvetet uteslutna - ingen befintlig kod fångar upp de valen än.
[System.Serializable]
public class SaveData
{
    public string raceName;
    public string className;
    public string subclassName;
    public bool isMale;

    public int level;
    public int currentXP;

    public string lastZoneName;
    public Vector3 lastPosition;

    // Questlogg - se QuestManager/QuestProgress. Spelar-specifik, ingår i samma save-fil.
    public List<QuestProgress> activeQuests = new List<QuestProgress>();
    public List<string> completedQuestIds = new List<string>();
}
