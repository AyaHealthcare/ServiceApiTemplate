﻿using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Functions.Infrastructure;
public class DatabaseService : IDatabaseService
{
    private readonly string? _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SampleDB");
    }

    public async Task MethodAsync(string? filename, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_connectionString) || string.IsNullOrEmpty(filename)) return;

        cancellationToken.ThrowIfCancellationRequested();
        using var connection = new SqlConnection(_connectionString);
        SqlCommand cmd = new($"exec sp_SomeProc '{filename}'", connection)
        {
            CommandType = CommandType.Text
        };
        await connection.OpenAsync(cancellationToken);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
