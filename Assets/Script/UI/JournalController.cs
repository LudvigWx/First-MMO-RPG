using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Naxestra.UI
{
    // Journal-menyn (Quests/Inventory/Map/Character) - samma "en rot, flera paneler, en aktiv
    // i taget"-struktur som PauseMenuController. Byggs av JournalMenuBuilder (Tools > Naxestra >
    // Build Journal Menu). Escape-samordning: se UITopLevelTracker.
    //
    // [DefaultExecutionOrder(100)] gör att den här körs EFTER PauseMenuController varje frame.
    // Det spelar roll bara den enda frame där du trycker Escape medan Journal är öppen: om
    // PauseMenuController läste tracker-statusen EFTER att Journal redan stängt sig själv och
    // nollställt den, skulle Pause-menyn kunna öppna sig omedelbart på samma knapptryck - precis
    // det som INTE ska hända. Med denna ordning har PauseMenuController redan läst "Journal ligger
    // överst" och avstått från att öppna sig innan Journal hinner stänga sig och nollställa trackern.
    [DefaultExecutionOrder(100)]
    public class JournalController : MonoBehaviour
    {
        public enum Tab { Quests, Inventory, Map, Character }

        [Header("Paneler (dra in från Canvas)")]
        [SerializeField] private GameObject journalRoot;
        [SerializeField] private GameObject questsPanel;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private GameObject characterPanel;

        [Header("Inventory (EN instans monteras om mellan solo/split, se InventoryPanelView)")]
        [SerializeField] private InventoryPanelView inventoryPanelView;
        [SerializeField] private RectTransform inventorySoloSlot;
        [SerializeField] private RectTransform inventorySplitSlot;

        [Header("Flik-knappar (för aktiv-markering)")]
        [SerializeField] private Outline questsTabOutline;
        [SerializeField] private Outline inventoryTabOutline;
        [SerializeField] private Outline mapTabOutline;
        [SerializeField] private Outline characterTabOutline;

        [Header("Lokal input medan Journal är öppen")]
        [SerializeField] private Behaviour[] disableWhileOpen;

        [Header("Muspekare")]
        [SerializeField] private bool freeCursorWhenOpen = true;

        public bool IsOpen { get; private set; }
        public Tab CurrentTab { get; private set; } = Tab.Quests;

        private void Start()
        {
            if (journalRoot) journalRoot.SetActive(false);
            IsOpen = false;
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // Pause-menyn ligger överst just nu - rör inte Journal-snabbtangenterna alls (undviker
            // att båda menyerna hamnar öppna samtidigt).
            if (UITopLevelTracker.TopLayer == UITopLevelTracker.Layer.Pause) return;

            if (Keyboard.current.qKey.wasPressedThisFrame) HandleTabKey(Tab.Quests);
            else if (Keyboard.current.iKey.wasPressedThisFrame) HandleTabKey(Tab.Inventory);
            else if (Keyboard.current.mKey.wasPressedThisFrame) HandleTabKey(Tab.Map);
            else if (Keyboard.current.cKey.wasPressedThisFrame) HandleTabKey(Tab.Character);

            if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseJournal();
        }

        // Tangent för en stängd Journal = öppna på den fliken. Samma tangent som redan aktiv
        // flik = stäng Journal. Annan flik-tangent medan Journal är öppen = byt flik utan att
        // stänga/öppna igen.
        private void HandleTabKey(Tab tab)
        {
            if (!IsOpen) OpenJournal(tab);
            else if (CurrentTab == tab) CloseJournal();
            else ShowTab(tab);
        }

        public void OpenJournal(Tab tab)
        {
            IsOpen = true;
            UITopLevelTracker.NotifyOpened(UITopLevelTracker.Layer.Journal);
            if (journalRoot) journalRoot.SetActive(true);
            ShowTab(tab);
            SetGameplayInput(false);
            SetCursor(freeCursorWhenOpen);
        }

        public void CloseJournal()
        {
            IsOpen = false;
            UITopLevelTracker.NotifyClosed(UITopLevelTracker.Layer.Journal);
            if (journalRoot) journalRoot.SetActive(false);
            SetGameplayInput(true);
            SetCursor(false);
        }

        public void ShowTab(Tab tab)
        {
            CurrentTab = tab;

            if (questsPanel) questsPanel.SetActive(tab == Tab.Quests);
            if (inventoryPanel) inventoryPanel.SetActive(tab == Tab.Inventory);
            if (mapPanel) mapPanel.SetActive(tab == Tab.Map);
            if (characterPanel) characterPanel.SetActive(tab == Tab.Character);

            // Samma InventoryPanelView-instans monteras om mellan de två slottarna beroende på
            // flik - stretchar till att fylla vilken container den råkar sitta i just nu (helt
            // annorlunda bredd solo vs split), så vi bevarar INTE skärmposition/storlek vid bytet
            // (till skillnad från HudDragHandles Detach/Reattach, som medvetet gör det).
            if (inventoryPanelView != null)
            {
                if (tab == Tab.Inventory) inventoryPanelView.MoveToSlot(inventorySoloSlot);
                else if (tab == Tab.Character) inventoryPanelView.MoveToSlot(inventorySplitSlot);
            }

            RefreshTabButtonVisuals();
        }

        // Bundna till respektive flik-knapps onClick av JournalMenuBuilder. Byter bara flik -
        // stänger aldrig Journal (till skillnad från snabbtangenterna, se HandleTabKey ovan).
        public void OnClickQuestsTab() => ShowTab(Tab.Quests);
        public void OnClickInventoryTab() => ShowTab(Tab.Inventory);
        public void OnClickMapTab() => ShowTab(Tab.Map);
        public void OnClickCharacterTab() => ShowTab(Tab.Character);
        public void OnClickClose() => CloseJournal();

        void RefreshTabButtonVisuals()
        {
            SetOutline(questsTabOutline, CurrentTab == Tab.Quests);
            SetOutline(inventoryTabOutline, CurrentTab == Tab.Inventory);
            SetOutline(mapTabOutline, CurrentTab == Tab.Map);
            SetOutline(characterTabOutline, CurrentTab == Tab.Character);
        }

        static void SetOutline(Outline o, bool on)
        {
            if (o != null) o.enabled = on;
        }

        void SetGameplayInput(bool enabled)
        {
            if (disableWhileOpen == null) return;
            foreach (var b in disableWhileOpen)
                if (b) b.enabled = enabled;
        }

        void SetCursor(bool free)
        {
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }
    }
}
