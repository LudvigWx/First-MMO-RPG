using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

// En rad med en eller flera valutor (platshållar-ikon + siffra). Currency-systemet är inte
// byggt än - amount är bara ett hårdkodat platshållarvärde. SetAmount() är redan klar att
// koppla in den dagen en riktig datakälla finns.
[System.Serializable]
public class CurrencyDisplayEntry
{
    public string label = "Gold";
    public Color iconColor = new Color(0.95f, 0.8f, 0.2f);
    public int amount;
}

public class CurrencyStatusUI : MonoBehaviour
{
    public List<CurrencyDisplayEntry> entries = new List<CurrencyDisplayEntry>();

    private readonly List<TMP_Text> amountTexts = new List<TMP_Text>();
    private bool built;

    public void BuildLayout()
    {
        if (built) return;
        built = true;

        HorizontalLayoutGroup layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        amountTexts.Clear();
        foreach (CurrencyDisplayEntry entry in entries)
            BuildEntry(entry);
    }

    void BuildEntry(CurrencyDisplayEntry entry)
    {
        GameObject entryGO = new GameObject((entry.label ?? "Currency") + "Entry", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        entryGO.transform.SetParent(transform, false);

        HorizontalLayoutGroup entryLayout = entryGO.GetComponent<HorizontalLayoutGroup>();
        entryLayout.spacing = 6;
        entryLayout.childAlignment = TextAnchor.MiddleLeft;
        entryLayout.childForceExpandWidth = false;
        entryLayout.childForceExpandHeight = false;
        entryLayout.childControlWidth = false;
        entryLayout.childControlHeight = false;

        // Platshållar-ikon (enkel färgad ruta) - byt ut mot en riktig sprite senare.
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconGO.transform.SetParent(entryGO.transform, false);
        iconGO.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 20f);
        LayoutElement iconLE = iconGO.GetComponent<LayoutElement>();
        iconLE.preferredWidth = 20f;
        iconLE.preferredHeight = 20f;
        iconGO.GetComponent<Image>().color = entry.iconColor;

        GameObject textGO = new GameObject("Amount", typeof(RectTransform), typeof(LayoutElement));
        textGO.transform.SetParent(entryGO.transform, false);
        LayoutElement textLE = textGO.GetComponent<LayoutElement>();
        textLE.preferredWidth = 60f;
        textLE.preferredHeight = 22f;

        TMP_Text text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = entry.amount.ToString();
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.raycastTarget = false;

        amountTexts.Add(text);
    }

    // Kopplas in den dagen ett riktigt currency-system finns.
    public void SetAmount(int index, int amount)
    {
        if (index < 0 || index >= entries.Count) return;
        entries[index].amount = amount;
        if (index < amountTexts.Count && amountTexts[index] != null)
            amountTexts[index].text = amount.ToString();
    }
}
