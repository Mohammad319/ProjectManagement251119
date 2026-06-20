using Application.Interfaces;
using ProjectManagement.Shared.DTO.Transfer;

namespace Application.Feature.Transfer.Commands
{
    public sealed record BuildProjectPackageCommand(
        Guid ProjectId,
        AtacostProjectExportRequest Request,
        int UserId,
        int? DepartmentId,
        bool IsViewer) : IRequest<byte[]?>;

    public class BuildProjectPackageCommandHandler(IAtacostTransferService service)
        : IRequestHandler<BuildProjectPackageCommand, byte[]?>
    {
        public Task<byte[]?> Handle(BuildProjectPackageCommand request, CancellationToken ct)
            => service.BuildProjectPackageAsync(request.ProjectId, request.Request, request.UserId, request.DepartmentId, request.IsViewer, ct);
    }

    public sealed record BuildCalculationPackageCommand(
        int CalculationId,
        AtacostCalculationExportRequest Request,
        int UserId,
        int? DepartmentId,
        bool IsViewer) : IRequest<byte[]?>;

    public class BuildCalculationPackageCommandHandler(IAtacostTransferService service)
        : IRequestHandler<BuildCalculationPackageCommand, byte[]?>
    {
        public Task<byte[]?> Handle(BuildCalculationPackageCommand request, CancellationToken ct)
            => service.BuildCalculationPackageAsync(request.CalculationId, request.Request, request.UserId, request.DepartmentId, request.IsViewer, ct);
    }

    public sealed record InspectPackageCommand(byte[] FileBytes) : IRequest<AtacostPackageInfoDTO>;

    public class InspectPackageCommandHandler(IAtacostTransferService service)
        : IRequestHandler<InspectPackageCommand, AtacostPackageInfoDTO>
    {
        public Task<AtacostPackageInfoDTO> Handle(InspectPackageCommand request, CancellationToken ct)
            => service.InspectPackageAsync(request.FileBytes, ct);
    }

    public sealed record PreviewProjectPackageCommand(
        byte[] FileBytes,
        Guid TargetFolderId,
        int UserId) : IRequest<AtacostImportPreviewDTO>;

    public class PreviewProjectPackageCommandHandler(IAtacostTransferService service)
        : IRequestHandler<PreviewProjectPackageCommand, AtacostImportPreviewDTO>
    {
        public Task<AtacostImportPreviewDTO> Handle(PreviewProjectPackageCommand request, CancellationToken ct)
            => service.PreviewProjectPackageAsync(request.FileBytes, request.TargetFolderId, request.UserId, ct);
    }

    public sealed record PreviewCalculationPackageCommand(
        byte[] FileBytes,
        Guid TargetProjectId,
        int UserId) : IRequest<AtacostImportPreviewDTO>;

    public class PreviewCalculationPackageCommandHandler(IAtacostTransferService service)
        : IRequestHandler<PreviewCalculationPackageCommand, AtacostImportPreviewDTO>
    {
        public Task<AtacostImportPreviewDTO> Handle(PreviewCalculationPackageCommand request, CancellationToken ct)
            => service.PreviewCalculationPackageAsync(request.FileBytes, request.TargetProjectId, request.UserId, ct);
    }

    public sealed record ImportProjectPackageCommand(
        byte[] FileBytes,
        Guid TargetFolderId,
        int UserId,
        int? DepartmentId,
        bool AllowCrossDepartment) : IRequest<Guid>;

    public class ImportProjectPackageCommandHandler(IAtacostTransferService service)
        : IRequestHandler<ImportProjectPackageCommand, Guid>
    {
        public Task<Guid> Handle(ImportProjectPackageCommand request, CancellationToken ct)
            => service.ImportProjectPackageAsync(request.FileBytes, request.TargetFolderId, request.UserId, request.DepartmentId, request.AllowCrossDepartment, ct);
    }

    public sealed record ImportCalculationPackageCommand(
        byte[] FileBytes,
        Guid TargetProjectId,
        int UserId,
        int? DepartmentId,
        bool AllowCrossDepartment) : IRequest<int>;

    public class ImportCalculationPackageCommandHandler(IAtacostTransferService service)
        : IRequestHandler<ImportCalculationPackageCommand, int>
    {
        public Task<int> Handle(ImportCalculationPackageCommand request, CancellationToken ct)
            => service.ImportCalculationPackageAsync(request.FileBytes, request.TargetProjectId, request.UserId, request.DepartmentId, request.AllowCrossDepartment, ct);
    }
}
