namespace ProjectManagement.Client.Helper.Converter
{
    public static class ConverterHelper
    {
        public static string? Get<TItem>(TItem item, string property) =>
            typeof(TItem).GetProperty(property)?.GetValue(item)?.ToString();

        public static void SetValue<TItem>(TItem item, string property, object value) =>
            typeof(TItem).GetProperty(property)?.SetValue(item, value);

        public static object? CallMethod<TItem>(TItem item, string name, object[] parameters) =>
            typeof(TItem).GetMethod(name)?.Invoke(item, parameters);

    }
}
