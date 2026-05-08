// ==========================================================
// Project: WpfHexEditor.App
// File: Analysis/UI/ContextMenus/AnalysisContextMenuBuilder.cs
// Description: Factories that produce ContextMenu instances for each kind of
//              row in the analysis report (project / file / method / issue /
//              coupling / duplication / dead symbol / treemap rectangle).
//              All MenuItem headers are pulled from AppResources via DynamicResource
//              wherever possible — strings here are fallback English.
// Architecture Notes:
//     Each builder takes the row item plus the collaborators it needs (host
//     service for navigation, view-model for filtering, options service for
//     toggling rules, etc.) and returns a ready-to-show ContextMenu.
// ==========================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfHexEditor.App.Analysis.Models;
using WpfHexEditor.App.Analysis.UI.ViewModels;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.App.Analysis.UI.ContextMenus;

internal sealed class AnalysisContextMenuBuilder
{
    private readonly IDocumentHostService?         _docHost;
    private readonly CodeAnalysisReportViewModel   _vm;
    private readonly Func<string, Task>?           _scopedReRun;  // (path) → re-run scoped

    internal AnalysisContextMenuBuilder(
        IDocumentHostService?       docHost,
        CodeAnalysisReportViewModel vm,
        Func<string, Task>?         scopedReRun = null)
    {
        _docHost     = docHost;
        _vm          = vm;
        _scopedReRun = scopedReRun;
    }

    // ── Project row ─────────────────────────────────────────────────────────

    internal ContextMenu BuildForProject(ProjectMetrics project)
    {
        var menu = new ContextMenu();
        Add(menu, "Open project folder",          () => AnalysisContextMenuActions.OpenContainingFolder(project.ProjectPath));
        Add(menu, "Reveal in Explorer",           () => AnalysisContextMenuActions.RevealInExplorer(project.ProjectPath));
        AddSeparator(menu);
        if (_scopedReRun is not null)
            Add(menu, "Re-analyze this project",  () => _ = _scopedReRun(project.ProjectPath));
        Add(menu, "Filter all tabs by project",   () => _vm.ProjectFilter = project.ProjectName);
        AddSeparator(menu);
        Add(menu, "Copy project name",            () => AnalysisContextMenuActions.Copy(project.ProjectName));
        Add(menu, "Copy metrics as CSV row",      () => AnalysisContextMenuActions.Copy(AnalysisContextMenuActions.FormatProjectAsCsv(project)));
        return menu;
    }

    // ── File row (FileMetrics or FileMetricsViewModel) ──────────────────────

    internal ContextMenu BuildForFile(FileMetrics file)
    {
        var menu = new ContextMenu();
        Add(menu, "Open",                         () => AnalysisContextMenuActions.OpenFile(_docHost, file.FilePath, 1));
        Add(menu, "Reveal in Explorer",           () => AnalysisContextMenuActions.RevealInExplorer(file.FilePath));
        Add(menu, "Open containing folder",       () => AnalysisContextMenuActions.OpenContainingFolder(file.FilePath));
        AddSeparator(menu);
        if (_scopedReRun is not null)
            Add(menu, "Re-analyze this file",     () => _ = _scopedReRun(file.FilePath));
        Add(menu, "Filter issues by this file",   () => _vm.IssueFilter = file.FileName);
        AddSeparator(menu);
        Add(menu, "Copy full path",               () => AnalysisContextMenuActions.Copy(file.FilePath));
        Add(menu, "Copy file name",               () => AnalysisContextMenuActions.Copy(file.FileName));
        return menu;
    }

    internal ContextMenu BuildForFileVm(FileMetricsViewModel fm)
    {
        var menu = new ContextMenu();
        Add(menu, "Open",                         () => AnalysisContextMenuActions.OpenFile(_docHost, fm.FilePath, 1));
        Add(menu, "Reveal in Explorer",           () => AnalysisContextMenuActions.RevealInExplorer(fm.FilePath));
        AddSeparator(menu);
        Add(menu, "Filter issues by this file",   () => _vm.IssueFilter = fm.FileName);
        Add(menu, "Copy full path",               () => AnalysisContextMenuActions.Copy(fm.FilePath));
        return menu;
    }

