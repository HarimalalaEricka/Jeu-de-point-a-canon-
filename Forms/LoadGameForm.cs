using System;
using System.Drawing;
using System.Windows.Forms;
using PointGame.Models;
using PointGame.Services;

namespace PointGame.Forms
{
    public class LoadGameForm : Form
    {
        private DatabaseService dbService;
        private ListBox listGames;

        public LoadGameForm(DatabaseService db)
        {
            this.dbService = db;
            this.Text = "Charger / Supprimer";
            this.Size = new Size(640, 460);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = UiTheme.AppBackground;

            var card = UiTheme.CreateCard(28, 24, 568, 378);

            var lblTitle = new Label
            {
                Text = "Parties sauvegardées",
                Font = UiTheme.TitleFont(16f),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 18),
                BackColor = Color.Transparent
            };

            var lblHint = new Label
            {
                Text = "Sélectionne une partie pour la charger ou la supprimer.",
                Font = UiTheme.BodyFont(9.5f),
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(20, 48),
                BackColor = Color.Transparent
            };

            listGames = new ListBox { Location = new Point(20, 82), Size = new Size(526, 220) };
            listGames.BorderStyle = BorderStyle.None;
            listGames.Font = UiTheme.BodyFont(10f);
            listGames.BackColor = UiTheme.GridBackground;
            listGames.ForeColor = Color.FromArgb(25, 33, 56);
            
            LoadGamesList();

            Button btnLoad = new Button { Text = "Charger", Location = new Point(258, 320), Size = new Size(130, 36) };
            UiTheme.StylePrimaryButton(btnLoad);
            btnLoad.Click += BtnLoad_Click;
            
            Button btnDelete = new Button { Text = "Supprimer", Location = new Point(416, 320), Size = new Size(130, 36) };
            UiTheme.StyleSecondaryButton(btnDelete);
            btnDelete.BackColor = Color.FromArgb(68, 44, 52);
            btnDelete.ForeColor = Color.FromArgb(255, 205, 205);
            btnDelete.FlatAppearance.BorderColor = UiTheme.Danger;
            btnDelete.Click += BtnDelete_Click;

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblHint);
            card.Controls.Add(listGames);
            card.Controls.Add(btnLoad);
            card.Controls.Add(btnDelete);
            this.Controls.Add(card);
        }

        private void LoadGamesList()
        {
            listGames.Items.Clear();
            try
            {
                var games = dbService.GetAllGames();
                foreach (var g in games)
                {
                    listGames.Items.Add(new GameItem { Game = g, DisplayText = $"Game #{g.Id} | W:{g.GridWidth} H:{g.GridHeight} | P1: {g.Player1Score} P2: {g.Player2Score}" });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("DB Error: " + ex.Message + "\n\nPlease ensure you run the db_setup.sql script and have PostgreSQL running on localhost.");
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            if (listGames.SelectedItem is GameItem item)
            {
                var gameForm = new GameForm(item.Game, dbService);
                this.Hide();
                gameForm.ShowDialog();
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select a game to load.");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (listGames.SelectedItem is GameItem item)
            {
                var result = MessageBox.Show($"Are you sure you want to delete Game #{item.Game.Id}?", "Confirm Delete", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        dbService.DeleteGame(item.Game.Id);
                        LoadGamesList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting game: " + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a game to delete.");
            }
        }

        private class GameItem
        {
            public Game Game { get; set; }
            public string DisplayText { get; set; }
            public override string ToString() => DisplayText;
        }
    }
}
