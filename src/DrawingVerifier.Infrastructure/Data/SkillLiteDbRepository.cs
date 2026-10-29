namespace DrawingVerifier.Infrastructure.Data;

using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;
using LiteDB;

/// <summary>
/// Verification profile (VerificationSkill) repository backed by the LiteDB embedded database.
/// </summary>
public class SkillLiteDbRepository : ISkillRepository
{
    public static string DefaultDatabasePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DrawingVerifier",
        "DrawingVerifier.db");

    public static string DefaultConnectionString => $"Filename={DefaultDatabasePath};Connection=shared";

    private readonly string _connectionString;

    public SkillLiteDbRepository(string? connectionString = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var dbDir = Path.GetDirectoryName(DefaultDatabasePath);
            if (!string.IsNullOrEmpty(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            _connectionString = DefaultConnectionString;
        }
        else
        {
            _connectionString = connectionString;
        }
    }

    public Task<VerificationSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<VerificationSkill>();
        var skill = collection.FindById(id);

        return Task.FromResult<VerificationSkill?>(skill);
    }

    public Task<IReadOnlyList<VerificationSkill>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<VerificationSkill>();
        var skills = collection.FindAll().ToList();

        return Task.FromResult<IReadOnlyList<VerificationSkill>>(skills);
    }

    public Task AddAsync(VerificationSkill skill, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skill);
        cancellationToken.ThrowIfCancellationRequested();

        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<VerificationSkill>();
        collection.Insert(skill);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(VerificationSkill skill, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skill);
        cancellationToken.ThrowIfCancellationRequested();

        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<VerificationSkill>();
        collection.Update(skill);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<VerificationSkill>();
        collection.Delete(id);

        return Task.CompletedTask;
    }
}
