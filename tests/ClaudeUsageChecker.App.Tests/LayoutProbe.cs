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
/// Do not measure the window content against the window content: where the width
/// is fixed the window always reports the specified value, no matter how much
/// overflows inside it. Where it grows with its content there is no specified
/// value at all - Width stays NaN, and every comparison against it is false,
/// which would quietly turn the whole check into a formality. Hence the actual
/// width once the window stands.
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

    /// <summary>
    /// The width the content has to live within: the specified one, or the one
    /// the window actually took where it sizes itself to its content.
    /// </summary>
    private static double Available(Window window) =>
        double.IsNaN(window.Width) ? window.Bounds.Width : window.Width;

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
            if (right.X > Available(window) + Tolerance)
            {
                fits = false;
            }
        }

        window.Hide();
        return fits;
    }
}
