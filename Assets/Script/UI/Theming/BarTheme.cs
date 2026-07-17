using UnityEngine;

// Ett visuellt tema för en stapel (Health/Rage/XP m.fl.). Varje stapel kopplas till sin
// EGNA BarTheme-asset, så de kan se olika ut samtidigt (t.ex. röd Health, orange Rage).
[CreateAssetMenu(menuName = "Naxestra/UI Theme/Bar Theme", fileName = "NewBarTheme")]
public class BarTheme : ScriptableObject
{
    [Header("Bakgrund (tom bar)")]
    public Sprite backgroundSprite;
    public Color backgroundTint = Color.white;

    [Header("Fyllning")]
    public Sprite fillSprite;
    public Color fillColor = Color.white;
}
