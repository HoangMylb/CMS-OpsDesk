using OpsDesk.Core.Services;
using OpsDesk.Infrastructure.Services;
using Xunit;

namespace OpsDesk.Tests.Unit;

public class TicketMessageTests
{
    private readonly TicketMessageService _messageService;

    public TicketMessageTests()
    {
        _messageService = new TicketMessageService(null!, null!, null!, null!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task AddMessage_EmptyContent_ShouldFailWithValidationMessage(string content)
    {
        // Act
        var result = await _messageService.AddMessageAsync(new AddMessageRequest(1, content, false), "user-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("không được để trống", result.FirstError);
    }
}
