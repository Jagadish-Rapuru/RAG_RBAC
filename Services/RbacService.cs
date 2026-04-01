using RAG_RBAC.Models;

namespace RAG_RBAC.Services;

/// <summary>
/// Manages user identities and provides role-based access control (RBAC) logic.
/// Determines which documents a user is authorized to access based on their assigned role.
/// </summary>
/// <remarks>
/// <para>
/// This service uses hardcoded users for demonstration purposes.
/// In a production environment, replace with Azure AD, ASP.NET Identity, or another identity provider.
/// </para>
/// <para>
/// Registered as a <b>singleton</b> in the DI container since user data is static and read-only.
/// </para>
/// </remarks>
public class RbacService
{
    private readonly Dictionary<string, AppUser> _users;

    /// <summary>
    /// Initializes the service with a predefined set of demo users spanning all role levels.
    /// User lookups are case-insensitive.
    /// </summary>
    public RbacService()
    {
        _users = new Dictionary<string, AppUser>(StringComparer.OrdinalIgnoreCase)
        {
            ["intern01"] = new AppUser
            {
                UserId = "intern01",
                DisplayName = "Alex (Intern)",
                Role = AppRole.Intern
            },
            ["dev01"] = new AppUser
            {
                UserId = "dev01",
                DisplayName = "Jagadish (Developer)",
                Role = AppRole.Developer
            },
            ["mgr01"] = new AppUser
            {
                UserId = "mgr01",
                DisplayName = "Rohit (Manager)",
                Role = AppRole.Manager
            },
            ["dir01"] = new AppUser
            {
                UserId = "dir01",
                DisplayName = "Sarah (Director)",
                Role = AppRole.Director
            },
            ["admin01"] = new AppUser
            {
                UserId = "admin01",
                DisplayName = "System Admin",
                Role = AppRole.Admin
            }
        };
    }

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The user ID to look up (case-insensitive).</param>
    /// <returns>The matching <see cref="AppUser"/>, or <c>null</c> if no user exists with the given ID.</returns>
    public AppUser? GetUser(string userId)
    {
        _users.TryGetValue(userId, out var user);
        return user;
    }

    /// <summary>
    /// Returns all role names that the specified user is authorized to access.
    /// </summary>
    /// <param name="userId">The user ID to evaluate.</param>
    /// <returns>
    /// A list of accessible role names (e.g., <c>["Intern", "Developer"]</c> for a Developer).
    /// Returns an empty list if the user is not found.
    /// </returns>
    public List<string> GetAccessibleRolesForUser(string userId)
    {
        var user = GetUser(userId);
        if (user is null) return new List<string>();
        return user.GetAccessibleRoles();
    }

    /// <summary>
    /// Determines whether a user has sufficient privileges to access a document
    /// with the specified minimum access role.
    /// </summary>
    /// <param name="userId">The user ID to check authorization for.</param>
    /// <param name="documentMinimumRole">
    /// The minimum role required by the document (must match an <see cref="AppRole"/> name).
    /// </param>
    /// <returns>
    /// <c>true</c> if the user's role level is greater than or equal to the document's required role;
    /// <c>false</c> if the user is not found, the role name is invalid, or access is denied.
    /// </returns>
    public bool CanAccess(string userId, string documentMinimumRole)
    {
        var user = GetUser(userId);
        if (user is null) return false;

        if (Enum.TryParse<AppRole>(documentMinimumRole, true, out var requiredRole))
        {
            return (int)user.Role >= (int)requiredRole;
        }

        return false;
    }

    /// <summary>
    /// Returns all registered users in the system.
    /// </summary>
    /// <returns>A read-only list of all <see cref="AppUser"/> instances.</returns>
    public IReadOnlyList<AppUser> GetAllUsers() => _users.Values.ToList().AsReadOnly();
}
