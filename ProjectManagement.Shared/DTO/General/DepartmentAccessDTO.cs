namespace ProjectManagement.Shared.DTO.General
{
    /// <summary>
    /// A department the current user may select in the folder workspace dropdown, tagged with how
    /// the user reaches it:
    /// <list type="bullet">
    /// <item><see cref="SharedOnly"/> == false: NORMAL access (own department, or any department for Admin).</item>
    /// <item><see cref="SharedOnly"/> == true: the user has NO normal access here, but one or more
    /// projects are shared/assigned to them from this department (shown with the 👥 marker). Folders
    /// in such a department are read-only visual groups — see <c>ListFolderDTO.IsSharedGroup</c>.</item>
    /// </list>
    /// </summary>
    public sealed class DepartmentAccessDTO : ListDTO
    {
        public bool SharedOnly { get; set; }
    }
}
