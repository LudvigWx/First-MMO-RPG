using UnityEngine;

// Ett visuellt tema för paneler/fönster-bakgrunder. Duplicera denna asset för att skapa
// en ny panel-variant (t.ex. sten istället för pergament) utan att röra kod.
[CreateAssetMenu(menuName = "Naxestra/UI Theme/Panel Theme", fileName = "NewPanelTheme")]
public class PanelTheme : ScriptableObject
{
    [Header("Bakgrund")]
    public Sprite backgroundSprite;
    public Color tint = Color.white;

    [Header("Valfri kant-färg (används bara om panelen har en egen Border-Image kopplad)")]
    public Color borderColor = Color.white;
}
