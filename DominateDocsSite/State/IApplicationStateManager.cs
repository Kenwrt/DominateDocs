using DominateDocsData.Models;
using System.Collections.Concurrent;

namespace DominateDocsSite.State;
public interface IApplicationStateManager
{
    ConcurrentDictionary<string, object> ActiveSessions { get; set; }
    bool IsUseFakeData { get; set; }

    void CreateUserProfile(UserProfile user);
    IEnumerable<string> GetRoles();
    Task<UserProfile> GetUserProfileByIdAsync(Guid userId);
    bool UserIs(string role);
}