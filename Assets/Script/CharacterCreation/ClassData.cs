using System.Collections.Generic;
using UnityEngine;

// Datamall för en spelbar klass. Skapa nya klasser via
// högerklick i Project-fönstret -> Create -> Character Creation -> Class Data.
// Subklasserna är bara namn här - ingen egen logik/förmågor kopplas i detta steg.
[CreateAssetMenu(fileName = "NewClassData", menuName = "Character Creation/Class Data")]
public class ClassData : ScriptableObject
{
    [Header("Grundinfo")]
    public string className;

    [Header("Subklasser")]
    [Tooltip("Bara namn för nu, ingen logik kopplad.")]
    public List<string> subclassNames = new List<string>();
}
