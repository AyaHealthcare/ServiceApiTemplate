using Application.Contracts.Model;
using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Data.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;
using Test.Support;

namespace Test.Unit.Repository;

[TestClass]
public class TodoRepositoryQueryTests : UnitTestBase
{
    public TodoRepositoryQueryTests() : base()
    {
    }

    [TestMethod]
    public async Task SearchTodoItemAsync_pass()
    {
        //arrange

        //custom data scenario that default seed data does not cover
        static void customData(List<TodoItem> entities)
        {
            entities.Add(new TodoItem("custom entity a"));
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .Build<TodoDbContextQuery>();

        var audit = new AuditDetail("Test.Unit");
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, audit, _mapper);

        //act & assert
        var search = new SearchRequest<TodoItemSearchFilter> { PageSize = 10, PageIndex = 1 };
        var response = await repoQuery.SearchTodoItemAsync(search);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        search = new SearchRequest<TodoItemSearchFilter> { PageSize = 2, PageIndex = 1 };
        response = await repoQuery.SearchTodoItemAsync(search);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(2, response.Data.Count);

        search = new SearchRequest<TodoItemSearchFilter> { PageSize = 3, PageIndex = 2 };
        response = await repoQuery.SearchTodoItemAsync(search);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(1, response.Data.Count);
    }

    [TestMethod]
    public async Task GetPageEntitiesAsync_pass()
    {
        //arrange
        static void customData(List<TodoItem> entities)
        {
            //custom data scenario that default seed data does not cover
            entities.Add(new TodoItem("some entity a"));
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .Build<TodoDbContextQuery>();

        var audit = new AuditDetail("Test.Unit");
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, audit, _mapper);

        //act & assert
        var response = await repoQuery.GetPageEntitiesAsync<TodoItem>(pageSize: 10, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        response = await repoQuery.GetPageEntitiesAsync<TodoItem>(pageSize: 2, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(2, response.Data.Count);

        response = await repoQuery.GetPageEntitiesAsync<TodoItem>(pageSize: 3, pageIndex: 2, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(1, response.Data.Count);
    }

    [TestMethod]
    public async Task SearchWithFilterAndSort_pass()
    {
        //arrange
        static void customData(List<TodoItem> entities)
        {
            //custom data scenario that default seed data does not cover
            entities.AddRange(new List<TodoItem>
                {
                    new TodoItem ("A some entity a", TodoItemStatus.InProgress),
                    new TodoItem ("B some entity a", TodoItemStatus.InProgress),
                    new TodoItem ("C some entity a", TodoItemStatus.InProgress)
                });
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .Build<TodoDbContextQuery>();

        var audit = new AuditDetail("Test.Unit");
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, audit, _mapper);

        //search criteria
        var search = new SearchRequest<TodoItemSearchFilter>
        {
            PageSize = 10,
            PageIndex = 1,
            Filter = new TodoItemSearchFilter { Statuses = new List<TodoItemStatus> { TodoItemStatus.InProgress } },
            Sorts = new List<Sort> { new Sort("Name", SortOrder.Descending) }
        };

        //act
        var response = await repoQuery.SearchTodoItemAsync(search);

        //assert
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        var indexOfA = response.Data.FindIndex(e => e.Name.StartsWith("A"));
        var indexOfB = response.Data.FindIndex(e => e.Name.StartsWith("B"));
        var indexOfC = response.Data.FindIndex(e => e.Name.StartsWith("C"));
        Assert.IsTrue(indexOfC < indexOfB);
        Assert.IsTrue(indexOfB < indexOfA);
    }
}
