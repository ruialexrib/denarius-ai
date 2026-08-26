namespace DenariusAI.IntegrationTests;

/// <summary>
/// Contains definitions for FoundationTests.
/// </summary>
public sealed class FoundationTests
{
    [Fact]
    public void WebAssemblyIsAvailable()
    {
        Assert.Equal("DenariusAI.Web", typeof(Program).Assembly.GetName().Name);
    }
}
