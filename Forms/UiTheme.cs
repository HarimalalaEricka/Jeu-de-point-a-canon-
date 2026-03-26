using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PointGame.Forms
{
    internal static class UiTheme
    {
        public static readonly Color AppBackground = Color.FromArgb(20, 24, 36);
        public static readonly Color CardBackground = Color.FromArgb(32, 38, 55);
        public static readonly Color GridBackground = Color.FromArgb(248, 250, 255);
        public static readonly Color TextPrimary = Color.FromArgb(238, 242, 255);
        public static readonly Color TextSecondary = Color.FromArgb(173, 184, 212);
        public static readonly Color Accent = Color.FromArgb(90, 147, 255);
        public static readonly Color AccentHover = Color.FromArgb(112, 164, 255);
        public static readonly Color Danger = Color.FromArgb(220, 92, 92);

        public static Font TitleFont(float size = 18f) => new Font("Segoe UI Semibold", size, FontStyle.Bold);
        public static Font BodyFont(float size = 10f) => new Font("Segoe UI", size, FontStyle.Regular);
        public static Font BodyBoldFont(float size = 10f) => new Font("Segoe UI Semibold", size, FontStyle.Bold);

        public static Panel CreateCard(int x, int y, int width, int height)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = CardBackground
            };

            panel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(54, 64, 92), 1);
                var rect = panel.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(pen, rect);
            };

            return panel;
        }

        public static void StylePrimaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Accent;
            button.ForeColor = Color.White;
            button.Font = BodyBoldFont(10f);
            button.Cursor = Cursors.Hand;
            button.Height = 38;

            button.MouseEnter += (_, __) => button.BackColor = AccentHover;
            button.MouseLeave += (_, __) => button.BackColor = Accent;
        }

        public static void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(86, 101, 144);
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = CardBackground;
            button.ForeColor = TextPrimary;
            button.Font = BodyBoldFont(10f);
            button.Cursor = Cursors.Hand;
            button.Height = 36;
        }

        public static Label CreateLabel(string text, int x, int y, float size = 10f, bool secondary = false)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = secondary ? TextSecondary : TextPrimary,
                Font = BodyFont(size),
                BackColor = Color.Transparent
            };
        }
    }
}