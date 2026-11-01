namespace Salzee.Services
{
    /// <summary>
    /// Resolves current user's permission for a module: NoAccess, Read, Edit, Admin.
    /// </summary>
    public interface IModulePermissionService
    {
        /// <summary>Gets the permission for the given user and module. Returns NoAccess if not found or not authenticated.</summary>
        Task<string> GetPermissionAsync(string? userId, string moduleKey);

        /// <summary>True if permission is at least Read.</summary>
        bool CanRead(string permission);

        /// <summary>True if permission is Edit or Admin (can create/edit).</summary>
        bool CanEdit(string permission);

        /// <summary>True if permission is Admin (full CRUD including delete).</summary>
        bool CanDelete(string permission);
    }
}
