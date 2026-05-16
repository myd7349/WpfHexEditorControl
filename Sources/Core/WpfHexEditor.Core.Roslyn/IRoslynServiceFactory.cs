// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: IRoslynServiceFactory.cs
// Description:
//     Factory contract for Roslyn workspace and analysis services.
//     Injected into RoslynLanguageClient so tests can provide mock
//     implementations without starting a real MSBuild workspace.
// ==========================================================

namespace WpfHexEditor.Core.Roslyn;

/// <summary>
/// Factory contract for the Roslyn workspace backing <see cref="RoslynLanguageClient"/>.
/// Inject a custom implementation in tests to avoid starting a real MSBuild workspace.
/// <see cref="BackgroundAnalysisService"/> is internal and not part of the factory contract.
/// </summary>
public interface IRoslynServiceFactory
{
    /// <summary>Creates (or returns a shared) <see cref="RoslynWorkspaceManager"/>.</summary>
    RoslynWorkspaceManager CreateWorkspaceManager();
}

/// <summary>Default factory — creates a real <see cref="RoslynWorkspaceManager"/>.</summary>
public sealed class DefaultRoslynServiceFactory : IRoslynServiceFactory
{
    public static readonly DefaultRoslynServiceFactory Instance = new();

    public RoslynWorkspaceManager CreateWorkspaceManager() => new();
}
