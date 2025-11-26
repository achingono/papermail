using System.Linq.Expressions;

namespace Papermail.Core.Entities;

/// <summary>
/// Defines a generic repository interface for managing entities.
/// </summary>
/// <typeparam name="TEntity">The type of the entity being managed.</typeparam>
public interface IRepository<TEntity>
{
    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The added entity.</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> DeleteAsync(TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all entities from the repository.
    /// </summary>
    /// <param name="predicate">An optional predicate to filter the entities.</param>
    /// <returns>A collection of entities.</returns>
    IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>>? predicate = default);

    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to replace.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<TEntity> UpdateAsync(Guid id, TEntity entity, CancellationToken cancellationToken);
}