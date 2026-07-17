using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Visar karma (platshållar-ikon + siffra). Karma-systemet är inte byggt än - SetKarma(int) är
// den enda inkopplingspunkten en riktig datakälla behöver anropa den dagen systemet finns.
public class KarmaDisplayUI : MonoBehaviour
{
    public Color iconColor = new Color(0.5f, 0.8f, 1f);

    private int karma;
    private TMP_Text amountText;
    private bool built;

    public void BuildLayout()
    {
        if (built) return;
        built = true;

        HorizontalLayoutGroup layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconGO.transform.SetParent(transform, false);
        iconGO.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 20f);
        LayoutElement iconLE = iconGO.GetComponent<LayoutElement>();
        iconLE.preferredWidth = 20f;
        iconLE.preferredHeight = 20f;
        iconGO.GetComponent<Image>().color = iconColor;

        GameObject textGO = new GameObject("Amount", typeof(RectTransform), typeof(LayoutElement));
        textGO.transform.SetParent(transform, false);
        LayoutElement textLE = textGO.GetComponent<LayoutElement>();
        textLE.preferredWidth = 60f;
        textLE.preferredHeight = 22f;

        amountText = textGO.AddComponent<TextMeshProUGUI>();
        amountText.fontSize = 18f;
        amountText.alignment = TextAlignmentOptions.MidlineLeft;
        amountText.color = Color.white;
        amountText.raycastTarget = false;

        RefreshText();
    }

    public void SetKarma(int value)
    {
        karma = value;
        RefreshText();
    }

    void RefreshText()
    {
        if (amountText != null) amountText.text = karma.ToString();
    }
}
