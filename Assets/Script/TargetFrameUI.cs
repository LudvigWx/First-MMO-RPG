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

    // Sätts av OptionsMenuController när ingen fiende är vald men spelaren ändå
    // vill förhandsgranska Enemy HUD Scale-slidern. Så länge den är true rör
    // Update() varken synlighet eller text/health — det sköter OptionsMenuController.
    [HideInInspector] public bool previewMode = false;

    void Start()
    {
        // Gör hela Enemy HUD-panelen flyttbar i Edit Mode (se HudEditModeController).
        if (panel != null)
        {
            HudDragHandle handle = panel.GetComponent<HudDragHandle>();
            if (handle == null) handle = panel.AddComponent<HudDragHandle>();
            handle.saveKey = "hud_enemyhud";
        }
    }

    void Update()
    {
        if (previewMode)
        {
            if (panel != null) panel.SetActive(true);
            return;
        }

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