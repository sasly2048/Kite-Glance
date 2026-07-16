using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// UseWindowsForms + UseWPF means BOTH worlds are in scope via implicit usings,
// and they collide on a dozen names. This file draws with GDI+, so every
// ambiguous name is pinned to System.Drawing. Without these, the compiler
// reports six CS0104s and the cause is entirely non-obvious.
using Brush = System.Drawing.Brush;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using Pen = System.Drawing.Pen;
using Point = System.Drawing.Point;
using PointF = System.Drawing.PointF;
using Rectangle = System.Drawing.Rectangle;
using SolidBrush = System.Drawing.SolidBrush;

namespace KiteGlance;

/// <summary>
/// WinForms' ToolStrip renders a grey slab with a gradient margin strip, and no
/// amount of colour-table fiddling gets it to stop. So take the pen: draw the
/// background, the border, the highlight, and the check mark ourselves.
///
/// This is the difference between a widget that has a dark menu and a widget
/// where nothing gives away that two UI frameworks are in the room.
/// </summary>
internal sealed class TrayTheme : ToolStripRenderer
{
    private static readonly Color Surface = Color.FromArgb(0xF2, 0x1A, 0x1C, 0x20);
    private static readonly Color Edge = Color.FromArgb(0x2A, 0x2F, 0x36);
    private static readonly Color Hover = Color.FromArgb(0x26, 0x2B, 0x32);
    private static readonly Color Text = Color.FromArgb(0xF2, 0xF4, 0xF5);
    private static readonly Color Dim = Color.FromArgb(0x8A, 0x90, 0x99);
    private static readonly Color Rule = Color.FromArgb(0x25, 0x2A, 0x30);
    private static readonly Color Accent = Color.FromArgb(0x0A, 0x84, 0xFF);

    public static void Apply(ToolStripDropDownMenu menu)
    {
        menu.Renderer = new TrayTheme();
        menu.BackColor = Surface;
        menu.ForeColor = Text;
        menu.ShowImageMargin = false;
        menu.Padding = new Padding(0, 5, 0, 5);
        menu.DropShadowEnabled = true;
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        var d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var r = new Rectangle(Point.Empty, e.AffectedBounds.Size);
        r.Width -= 1;
        r.Height -= 1;

        using var path = Rounded(r, 9);
        using var fill = new SolidBrush(Surface);
        e.Graphics.FillPath(fill, path);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var r = new Rectangle(Point.Empty, e.AffectedBounds.Size);
        r.Width -= 1;
        r.Height -= 1;

        using var path = Rounded(r, 9);
        using var pen = new Pen(Edge);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled) return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var r = new Rectangle(4, 0, e.Item.Width - 8, e.Item.Height);
        using var path = Rounded(r, 6);
        using var fill = new SolidBrush(Hover);
        e.Graphics.FillPath(fill, path);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Text : Dim;
        e.TextFont = new Font("Segoe UI", 9.25f, FontStyle.Regular);

        // Leave room for the check column so labels don't jump when toggled.
        e.TextRectangle = new Rectangle(
            e.TextRectangle.X + 22, e.TextRectangle.Y,
            e.TextRectangle.Width, e.TextRectangle.Height);

        base.OnRenderItemText(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var cx = 14;
        var cy = e.Item.Height / 2;

        using var pen = new Pen(Accent, 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        e.Graphics.DrawLines(pen, new[]
        {
            new PointF(cx - 4, cy),
            new PointF(cx - 1, cy + 3),
            new PointF(cx + 5, cy - 4)
        });
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(Rule);
        var y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
    }
}
