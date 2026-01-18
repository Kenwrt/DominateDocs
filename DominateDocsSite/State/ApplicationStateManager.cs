using DominateDocsData.Models;
using DominateDocsSite.Data;
using DominateDocsSite.Database;
using SixLabors.ImageSharp;
using System.Collections.Concurrent;

namespace DominateDocsSite.State;

public class ApplicationStateManager : IApplicationStateManager
{
    public bool IsUseFakeData { get; set; } = false;
    public ConcurrentDictionary<string, object> ActiveSessions { get; set; } = new();

    private readonly ILogger<ApplicationStateManager> logger;
    private readonly IServiceScopeFactory scopeFactory;
    private IMongoDatabaseRepo appDb;
    private ApplicationDbContext dbContext;
    private readonly IConfiguration config;
    private readonly IHttpContextAccessor http;

    public ApplicationStateManager(ILogger<ApplicationStateManager> logger, IServiceScopeFactory scopeFactory, IMongoDatabaseRepo appDb, IConfiguration config, IHttpContextAccessor http)
    {
        this.logger = logger;
        this.scopeFactory = scopeFactory;
        this.config = config;
        this.appDb = appDb;
        this.http = http;

        IsUseFakeData = config.GetValue<bool>("IsUseFakeData");
    }

    public async Task<UserProfile> GetUserProfileByIdAsync(Guid userId)
    {
        UserProfile userProfile = null;

        try
        {
            userProfile = appDb.GetRecordByUserId<UserProfile>(userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return userProfile;
    }

    public void CreateUserProfile(UserProfile user)
    {

        try
        {
            appDb.UpSertRecord<UserProfile>(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

    }


    public bool UserIs(string role) => http.HttpContext?.User?.IsInRole(role) == true;

    public IEnumerable<string> GetRoles() => http.HttpContext?.User?
            .FindAll(System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value) ?? Enumerable.Empty<string>();
}