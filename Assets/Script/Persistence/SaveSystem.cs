using System.IO;
using UnityEngine;

// Minimal lokal fil-persistens - EN save-fil, inga karaktärsslots.
// Detta är INTE ett konto-/auth-system, bara grunden för det.
public static class SaveSystem
{
    private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "naxestra_save.json");

    public static void SaveGame(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    // Returnerar null om ingen save-fil finns.
    public static SaveData LoadGame()
    {
        if (!HasSaveFile()) return null;
        string json = File.ReadAllText(SavePath);
        return JsonUtility.FromJson<SaveData>(json);
    }

    // Tänkt att återanvändas av en framtida karaktärsval-meny (Continue/New Character).
    public static bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }
}
