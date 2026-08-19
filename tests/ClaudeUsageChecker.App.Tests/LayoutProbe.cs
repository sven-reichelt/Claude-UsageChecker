using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Measures the content of a window and looks for elements that extend past
/// its width.
/// </summary>
/// <remarks>
/// <para>
/// Do not measure the window: its width is fixed, so it always reports the
/// specified value, no matter how much overflows inside it.
/// </para>
/// <para>
/// And do not measure without a constraint: wrapping text blocks then report
/// their full line length, which would falsely mark every longer sentence as an
/// overflow. Given a constraint they comply, while a row of buttons side by side
/// reports its requirement unchanged - which is exactly what should stand out.
/// </para>
/// </remarks>
internal static class LayoutProbe
{
    /// <summary>Rounding leeway of the layout, in pixels.</summary>
    private const double Tolerance = 0.5;

    public static bool FitsTheWidth(Window window, out double width)
    {
        window.Show();

        width = 0;
        var fits = true;

        foreach (var child in window.GetLogicalDescendants().OfType<Control>())
        {
            if (!child.IsVisible || child.Bounds.Width <= 0)
            {
                continue;
            }

            // The right edge of the element, converted to window coordinates.
            if (child.TranslatePoint(new Point(child.Bounds.Width, 0), window) is not { } right)
            {
                continue;
            }

            width = Math.Max(width, right.X);
            if (right.X > window.Width + Tolerance)
            {
                fits = false;
            }
        }

        window.Hide();
        return fits;
    }
}
