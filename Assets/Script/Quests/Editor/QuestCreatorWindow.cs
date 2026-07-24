using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Enkelt formulär för att skapa en ny quest utan att fylla i råa ScriptableObject-fält i
// Inspector. Skapar assets i Assets/Resources/Quests, samma mapp som QuestManager läser in
// automatiskt (se QuestManager.LoadQuestsFromResources) - ingen manuell drag-och-släpp behövs.
public class QuestCreatorWindow : EditorWindow
{
    private const string QuestFolder = "Assets/Resources/Quests";

    private string questId = "";
    private string title = "";
    private string description = "";
    private string questGiverId = "";
    private int rewardXp = 50;
    private Sprite icon;
    private Color accentColor = new Color(1f, 1f, 1f, 0f);
    private readonly List<QuestObjective> objectives = new List<QuestObjective>();
    private Vector2 scroll;

    [MenuItem("Quests/Ny Quest...")]
    public static void Open()
    {
        QuestCreatorWindow window = GetWindow<QuestCreatorWindow>(true, "Ny Quest");
        window.minSize = new Vector2(420f, 480f);
        if (window.objectives.Count == 0) window.objectives.Add(new QuestObjective { requiredAmount = 1 });
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Grundinfo", EditorStyles.boldLabel);
        questId = EditorGUILayout.TextField(new GUIContent("Quest Id", "Unikt internt id, t.ex. \"skogens_hemlighet\". Inga mellanslag."), questId);
        title = EditorGUILayout.TextField(new GUIContent("Titel", "Visas i Journal-listan."), title);
        EditorGUILayout.LabelField("Beskrivning");
        description = EditorGUILayout.TextArea(description, GUILayout.Height(50f));
        questGiverId = EditorGUILayout.TextField(new GUIContent("Quest Giver Id", "Frivilligt - vilken NPC som ger questen."), questGiverId);

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Mål", EditorStyles.boldLabel);

        for (int i = 0; i < objectives.Count; i++)
        {
            QuestObjective obj = objectives[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mål " + (i + 1), EditorStyles.boldLabel);
            GUI.enabled = objectives.Count > 1;
            if (GUILayout.Button("Ta bort", GUILayout.Width(70f))) { objectives.RemoveAt(i); GUI.enabled = true; EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            obj.type = (ObjectiveType)EditorGUILayout.EnumPopup(new GUIContent("Typ", "Kill = döda fiende, Collect/Interact/TalkTo = item-/NPC-id, ReachZone = scennamn."), obj.type);
            obj.targetId = EditorGUILayout.TextField(new GUIContent("Target Id", "T.ex. Enemy.enemyName för Kill-mål (matchar exakt, skiftlägeskänsligt)."), obj.targetId);
            obj.requiredAmount = Mathf.Max(1, EditorGUILayout.IntField("Antal som krävs", obj.requiredAmount));
            obj.description = EditorGUILayout.TextField(new GUIContent("Beskrivning", "Visas i quest-listan, t.ex. \"Döda 3 vargar\"."), obj.description);

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("+ Lägg till mål"))
            objectives.Add(new QuestObjective { requiredAmount = 1 });

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Belöning", EditorStyles.boldLabel);
        rewardXp = Mathf.Max(0, EditorGUILayout.IntField("XP", rewardXp));

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Utseende i Journal (frivilligt)", EditorStyles.boldLabel);
        icon = (Sprite)EditorGUILayout.ObjectField("Ikon", icon, typeof(Sprite), false);
        accentColor = EditorGUILayout.ColorField(new GUIContent("Accentfärg", "Höj alpha för en färgad kant på quest-raden."), accentColor);

        EditorGUILayout.Space(16f);

        bool valid = !string.IsNullOrEmpty(questId) && !string.IsNullOrEmpty(title);
        if (!valid) EditorGUILayout.HelpBox("Quest Id och Titel krävs.", MessageType.Warning);

        GUI.enabled = valid;
        if (GUILayout.Button("Skapa Quest", GUILayout.Height(32f)))
            CreateQuest();
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    void CreateQuest()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(QuestFolder);

        string path = QuestFolder + "/" + questId + ".asset";
        if (AssetDatabase.LoadAssetAtPath<QuestData>(path) != null)
        {
            EditorUtility.DisplayDialog("Finns redan", "En quest med Quest Id \"" + questId + "\" finns redan (" + path + ").", "OK");
            return;
        }

        QuestData data = CreateInstance<QuestData>();
        data.questId = questId;
        data.title = title;
        data.description = description;
        data.questGiverId = questGiverId;
        data.objectives = new List<QuestObjective>(objectives);
        data.rewardXp = rewardXp;
        data.icon = icon;
        data.accentColor = accentColor;

        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = data;
        EditorGUIUtility.PingObject(data);

        EditorUtility.DisplayDialog("Klart", "Questen \"" + title + "\" skapades i " + path + ".\n\n" +
            "Den hittas automatiskt av QuestManager vid Play - ingen manuell koppling behövs.", "OK");

        Close();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string newFolderName = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, newFolderName);
    }
}
