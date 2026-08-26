namespace DenariusAI.UnitTests;

public sealed class FoundationTests
{
    [Fact]
    public void ApplicationAssemblyIsAvailable()
    {
        Assert.Equal("DenariusAI.Application", typeof(DenariusAI.Application.DependencyInjection).Assembly.GetName().Name);
    }
}
