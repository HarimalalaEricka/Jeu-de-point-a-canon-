using System;
using System.Drawing;
using System.Windows.Forms;
using PointGame.Services;

namespace PointGame.Forms
{
    public class MainMenuForm : Form
    {
        private DatabaseService dbService;

        public MainMenuForm()
        {
            dbService = new DatabaseService();

            this.Text = "Point Game";
            this.Size = new Size(520, 340);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = UiTheme.AppBackground;

            var card = UiTheme.CreateCard(40, 36, 424, 248);

            var lblTitle = new Label
            {
                Text = "Point à Canon",
                Font = UiTheme.TitleFont(22f),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(118, 28),
                BackColor = Color.Transparent
            };

            var lblSubtitle = new Label
            {
                Text = "Choisis une action pour commencer",
                Font = UiTheme.BodyFont(10f),
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(116, 68),
                BackColor = Color.Transparent
            };

            Button btnNewGame = new Button { Text = "Nouvelle partie", Location = new Point(84, 112), Size = new Size(252, 40) };
            UiTheme.StylePrimaryButton(btnNewGame);
            btnNewGame.Click += (s, e) => {
                var setupForm = new GameSetupForm(dbService);
                this.Hide();
                setupForm.ShowDialog();
                this.Show();
            };

            Button btnLoadGame = new Button { Text = "Charger une partie", Location = new Point(84, 166), Size = new Size(252, 40) };
            UiTheme.StyleSecondaryButton(btnLoadGame);
            btnLoadGame.Click += (s, e) => {
                var loadForm = new LoadGameForm(dbService);
                this.Hide();
                loadForm.ShowDialog();
                this.Show();
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblSubtitle);
            card.Controls.Add(btnNewGame);
            card.Controls.Add(btnLoadGame);
            this.Controls.Add(card);
        }
    }
}
