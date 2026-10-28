namespace DrawingVerifier.Domain.Interfaces;

using DrawingVerifier.Domain.Entities;

/// <summary>
/// Repository contract for persisting and querying verification profiles (VerificationSkill).
/// </summary>
public interface ISkillRepository
{
    Task<VerificationSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VerificationSkill>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(VerificationSkill skill, CancellationToken cancellationToken = default);

    Task UpdateAsync(VerificationSkill skill, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
