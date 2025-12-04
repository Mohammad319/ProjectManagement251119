namespace ProjectManagement.Shared.Enums
{
    public enum CalculationItemType
    {
        resource = 0, task = 1, calcList = 2, calculation = 3
    }
    public enum ObjectTypHub
    {
        task = 2, resource = 3, calculation = 4,
        Opportunity = 5, Factor = 6, Offer = 7, HourlyPrice = 8, netcalc=9
    }

    public enum AuthorityStorage
    {
        _private = 0,
        department = 1,
        _public = 2,
        program = 3,
        ImportFromOtherCalc = 4,
    }
    public enum OperationType
    {
        Add, Update, AddRange, Remove, AddUpdateRange, RemoveRange, MoveRange
    }

    public enum CopyType
    {
        Copy,
        CopyWithContent,
        Move,
        Storage,
        remove
    }
}
