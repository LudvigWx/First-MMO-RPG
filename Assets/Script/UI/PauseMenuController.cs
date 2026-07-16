using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Naxestra.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Paneler (dra in från Canvas)")]
        [SerializeField] private GameObject pauseRoot;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject editModePanel;

        [Header("Mörk bakgrund (döljs i Edit Mode så HUD:et syns)")]
        [SerializeField] private Image pauseBackgroundImage;

        [Header("Lokal input medan menyn är öppen")]
        [SerializeField] private Behaviour[] disableWhileOpen;

        [Header("Muspekare")]
        [SerializeField] private bool freeCursorWhenOpen = true;

        [Header("Endast för solo-testning")]
        [SerializeField] private bool freezeTimeInSingleplayer = false;

        public bool IsOpen { get; private set; }

        private Canvas hudCanvas;

        private void Start()
        {
            if (pauseRoot) pauseRoot.SetActive(false);
            IsOpen = false;
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (IsOpen)
                {
                    if (editModePanel && editModePanel.activeSelf) { ShowOptions(); return; }
                    if (optionsPanel && optionsPanel.activeSelf) { ShowMain(); return; }
                    if (gameplayPanel && gameplayPanel.activeSelf) { ShowMain(); return; }
                    CloseMenu();
                }
                else
                {
                    OpenMenu();
                }
            }
        }

        public void OpenMenu()
        {
            IsOpen = true;
            if (pauseRoot) pauseRoot.SetActive(true);
            ShowMain();
            SetGameplayInput(false);
            SetCursor(freeCursorWhenOpen);

            if (freezeTimeInSingleplayer) Time.timeScale = 0f;
        }

        public void CloseMenu()
        {
            IsOpen = false;
            if (pauseRoot) pauseRoot.SetActive(false);
            SetGameplayInput(true);
            SetCursor(false);
            SetHudVisible(true);

            if (freezeTimeInSingleplayer) Time.timeScale = 1f;
        }

        public void OnResume() => CloseMenu();

        public void ShowMain()
        {
            if (mainPanel) mainPanel.SetActive(true);
            if (optionsPanel) optionsPanel.SetActive(false);
            if (gameplayPanel) gameplayPanel.SetActive(false);
            if (editModePanel) editModePanel.SetActive(false);
            SetPauseBackground(true);
            SetHudVisible(false);
        }

        public void ShowOptions()
        {
            if (mainPanel) mainPanel.SetActive(false);
            if (optionsPanel) optionsPanel.SetActive(true);
            if (gameplayPanel) gameplayPanel.SetActive(false);
            if (editModePanel) editModePanel.SetActive(false);
            SetPauseBackground(true);
            SetHudVisible(false);
        }

        public void ShowGameplay()
        {
            if (mainPanel) mainPanel.SetActive(false);
            if (optionsPanel) optionsPanel.SetActive(false);
            if (gameplayPanel) gameplayPanel.SetActive(true);
            if (editModePanel) editModePanel.SetActive(false);
            SetPauseBackground(true);
            SetHudVisible(false);
        }

        // Edit Mode döljer den mörka pausbakgrunden OCH visar HUD-Canvaset igen (till skillnad
        // från de andra flikarna, där HUD:et hålls helt dolt så det inte lyser igenom den
        // halvgenomskinliga pausbakgrunden och krockar visuellt med menyknapparna).
        public void ShowEditMode()
        {
            if (mainPanel) mainPanel.SetActive(false);
            if (optionsPanel) optionsPanel.SetActive(false);
            if (gameplayPanel) gameplayPanel.SetActive(false);
            if (editModePanel) editModePanel.SetActive(true);
            SetPauseBackground(false);
            SetHudVisible(true);
        }

        private void SetPauseBackground(bool visible)
        {
            if (pauseBackgroundImage) pauseBackgroundImage.enabled = visible;
        }

        // PlayerHudCanvas byggs i kod av PlayerHudUI/HotbarUI vid Start() och finns därför
        // inte i scenen vid Editor-byggtid — måste hittas på namn i Play mode istället.
        private void SetHudVisible(bool visible)
        {
            if (hudCanvas == null)
            {
                GameObject go = GameObject.Find("PlayerHudCanvas");
                if (go != null) hudCanvas = go.GetComponent<Canvas>();
            }
            if (hudCanvas != null) hudCanvas.enabled = visible;
        }

        public void OnLogout()
        {
            if (freezeTimeInSingleplayer) Time.timeScale = 1f;
            Debug.Log("[EscapeMenu] Logout — koppla in SceneManager.LoadScene(\"CharacterSelect\") här.");
            // UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterSelect");
        }

        public void OnExit()
        {
            Debug.Log("[EscapeMenu] Exit");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetGameplayInput(bool enabled)
        {
            if (disableWhileOpen == null) return;
            foreach (var b in disableWhileOpen)
                if (b) b.enabled = enabled;
        }

        private void SetCursor(bool free)
        {
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        private void OnDisable()
        {
            if (freezeTimeInSingleplayer && IsOpen) Time.timeScale = 1f;
        }
    }
}
