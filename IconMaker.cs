using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

class IconMaker
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern bool DestroyIcon(IntPtr handle);

    static void Main(string[] args)
    {
        var output = args.Length > 0 ? args[0] : "ai-assistant.ico";
        using (var bitmap = new Bitmap(64, 64))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, 64, 64),
                Color.FromArgb(226, 244, 255),
                Color.FromArgb(190, 224, 248),
                45F))
            using (var pen = new Pen(Color.FromArgb(104, 172, 220), 3F))
            {
                graphics.FillEllipse(brush, 3, 3, 58, 58);
                graphics.DrawEllipse(pen, 4, 4, 56, 56);
            }

            DrawWrench(graphics, new Rectangle(15, 13, 36, 38), Color.FromArgb(35, 92, 145));

            IntPtr handle = bitmap.GetHicon();
            try
            {
                using (var icon = Icon.FromHandle(handle))
                using (var stream = System.IO.File.Create(output))
                {
                    icon.Save(stream);
                }
            }
            finally
            {
                DestroyIcon(handle);
            }
        }
    }

    static void DrawWrench(Graphics graphics, Rectangle bounds, Color color)
    {
        using (var pen = new Pen(color, 7F))
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            graphics.DrawLine(pen, bounds.Left + 10, bounds.Bottom - 8, bounds.Right - 10, bounds.Top + 12);
        }

        using (var brush = new SolidBrush(color))
        {
            graphics.FillEllipse(brush, bounds.Left + 4, bounds.Bottom - 15, 14, 14);
            graphics.FillPie(brush, bounds.Right - 20, bounds.Top + 1, 21, 21, 30, 250);
        }

        using (var cutout = new SolidBrush(Color.FromArgb(212, 236, 252)))
        {
            graphics.FillEllipse(cutout, bounds.Right - 13, bounds.Top + 8, 10, 10);
        }
    }
}
