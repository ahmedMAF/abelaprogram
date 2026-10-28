using Abela.Application.Common;
using Abela.Domain.Entities;
using Abela.Infrastructure.Data;

namespace Abela.Application.Services;

public class AuditService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string actionType, string description, string? entityName = null, int? entityId = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId == 0 ? null : _currentUser.UserId,
            UserName = _currentUser.UserName,
            ActionType = actionType,
            Description = description,
            EntityName = entityName,
            EntityId = entityId,
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
    }
}
