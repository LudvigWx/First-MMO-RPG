using UnityEngine;
using UnityEngine.UI;

// Applicerar en PanelTheme på GameObjectets egen Image (bakgrund). Om "Border Image" är
// kopplat (valfritt, en separat Image för en kant/ram) får den PanelTheme.borderColor.
[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class ThemedPanel : MonoBehaviour
{
    public PanelTheme theme;

    [Tooltip("Valfri – en separat Image som fungerar som kant/ram, om panelen har en.")]
    public Image borderImage;

    private Image background;

    void OnValidate() => Apply();
    void Start() => Apply();

    public void Apply()
    {
        if (background == null) background = GetComponent<Image>();
        if (theme == null || background == null) return;

        if (theme.backgroundSprite != null) background.sprite = theme.backgroundSprite;
        background.color = theme.tint;

        if (borderImage != null) borderImage.color = theme.borderColor;
    }
}
