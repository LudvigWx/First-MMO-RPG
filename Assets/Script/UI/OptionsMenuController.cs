using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace Naxestra.UI
{
    public class OptionsMenuController : MonoBehaviour
    {
        [Header("Ljud")]
        [SerializeField] private Slider masterVolumeSlider;

        [Header("Grafik")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown resolutionDropdown;

        [Header("UI-skalning (var för sig)")]
        [SerializeField] private Slider hudScaleSlider;
        [SerializeField] private Slider hotbarScaleSlider;
        [SerializeField] private Slider enemyHudScaleSlider;

        [Header("Skal-mål (hittas automatiskt om tomma)")]
        [SerializeField] private Transform hudRoot;
        [SerializeField] private Transform hotbarRoot;
        [SerializeField] private Transform enemyHudRoot;

        private Resolution[] _resolutions;
        private TargetFrameUI _targetFrame;
        private bool _previewingEnemyHud;

        const string K_VOL = "opt_master_volume";
        const string K_QUAL = "opt_quality";
        const string K_FULL = "opt_fullscreen";
        const string K_RES = "opt_resolution";
        const string K_HUD_SCALE = "opt_hud_scale";
        const string K_HOTBAR_SCALE = "opt_hotbar_scale";
        const string K_ENEMY_HUD_SCALE = "opt_enemy_hud_scale";

        private void Start()
        {
            SetupQuality();
            SetupResolutions();
            FindScaleTargets();
            LoadAndApply();
            HookEvents();
        }

        // OptionsPanel SetActive(true/false) sker varje gång spelaren öppnar/stänger fliken
        // (ShowOptions/ShowMain/ShowGameplay) — så OnEnable/OnDisable körs vid varje besök,
        // till skillnad från Start() som bara kör en gång.
        private void OnEnable()
        {
            if (_targetFrame == null) _targetFrame = FindFirstObjectByType<TargetFrameUI>();
            TryStartEnemyHudPreview();
        }

        private void OnDisable()
        {
            TryStopEnemyHudPreview();
        }

        // Ingen fiende vald? Visa en platshållar-nameplate så Enemy HUD Scale-slidern
        // går att förhandsgranska live, precis som HUD/Hotbar-slidrarna redan kan.
        private void TryStartEnemyHudPreview()
        {
            if (_targetFrame == null) return;

            bool hasRealTarget = _targetFrame.targeting != null && _targetFrame.targeting.CurrentTarget != null;
            if (hasRealTarget) return;

            _targetFrame.previewMode = true;
            if (_targetFrame.panel != null) _targetFrame.panel.SetActive(true);
            if (_targetFrame.nameText != null) _targetFrame.nameText.text = "Enemy";
            if (_targetFrame.levelText != null) _targetFrame.levelText.text = "Lvl 1";
            if (_targetFrame.healthFill != null) _targetFrame.healthFill.fillAmount = 0.6f;

            _previewingEnemyHud = true;
        }

        private void TryStopEnemyHudPreview()
        {
            if (!_previewingEnemyHud || _targetFrame == null) return;

            _targetFrame.previewMode = false;
            _previewingEnemyHud = false;
        }

        private void SetupQuality()
        {
            if (!qualityDropdown) return;
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        }

        private void SetupResolutions()
        {
            if (!resolutionDropdown) return;
            _resolutions = Screen.resolutions;
            var opts = new List<string>();
            int current = 0;
            for (int i = 0; i < _resolutions.Length; i++)
            {
                var r = _resolutions[i];
                opts.Add($"{r.width} x {r.height} @{Mathf.RoundToInt((float)r.refreshRateRatio.value)}Hz");
                if (r.width == Screen.width && r.height == Screen.height) current = i;
            }
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(opts);
            resolutionDropdown.value = PlayerPrefs.GetInt(K_RES, current);
            resolutionDropdown.RefreshShownValue();
        }

        // HUD/Hotbar byggs i kod vid Start() på respektive script och finns därför inte i
        // scenen förrän spelet körs — samma sak gäller fiendens target-frame-panel, som
        // hämtas via den redan existerande TargetFrameUI-referensen istället för att gissa namn.
        private void FindScaleTargets()
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

            if (enemyHudRoot == null)
            {
                TargetFrameUI targetFrame = FindFirstObjectByType<TargetFrameUI>();
                if (targetFrame != null && targetFrame.panel != null) enemyHudRoot = targetFrame.panel.transform;
            }
        }

        private void LoadAndApply()
        {
            float vol = PlayerPrefs.GetFloat(K_VOL, 1f);
            if (masterVolumeSlider) masterVolumeSlider.value = vol;
            AudioListener.volume = vol;

            int qual = PlayerPrefs.GetInt(K_QUAL, QualitySettings.GetQualityLevel());
            if (qualityDropdown) qualityDropdown.value = qual;
            QualitySettings.SetQualityLevel(qual, true);

            bool full = PlayerPrefs.GetInt(K_FULL, Screen.fullScreen ? 1 : 0) == 1;
            if (fullscreenToggle) fullscreenToggle.isOn = full;
            Screen.fullScreen = full;

            float hudScale = PlayerPrefs.GetFloat(K_HUD_SCALE, 1f);
            if (hudScaleSlider) hudScaleSlider.value = hudScale;
            ApplyHudScale(hudScale);

            float hotbarScale = PlayerPrefs.GetFloat(K_HOTBAR_SCALE, 1f);
            if (hotbarScaleSlider) hotbarScaleSlider.value = hotbarScale;
            ApplyHotbarScale(hotbarScale);

            float enemyHudScale = PlayerPrefs.GetFloat(K_ENEMY_HUD_SCALE, 1f);
            if (enemyHudScaleSlider) enemyHudScaleSlider.value = enemyHudScale;
            ApplyEnemyHudScale(enemyHudScale);
        }

        private void HookEvents()
        {
            if (masterVolumeSlider)   masterVolumeSlider.onValueChanged.AddListener(OnVolume);
            if (qualityDropdown)      qualityDropdown.onValueChanged.AddListener(OnQuality);
            if (fullscreenToggle)     fullscreenToggle.onValueChanged.AddListener(OnFullscreen);
            if (resolutionDropdown)   resolutionDropdown.onValueChanged.AddListener(OnResolution);
            if (hudScaleSlider)       hudScaleSlider.onValueChanged.AddListener(OnHudScale);
            if (hotbarScaleSlider)    hotbarScaleSlider.onValueChanged.AddListener(OnHotbarScale);
            if (enemyHudScaleSlider)  enemyHudScaleSlider.onValueChanged.AddListener(OnEnemyHudScale);
        }

        public void OnVolume(float v)
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(K_VOL, v);
        }

        public void OnQuality(int level)
        {
            QualitySettings.SetQualityLevel(level, true);
            PlayerPrefs.SetInt(K_QUAL, level);
        }

        public void OnFullscreen(bool full)
        {
            Screen.fullScreen = full;
            PlayerPrefs.SetInt(K_FULL, full ? 1 : 0);
        }

        public void OnResolution(int index)
        {
            if (_resolutions == null || index < 0 || index >= _resolutions.Length) return;
            var r = _resolutions[index];
            Screen.SetResolution(r.width, r.height, Screen.fullScreenMode);
            PlayerPrefs.SetInt(K_RES, index);
        }

        public void OnHudScale(float scale)
        {
            ApplyHudScale(scale);
            PlayerPrefs.SetFloat(K_HUD_SCALE, scale);
        }

        public void OnHotbarScale(float scale)
        {
            ApplyHotbarScale(scale);
            PlayerPrefs.SetFloat(K_HOTBAR_SCALE, scale);
        }

        public void OnEnemyHudScale(float scale)
        {
            ApplyEnemyHudScale(scale);
            PlayerPrefs.SetFloat(K_ENEMY_HUD_SCALE, scale);
        }

        private void ApplyHudScale(float scale)
        {
            if (hudRoot) hudRoot.localScale = Vector3.one * scale;
        }

        private void ApplyHotbarScale(float scale)
        {
            if (hotbarRoot) hotbarRoot.localScale = Vector3.one * scale;
        }

        private void ApplyEnemyHudScale(float scale)
        {
            if (enemyHudRoot) enemyHudRoot.localScale = Vector3.one * scale;
        }

        public void Save() => PlayerPrefs.Save();

        // Kallas av HudEditModeController:s "Standard"-knapp — nollställer alla tre
        // skal-slidrar till 1.0, vilket via OnHudScale/OnHotbarScale/OnEnemyHudScale
        // både applicerar skalan direkt och skriver över det sparade PlayerPrefs-värdet.
        public void ResetScales()
        {
            if (hudScaleSlider) hudScaleSlider.value = 1f;
            if (hotbarScaleSlider) hotbarScaleSlider.value = 1f;
            if (enemyHudScaleSlider) enemyHudScaleSlider.value = 1f;
        }
    }
}
