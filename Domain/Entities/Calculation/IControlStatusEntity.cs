namespace Domain.Entities.Calculation;

public interface IControlStatusEntity
{
    string Code { get; }
    bool IsDefault { get; }
    bool IsSystemDefault { get; }
    void SetControlStatusSettings(string? code, bool isDefault, bool isSystemDefault);
}
