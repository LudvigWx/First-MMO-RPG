using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace Naxestra.UI
{
    // Sitter på EditModePanel (byggs av EscapeMenuBuilder). Aktiverar drag-läget för
    // Player HUD, Hotbar och Enemy HUD så länge just den här panelen är synlig,
    // och sköter "Standard"-knappen som återställer position + skala för alla tre.
    public class HudEditModeController : MonoBehaviour
    {
        [Header("Kopplas av EscapeMenuBuilder")]
        [SerializeField] private OptionsMenuController optionsController;
        [SerializeField] private UnityEngine.UI.Toggle fullEditModeToggle;
        [SerializeField] private GameObject fullEditModeSubPanel;
        [SerializeField] private UnityEngine.UI.Slider portraitScaleSlider;
        [SerializeField] private UnityEngine.UI.Slider abilityScaleSlider;
        [SerializeField] private UnityEngine.UI.Slider quickScaleSlider;
        [SerializeField] private UnityEngine.UI.Toggle healthNumbersToggle;
        [SerializeField] private UnityEngine.UI.Toggle rageNumbersToggle;
        [SerializeField] private UnityEngine.UI.Toggle xpNumbersToggle;
        [SerializeField] private TMP_Dropdown abilityLayoutDropdown;
        [SerializeField] private TMP_Dropdown quickItemLayoutDropdown;

        [Header("Mål (hittas automatiskt om tomma)")]
        [SerializeField] private Transform hudRoot;
        [SerializeField] private Transform hotbarRoot;
        [SerializeField] private Transform enemyHudRoot;
        [SerializeField] private Transform questTrackerRoot;
        [SerializeField] private PlayerHudUI playerHudUI;
        [SerializeField] private HotbarUI hotbarUI;

        private readonly List<HudDragHandle> handles = new List<HudDragHandle>();
        private TargetFrameUI targetFrame;
        private bool previewingEnemyHud;

        private void Awake()
        {
            if (optionsController == null) optionsController = FindFirstObjectByType<OptionsMenuController>();
            if (fullEditModeToggle != null) fullEditModeToggle.onValueChanged.AddListener(OnFullEditModeChanged);
            if (portraitScaleSlider != null) portraitScaleSlider.onValueChanged.AddListener(OnPortraitScaleChanged);
            if (abilityScaleSlider != null) abilityScaleSlider.onValueChanged.AddListener(OnAbilityScaleChanged);
            if (quickScaleSlider != null) quickScaleSlider.onValueChanged.AddListener(OnQuickScaleChanged);
            if (healthNumbersToggle != null) healthNumbersToggle.onValueChanged.AddListener(OnHealthNumbersChanged);
            if (rageNumbersToggle != null) rageNumbersToggle.onValueChanged.AddListener(OnRageNumbersChanged);
            if (xpNumbersToggle != null) xpNumbersToggle.onValueChanged.AddListener(OnXpNumbersChanged);

            if (abilityLayoutDropdown != null)
            {
                abilityLayoutDropdown.ClearOptions();
                abilityLayoutDropdown.AddOptions(new List<string>(HotbarUI.AbilityColumnLabels));
                abilityLayoutDropdown.onValueChanged.AddListener(OnAbilityLayoutChanged);
            }
            if (quickItemLayoutDropdown != null)
            {
                quickItemLayoutDropdown.ClearOptions();
                quickItemLayoutDropdown.AddOptions(new List<string>(HotbarUI.QuickColumnLabels));
                quickItemLayoutDropdown.onValueChanged.AddListener(OnQuickItemLayoutChanged);
            }
        }

        private void OnEnable()
        {
            FindTargets();
            EnsureHandles();

            HudDragHandle.EditModeActive = true;
            foreach (HudDragHandle h in handles) h.ApplyEditVisual();
            if (playerHudUI != null) playerHudUI.RefreshEditVisuals();
            if (hotbarUI != null) hotbarUI.RefreshEditVisuals();

            SyncControlsFromPlayerHud();

            StartEnemyHudPreview();
        }

        private void OnDisable()
        {
            HudDragHandle.EditModeActive = false;
            foreach (HudDragHandle h in handles) h.ApplyEditVisual();
            if (playerHudUI != null) playerHudUI.RefreshEditVisuals();
            if (hotbarUI != null) hotbarUI.RefreshEditVisuals();

            StopEnemyHudPreview();
        }

        // Kopplas till fullEditModeToggle.onValueChanged av EscapeMenuBuilder. Styr BÅDE
        // PlayerHudUI (Health/Rage/XP/Portrait) och HotbarUI (Ability/Quick Item) — en
        // gemensam kryssruta för allt som kan brytas loss till individuella element.
        public void OnFullEditModeChanged(bool on)
        {
            if (playerHudUI == null) playerHudUI = FindFirstObjectByType<PlayerHudUI>();
            if (playerHudUI != null)
            {
                playerHudUI.SetFullEditMode(on);
                playerHudUI.RefreshEditVisuals();
            }

            if (hotbarUI == null) hotbarUI = FindFirstObjectByType<HotbarUI>();
            if (hotbarUI != null)
            {
                hotbarUI.SetFullEditMode(on);
                hotbarUI.RefreshEditVisuals();
            }

            foreach (HudDragHandle h in handles) h.ApplyEditVisual();
            UpdateSubPanelVisibility();
        }

        public void OnPortraitScaleChanged(float scale)
        {
            if (playerHudUI == null) playerHudUI = FindFirstObjectByType<PlayerHudUI>();
            if (playerHudUI != null) playerHudUI.SetPortraitScale(scale);
        }

        public void OnHealthNumbersChanged(bool on)
        {
            if (playerHudUI == null) playerHudUI = FindFirstObjectByType<PlayerHudUI>();
            if (playerHudUI != null) playerHudUI.SetHealthNumbersVisible(on);
        }

        public void OnRageNumbersChanged(bool on)
        {
            if (playerHudUI == null) playerHudUI = FindFirstObjectByType<PlayerHudUI>();
            if (playerHudUI != null) playerHudUI.SetRageNumbersVisible(on);
        }

        public void OnXpNumbersChanged(bool on)
        {
            if (playerHudUI == null) playerHudUI = FindFirstObjectByType<PlayerHudUI>();
            if (playerHudUI != null) playerHudUI.SetXpNumbersVisible(on);
        }

        public void OnAbilityScaleChanged(float scale)
        {
            if (hotbarUI == null) hotbarUI = FindFirstObjectByType<HotbarUI>();
            if (hotbarUI != null) hotbarUI.SetAbilityScale(scale);
        }

        public void OnQuickScaleChanged(float scale)
        {
            if (hotbarUI == null) hotbarUI = FindFirstObjectByType<HotbarUI>();
            if (hotbarUI != null) hotbarUI.SetQuickScale(scale);
        }

        // Kopplas till abilityLayoutDropdown/quickItemLayoutDropdown.onValueChanged av EscapeMenuBuilder.
        public void OnAbilityLayoutChanged(int index)
        {
            if (hotbarUI == null) hotbarUI = FindFirstObjectByType<HotbarUI>();
            if (hotbarUI == null) return;
            index = Mathf.Clamp(index, 0, HotbarUI.AbilityColumnOptions.Length - 1);
            hotbarUI.SetAbilityColumns(HotbarUI.AbilityColumnOptions[index]);
        }

        public void OnQuickItemLayoutChanged(int index)
        {
            if (hotbarUI == null) hotbarUI = FindFirstObjectByType<HotbarUI>();
            if (hotbarUI == null) return;
            index = Mathf.Clamp(index, 0, HotbarUI.QuickColumnOptions.Length - 1);
            hotbarUI.SetQuickColumns(HotbarUI.QuickColumnOptions[index]);
        }

        // Sätter aktiv/inaktiv på undersektionen och tvingar sedan fram en omritning av HELA
        // panelen. Utan detta hann inte VerticalLayoutGroup/ContentSizeFitter reagera på att
        // undersektionen dök upp/försvann, så resten av panelen kunde bli kvar i fel positioner
        // (överlappande knappar/kryssrutor) tills nästa naturliga omritning (t.ex. stäng+öppna).
        private void UpdateSubPanelVisibility()
        {
            if (fullEditModeSubPanel != null && playerHudUI != null)
                fullEditModeSubPanel.SetActive(playerHudUI.IsFullEditModeOn);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }

        // Synkar alla kontroller (checkbox/slider/togglar) till PlayerHudUI:s faktiska
        // sparade state, utan att trigga onValueChanged (som annars skulle skriva över
        // samma värde igen). Körs när panelen öppnas, efter Full Edit Mode-byte och Standard.
        private void SyncControlsFromPlayerHud()
        {
            if (playerHudUI != null)
            {
                if (fullEditModeToggle != null) fullEditModeToggle.SetIsOnWithoutNotify(playerHudUI.IsFullEditModeOn);
                if (portraitScaleSlider != null) portraitScaleSlider.SetValueWithoutNotify(playerHudUI.GetPortraitScale());
                if (healthNumbersToggle != null) healthNumbersToggle.SetIsOnWithoutNotify(playerHudUI.GetHealthNumbersVisible());
                if (rageNumbersToggle != null) rageNumbersToggle.SetIsOnWithoutNotify(playerHudUI.GetRageNumbersVisible());
                if (xpNumbersToggle != null) xpNumbersToggle.SetIsOnWithoutNotify(playerHudUI.GetXpNumbersVisible());
            }

            if (hotbarUI == null) hotbarUI = FindFirstObjectByType<HotbarUI>();
            if (hotbarUI != null)
            {
                if (abilityLayoutDropdown != null)
                {
                    int idx = System.Array.IndexOf(HotbarUI.AbilityColumnOptions, hotbarUI.GetAbilityColumns());
                    abilityLayoutDropdown.SetValueWithoutNotify(Mathf.Max(0, idx));
                }
                if (quickItemLayoutDropdown != null)
                {
                    int idx = System.Array.IndexOf(HotbarUI.QuickColumnOptions, hotbarUI.GetQuickColumns());
                    quickItemLayoutDropdown.SetValueWithoutNotify(Mathf.Max(0, idx));
                }
                if (abilityScaleSlider != null) abilityScaleSlider.SetValueWithoutNotify(hotbarUI.GetAbilityScale());
                if (quickScaleSlider != null) quickScaleSlider.SetValueWithoutNotify(hotbarUI.GetQuickScale());
            }

            UpdateSubPanelVisibility();
        }

        private void FindTargets()
        {
            if (hudRoot == null)
            {
                GameObject go = GameObject.Find("PlayerHudRoot");
                if (go != null) hudRoot = go.transform;
            }

            if (hotbarRoot == null)
            {
                GameObject go = GameObject.Find("HotbarRoot");
                if (go != null) hotbarRoot = go.transform;
            }

            if (targetFrame == null) targetFrame = FindFirstObjectByType<TargetFrameUI>();
            if (enemyHudRoot == null && targetFrame != null && targetFrame.panel != null)
            {
                enemyHudRoot = targetFrame.panel.transform;
            }

            if (questTrackerRoot == null)
            {
                GameObject go = GameObject.Find("QuestTrackerRoot");
                if (go != null) questTrackerRoot = go.transform;
            }

            if (playerHudUI == null) playerHudUI = FindFirstObjectByType<PlayerHudUI>();
            if (hotbarUI == null) hotbarUI = FindFirstObjectByType<HotbarUI>();
        }

        private void EnsureHandles()
        {
            handles.Clear();
            TryAddHandle(hudRoot, "hud_playerhud");
            TryAddHandle(hotbarRoot, "hud_hotbar");
            TryAddHandle(enemyHudRoot, "hud_enemyhud");
            TryAddHandle(questTrackerRoot, "hud_questtracker");
        }

        private void TryAddHandle(Transform target, string saveKey)
        {
            if (target == null) return;

            HudDragHandle handle = target.GetComponent<HudDragHandle>();
            if (handle == null)
            {
                handle = target.gameObject.AddComponent<HudDragHandle>();
                handle.saveKey = saveKey;
            }
            handles.Add(handle);
        }

        // Ingen fiende vald? Visa en platshållar-nameplate så Enemy HUD går att se/dra
        // även utan mål, precis som OptionsMenuController redan gör för skal-slidern.
        private void StartEnemyHudPreview()
        {
            if (targetFrame == null) return;

            bool hasRealTarget = targetFrame.targeting != null && targetFrame.targeting.CurrentTarget != null;
            if (hasRealTarget) return;

            targetFrame.previewMode = true;
            if (targetFrame.panel != null) targetFrame.panel.SetActive(true);
            if (targetFrame.nameText != null) targetFrame.nameText.text = "Enemy";
            if (targetFrame.levelText != null) targetFrame.levelText.text = "Lvl 1";
            if (targetFrame.healthFill != null) targetFrame.healthFill.fillAmount = 0.6f;

            previewingEnemyHud = true;
        }

        private void StopEnemyHudPreview()
        {
            if (!previewingEnemyHud || targetFrame == null) return;
            targetFrame.previewMode = false;
            previewingEnemyHud = false;
        }

        public void OnStandardClick()
        {
            foreach (HudDragHandle h in handles) h.ResetToDefault();
            if (optionsController != null) optionsController.ResetScales();

            if (playerHudUI == null) playerHudUI = FindFirstObjectByType<PlayerHudUI>();
            if (playerHudUI != null)
            {
                playerHudUI.ResetFullModeElements();
                playerHudUI.RefreshEditVisuals();
            }

            if (hotbarUI == null) hotbarUI = FindFirstObjectByType<HotbarUI>();
            if (hotbarUI != null)
            {
                hotbarUI.ResetLayoutToDefault();
                hotbarUI.RefreshEditVisuals();
            }

            foreach (HudDragHandle h in handles) h.ApplyEditVisual();

            SyncControlsFromPlayerHud();
        }
    }
}
