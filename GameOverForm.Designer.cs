namespace HexWargame.UI
{
    partial class GameOverForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            winnerLabel = new Label();
            statsTextBox = new TextBox();
            newGameButton = new Button();
            newMapButton = new Button();
            exitButton = new Button();
            buttonPanel = new Panel();
            SuspendLayout();
            // 
            // winnerLabel
            // 
            winnerLabel.Font = new Font("Arial", 16F, FontStyle.Bold);
            winnerLabel.Location = new Point(20, 20);
            winnerLabel.Name = "winnerLabel";
            winnerLabel.Size = new Size(360, 40);
            winnerLabel.TabIndex = 0;
            winnerLabel.Text = "Red Team Wins!";
            winnerLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // statsTextBox
            // 
            statsTextBox.BackColor = Color.White;
            statsTextBox.Font = new Font("Consolas", 10F);
            statsTextBox.Location = new Point(20, 70);
            statsTextBox.Multiline = true;
            statsTextBox.Name = "statsTextBox";
            statsTextBox.ReadOnly = true;
            statsTextBox.ScrollBars = ScrollBars.Vertical;
            statsTextBox.Size = new Size(360, 250);
            statsTextBox.TabIndex = 1;
            statsTextBox.TabStop = false;
            // 
            // buttonPanel
            // 
            buttonPanel.Controls.Add(exitButton);
            buttonPanel.Controls.Add(newMapButton);
            buttonPanel.Controls.Add(newGameButton);
            buttonPanel.Location = new Point(20, 330);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(360, 50);
            buttonPanel.TabIndex = 2;
            // 
            // newGameButton
            // 
            newGameButton.Location = new Point(0, 10);
            newGameButton.Name = "newGameButton";
            newGameButton.Size = new Size(110, 30);
            newGameButton.TabIndex = 0;
            newGameButton.Text = "New Game";
            newGameButton.UseVisualStyleBackColor = true;
            newGameButton.Click += NewGameButton_Click;
            // 
            // newMapButton
            // 
            newMapButton.Location = new Point(125, 10);
            newMapButton.Name = "newMapButton";
            newMapButton.Size = new Size(110, 30);
            newMapButton.TabIndex = 1;
            newMapButton.Text = "New Map";
            newMapButton.UseVisualStyleBackColor = true;
            newMapButton.Click += NewMapButton_Click;
            // 
            // exitButton
            // 
            exitButton.Location = new Point(250, 10);
            exitButton.Name = "exitButton";
            exitButton.Size = new Size(110, 30);
            exitButton.TabIndex = 2;
            exitButton.Text = "Exit Game";
            exitButton.UseVisualStyleBackColor = true;
            exitButton.Click += ExitButton_Click;
            // 
            // GameOverForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.LightGray;
            ClientSize = new Size(400, 400);
            Controls.Add(buttonPanel);
            Controls.Add(statsTextBox);
            Controls.Add(winnerLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "GameOverForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Game Over";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label winnerLabel;
        private TextBox statsTextBox;
        private Button newGameButton;
        private Button newMapButton;
        private Button exitButton;
        private Panel buttonPanel;

        private void NewGameButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Tag = "NewGame";
            Close();
        }

        private void NewMapButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Tag = "NewMap";
            Close();
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
