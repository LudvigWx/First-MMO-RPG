using UnityEngine;

// Enkel statisk "minneslapp" som håller vad spelaren valt i Character Creation.
// Steg 2 (Edit Appearance) och senare speluppstart läser härifrån.
// Statiska fält nollställs INTE automatiskt när man byter scen - det är avsiktligt,
// så valet följer med från Steg 1 till nästa scen/skärm.
public static class CharacterCreationSelection
{
    public static RaceData chosenRace;
    public static ClassData chosenClass;
    public static string chosenSubclassName;
    public static bool isMale = true;

    // Sparas i Steg 2, men tonar INTE huden ännu (se CharacterCreationStep2UI) -
    // paketets karaktär delar ett enda material för hela kroppen.
    public static Color chosenSkinColor = Color.white;

    public static void Reset()
    {
        chosenRace = null;
        chosenClass = null;
        chosenSubclassName = null;
        isMale = true;
        chosenSkinColor = Color.white;
    }
}
