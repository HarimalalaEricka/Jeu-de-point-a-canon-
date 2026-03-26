using System;
using System.Drawing;
using System.Windows.Forms;
using PointGame.Models;
using PointGame.Services;

namespace PointGame.Forms
{
    public class GameSetupForm : Form
    {
        private NumericUpDown nudWidth;
        private NumericUpDown nudHeight;
        private Button btnColor1;
        private Button btnColor2;
        private Color color1 = Color.Pink;
        private Color color2 = Color.Purple;
        private DatabaseService dbService;

        public GameSetupForm(DatabaseService db)
        {
            this.dbService = db;
            this.Text = "Configuration de partie";
            this.Size = new Size(560, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = UiTheme.AppBackground;

            var card = UiTheme.CreateCard(36, 28, 472, 320);

            var lblTitle = new Label
            {
                Text = "Nouvelle partie   ------------ ETU003350------------",
                Font = UiTheme.TitleFont(18f),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 18),
                BackColor = Color.Transparent
            };

            var lblHint = new Label
            {
                Text = "Définis la taille de la grille et les couleurs des joueurs.",
                Font = UiTheme.BodyFont(9.5f),
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 50),
                BackColor = Color.Transparent
            };

            Label lblWidth = UiTheme.CreateLabel("Largeur de la grille", 24, 92);
            nudWidth = new NumericUpDown { Location = new Point(262, 88), Minimum = 5, Maximum = 50, Value = 15, Width = 160 };
            nudWidth.Font = UiTheme.BodyFont(10f);
            nudWidth.BackColor = UiTheme.GridBackground;

            Label lblHeight = UiTheme.CreateLabel("Hauteur de la grille", 24, 132);
            nudHeight = new NumericUpDown { Location = new Point(262, 128), Minimum = 5, Maximum = 50, Value = 15, Width = 160 };
            nudHeight.Font = UiTheme.BodyFont(10f);
            nudHeight.BackColor = UiTheme.GridBackground;

            Label lblP1 = UiTheme.CreateLabel("Couleur Joueur 1", 24, 176);
            btnColor1 = new Button { Location = new Point(262, 168), BackColor = color1, Width = 160, Height = 32, Text = "Choisir" };
            UiTheme.StyleSecondaryButton(btnColor1);
            btnColor1.BackColor = color1;
            btnColor1.ForeColor = Color.White;
            btnColor1.Click += (s, e) => { color1 = ChooseColor(color1); btnColor1.BackColor = color1; };

            Label lblP2 = UiTheme.CreateLabel("Couleur Joueur 2", 24, 220);
            btnColor2 = new Button { Location = new Point(262, 212), BackColor = color2, Width = 160, Height = 32, Text = "Choisir" };
            UiTheme.StyleSecondaryButton(btnColor2);
            btnColor2.BackColor = color2;
            btnColor2.ForeColor = Color.White;
            btnColor2.Click += (s, e) => { color2 = ChooseColor(color2); btnColor2.BackColor = color2; };

            Button btnStart = new Button { Text = "Démarrer", Location = new Point(262, 266), Size = new Size(160, 38) };
            UiTheme.StylePrimaryButton(btnStart);
            btnStart.Click += BtnStart_Click;

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblHint);
            card.Controls.Add(lblWidth); card.Controls.Add(nudWidth);
            card.Controls.Add(lblHeight); card.Controls.Add(nudHeight);
            card.Controls.Add(lblP1); card.Controls.Add(btnColor1);
            card.Controls.Add(lblP2); card.Controls.Add(btnColor2);
            card.Controls.Add(btnStart);

            this.Controls.Add(card);
        }

        private Color ChooseColor(Color current)
        {
            using (ColorDialog cd = new ColorDialog { Color = current })
            {
                if (cd.ShowDialog() == DialogResult.OK) return cd.Color;
                return current;
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            var game = new Game
            {
                Id = 0,
                GridWidth = (int)nudWidth.Value,
                GridHeight = (int)nudHeight.Value,
                Player1Color = ColorTranslator.ToHtml(color1),
                Player2Color = ColorTranslator.ToHtml(color2),
                CurrentTurn = 1,
                Player1Score = 0,
                Player2Score = 0,
                Status = "InProgress"
            };

            var gameForm = new GameForm(game, dbService);
            this.Hide();
            gameForm.ShowDialog();
            this.Close();
        }
    }
}
