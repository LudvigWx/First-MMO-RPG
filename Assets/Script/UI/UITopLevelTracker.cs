namespace Naxestra.UI
{
    // Delad koll över vilket UI-lager (Pause-menyn eller Journal) som ligger överst just nu.
    // Låter Escape alltid stänga det som redan är öppet istället för att lägga ett nytt lager
    // ovanpå - se PauseMenuController.Update() och JournalController.Update()/CloseJournal().
    public static class UITopLevelTracker
    {
        public enum Layer { None, Pause, Journal }

        public static Layer TopLayer { get; private set; } = Layer.None;

        public static void NotifyOpened(Layer layer)
        {
            TopLayer = layer;
        }

        public static void NotifyClosed(Layer layer)
        {
            if (TopLayer == layer) TopLayer = Layer.None;
        }
    }
}
