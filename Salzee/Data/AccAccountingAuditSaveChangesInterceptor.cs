using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Salzee.Models.Acc;

namespace Salzee.Data;

/// <summary>
/// Writes <see cref="AccAccountingAuditLog"/> rows for inserts/updates/deletes on <c>acc</c> schema tables.
/// </summary>
public sealed class AccAccountingAuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    private const int MaxJsonChars = 400_000;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccAccountingAuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        TryAppendAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        TryAppendAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void TryAppendAuditEntries(DbContext? context)
    {
        if (context is not PMSDbContext db)
            return;

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var changedBy = string.IsNullOrEmpty(userId)
            ? "SYSTEM"
            : userId.Length <= 10 ? userId : userId[..10];

        string? ip = null;
        string? ua = null;
        var httpCtx = _httpContextAccessor.HttpContext;
        if (httpCtx != null)
        {
            ip = httpCtx.Connection.RemoteIpAddress?.ToString();
            if (ip != null && ip.Length > 50)
                ip = ip[..50];
            if (httpCtx.Request.Headers.TryGetValue("User-Agent", out var h))
            {
                ua = h.ToString();
                if (ua.Length > 300)
                    ua = ua[..300];
            }
        }

        var now = DateTime.UtcNow;

        var entries = db.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity is not AccAccountingAuditLog)
            .ToList();

        foreach (var entry in entries)
        {

            var entityType = entry.Metadata;
            var schema = entityType.GetSchema();
            var table = entityType.GetTableName();
            if (string.IsNullOrEmpty(table))
                continue;
            if (!string.Equals(schema, "acc", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.Equals(table, "AccountingAuditLog", StringComparison.OrdinalIgnoreCase))
                continue;

            var fullTable = string.IsNullOrEmpty(schema) ? table : $"{schema}.{table}";
            if (fullTable.Length > 100)
                fullTable = fullTable[..100];

            var pk = entityType.FindPrimaryKey();
            if (pk == null)
                continue;

            var recordId = string.Join(
                "|",
                pk.Properties.Select(p =>
                {
                    var prop = entry.Property(p.Name);
                    var v = entry.State == EntityState.Deleted ? prop.OriginalValue : prop.CurrentValue;
                    return v?.ToString() ?? "";
                }));
            if (recordId.Length > 50)
                recordId = recordId[..50];

            string action = entry.State switch
            {
                EntityState.Added => "INSERT",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted => "DELETE",
                _ => "UNKNOWN"
            };

            string? oldJson = null;
            string? newJson = null;
            if (entry.State == EntityState.Added)
                newJson = TruncateJson(SerializeEntry(entry, original: false));
            else if (entry.State == EntityState.Deleted)
                oldJson = TruncateJson(SerializeEntry(entry, original: true));
            else if (entry.State == EntityState.Modified)
            {
                oldJson = TruncateJson(SerializeEntry(entry, original: true));
                newJson = TruncateJson(SerializeEntry(entry, original: false));
            }

            db.AccAccountingAuditLogs.Add(new AccAccountingAuditLog
            {
                TableName = fullTable,
                RecordID = recordId,
                Action = action,
                OldValues = oldJson,
                NewValues = newJson,
                ChangedBy = changedBy,
                ChangedAt = now,
                IPAddress = ip,
                UserAgent = ua
            });
        }
    }

    private static string SerializeEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, bool original)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var prop in entry.Properties)
        {
            if (prop.Metadata.IsShadowProperty())
                continue;
            dict[prop.Metadata.Name] = original ? prop.OriginalValue : prop.CurrentValue;
        }

        return JsonSerializer.Serialize(dict, JsonOpts);
    }

    private static string? TruncateJson(string? json)
    {
        if (json == null)
            return null;
        return json.Length <= MaxJsonChars ? json : json[..MaxJsonChars];
    }
}
