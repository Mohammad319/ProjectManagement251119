using Domain.Entities.Base;

namespace Domain.Entities.Users;

public sealed class UserDepartmentAccessEntity : AuditableEntity<int>
{
    public int UserId { get; private set; }
    public int DepartmentId { get; private set; }
    public bool IsPrimary { get; private set; }

    public UserEntity User { get; private set; } = default!;
    public DepartmentEntity Department { get; private set; } = default!;

    private UserDepartmentAccessEntity() { }

    public static UserDepartmentAccessEntity Create(int userId, int departmentId, bool isPrimary)
    {
        return new UserDepartmentAccessEntity
        {
            UserId = userId,
            DepartmentId = departmentId,
            IsPrimary = isPrimary
        };
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }
}
