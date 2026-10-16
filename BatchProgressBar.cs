using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PS5SavefileBatcher;

// Owner drawing keeps the completion color independent of the Windows accent/theme.
public sealed class BatchProgressBar : Control
{
    private int maximum = 100, value, offset;
    private ProgressBarStyle style;
    private readonly Timer animation = new() { Interval = 40 };
    public BatchProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Height = 20; ForeColor = Color.DodgerBlue; BackColor = SystemColors.ControlLight;
        AccessibleRole = AccessibleRole.ProgressBar; AccessibleName = "Backup progress";
        animation.Tick += (_, _) => { offset = (offset + 4) % Math.Max(1, Width); Invalidate(); };
    }
    [DefaultValue(100)]
    public int Maximum
    {
        get => maximum;
        set { maximum = Math.Max(1, value); this.value = Math.Min(this.value, maximum); Invalidate(); }
    }
    [DefaultValue(0)]
    public int Value
    {
        get => value;
        set { this.value = Math.Clamp(value, 0, maximum); Invalidate(); }
    }
    [DefaultValue(ProgressBarStyle.Blocks)]
    public ProgressBarStyle Style
    {
        get => style;
        set { style = value; animation.Enabled = value == ProgressBarStyle.Marquee; Invalidate(); }
    }
    public void SetOutcome(BatchTiming timing, int completed, int total)
    {
        ForeColor = timing.IsSuccessful(completed, total) ? Color.ForestGreen : Color.DodgerBlue;
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var fill = Rectangle.Inflate(ClientRectangle, -1, -1);
        using var brush = new SolidBrush(ForeColor);
        if (style == ProgressBarStyle.Marquee)
        {
            int segment = Math.Max(1, fill.Width / 4);
            e.Graphics.FillRectangle(brush, fill.Left + offset, fill.Top, Math.Min(segment, Math.Max(0, fill.Width - offset)), fill.Height);
        }
        else
        {
            fill.Width = (int)((long)fill.Width * value / maximum);
            if (fill.Width > 0 && fill.Height > 0) e.Graphics.FillRectangle(brush, fill);
        }
        ControlPaint.DrawBorder(e.Graphics, ClientRectangle, SystemColors.ControlDark, ButtonBorderStyle.Solid);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) animation.Dispose();
        base.Dispose(disposing);
    }
}
