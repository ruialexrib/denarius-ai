using System.Xml.Linq;

namespace DenariusAI.IntegrationTests;

/// <summary>Protects the verified project and namespace dependency boundaries of DenariusAI.</summary>
public sealed class ArchitectureBoundaryTests
{
    /// <summary>Verifies that each production project references only the currently intended layers.</summary>
    [Fact]
    public void ProjectReferencesRespectLayerBoundaries()
    {
        var root = FindRepositoryRoot();
        AssertProjectReferences(root, "DenariusAI.Domain", []);
        AssertProjectReferences(root, "DenariusAI.Application", ["DenariusAI.Domain"]);
        AssertProjectReferences(root, "DenariusAI.Infrastructure", ["DenariusAI.Application", "DenariusAI.Domain"]);
        AssertProjectReferences(root, "DenariusAI.Web", ["DenariusAI.Application", "DenariusAI.Infrastructure"]);
        AssertProjectReferences(root, "DenariusAI.Mcp", ["DenariusAI.Application", "DenariusAI.Infrastructure"]);
    }

    /// <summary>Verifies that core source code does not import Web or Infrastructure namespaces.</summary>
    [Fact]
    public void CoreProjectsDoNotLeakOuterLayerNamespaces()
    {
        var root = FindRepositoryRoot();
        AssertNoNamespaceReference(root, "DenariusAI.Domain", ["DenariusAI.Application", "DenariusAI.Infrastructure", "DenariusAI.Web", "DenariusAI.Mcp"]);
        AssertNoNamespaceReference(root, "DenariusAI.Application", ["DenariusAI.Infrastructure", "DenariusAI.Web", "DenariusAI.Mcp"]);
    }

    /// <summary>Finds the repository root from the test execution directory.</summary>
    /// <returns>The directory that contains the solution file.</returns>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DenariusAI.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Could not locate the DenariusAI repository root.");
    }

    /// <summary>Compares a project's direct project references with its allowed dependencies.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="projectName">Production project name.</param>
    /// <param name="allowedReferences">Allowed direct project names.</param>
    private static void AssertProjectReferences(string root, string projectName, IReadOnlyCollection<string> allowedReferences)
    {
        var projectPath = Path.Combine(root, "src", projectName, projectName + ".csproj");
        var document = XDocument.Load(projectPath);
        var actual = document.Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension((string?)element.Attribute("Include") ?? string.Empty))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expected = allowedReferences.Order(StringComparer.Ordinal).ToArray();
        Assert.True(expected.SequenceEqual(actual), $"{projectName} references [{string.Join(", ", actual)}], expected only [{string.Join(", ", expected)}].");
    }

    /// <summary>Ensures source files do not reference forbidden outer-layer namespaces.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="projectName">Production project name.</param>
    /// <param name="forbiddenNamespaces">Namespace prefixes that must not appear.</param>
    private static void AssertNoNamespaceReference(string root, string projectName, IReadOnlyCollection<string> forbiddenNamespaces)
    {
        var projectDirectory = Path.Combine(root, "src", projectName);
        foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                    && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            var source = File.ReadAllText(file);
            foreach (var forbiddenNamespace in forbiddenNamespaces)
            {
                Assert.DoesNotContain(forbiddenNamespace + ".", source, StringComparison.Ordinal);
            }
        }
    }
}
