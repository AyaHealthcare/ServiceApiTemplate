﻿using Domain.Model;
using Microsoft.EntityFrameworkCore;
using Package.Infrastructure.Data;
using System;
using System.Linq;

namespace Infrastructure.Data;

//EF Core PMC commands
//https://ef.readthedocs.io/en/staging/miscellaneous/cli/powershell.html#add-migration

//Null reference
//https://docs.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types

//Fluent Migration
//http://www.learnentityframeworkcore.com/configuration/fluent-api/totable-method

/*
 * Migrations applied during design time use the design time factory to create a DBContext; 
 * set the env variable for connection string in PMC before running commands
 * $env:EFCORETOOLSDB = "Server=localhost;Database=[db name];Integrated Security=true;MultipleActiveResultSets=True;Column Encryption Setting=enabled;"
 * 
 * if EF6 and EFCore tools both installed, prefix with EntityFrameworkCore\
 * EntityFrameworkCore\update-database -migration 0  -Context TodoDbContextQuery : Db back to ground zero
 * EntityFrameworkCore\remove-migration -Context TodoDbContextQuery : removes the last migration
 * EntityFrameworkCore\add-migration [name] -Context TodoDbContextQuery : adds a new migration
 * EntityFrameworkCore\script-migration -Context TodoDbContextQuery -From Specifies the starting migration. -To Specifies target migration
 * EntityFrameworkCore\update-database
 */

/*Default Retry execution strategy when adding context to DI (in Bootstrapper) does not support user initiated transactions:
 * https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency
 */

public abstract class TodoDbContextBase : DbContextBase
{
    protected TodoDbContextBase(DbContextOptions options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //schema
        modelBuilder.HasDefaultSchema("todo");

        //datatype defaults for sql
        //decimal
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            if (property.GetColumnType() == null) property.SetColumnType("decimal(10,4)");
        }
        //dates
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            if (property.GetColumnType() == null) property.SetColumnType("datetime2");
        }

        //table configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodoDbContextTrxn).Assembly);
    }

    //DbSets
    public DbSet<TodoItem> TodoItems { get; set; } = null!;
}
