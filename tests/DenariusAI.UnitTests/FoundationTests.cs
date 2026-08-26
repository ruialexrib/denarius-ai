namespace DenariusAI.UnitTests;

/// <summary>
/// Contains definitions for FoundationTests.
/// </summary>
public sealed class FoundationTests
{
    [Fact]
    public void ApplicationAssemblyIsAvailable()
    {
        Assert.Equal("DenariusAI.Application", typeof(DenariusAI.Application.DependencyInjection).Assembly.GetName().Name);
    }
}
