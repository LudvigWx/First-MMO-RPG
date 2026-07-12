using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TargetFrameUI : MonoBehaviour
{
    [Header("Referenser")]
    public TargetingController targeting;   // Spelarens targeting-script
    public GameObject panel;                // Hela rutan (TargetFrame)
    public TMP_Text nameText;
    public TMP_Text levelText;
    public Image healthFill;

    void Update()
    {
        // Vem har spelaren valt?
        Enemy target = targeting != null ? targeting.CurrentTarget : null;

        // Ingen vald? Göm rutan och avsluta.
        if (target == null)
        {
            if (panel != null) panel.SetActive(false);
            return;
        }

        // Någon vald? Visa rutan och fyll i info.
        if (panel != null) panel.SetActive(true);
        if (nameText != null) nameText.text = target.enemyName;
        if (levelText != null) levelText.text = "Lvl " + target.level;

        if (healthFill != null)
        {
            // Andel health kvar: 0.0 (tom) till 1.0 (full)
            float pct = (float)target.currentHealth / target.maxHealth;
            healthFill.fillAmount = pct;
        }
    }
}