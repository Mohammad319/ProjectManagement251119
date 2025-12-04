using ProjectManagement.Client.Services.Calculation.CalculationItems;
using ProjectManagement.Client.Services.Folder;

namespace ProjectManagement.Client.Services
{
    /// <summary>
    /// Defines a central access point to all services used in the application.
    /// </summary>
    /// 
    //make interface

    public interface IUnitOfWorkService
    {
        ResourceService Resource { get; }
        TaskService Task { get; }
        FolderService Folder { get; }
        MhdServices Mhd { get; }
    }

    /// <summary>
    /// Implements the unit-of-work pattern for service management.
    /// </summary>
    public class UnitOfWorkService(

        ResourceService resourceService,
        TaskService taskService,
        FolderService folderService,
        MhdServices mhdServices, FolderState fs) : IUnitOfWorkService
    {
        #region Services
        public ResourceService Resource { get; } = resourceService;
        public TaskService Task { get; } = taskService;
        public FolderService Folder { get; } = folderService;
        public MhdServices Mhd { get; } = mhdServices;

        public FolderState FolderState => fs;
        #endregion
    }
}
