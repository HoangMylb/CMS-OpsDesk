using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpsDesk.Core.Data;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;
using OpsDesk.Infrastructure.Data;
using OpsDesk.Infrastructure.Services;
using Xunit;

namespace OpsDesk.Tests.Integration;

public class CustomerIntegrationTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ShouldReturnFailureResult()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var uow = new UnitOfWork(context);
        var auditService = new AuditService(uow, NullLogger<AuditService>.Instance);
        var customerService = new CustomerService(uow, auditService, NullLogger<CustomerService>.Instance);

        // Seed existing customer
        context.Customers.Add(new Customer
        {
            Name = "Existing Customer",
            Email = "duplicate@example.com"
        });
        await context.SaveChangesAsync();

        // Act
        var request = new CreateCustomerRequest("New Customer", "duplicate@example.com", null, null);
        var result = await customerService.CreateAsync(request, "admin-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("already in use", result.FirstError);
    }

    [Fact]
    public async Task CreateAsync_UniqueEmail_ShouldPersistCustomerSuccessfully()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var uow = new UnitOfWork(context);
        var auditService = new AuditService(uow, NullLogger<AuditService>.Instance);
        var customerService = new CustomerService(uow, auditService, NullLogger<CustomerService>.Instance);

        // Act
        var request = new CreateCustomerRequest("Acme Corp", "support@acme.com", "0123456789", "Acme Inc");
        var result = await customerService.CreateAsync(request, "admin-1");

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.Data > 0);

        var savedCustomer = await context.Customers.FindAsync(result.Data);
        Assert.NotNull(savedCustomer);
        Assert.Equal("Acme Corp", savedCustomer.Name);
        Assert.Equal("support@acme.com", savedCustomer.Email);
    }
}
