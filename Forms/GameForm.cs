using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using PointGame.Models;
using PointGame.Services;

namespace PointGame.Forms
{
    public class GameForm : Form
    {
        private Game game;
        private DatabaseService dbService;
        private GameLogic logic;
        private List<Move> moves;
        private Panel gridPanel;
        
        private int cellSize = 30;
        private int pointRadius = 8;
        private int cannonMargin = 40;
        private int verticalGridMargin = 20;
        
        private List<WinResult> winLines = new List<WinResult>();

        // Cannon state (row index 0..GridHeight)
        private int cannon1Y;
        private int cannon2Y;

        // Shooting state
        private enum ActionMode { PlacePoint, Shoot }
        private ActionMode currentMode = ActionMode.PlacePoint;
        private int shotPower = 0; // 1-9
        private Label lblStatus;
        private Button btnToggleMode;
        private Button btnFire;
        private Button btnSave;

        // Ball animation — moves purely horizontally
        private System.Windows.Forms.Timer ballTimer;
        private float ballX, ballY;
        private float ballDx;          // horizontal speed (+right, -left)
        private float ballDistTraveled;
        private float ballMaxDist;
        private float ballTargetX;
        private int shotTargetGx = -1;
        private bool ballFlying = false;
        private int ballRow;           // grid row the ball travels on

        private readonly List<DestroyedPointMemory> destroyedPointMemories;

        public GameForm(Game game, DatabaseService dbService)
        {
            this.game = game;
            this.dbService = dbService;
            this.logic = new GameLogic(game.GridWidth, game.GridHeight);

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            this.UpdateStyles();
            
            if (game.Id > 0)
            {
                this.moves = dbService.GetMoves(game.Id);
                this.destroyedPointMemories = dbService.GetDestroyedPointMemories(game.Id);
            }
            else
            {
                this.moves = new List<Move>();
                this.destroyedPointMemories = new List<DestroyedPointMemory>();
            }
            
            winLines = this.logic.ReplayAndGetWinLines(moves);

            cannon1Y = game.GridHeight / 2;
            cannon2Y = game.GridHeight / 2;

            this.KeyPreview = true;
            this.KeyDown += GameForm_KeyDown;
            this.BackColor = UiTheme.AppBackground;

            int panelWidth = cannonMargin + Math.Max(0, game.GridWidth - 1) * cellSize + cannonMargin;
            int panelHeight = verticalGridMargin * 2 + Math.Max(0, game.GridHeight - 1) * cellSize;

            UpdateFormTitle();
            this.Size = new Size(Math.Max(760, panelWidth + 80), panelHeight + 220);
            this.StartPosition = FormStartPosition.CenterScreen;

            var hudCard = UiTheme.CreateCard(20, 18, Math.Max(700, panelWidth + 20), 64);

            var lblGameHint = new Label
            {
                Text = "Flèches Haut/Bas : déplacer le canon • Ctrl+1..9 : puissance",
                AutoSize = true,
                Location = new Point(16, 10),
                ForeColor = UiTheme.TextSecondary,
                Font = UiTheme.BodyFont(9.2f),
                BackColor = Color.Transparent
            };

            var lblPlayers = new Label
            {
                Text = "  J1 à gauche • J2 à droite  -----------------ETU003350------------------",
                AutoSize = true,
                Location = new Point(16, 34),
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.BodyBoldFont(9.8f),
                BackColor = Color.Transparent
            };

            hudCard.Controls.Add(lblGameHint);
            hudCard.Controls.Add(lblPlayers);

            gridPanel = new Panel
            {
                Location = new Point(20, 98),
                Size = new Size(panelWidth + 1, panelHeight + 1),
                BackColor = UiTheme.GridBackground
            };
            EnableDoubleBuffer(gridPanel);
            gridPanel.Paint += GridPanel_Paint;
            // In shoot mode, clicking the cannon areas repositions them
            gridPanel.MouseClick += GridPanel_MouseClick;

            int bottomY = panelHeight + 116;

            btnSave = new Button { Text = "Sauvegarder", Location = new Point(20, bottomY), Size = new Size(120, 36) };
            UiTheme.StyleSecondaryButton(btnSave);
            btnSave.Click += BtnSave_Click;

            btnToggleMode = new Button { Text = "Mode Tir", Location = new Point(150, bottomY), Size = new Size(120, 36) };
            UiTheme.StylePrimaryButton(btnToggleMode);
            btnToggleMode.Click += (s, e) => {
                if (ballFlying) return;
                if (currentMode == ActionMode.PlacePoint) {
                    currentMode = ActionMode.Shoot;
                    btnToggleMode.Text = "Mode Pose";
                    shotPower = 0;
                } else {
                    currentMode = ActionMode.PlacePoint;
                    btnToggleMode.Text = "Mode Tir";
                    shotPower = 0;
                }
                UpdateStatus();
                gridPanel.Invalidate();
            };

            // Fire button: enabled when in shoot mode and power is selected
            btnFire = new Button { Text = "Tirer", Location = new Point(280, bottomY), Size = new Size(90, 36), Enabled = false };
            UiTheme.StylePrimaryButton(btnFire);
            btnFire.Click += (s, e) => {
                if (ballFlying) return;
                if (currentMode == ActionMode.Shoot && shotPower > 0)
                    FireBall();
            };

            lblStatus = new Label
            {
                Location = new Point(390, bottomY + 8),
                AutoSize = true,
                Font = UiTheme.BodyFont(9.8f),
                ForeColor = UiTheme.TextPrimary,
                BackColor = UiTheme.AppBackground
            };
            UpdateStatus();

            this.Controls.Add(hudCard);
            this.Controls.Add(gridPanel);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnToggleMode);
            this.Controls.Add(btnFire);
            this.Controls.Add(lblStatus);

