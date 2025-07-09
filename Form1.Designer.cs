namespace HexWargame;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        hexGridControl = new UI.HexGridControl();
        statusPanel = new Panel();
        gameInfoLabel = new Label();
        currentTeamLabel = new Label();
        mapNameLabel = new Label();
        selectedUnitLabel = new Label();
        unitStatsLabel = new Label();
        damageLogPanel = new Panel();
        damageLogLabel = new Label();
        damageLogTextBox = new TextBox();
        buttonPanel = new Panel();
        newGameButton = new Button();
        newMapButton = new Button();
        endTurnButton = new Button();
        helpButton = new Button();
        SuspendLayout();
        // 
        // hexGridControl
        // 
        hexGridControl.BackColor = Color.White;
        hexGridControl.Dock = DockStyle.Fill;
        hexGridControl.Location = new Point(0, 0);
        hexGridControl.Name = "hexGridControl";
        hexGridControl.Size = new Size(600, 400);
        hexGridControl.TabIndex = 0;
        // 
        // statusPanel
        // 
        statusPanel.Controls.Add(buttonPanel);
        statusPanel.Controls.Add(unitStatsLabel);
        statusPanel.Controls.Add(selectedUnitLabel);
        statusPanel.Controls.Add(mapNameLabel);
        statusPanel.Controls.Add(currentTeamLabel);
        statusPanel.Controls.Add(gameInfoLabel);
        statusPanel.Dock = DockStyle.Right;
        statusPanel.Location = new Point(600, 0);
        statusPanel.Name = "statusPanel";
        statusPanel.Size = new Size(200, 400);
        statusPanel.TabIndex = 1;
        statusPanel.BackColor = Color.LightGray;
        // 
        // gameInfoLabel
        // 
        gameInfoLabel.Font = new Font("Arial", 12F, FontStyle.Bold);
        gameInfoLabel.Location = new Point(10, 10);
        gameInfoLabel.Name = "gameInfoLabel";
        gameInfoLabel.Size = new Size(180, 30);
        gameInfoLabel.TabIndex = 0;
        gameInfoLabel.Text = "Hex Wargame";
        // 
        // currentTeamLabel
        // 
        currentTeamLabel.Font = new Font("Arial", 10F);
        currentTeamLabel.Location = new Point(10, 50);
        currentTeamLabel.Name = "currentTeamLabel";
        currentTeamLabel.Size = new Size(180, 25);
        currentTeamLabel.TabIndex = 1;
        currentTeamLabel.Text = "Current Team: Red";
        // 
        // mapNameLabel
        // 
        mapNameLabel.Font = new Font("Arial", 9F, FontStyle.Italic);
        mapNameLabel.Location = new Point(10, 80);
        mapNameLabel.Name = "mapNameLabel";
        mapNameLabel.Size = new Size(180, 20);
        mapNameLabel.TabIndex = 2;
        mapNameLabel.Text = "Map: Small Town";
        // 
        // selectedUnitLabel
        // 
        selectedUnitLabel.Font = new Font("Arial", 9F);
        selectedUnitLabel.Location = new Point(10, 105);
        selectedUnitLabel.Name = "selectedUnitLabel";
        selectedUnitLabel.Size = new Size(180, 25);
        selectedUnitLabel.TabIndex = 3;
        selectedUnitLabel.Text = "Selected: None";
        // 
        // unitStatsLabel
        // 
        unitStatsLabel.Font = new Font("Arial", 8F);
        unitStatsLabel.Location = new Point(10, 135);
        unitStatsLabel.Name = "unitStatsLabel";
        unitStatsLabel.Size = new Size(180, 100);
        unitStatsLabel.TabIndex = 4;
        unitStatsLabel.Text = "";
        // 
        // damageLogPanel
        // 
        damageLogPanel.Controls.Add(damageLogTextBox);
        damageLogPanel.Controls.Add(damageLogLabel);
        damageLogPanel.Dock = DockStyle.Bottom;
        damageLogPanel.Location = new Point(0, 400);
        damageLogPanel.Name = "damageLogPanel";
        damageLogPanel.Size = new Size(800, 120);
        damageLogPanel.TabIndex = 5;
        damageLogPanel.BackColor = Color.DarkGray;
        // 
        // damageLogLabel
        // 
        damageLogLabel.Font = new Font("Arial", 10F, FontStyle.Bold);
        damageLogLabel.ForeColor = Color.White;
        damageLogLabel.Location = new Point(10, 5);
        damageLogLabel.Name = "damageLogLabel";
        damageLogLabel.Size = new Size(200, 25);
        damageLogLabel.TabIndex = 0;
        damageLogLabel.Text = "Combat Log";
        // 
        // damageLogTextBox
        // 
        damageLogTextBox.BackColor = Color.Black;
        damageLogTextBox.ForeColor = Color.Lime;
        damageLogTextBox.Font = new Font("Consolas", 9F);
        damageLogTextBox.Location = new Point(10, 30);
        damageLogTextBox.Multiline = true;
        damageLogTextBox.Name = "damageLogTextBox";
        damageLogTextBox.ReadOnly = true;
        damageLogTextBox.ScrollBars = ScrollBars.Vertical;
        damageLogTextBox.Size = new Size(780, 80);
        damageLogTextBox.TabIndex = 1;
        damageLogTextBox.TabStop = false;
        damageLogTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        // 
        // buttonPanel
        // 
        buttonPanel.Controls.Add(helpButton);
        buttonPanel.Controls.Add(endTurnButton);
        buttonPanel.Controls.Add(newMapButton);
        buttonPanel.Controls.Add(newGameButton);
        buttonPanel.Dock = DockStyle.None;
        buttonPanel.Location = new Point(10, 250);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Size = new Size(180, 100);
        buttonPanel.TabIndex = 6;
        buttonPanel.BackColor = Color.LightGray;
        // 
        // newGameButton
        // 
        newGameButton.Location = new Point(10, 10);
        newGameButton.Name = "newGameButton";
        newGameButton.Size = new Size(80, 30);
        newGameButton.TabIndex = 0;
        newGameButton.Text = "New Game";
        newGameButton.UseVisualStyleBackColor = true;
        // 
        // newMapButton
        // 
        newMapButton.Location = new Point(100, 10);
        newMapButton.Name = "newMapButton";
        newMapButton.Size = new Size(80, 30);
        newMapButton.TabIndex = 1;
        newMapButton.Text = "New Map";
        newMapButton.UseVisualStyleBackColor = true;
        // 
        // endTurnButton
        // 
        endTurnButton.Location = new Point(10, 50);
        endTurnButton.Name = "endTurnButton";
        endTurnButton.Size = new Size(80, 30);
        endTurnButton.TabIndex = 2;
        endTurnButton.Text = "End Turn";
        endTurnButton.UseVisualStyleBackColor = true;
        // 
        // helpButton
        // 
        helpButton.Location = new Point(100, 50);
        helpButton.Name = "helpButton";
        helpButton.Size = new Size(80, 30);
        helpButton.TabIndex = 3;
        helpButton.Text = "Help";
        helpButton.UseVisualStyleBackColor = true;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 520);
        Controls.Add(hexGridControl);
        Controls.Add(statusPanel);
        Controls.Add(damageLogPanel);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        Text = "Hex Wargame - Two Squads Battle";
        ResumeLayout(false);
    }

    #endregion

    private UI.HexGridControl hexGridControl;
    private Panel statusPanel;
    private Label gameInfoLabel;
    private Label currentTeamLabel;
    private Label mapNameLabel;
    private Label selectedUnitLabel;
    private Label unitStatsLabel;
    private Panel damageLogPanel;
    private Label damageLogLabel;
    private TextBox damageLogTextBox;
    private Panel buttonPanel;
    private Button newGameButton;
    private Button newMapButton;
    private Button endTurnButton;
    private Button helpButton;
}