using System.Data;
using System.Data.Common;
using Npgsql;
using WebApplication.asp.net.c3.DAL.Interfaces;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.DAL.Repositories;

/// <summary>
/// Category repository implementation using pure ADO.NET
/// Demonstrates: connection management, parameterization, transactions
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public CategoryRepository(IDbConnection connection, IDbTransaction? transaction)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction;
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, name, description, image_url, parent_category_id, 
                   is_active, sort_order, created_at, updated_at, is_deleted
            FROM categories
            WHERE id = @Id AND is_deleted = FALSE";

        using var command = CreateCommand(sql);
        AddParameter(command, "@Id", id);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToCategory(reader);
        }

        return null;
    }

    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, name, description, image_url, parent_category_id, 
                   is_active, sort_order, created_at, updated_at, is_deleted
            FROM categories
            WHERE is_deleted = FALSE
            ORDER BY sort_order, name";

        using var command = CreateCommand(sql);
        using var reader = await ExecuteReaderAsync(command, cancellationToken);

        var categories = new List<Category>();
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(MapToCategory(reader));
        }

        return categories;
    }

    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, name, description, image_url, parent_category_id, 
                   is_active, sort_order, created_at, updated_at, is_deleted
            FROM categories
            WHERE is_deleted = FALSE AND is_active = TRUE
            ORDER BY sort_order, name";

        using var command = CreateCommand(sql);
        using var reader = await ExecuteReaderAsync(command, cancellationToken);

        var categories = new List<Category>();
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(MapToCategory(reader));
        }

        return categories;
    }

    public async Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, name, description, image_url, parent_category_id, 
                   is_active, sort_order, created_at, updated_at, is_deleted
            FROM categories
            WHERE is_deleted = FALSE AND parent_category_id = @ParentId
            ORDER BY sort_order, name";

        using var command = CreateCommand(sql);
        AddParameter(command, "@ParentId", parentId);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);

        var categories = new List<Category>();
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(MapToCategory(reader));
        }

        return categories;
    }

    public async Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, name, description, image_url, parent_category_id, 
                   is_active, sort_order, created_at, updated_at, is_deleted
            FROM categories
            WHERE is_deleted = FALSE AND LOWER(name) = LOWER(@Name)";

        using var command = CreateCommand(sql);
        AddParameter(command, "@Name", name);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToCategory(reader);
        }

        return null;
    }

    public async Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM products
            WHERE category_id = @CategoryId AND is_deleted = FALSE";

        using var command = CreateCommand(sql);
        AddParameter(command, "@CategoryId", categoryId);

        var result = await ExecuteScalarAsync(command, cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    public async Task<int> AddAsync(Category entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO categories (name, description, image_url, parent_category_id, is_active, sort_order, created_at, is_deleted)
            VALUES (@Name, @Description, @ImageUrl, @ParentCategoryId, @IsActive, @SortOrder, @CreatedAt, FALSE)
            RETURNING id";

        using var command = CreateCommand(sql);
        AddParameter(command, "@Name", entity.Name);
        AddParameter(command, "@Description", entity.Description);
        AddParameter(command, "@ImageUrl", entity.ImageUrl);
        AddParameter(command, "@ParentCategoryId", entity.ParentCategoryId, DbType.Int32, true);
        AddParameter(command, "@IsActive", entity.IsActive);
        AddParameter(command, "@SortOrder", entity.SortOrder);
        AddParameter(command, "@CreatedAt", entity.CreatedAt);

        var result = await ExecuteScalarAsync(command, cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE categories
            SET name = @Name,
                description = @Description,
                image_url = @ImageUrl,
                parent_category_id = @ParentCategoryId,
                is_active = @IsActive,
                sort_order = @SortOrder,
                updated_at = @UpdatedAt
            WHERE id = @Id AND is_deleted = FALSE";

        using var command = CreateCommand(sql);
        AddParameter(command, "@Id", entity.Id);
        AddParameter(command, "@Name", entity.Name);
        AddParameter(command, "@Description", entity.Description);
        AddParameter(command, "@ImageUrl", entity.ImageUrl);
        AddParameter(command, "@ParentCategoryId", entity.ParentCategoryId, DbType.Int32, true);
        AddParameter(command, "@IsActive", entity.IsActive);
        AddParameter(command, "@SortOrder", entity.SortOrder);
        AddParameter(command, "@UpdatedAt", DateTime.UtcNow);

        var rowsAffected = await ExecuteNonQueryAsync(command, cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // Soft delete
        const string sql = @"
            UPDATE categories
            SET is_deleted = TRUE, updated_at = @UpdatedAt
            WHERE id = @Id AND is_deleted = FALSE";

        using var command = CreateCommand(sql);
        AddParameter(command, "@Id", id);
        AddParameter(command, "@UpdatedAt", DateTime.UtcNow);

        var rowsAffected = await ExecuteNonQueryAsync(command, cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM categories WHERE id = @Id AND is_deleted = FALSE";

        using var command = CreateCommand(sql);
        AddParameter(command, "@Id", id);

        var result = await ExecuteScalarAsync(command, cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    #region Helper Methods

    private IDbCommand CreateCommand(string sql)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transaction;
        return command;
    }

    private void AddParameter(IDbCommand command, string name, object? value, DbType? dbType = null, bool isNullable = false)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        
        if (value == null && isNullable)
        {
            parameter.Value = DBNull.Value;
        }
        else
        {
            parameter.Value = value ?? DBNull.Value;
        }

        if (dbType.HasValue)
        {
            parameter.DbType = dbType.Value;
        }

        command.Parameters.Add(parameter);
    }

    private async Task<DbDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand npgsqlCommand)
        {
            return await npgsqlCommand.ExecuteReaderAsync(cancellationToken);
        }
        throw new NotSupportedException("Only NpgsqlCommand is supported for async operations");
    }

    private async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand npgsqlCommand)
        {
            return await npgsqlCommand.ExecuteScalarAsync(cancellationToken);
        }
        return command.ExecuteScalar();
    }

    private async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand npgsqlCommand)
        {
            return await npgsqlCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        return command.ExecuteNonQuery();
    }

    private Category MapToCategory(IDataReader reader)
    {
        return new Category
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
            ParentCategoryId = reader.IsDBNull(reader.GetOrdinal("parent_category_id")) ? null : reader.GetInt32(reader.GetOrdinal("parent_category_id")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
            SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
            IsDeleted = reader.GetBoolean(reader.GetOrdinal("is_deleted"))
        };
    }

    #endregion
}