            ballTimer = new System.Windows.Forms.Timer { Interval = 30 };
            ballTimer.Tick += BallTimer_Tick;
        }

        private static void EnableDoubleBuffer(Control control)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(control, true, null);
        }

        private int IntersectionPxX(int gx) => cannonMargin + gx * cellSize;
        private int IntersectionPxY(int gy) => verticalGridMargin + gy * cellSize;
        private int GetTargetColumnOneBased(int power)
        {
            int colFromLeft = (power * game.GridWidth) / 9;
            colFromLeft = Math.Max(1, Math.Min(colFromLeft, game.GridWidth));

            if (game.CurrentTurn == 1)
                return colFromLeft;

            return game.GridWidth - colFromLeft + 1;
        }

        private void UpdateStatus()
        {
            int cannonRow = game.CurrentTurn == 1 ? cannon1Y : cannon2Y;
            if (currentMode == ActionMode.PlacePoint)
                lblStatus.Text = $"Mode : Pose de point • Tour : Joueur {game.CurrentTurn}";
            else
            {
                string pwr = shotPower > 0 ? shotPower.ToString() : "aucune (Ctrl+1-9)";
                string target = shotPower > 0 ? GetTargetColumnOneBased(shotPower).ToString() : "-";
                lblStatus.Text = $"Mode : Tir • Ligne canon : {cannonRow + 1} • Puissance : {pwr} • Colonne cible : {target}";
            }

            btnFire.Enabled = (currentMode == ActionMode.Shoot && shotPower > 0 && !ballFlying);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (game.Id == 0)
                    game.Id = dbService.CreateGame(game);
                dbService.SaveMovesBulk(game.Id, moves);
                dbService.SaveDestroyedPointMemoriesBulk(game.Id, destroyedPointMemories);
                dbService.UpdateGameState(game.Id, game.CurrentTurn, game.Player1Score, game.Player2Score);
                UpdateFormTitle();
                MessageBox.Show("Game saved successfully!", "Save");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving game: " + ex.Message);
            }
        }

        private void UpdateFormTitle()
        {
            this.Text = $"Game {(game.Id == 0 ? "(Unsaved)" : "#" + game.Id)} | Turn: P{game.CurrentTurn} | P1: {game.Player1Score} pts | P2: {game.Player2Score} pts";
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (HandleGameKey(keyData))
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private bool HandleGameKey(Keys keyData)
        {
            if (ballFlying)
                return false;

            Keys keyCode = keyData & Keys.KeyCode;
            bool ctrlPressed = (keyData & Keys.Control) == Keys.Control;

            if (keyCode == Keys.Up)
            {
                if (game.CurrentTurn == 1) { if (cannon1Y > 0) cannon1Y--; }
                else { if (cannon2Y > 0) cannon2Y--; }

                gridPanel.Invalidate();
                UpdateStatus();
                return true;
            }

            if (keyCode == Keys.Down)
            {
                if (game.CurrentTurn == 1) { if (cannon1Y < game.GridHeight - 1) cannon1Y++; }
                else { if (cannon2Y < game.GridHeight - 1) cannon2Y++; }

                gridPanel.Invalidate();
                UpdateStatus();
                return true;
            }

            if (currentMode == ActionMode.Shoot && ctrlPressed)
            {
                int num = -1;
                if (keyCode >= Keys.D1 && keyCode <= Keys.D9) num = keyCode - Keys.D0;
                if (keyCode >= Keys.NumPad1 && keyCode <= Keys.NumPad9) num = keyCode - Keys.NumPad0;
                if (num >= 1 && num <= 9)
                {
                    shotPower = num;
                    UpdateStatus();
                    return true;
                }
            }

            return false;
        }

        private void GameForm_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = HandleGameKey(e.KeyData);
        }

        private void FireBall()
        {
            int cannonRow = game.CurrentTurn == 1 ? cannon1Y : cannon2Y;
            ballRow = cannonRow;
            ballY = IntersectionPxY(cannonRow);

            int targetColumnOneBased = GetTargetColumnOneBased(shotPower);
            shotTargetGx = targetColumnOneBased - 1;
            ballTargetX = IntersectionPxX(shotTargetGx);

            if (game.CurrentTurn == 1)
            {
                // P1: fires from left → right
                ballX = cannonMargin - 10;
                ballDx = 6f;
            }
            else
            {
                // P2: fires from right → left
                ballX = cannonMargin + Math.Max(0, game.GridWidth - 1) * cellSize + 10;
                ballDx = -6f;
            }

            ballMaxDist = Math.Abs(ballTargetX - ballX);
            ballDistTraveled = 0;

            ballFlying = true;
            ballTimer.Start();
            UpdateStatus();
        }

        private void BallTimer_Tick(object sender, EventArgs e)
        {
            ballX += ballDx;
            ballDistTraveled += Math.Abs(ballDx);

            bool reachedTarget = (ballDx > 0 && ballX >= ballTargetX) ||
                                 (ballDx < 0 && ballX <= ballTargetX);
            if (reachedTarget)
            {
                ballX = ballTargetX;

                bool hit = false;
                if (shotTargetGx >= 0)
                {
                    hit = TryRestoreDestroyedPoint(shotTargetGx, ballRow, game.CurrentTurn);
                    if (!hit)
                        hit = TryRemovePointByCannon(shotTargetGx, ballRow, game.CurrentTurn);
                }

                EndShot(hit);
                return;
            }

            // Out of grid bounds
            if (ballX < cannonMargin - 20 || ballX > cannonMargin + Math.Max(0, game.GridWidth - 1) * cellSize + 20)
            {
                EndShot(false);
                return;
            }

            gridPanel.Invalidate();
        }

        private bool TryRemovePointByCannon(int x, int y, int shooterPlayer)
        {
            var targetMove = moves.LastOrDefault(m => m.X == x && m.Y == y);
            if (targetMove == null)
                return false;

            if (!logic.RemovePoint(x, y, shooterPlayer))
                return false;

            destroyedPointMemories.Add(new DestroyedPointMemory
            {
                X = x,
                Y = y,
                PlayerNumber = targetMove.PlayerNumber
            });

            moves.RemoveAll(m => m.X == x && m.Y == y);
            for (int i = 0; i < moves.Count; i++) moves[i].MoveOrder = i + 1;
            return true;
        }

        private bool TryRestoreDestroyedPoint(int x, int y, int shooterPlayer)
        {
            var memory = destroyedPointMemories.LastOrDefault(m => m.PlayerNumber == shooterPlayer && m.X == x && m.Y == y);
            if (memory == null)
                return false;

            if (logic.IsPointInLine(x, y))
                return false;

            var occupant = moves.LastOrDefault(m => m.X == x && m.Y == y);
            if (occupant != null)
            {
                if (occupant.PlayerNumber == shooterPlayer)
                    return false;

                if (!TryRemovePointByCannon(x, y, shooterPlayer))
                    return false;
            }

            if (!logic.IsValidMove(x, y))
                return false;

            var restoredMove = new Move
            {
                GameId = game.Id,
                X = x,
                Y = y,
                PlayerNumber = shooterPlayer,
                MoveOrder = moves.Count + 1
            };

            moves.Add(restoredMove);
            var wins = logic.PlaceMove(x, y, shooterPlayer);
            if (wins.Count > 0)
            {
                winLines.AddRange(wins);
                if (shooterPlayer == 1) game.Player1Score += wins.Count;
                else game.Player2Score += wins.Count;
            }

            destroyedPointMemories.Remove(memory);
            return true;
        }

        private void EndShot(bool hit)
        {
            ballTimer.Stop();
            ballFlying = false;
            shotPower = 0;

            // If hit an opponent point → shooter keeps turn; otherwise pass
            // if (!hit)
                game.CurrentTurn = game.CurrentTurn == 1 ? 2 : 1;
            
            UpdateFormTitle();
            UpdateStatus();
            gridPanel.Invalidate();
        }

        private void GridPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (ballFlying) return;

            int gx = (int)Math.Round((double)(e.X - cannonMargin) / cellSize);
            int gy = (int)Math.Round((double)(e.Y - verticalGridMargin) / cellSize);
            gy = Math.Max(0, Math.Min(gy, game.GridHeight - 1));

            if (currentMode == ActionMode.PlacePoint)
            {
                if (gx >= 0 && logic.IsValidMove(gx, gy))
                {
                    var move = new Move
                    {
                        GameId = game.Id,
                        X = gx,
                        Y = gy,
                        PlayerNumber = game.CurrentTurn,
                        MoveOrder = moves.Count + 1
                    };

                    moves.Add(move);
                    var wins = logic.PlaceMove(gx, gy, game.CurrentTurn);
                    
                    if (wins.Count > 0)
                    {
                        winLines.AddRange(wins);
                        if (game.CurrentTurn == 1) game.Player1Score += wins.Count;
                        else game.Player2Score += wins.Count;
                        // Keep turn on score
                    }
                    // else
                    // {
                        game.CurrentTurn = game.CurrentTurn == 1 ? 2 : 1;
                    // }
                    
                    UpdateFormTitle();
                    UpdateStatus();
                    gridPanel.Invalidate();
                }
            }
            else if (currentMode == ActionMode.Shoot)
            {
                // Click on cannon area to reposition it
                int snappedY = gy;
                if (game.CurrentTurn == 1 && e.X < cannonMargin)
                {
                    cannon1Y = snappedY;
                    UpdateStatus();
                    gridPanel.Invalidate();
                }
                else if (game.CurrentTurn == 2 && e.X > cannonMargin + Math.Max(0, game.GridWidth - 1) * cellSize)
                {
                    cannon2Y = snappedY;
                    UpdateStatus();
                    gridPanel.Invalidate();
                }
            }
        }

        private void GridPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int left = IntersectionPxX(0);
            int top = IntersectionPxY(0);
            int right = IntersectionPxX(game.GridWidth - 1);
            int bottom = IntersectionPxY(game.GridHeight - 1);

            using (var frameBrush = new SolidBrush(Color.FromArgb(236, 236, 236)))
            using (var framePen = new Pen(Color.FromArgb(152, 152, 152), 1.4f))
            {
                g.FillRectangle(frameBrush, left - pointRadius, top - pointRadius, (right - left) + pointRadius * 2, (bottom - top) + pointRadius * 2);
                g.DrawRectangle(framePen, left - pointRadius, top - pointRadius, (right - left) + pointRadius * 2, (bottom - top) + pointRadius * 2);
            }

            using (var gridPen = new Pen(Color.FromArgb(168, 168, 168), 1f))
            {
                for (int i = 0; i < game.GridWidth; i++)
                    g.DrawLine(gridPen, IntersectionPxX(i), top, IntersectionPxX(i), bottom);
                for (int j = 0; j < game.GridHeight; j++)
                    g.DrawLine(gridPen, left, IntersectionPxY(j), right, IntersectionPxY(j));
            }

            using (var nodeBrush = new SolidBrush(Color.FromArgb(126, 126, 126)))
            {
                for (int i = 0; i < game.GridWidth; i++)
                {
                    for (int j = 0; j < game.GridHeight; j++)
                    {
                        int px = IntersectionPxX(i);
                        int py = IntersectionPxY(j);
                        g.FillEllipse(nodeBrush, px - 2, py - 2, 4, 4);
                    }
                }
            }

            using (var lblBrush = new SolidBrush(Color.FromArgb(58, 58, 58)))
            using (var lblFont = UiTheme.BodyBoldFont(8.8f))
            using (var centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                for (int col = 0; col < game.GridWidth; col++)
                {
                    int x = IntersectionPxX(col);
                    g.DrawString((col + 1).ToString(), lblFont, lblBrush, new RectangleF(x - 10, 2, 20, 14), centerFmt);
                    g.DrawString((game.GridWidth - col).ToString(), lblFont, lblBrush, new RectangleF(x - 10, bottom + 6, 20, 14), centerFmt);
                }

                for (int row = 0; row < game.GridHeight; row++)
                {
                    int y = IntersectionPxY(row);
                    g.DrawString((row + 1).ToString(), lblFont, lblBrush, new RectangleF(left - 30, y - 8, 22, 16), centerFmt);
                    g.DrawString((game.GridHeight - row).ToString(), lblFont, lblBrush, new RectangleF(right + 8, y - 8, 22, 16), centerFmt);
                }
            }

            DrawCannon(g, 1, cannon1Y, ColorTranslator.FromHtml(game.Player1Color));
            DrawCannon(g, 2, cannon2Y, ColorTranslator.FromHtml(game.Player2Color));

            Color c1 = ColorTranslator.FromHtml(game.Player1Color);
            Color c2 = ColorTranslator.FromHtml(game.Player2Color);

            // Highlight cannon row when in shoot mode
            if (currentMode == ActionMode.Shoot && !ballFlying)
            {
                int activeRow = game.CurrentTurn == 1 ? cannon1Y : cannon2Y;
                int ry = IntersectionPxY(activeRow);
                using var rowBrush = new SolidBrush(Color.FromArgb(48, UiTheme.Accent));
                g.FillRectangle(rowBrush, IntersectionPxX(0), ry - pointRadius, game.GridWidth * cellSize, pointRadius * 2);
            }

            // Points
            foreach (var m in moves)
            {
                Color color = m.PlayerNumber == 1 ? c1 : c2;
                using var brush = new SolidBrush(color);
                using var ringPen = new Pen(Color.FromArgb(34, 40, 58), 1f);
                int px = IntersectionPxX(m.X);
                int py = IntersectionPxY(m.Y);
                g.FillEllipse(brush, px - pointRadius, py - pointRadius, pointRadius * 2, pointRadius * 2);
                g.DrawEllipse(ringPen, px - pointRadius, py - pointRadius, pointRadius * 2, pointRadius * 2);
            }

            // Win lines
            foreach (var win in winLines)
            {
                Color color = win.Player == 1 ? c1 : c2;
                using var pen = new Pen(color, 4);
                g.DrawLine(pen, IntersectionPxX(win.StartWinPoint.X), IntersectionPxY(win.StartWinPoint.Y),
                                IntersectionPxX(win.EndWinPoint.X), IntersectionPxY(win.EndWinPoint.Y));
            }

            // Ball
            if (ballFlying)
            {
                using var ballBrush = new SolidBrush(Color.FromArgb(30, 35, 48));
                using var ballPen = new Pen(Color.FromArgb(225, 230, 245), 1f);
                g.FillEllipse(ballBrush, ballX - 5, ballY - 5, 10, 10);
                g.DrawEllipse(ballPen, ballX - 5, ballY - 5, 10, 10);
            }
        }

        private void DrawCannon(Graphics g, int player, int cannonRow, Color color)
        {
            int cannonW = 34;
            int cannonH = 18;
            int cx, cy;

            cy = IntersectionPxY(cannonRow) - cannonH / 2;
            using var cannonBrush = new SolidBrush(color);
            using var darkPen = new Pen(Color.FromArgb(36, 43, 63), 1.4f);
            using var wheelBrush = new SolidBrush(Color.FromArgb(65, 74, 98));

            if (player == 1)
            {
                cx = 4;

                var bodyRect = new Rectangle(cx, cy, cannonW, cannonH);
                var barrelRect = new Rectangle(cx + cannonW - 2, cy + cannonH / 2 - 3, 14, 6);
                var wheelBack = new Rectangle(cx + 3, cy + cannonH - 1, 8, 8);
                var wheelFront = new Rectangle(cx + cannonW - 10, cy + cannonH - 1, 8, 8);

                using (var bodyPath = CreateRoundedRectPath(bodyRect, 4))
                {
                    g.FillPath(cannonBrush, bodyPath);
                    g.DrawPath(darkPen, bodyPath);
                }

                using (var barrelPath = CreateRoundedRectPath(barrelRect, 3))
                {
                    g.FillPath(cannonBrush, barrelPath);
                    g.DrawPath(darkPen, barrelPath);
                }

                g.FillEllipse(wheelBrush, wheelBack);
                g.FillEllipse(wheelBrush, wheelFront);
            }
            else
            {
                cx = cannonMargin + Math.Max(0, game.GridWidth - 1) * cellSize + 3;

                var bodyRect = new Rectangle(cx, cy, cannonW, cannonH);
                var barrelRect = new Rectangle(cx - 12, cy + cannonH / 2 - 3, 14, 6);
                var wheelBack = new Rectangle(cx + 3, cy + cannonH - 1, 8, 8);
                var wheelFront = new Rectangle(cx + cannonW - 10, cy + cannonH - 1, 8, 8);

                using (var bodyPath = CreateRoundedRectPath(bodyRect, 4))
                {
                    g.FillPath(cannonBrush, bodyPath);
                    g.DrawPath(darkPen, bodyPath);
                }

                using (var barrelPath = CreateRoundedRectPath(barrelRect, 3))
                {
                    g.FillPath(cannonBrush, barrelPath);
                    g.DrawPath(darkPen, barrelPath);
                }

                g.FillEllipse(wheelBrush, wheelBack);
                g.FillEllipse(wheelBrush, wheelFront);
            }

            if (game.CurrentTurn == player)
            {
                using var highlightPen = new Pen(UiTheme.Accent, 2);
                if (player == 1)
                    g.DrawRectangle(highlightPen, 2, cy - 2, cannonW + 16, cannonH + 12);
                else
                    g.DrawRectangle(highlightPen, cx - 14, cy - 2, cannonW + 16, cannonH + 12);
            }
        }

        private GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
