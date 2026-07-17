using UnityEngine;
using UnityEngine.UI;

// Applicerar en BarTheme på en stapels bakgrunds- och fyllnads-Image. Rör bara sprite/färg
// — fillImage.type/fillMethod/fillAmount (som styr hur mycket av baren som är fylld)
// sätts fortfarande av t.ex. PlayerHudUI, inte här.
[ExecuteAlways]
public class ThemedBar : MonoBehaviour
{
    public BarTheme theme;
    public Image backgroundImage;
    public Image fillImage;

    void OnValidate() => Apply();
    void Start() => Apply();

    public void Apply()
    {
        if (theme == null) return;

        if (backgroundImage != null)
        {
            if (theme.backgroundSprite != null)
            {
                // Sliced (9-slice) istället för Simple så en rundad-hörn-sprite inte blir
                // utsträckt/förvrängd när baren är mycket bredare än den är hög.
                backgroundImage.sprite = theme.backgroundSprite;
                backgroundImage.type = Image.Type.Sliced;
            }
            backgroundImage.color = theme.backgroundTint;
        }

        if (fillImage != null)
        {
            // OBS: fillImage använder Image.Type.Filled (satt av t.ex. PlayerHudUI) för att
            // maskera hur mycket av baren som visas — Filled stöder INTE 9-slice, så
            // fillSprite måste vara en helt platt sprite utan rundade hörn/kant, annars
            // blir baren en förvrängd "kudde"/lins när den är bred och tunn.
            if (theme.fillSprite != null) fillImage.sprite = theme.fillSprite;
            fillImage.color = theme.fillColor;
        }
    }
}
