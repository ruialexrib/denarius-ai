namespace DenariusAI.IntegrationTests;

/// <summary>
/// Contains tests for the Foundation type.
/// </summary>
public sealed class FoundationTests
{
    [Fact]
    public void WebAssemblyIsAvailable()
    {
        Assert.Equal("DenariusAI.Web", typeof(Program).Assembly.GetName().Name);
    }
}
