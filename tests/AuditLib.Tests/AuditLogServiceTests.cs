using AuditLib.Abstractions;
using AuditLib.Domain;
using AuditLib.Infrastructure;
using AuditLib.Options;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuditLib.Tests;

public class AuditLogServiceTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly AuditLogRepository _repository;
    private readonly AuditLogService _service;
    private static readonly DateTime FixedTimestamp = new(2026, 6, 28, 14, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId1 = Guid.CreateVersion7();
    private readonly Guid _userId2 = Guid.CreateVersion7();

    public AuditLogServiceTests()
    {
        var options = new AuditLibOptions { TimestampProvider = () => FixedTimestamp };
        var userContext = new TestUserContext(_userId1);
        var interceptor = new AuditInterceptor(options, userContext);
        _context = TestDbContextFactory.Create(interceptor);
        _repository = new AuditLogRepository(_context);
        _service = new AuditLogService(_repository);

        SeedData();
    }

    private void SeedData()
    {
        var logs = new List<AuditLog>
        {
            AuditLog.Create("EntityA", "1", "Adicionar", _userId1, FixedTimestamp.AddMinutes(-5), null, "{}", "Registro criado."),
            AuditLog.Create("EntityA", "1", "Alterar", _userId1, FixedTimestamp.AddMinutes(-4), "{}", "{\"name\":\"new\"}", "Name alterado."),
            AuditLog.Create("EntityA", "1", "Alterar", _userId2, FixedTimestamp.AddMinutes(-3), "{\"name\":\"new\"}", "{\"name\":\"newer\"}", "Name alterado."),
            AuditLog.Create("EntityA", "2", "Adicionar", _userId1, FixedTimestamp.AddMinutes(-2), null, "{}", "Registro criado."),
            AuditLog.Create("EntityB", "1", "Adicionar", _userId2, FixedTimestamp.AddMinutes(-1), null, "{}", "Registro criado."),
        };

        _context.Set<AuditLog>().AddRange(logs);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByEntityAsync_should_return_paginated_results()
    {
        var results = await _service.GetByEntityAsync("EntityA", "1", page: 1, pageSize: 10);

        results.Should().HaveCount(3);
        results.Should().BeInDescendingOrder(r => r.Timestamp);
        results.Should().AllSatisfy(r =>
        {
            r.EntityName.Should().Be("EntityA");
            r.PrimaryKey.Should().Be("1");
        });
    }

    [Fact]
    public async Task GetByEntityAsync_should_return_empty_for_nonexistent_entity()
    {
        var results = await _service.GetByEntityAsync("NonExistent", "1");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByEntityAsync_should_respect_pagination()
    {
        var page1 = await _service.GetByEntityAsync("EntityA", "1", page: 1, pageSize: 2);
        var page2 = await _service.GetByEntityAsync("EntityA", "1", page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(1);
        page1.Should().NotIntersectWith(page2);
    }

    [Fact]
    public async Task GetCountByEntityAsync_should_return_correct_count()
    {
        var count = await _service.GetCountByEntityAsync("EntityA", "1");

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetCountByEntityAsync_should_return_zero_for_nonexistent()
    {
        var count = await _service.GetCountByEntityAsync("NonExistent", "1");

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetByUserAsync_should_return_user_audit_logs()
    {
        var results = await _service.GetByUserAsync(_userId1);

        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.UserId.Should().Be(_userId1));
    }

    [Fact]
    public async Task GetByUserAsync_should_return_empty_for_user_with_no_logs()
    {
        var results = await _service.GetByUserAsync(Guid.NewGuid());

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserAsync_should_respect_pagination()
    {
        var page1 = await _service.GetByUserAsync(_userId1, page: 1, pageSize: 2);
        var page2 = await _service.GetByUserAsync(_userId1, page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCountByUserAsync_should_return_correct_count()
    {
        var count = await _service.GetCountByUserAsync(_userId1);

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetCountByUserAsync_should_return_zero_for_nonexistent_user()
    {
        var count = await _service.GetCountByUserAsync(Guid.NewGuid());

        count.Should().Be(0);
    }
}
