namespace DenariusAI.IntegrationTests;

public sealed class FoundationTests
{
    [Fact]
    public void WebAssemblyIsAvailable()
    {
        Assert.Equal("DenariusAI.Web", typeof(Program).Assembly.GetName().Name);
    }
}
