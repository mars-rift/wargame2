using HexWargame.Core;
using HexWargame.UI;

namespace HexWargame;

public partial class MainForm : Form
{
    private Game _game = null!;
    private bool _aiTurnInProgress;

    public MainForm()
    {
        InitializeComponent();
        InitializeGame();
        SetupEventHandlers();
    }

    private void InitializeGame()
    {
        _game = new Game();
        hexGridControl.Game = _game;
        hexGridControl.CenterView();
        UpdateUI();
    }

    private void SetupEventHandlers()
    {
        // Game events
        _game.TeamChanged += OnTeamChanged;
        _game.AttackExecuted += OnAttackExecuted;
        _game.GameEnded += OnGameEnded;

        // UI events
        hexGridControl.UnitSelected += OnUnitSelected;
        hexGridControl.HexClicked += OnHexClicked;

        // Button events
        newGameButton.Click += OnNewGameClicked;
        newMapButton.Click += OnNewMapClicked;
        endTurnButton.Click += OnEndTurnClicked;
        helpButton.Click += OnHelpClicked;
    }

    private void UpdateUI()
    {
        var stats = _game.GetGameStats();
        
        gameInfoLabel.Text = $"Turn {stats["TurnNumber"]}";
        currentTeamLabel.Text = $"Current Team: {stats["CurrentTeam"]}";
        mapNameLabel.Text = $"Map: {stats["MapName"]}";
        
        if (hexGridControl.SelectedUnit != null)
        {
            var unit = hexGridControl.SelectedUnit;
            selectedUnitLabel.Text = $"Selected: {unit.UnitType}";
            unitStatsLabel.Text = $"HP: {unit.CurrentHp}/{unit.MaxHp}\n" +
                                 $"Movement: {unit.Movement}\n" +
                                 $"Range: {unit.AttackRange}\n" +
                                 $"Power: {unit.AttackPower}\n" +
                                 $"Can Move: {(unit.CanMove ? "Yes" : "No")}\n" +
                                 $"Can Attack: {(unit.CanAttack ? "Yes" : "No")}";
        }
        else
        {
            selectedUnitLabel.Text = "Selected: None";
            unitStatsLabel.Text = "";
        }

        // Update button states
        endTurnButton.Enabled = !_game.GameOver && !_aiTurnInProgress;
        
        // Change team label color
        currentTeamLabel.ForeColor = _game.CurrentTeam == Team.Red ? Color.Red : Color.Blue;
    }

    private async void OnTeamChanged(object? sender, Team newTeam)
    {
        UpdateUI();
        
        // If it's AI's turn, execute AI moves
        if (newTeam == Team.Blue && !_game.GameOver)
        {
            _aiTurnInProgress = true;
            endTurnButton.Enabled = false;
            
            await Task.Delay(1000); // Brief pause before AI starts
            await _game.ExecuteAITurn();
            
            _aiTurnInProgress = false;
            UpdateUI();
        }
    }

    private void OnAttackExecuted(object? sender, AttackResult result)
    {
        if (result.Hit)
        {
            var message = $"Hit! {result.Damage} damage dealt.";
            if (result.TargetKilled)
                message += " Target eliminated!";
            
            ShowMessage(message, "Attack Result");
        }
        else
        {
            ShowMessage("Attack missed!", "Attack Result");
        }
        
        UpdateUI();
    }

    private void OnGameEnded(object? sender, EventArgs e)
    {
        UpdateUI();
        
        // Show game over screen with detailed statistics
        var gameStats = _game.GetGameStats();
        var winner = _game.Winner ?? Team.Red; // Fallback, should never be null here
        
        using var gameOverForm = new GameOverForm(gameStats, winner);
        var result = gameOverForm.ShowDialog(this);
        
        if (result == DialogResult.OK)
        {
            var action = gameOverForm.Tag?.ToString();
            switch (action)
            {
                case "NewGame":
                    _game.Reset();
                    hexGridControl.ClearSelection();
                    hexGridControl.CenterView();
                    UpdateUI();
                    break;
                    
                case "NewMap":
                    _game.Reset(); // This generates a new random map
                    hexGridControl.ClearSelection();
                    hexGridControl.CenterView();
                    UpdateUI();
                    break;
            }
        }
        else
        {
            // User chose to exit
            Application.Exit();
        }
    }

    private void OnUnitSelected(object? sender, Unit unit)
    {
        UpdateUI();
    }

    private void OnHexClicked(object? sender, HexCoord coord)
    {
        // This could be used for additional hex click handling
    }

    private void OnNewGameClicked(object? sender, EventArgs e)
    {
        var result = MessageBox.Show("Start a new game?", "New Game", 
                                   MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        
        if (result == DialogResult.Yes)
        {
            _game.Reset();
            hexGridControl.ClearSelection();
            UpdateUI();
        }
    }

    private void OnNewMapClicked(object? sender, EventArgs e)
    {
        var result = MessageBox.Show("Generate a new random map? Current game will be reset.", "New Map", 
                                   MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        
        if (result == DialogResult.Yes)
        {
            _game.Reset();
            hexGridControl.ClearSelection();
            hexGridControl.CenterView();
            UpdateUI();
        }
    }

    private void OnEndTurnClicked(object? sender, EventArgs e)
    {
        if (!_game.GameOver && !_aiTurnInProgress)
        {
            hexGridControl.ClearSelection();
            _game.NextTurn();
        }
    }

    private void OnHelpClicked(object? sender, EventArgs e)
    {
        var helpText = @"HEX WARGAME - HELP

OBJECTIVE:
Eliminate all enemy units to win!

UNIT TYPES:
• Infantry (I): Balanced unit, 3 movement, 2 range
• Sniper (S): Long range specialist, 2 movement, 4 range  
• Heavy (H): Tank unit, 2 movement, 1 range, high HP
• Medic (M): Support unit, 3 movement, 1 range

TERRAIN:
• Green: Open ground (no bonus)
• Gray: Buildings (+20 defense)
• Brown: Cover (+15 defense)
• Blue: Water (impassable)

HOW TO PLAY:
1. Click a unit to select it
2. Click a highlighted hex to move
3. Click an enemy unit to attack
4. Each unit can move and attack once per turn
5. Click 'End Turn' when finished

CONTROLS:
• Mouse: Select units and targets
• New: Start a new game
• End Turn: End your turn
• Help: Show this help";

        MessageBox.Show(helpText, "Help - Hex Wargame", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowMessage(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
