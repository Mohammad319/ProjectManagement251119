namespace Application.Interfaces
{
    public interface INotificationHub
    {
        //  public Task SendNotificationAsync(CalculationItemType ty, OperationType o, object obj);
        Task SendNotificationAsync(string group, ObjectTypHub ty, OperationType o, object obj);
        Task SendNotificationAsync(string group, ObjectTypHub ty, OperationType o, int parentId, object obj);
    }
}