    // ── Method row ──────────────────────────────────────────────────────────

    internal ContextMenu BuildForMethod(MethodMetrics method, string? filePath)
    {
        var menu = new ContextMenu();
        if (!string.IsNullOrEmpty(filePath))
        {
            Add(menu, "Open at method declaration", () => AnalysisContextMenuActions.OpenFile(_docHost, filePath, method.Line));
            AddSeparator(menu);
        }
        Add(menu, "Copy method name",              () => AnalysisContextMenuActions.Copy(method.Name));
        Add(menu, "Copy fully-qualified name",     () => AnalysisContextMenuActions.Copy(method.FullyQualifiedName));
        return menu;
    }

    // ── Issue row ───────────────────────────────────────────────────────────

    internal ContextMenu BuildForIssue(IssueViewModel issue)
    {
        var menu = new ContextMenu();
        Add(menu, "Open at issue location",        () => AnalysisContextMenuActions.OpenFile(_docHost, issue.FilePath, issue.Line));
        AddSeparator(menu);
        Add(menu, "Filter by this rule",           () => _vm.IssueFilter = issue.Id);
        Add(menu, "Filter by this file",           () => _vm.IssueFilter = issue.FileName);
        AddSeparator(menu);
        Add(menu, "Suppress this occurrence (inline marker)",
            () => AnalysisContextMenuActions.AddInlineSuppressMarker(issue.FilePath, issue.Line, issue.Id));
        AddSeparator(menu);
        Add(menu, "Copy as Markdown",              () => AnalysisContextMenuActions.Copy(AnalysisContextMenuActions.FormatIssueAsMarkdown(issue)));
        Add(menu, "Copy issue ID",                 () => AnalysisContextMenuActions.Copy(issue.Id));
        return menu;
    }

    // ── Coupling row ────────────────────────────────────────────────────────

    internal ContextMenu BuildForCoupling(CouplingMetrics coupling)
    {
        var menu = new ContextMenu();
        Add(menu, "Open source file",              () => AnalysisContextMenuActions.OpenFile(_docHost, coupling.FilePath, coupling.Line));
        AddSeparator(menu);
        Add(menu, "Copy type name",                () => AnalysisContextMenuActions.Copy(coupling.TypeName));
        Add(menu, "Copy dependency list",          () => AnalysisContextMenuActions.Copy(string.Join("\n", coupling.DependsOn)));
        return menu;
    }

    // ── Duplication group ──────────────────────────────────────────────────

    internal ContextMenu BuildForDuplication(DuplicationGroup group)
    {
        var menu = new ContextMenu();
        if (group.Occurrences.Count > 0)
        {
            var first = group.Occurrences[0];
            Add(menu, "Open first occurrence",     () => AnalysisContextMenuActions.OpenFile(_docHost, first.FilePath, first.StartLine));
        }
        AddSeparator(menu);
        Add(menu, $"Copy locations ({group.Occurrences.Count})",
            () => AnalysisContextMenuActions.Copy(string.Join("\n", group.Occurrences.Select(o => $"{o.FilePath}:{o.StartLine}"))));
        return menu;
    }

    // ── Dead symbol ────────────────────────────────────────────────────────

    internal ContextMenu BuildForDeadSymbol(DeadSymbol dead)
    {
        var menu = new ContextMenu();
        Add(menu, "Open at declaration",           () => AnalysisContextMenuActions.OpenFile(_docHost, dead.FilePath, dead.Line));
        AddSeparator(menu);
        Add(menu, "Copy symbol name",              () => AnalysisContextMenuActions.Copy(dead.Name));
        return menu;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static void Add(ContextMenu menu, string header, Action onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => onClick();
        menu.Items.Add(item);
    }

    private static void AddSeparator(ContextMenu menu)
        => menu.Items.Add(new Separator());
}
