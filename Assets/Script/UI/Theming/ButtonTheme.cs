using UnityEngine;
using TMPro;

// Ett visuellt tema för knappar. Duplicera denna asset (högerklick i Project-fönstret >
// Create > Naxestra > UI Theme > Button Theme) och byt ut sprites/färger för att skapa
// en ny knapp-variant, utan att röra någon kod.
[CreateAssetMenu(menuName = "Naxestra/UI Theme/Button Theme", fileName = "NewButtonTheme")]
public class ButtonTheme : ScriptableObject
{
    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Sprite pressedSprite;
    public Sprite disabledSprite;

    [Header("Färg-tint (multipliceras med spriten, vit = ingen förändring)")]
    public Color tint = Color.white;

    [Header("Text")]
    public TMP_FontAsset font;
    public Color textColor = Color.white;
}
