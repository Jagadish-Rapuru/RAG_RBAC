namespace RAG_RBAC.Models;

/// <summary>
/// Defines the hierarchical access levels used for document-level authorization.
/// Each role has a numeric value representing its position in the hierarchy,
/// where higher values grant access to more restricted content.
/// </summary>
/// <remarks>
/// Role hierarchy: Intern(0) &lt; Developer(1) &lt; Manager(2) &lt; Director(3) &lt; Admin(4).
/// A user with a given role can access all documents at their level and below.
/// In a production system, these roles would map to claims from an identity provider (e.g., Azure AD).
/// </remarks>
public enum AppRole
{
    /// <summary>Entry-level access. Can only view publicly available documents.</summary>
    Intern = 0,

    /// <summary>Standard engineering access. Includes infrastructure and DevOps documentation.</summary>
    Developer = 1,

    /// <summary>Management access. Includes budgets, performance reviews, and roadmaps.</summary>
    Manager = 2,

    /// <summary>Executive access. Includes M&amp;A targets, board minutes, and strategic plans.</summary>
    Director = 3,

    /// <summary>Full system access. Includes security playbooks, credentials, and compliance data.</summary>
    Admin = 4
}

/// <summary>
/// Represents an authenticated user with an assigned role for RBAC enforcement.
/// </summary>
/// <remarks>
/// In this demo, users are hardcoded in <see cref="RAG_RBAC.Services.RbacService"/>.
/// In production, this would be populated from Azure AD / ASP.NET Identity claims.
/// </remarks>
public class AppUser
{
    /// <summary>Gets or sets the unique identifier for the user (e.g., "dev01").</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable display name (e.g., "Jagadish (Developer)").</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's assigned role, which determines document access scope.</summary>
    public AppRole Role { get; set; }

    /// <summary>
    /// Computes all role levels this user is authorized to access.
    /// Access is inclusive downward: a Manager can view Intern, Developer, and Manager documents.
    /// </summary>
    /// <returns>
    /// A list of role names (as strings) that the user has permission to access,
    /// ordered from lowest to highest privilege.
    /// </returns>
    /// <example>
    /// For a user with <see cref="AppRole.Manager"/>:
    /// <code>["Intern", "Developer", "Manager"]</code>
    /// </example>
    public List<string> GetAccessibleRoles()
    {
        return Enum.GetValues<AppRole>()
            .Where(r => (int)r <= (int)Role)
            .Select(r => r.ToString())
            .ToList();
    }
}
