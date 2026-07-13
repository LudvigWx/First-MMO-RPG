using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor-verktyg (körs bara i Unity Editor, hamnar aldrig i ett bygge).
// Skapar exempel-RaceData och exempel-ClassData automatiskt så vi slipper
// klicka ihop 6 assets för hand. Säker att köra flera gånger - hoppar
// över assets som redan finns istället för att skriva över dem.
//
// Använd: menyn "Character Creation" -> "Skapa exempel-data (Steg 1)".
public static class CharacterCreationDataSetup
{
    private const string RaceFolder = "Assets/Data/Races";
    private const string ClassFolder = "Assets/Data/Classes";

    [MenuItem("Character Creation/Skapa exempel-data (Steg 1)")]
    public static void CreateExampleData()
    {
        EnsureFolder("Assets/Data");
        EnsureFolder(RaceFolder);
        EnsureFolder(ClassFolder);

        // --- Klasser (skapas först, så raser kan referera dem i excludedClasses) ---
        ClassData warbrand = CreateClassAsset(
            "Warbrand",
            new List<string> { "Bulwark", "Ravager" });

        ClassData magebound = CreateClassAsset(
            "Magebound",
            new List<string> { "Pyromancer", "Frostbinder" });

        ClassData hallowpriest = CreateClassAsset(
            "Hallowpriest",
            new List<string> { "Lightbringer", "Dawnkeeper" });

        // --- Raser ---
        CreateRaceAsset(
            "Sunhaven Human",
            "Sunhaven",
            new List<Color>
            {
                new Color(1.00f, 0.86f, 0.72f),
                new Color(0.94f, 0.76f, 0.56f),
                new Color(0.76f, 0.57f, 0.40f),
            });

        CreateRaceAsset(
            "Sandheimr Human",
            "Sandheimr",
            new List<Color>
            {
                new Color(0.87f, 0.68f, 0.48f),
                new Color(0.70f, 0.50f, 0.32f),
                new Color(0.55f, 0.38f, 0.24f),
            });

        CreateRaceAsset(
            "Bol'Gath Human",
            "Bol'Gath",
            new List<Color>
            {
                new Color(0.65f, 0.50f, 0.40f),
                new Color(0.45f, 0.33f, 0.26f),
                new Color(0.30f, 0.22f, 0.18f),
            });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Character Creation: exempel-data klar (3 raser i " + RaceFolder + ", 3 klasser i " + ClassFolder + ").");
    }

    private static ClassData CreateClassAsset(string className, List<string> subclassNames)
    {
        string path = ClassFolder + "/" + className + ".asset";
        ClassData existing = AssetDatabase.LoadAssetAtPath<ClassData>(path);
        if (existing != null)
        {
            Debug.Log("Hoppar över (finns redan): " + path);
            return existing;
        }

        ClassData data = ScriptableObject.CreateInstance<ClassData>();
        data.className = className;
        data.subclassNames = subclassNames;

        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static void CreateRaceAsset(string raceName, string worldRegion, List<Color> skinColors)
    {
        string path = RaceFolder + "/" + raceName + ".asset";
        if (AssetDatabase.LoadAssetAtPath<RaceData>(path) != null)
        {
            Debug.Log("Hoppar över (finns redan): " + path);
            return;
        }

        RaceData data = ScriptableObject.CreateInstance<RaceData>();
        data.raceName = raceName;
        data.worldRegion = worldRegion;
        data.excludedClasses = new List<ClassData>(); // inga uteslutningar för exempel-raserna
        data.availableSkinColors = skinColors;

        AssetDatabase.CreateAsset(data, path);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string newFolderName = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, newFolderName);
    }
}
