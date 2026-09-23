namespace OpsDesk.Infrastructure.Seeding;

/// <summary>
/// Public, disposable identities for the isolated portfolio deployment.
/// The password is supplied only through DemoSeed:Password when demo seeding is enabled.
/// </summary>
public static class PortfolioDemoAccounts
{
    public const string ManagerEmail = "demo.manager@opsdesk.example";
    public const string SupportAgentEmail = "demo.agent@opsdesk.example";

    public static bool IsPublicDemoEmail(string? email) =>
        string.Equals(email, ManagerEmail, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(email, SupportAgentEmail, StringComparison.OrdinalIgnoreCase);
}
