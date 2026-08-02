using System.Text.RegularExpressions;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// The cancel button is always the rightmost button in its row.
/// </summary>
/// <remarks>
/// A source-scanning test rather than a reflection one, because button order lives in markup and there
/// is nothing in the compiled component to reflect over. It is worth the oddity: the convention is
/// invisible at the point of writing a dialog, four of them had drifted, and the two that were right
/// were right only because they use the shared <c>CancelButton</c> component.
/// <para>
/// Scope is deliberately narrow — a horizontal <c>RadzenStack</c> containing a cancel button. That is the
/// shape every dialog footer in this codebase uses, and widening it would start reporting toolbars and
/// grid cells where the rule does not apply.
/// </para>
/// </remarks>
public class DialogButtonOrderTests
{
    private static readonly Regex ButtonStack = new(
        @"<RadzenStack[^>]*Orientation\.Horizontal.*?</RadzenStack>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex AnyButton = new(
        @"<(RadzenButton|CancelButton)\b",
        RegexOptions.Compiled);

    private static bool IsCancel(string markup, int buttonStart)
    {
        // The tag runs to the next '/>' or '>' -- enough to see its Text, and CancelButton needs no Text.
        var end = markup.IndexOf("/>", buttonStart, StringComparison.Ordinal);
        var tag = end < 0 ? markup[buttonStart..] : markup[buttonStart..end];
        return tag.Contains("<CancelButton", StringComparison.Ordinal)
               || tag.Contains("Text=\"Cancel\"", StringComparison.Ordinal)
               || tag.Contains("title=\"Cancel\"", StringComparison.Ordinal);
    }

    private static DirectoryInfo RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !dir.GetDirectories("Tharga.Team.Blazor").Any()) dir = dir.Parent;
        return dir;
    }

    public static TheoryData<string> RazorFiles()
    {
        var root = RepoRoot();
        Assert.NotNull(root);

        var data = new TheoryData<string>();
        foreach (var project in new[] { "Tharga.Team.Blazor", "Tharga.Team.Sample" })
        {
            var dir = Path.Combine(root.FullName, project);
            if (!Directory.Exists(dir)) continue;

            foreach (var file in Directory.EnumerateFiles(dir, "*.razor", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
                data.Add(Path.GetRelativePath(root.FullName, file));
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(RazorFiles))]
    public void CancelIsTheRightmostButtonInItsRow(string relativePath)
    {
        var markup = File.ReadAllText(Path.Combine(RepoRoot().FullName, relativePath));

        foreach (Match stack in ButtonStack.Matches(markup))
        {
            var buttons = AnyButton.Matches(stack.Value).Select(m => m.Index).ToArray();
            if (buttons.Length < 2) continue;

            var cancelIndex = Array.FindIndex(buttons, i => IsCancel(stack.Value, i));
            if (cancelIndex < 0) continue;

            Assert.True(cancelIndex == buttons.Length - 1,
                $"{relativePath}: the cancel button is at position {cancelIndex + 1} of {buttons.Length} " +
                $"in its row; it must be last so it sits on the far right.\n\n{stack.Value.Trim()}");
        }
    }

    /// <summary>
    /// The guard finds files at all. A source scan that silently matches nothing passes forever, which
    /// would be worse than not having it — it would read as "every dialog checked".
    /// </summary>
    [Fact]
    public void TheGuard_FindsRazorFilesToCheck()
    {
        Assert.NotEmpty(RazorFiles());
    }

    /// <summary>And that it actually rejects the shape it exists to catch.</summary>
    [Fact]
    public void TheGuard_DetectsACancelButtonThatIsNotLast()
    {
        const string bad = """
            <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.End">
                <RadzenButton Text="Cancel" Click="@(_ => ds.Close(false))" />
                <RadzenButton Text="Save" Click="@(_ => ds.Close(true))" />
            </RadzenStack>
            """;

        var stack = ButtonStack.Match(bad);
        Assert.True(stack.Success);

        var buttons = AnyButton.Matches(stack.Value).Select(m => m.Index).ToArray();
        var cancelIndex = Array.FindIndex(buttons, i => IsCancel(stack.Value, i));

        Assert.Equal(2, buttons.Length);
        Assert.Equal(0, cancelIndex);          // found it...
        Assert.NotEqual(buttons.Length - 1, cancelIndex);   // ...and it is not last, so the theory would fail
    }

    /// <summary>The shared <c>CancelButton</c> component counts as a cancel button, not just a Radzen one.</summary>
    [Fact]
    public void TheGuard_RecognisesTheSharedCancelButtonComponent()
    {
        const string good = """
            <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Right">
                <RadzenButton ButtonType="ButtonType.Submit" Text="OK" Icon="check_circle" />
                <CancelButton Click="CloseDialog" />
            </RadzenStack>
            """;

        var stack = ButtonStack.Match(good);
        var buttons = AnyButton.Matches(stack.Value).Select(m => m.Index).ToArray();
        var cancelIndex = Array.FindIndex(buttons, i => IsCancel(stack.Value, i));

        Assert.Equal(2, buttons.Length);
        Assert.Equal(buttons.Length - 1, cancelIndex);
    }
}
