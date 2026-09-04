using DenariusAI.Web.Models;

namespace DenariusAI.UnitTests;

/// <summary>Verifies deployment-level public demonstration configuration behavior.</summary>
public sealed class DemoModeOptionsTests
{
    /// <summary>Verifies that credentials are exposed only for an enabled and fully configured demo installation.</summary>
    [Fact]
    public void HasCredentialsRequiresEnabledModeAndCompleteCredentials()
    {
        Assert.False(new DemoModeOptions { Enabled = false, Email = "guest@example.test", Password = "demo-password" }.HasCredentials);
        Assert.False(new DemoModeOptions { Enabled = true, Email = "guest@example.test" }.HasCredentials);
        Assert.False(new DemoModeOptions { Enabled = true, Password = "demo-password" }.HasCredentials);
        Assert.True(new DemoModeOptions { Enabled = true, Email = "guest@example.test", Password = "demo-password" }.HasCredentials);
    }
}
