namespace ProjectManagement.Client.Helper
{
    public class AppState
    {
        public StorageState StorageApp { get; set; } = new StorageState();
    }

    public class StorageState
    {
        public event Action? OnChange;

        public void NotifyStateChanged() => OnChange?.Invoke();

        public bool Name { get; set; } = false;
        public bool Cost { get; set; } = false;
        public bool CO2 { get; set; } = false;

    }
}
