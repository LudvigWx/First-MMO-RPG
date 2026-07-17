using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Applicerar en ButtonTheme på Button-komponentens Image (sprites/SpriteState) och på ett
// TMP_Text-barn (font/färg). [ExecuteAlways] gör att ändringar syns direkt i Editorn,
// utan Play Mode, precis som resten av det programmatiskt byggda UI:t i projektet.
[ExecuteAlways]
[RequireComponent(typeof(Button))]
public class ThemedButton : MonoBehaviour
{
    public ButtonTheme theme;

    private Button button;
    private Image image;
    private TMP_Text label;

    void OnValidate() => Apply();
    void Start() => Apply();

    public void Apply()
    {
        if (button == null) button = GetComponent<Button>();
        if (button == null) return;

        if (image == null) image = button.targetGraphic as Image;
        if (image == null) image = GetComponent<Image>();
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);

        if (theme == null) return;

        if (image != null)
        {
            if (button.targetGraphic == null) button.targetGraphic = image;
            if (theme.normalSprite != null) image.sprite = theme.normalSprite;
            image.color = theme.tint;
        }

        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = theme.hoverSprite,
            pressedSprite = theme.pressedSprite,
            disabledSprite = theme.disabledSprite
        };

        if (label != null)
        {
            if (theme.font != null) label.font = theme.font;
            label.color = theme.textColor;
        }
    }
}
