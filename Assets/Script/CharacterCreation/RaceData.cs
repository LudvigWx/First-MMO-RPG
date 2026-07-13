using System.Collections.Generic;
using UnityEngine;

// Datamall för en spelbar ras. Skapa nya raser via
// högerklick i Project-fönstret -> Create -> Character Creation -> Race Data.
// Lägg INGEN logik här - bara data. UI:t läser detta och bygger knappar automatiskt.
[CreateAssetMenu(fileName = "NewRaceData", menuName = "Character Creation/Race Data")]
public class RaceData : ScriptableObject
{
    [Header("Grundinfo")]
    public string raceName;

    [Tooltip("Vilken världsdel/region rasen kommer från, t.ex. \"Sunhaven\".")]
    public string worldRegion;

    [Header("Klassbegränsningar")]
    [Tooltip("Klasser som INTE går att välja för denna ras. Tom lista = alla klasser tillåtna.")]
    public List<ClassData> excludedClasses = new List<ClassData>();

    [Header("Utseende")]
    [Tooltip("Hudfärger spelaren kan välja mellan för denna ras.")]
    public List<Color> availableSkinColors = new List<Color>();

    // Hjälpmetod: true om denna ras får spela given klass.
    public bool AllowsClass(ClassData classData)
    {
        return !excludedClasses.Contains(classData);
    }
}
