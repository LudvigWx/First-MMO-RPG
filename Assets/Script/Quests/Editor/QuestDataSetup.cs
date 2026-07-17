using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor-verktyg (körs bara i Unity Editor, hamnar aldrig i ett bygge).
// Skapar en testquest automatiskt - bara till för att verifiera att QuestManager
// tickar progress, ingen quest-giver-NPC eller UI hör till detta steg.
// Säker att köra flera gånger - hoppar över asset som redan finns.
//
// Använd: menyn "Quests" -> "Skapa testquest (Bevisa dig sjalv)".
public static class QuestDataSetup
{
    private const string QuestFolder = "Assets/Data/Quests";

    [MenuItem("Quests/Skapa testquest (Bevisa dig sjalv)")]
    public static void CreateTestQuest()
    {
        EnsureFolder("Assets/Data");
        EnsureFolder(QuestFolder);

        string path = QuestFolder + "/BevisaDigSjalv.asset";
        if (AssetDatabase.LoadAssetAtPath<QuestData>(path) != null)
        {
            Debug.Log("Hoppar över (finns redan): " + path);
            return;
        }

        QuestData data = ScriptableObject.CreateInstance<QuestData>();
        data.questId = "bevisa_dig_sjalv";
        data.title = "Bevisa dig själv";
        data.description = "Döda 3 Roger för att visa att du är redo.";
        data.questGiverId = "";
        data.objectives = new List<QuestObjective>
        {
            new QuestObjective
            {
                type = ObjectiveType.Kill,
                targetId = "Roger", // Matchar Enemy.enemyName på Enemy_Roger-prefaben, inte prefabens filnamn
                requiredAmount = 3,
                description = "Döda Roger (0/3)"
            }
        };
        data.rewardXp = 50;

        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Testquest skapad: " + path);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string newFolderName = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, newFolderName);
    }
}
