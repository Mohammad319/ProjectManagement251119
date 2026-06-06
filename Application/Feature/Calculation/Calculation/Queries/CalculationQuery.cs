using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Calculation.Queries;

// ============================================
//  GetAllCalculationsQuery
// ============================================

public sealed record GetAllCalculationsQuery(
    Guid ProjectId,
    bool IsArchived,
    int UserId,
    int? DepartmentId
) : IRequest<IEnumerable<ListCalculationDTO>>;

public sealed class GetAllCalculationsQueryHandler(ICalculationQueryService service)
            : IRequestHandler<GetAllCalculationsQuery, IEnumerable<ListCalculationDTO>>
{
    public async Task<IEnumerable<ListCalculationDTO>> Handle(
            GetAllCalculationsQuery request,
            CancellationToken cancellationToken)
    {
        return await service.GetAllAsync(
            request.ProjectId,
            request.IsArchived,
            request.UserId,
            request.DepartmentId,
            cancellationToken);
    }
}

// ============================================
//  GetAllCalculationsByDepartmentQuery
// ============================================

public sealed record GetAllCalculationsByDepartmentQuery(
    Guid ProjectId,
    int UserId,
    int? DepartmentId
) : IRequest<IEnumerable<ListCalculationDTO>>;

public sealed class GetAllCalculationsByDepartmentQueryHandler(ICalculationQueryService service)
            : IRequestHandler<GetAllCalculationsByDepartmentQuery, IEnumerable<ListCalculationDTO>>
{
    public Task<IEnumerable<ListCalculationDTO>> Handle(GetAllCalculationsByDepartmentQuery request,
        CancellationToken cancellationToken)
    {
        return service.GetByDepartmentAsync(
            request.ProjectId,
            request.UserId,
            request.DepartmentId,
            cancellationToken);
    }
}

// ============================================
//  GetCalculationDetailsQuery
// ============================================

public sealed record GetCalculationDetailsQuery(
    int Id,
    int UserId,
    int? DepartmentId
) : IRequest<CalculationDetailsDTO?>;

public sealed class GetCalculationDetailsQueryHandler(ICalculationQueryService service)
            : IRequestHandler<GetCalculationDetailsQuery, CalculationDetailsDTO?>
{
    public Task<CalculationDetailsDTO?> Handle(
            GetCalculationDetailsQuery request,
            CancellationToken cancellationToken)
    {
        return service.GetDetailsAsync(
            request.Id,
            request.UserId,
            request.DepartmentId,
            cancellationToken);
    }
}

// ============================================
//  GetCalculationPostQuery (للنموذج في UI)
// ============================================

public sealed record GetCalculationPostQuery(
    int Id,
    int UserId,
    int? DepartmentId
) : IRequest<CalculationPostDTO?>;

public sealed class GetCalculationPostQueryHandler(ICalculationQueryService service)
            : IRequestHandler<GetCalculationPostQuery, CalculationPostDTO?>
{
    public Task<CalculationPostDTO?> Handle(
            GetCalculationPostQuery request,
            CancellationToken cancellationToken)
    {
        return service.GetPostModelAsync(
            request.Id,
            request.UserId,
            request.DepartmentId,
            cancellationToken);
    }
}

// ============================================
//  GetCalculationPageQuery (الصفحة الكاملة مع Tasks/Resources/Offers)
// ============================================

public sealed record GetCalculationPageQuery(
    int Id,
    int UserId,
    int? DepartmentId
) : IRequest<CalculationPageDTO?>;

public sealed class GetCalculationPageQueryHandler(ICalculationQueryService service)
            : IRequestHandler<GetCalculationPageQuery, CalculationPageDTO?>
{
    public Task<CalculationPageDTO?> Handle(
            GetCalculationPageQuery request,
            CancellationToken cancellationToken)
    {
        return service.GetPageAsync(
            request.Id,
            request.UserId,
            request.DepartmentId,
            cancellationToken);
    }
}

// ============================================
//  GetShareCalculationPageQuery
//  (إن أحببت أن تفرق بين صفحة المشاركة وصفحة العادية،
//   يمكنك لاحقاً إضافة منطق مختلف في ICalculationQueryService)
//  حالياً سنعيد استخدام نفس GetPageAsync.
// ============================================

public sealed record GetShareCalculationPageQuery(
    int Id,
    int UserId,
    int? DepartmentId
) : IRequest<CalculationPageDTO?>;

public sealed class GetShareCalculationPageQueryHandler(ICalculationQueryService service)
            : IRequestHandler<GetShareCalculationPageQuery, CalculationPageDTO?>
{
    public Task<CalculationPageDTO?> Handle(
            GetShareCalculationPageQuery request,
            CancellationToken cancellationToken)
    {
        // لو أردت مستقبلاً منطق مختلف للـ Share،
        // أضف متداً منفصلاً في ICalculationQueryService.
        return service.GetPageAsync(
            request.Id,
            request.UserId,
            request.DepartmentId,
            cancellationToken);
    }
}

// ============================================
//  HourlyPriceListQuery
// ============================================

public sealed record HourlyPriceListQuery(
    int Id,
    int? DepartmentId
) : IRequest<List<HourlyPriceListGroupDTO>>;

public sealed class HourlyPriceListQueryHandler(ICalculationQueryService service)
            : IRequestHandler<HourlyPriceListQuery, List<HourlyPriceListGroupDTO>>
{
    public Task<List<HourlyPriceListGroupDTO>> Handle(
            HourlyPriceListQuery request,
            CancellationToken cancellationToken)
    {
        return service.GetHourlyPriceListAsync(
            request.Id,
            request.DepartmentId,
            cancellationToken);
    }
}

