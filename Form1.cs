using HexWargame.Core;
using HexWargame.UI;

namespace HexWargame;

public partial class MainForm : Form
{
    private Game _game = null!;
    private List<string> _damageLog = new List<string>();

    public MainForm()
    {
        InitializeComponent();
        InitializeGame();
        SetupEventHandlers();
        
        // Call CenterView after form is loaded and sized
        this.Load += MainForm_Load;
        this.Resize += MainForm_Resize;
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        // Force layout update and give time for maximizing
        this.PerformLayout();
        await Task.Delay(200);
        
        // Ensure the control is properly sized, then center the view
        hexGridControl.CenterView();
        UpdateUI(); // Make sure UI is updated after centering
        
        // Force another center view after a short delay to ensure full sizing
        await Task.Delay(100);
        hexGridControl.CenterView();
    }

    private async void MainForm_Resize(object? sender, EventArgs e)
    {
        // Give the controls time to resize, then recenter the view
        await Task.Delay(50);
        if (_game?.Map != null)
        {
            hexGridControl.CenterView();
        }
    }

    private void InitializeGame()
    {
        _game = new Game();
        hexGridControl.Game = _game;
        // Don't call CenterView here - wait until form is shown
        AddToDamageLog("🎮 Welcome to Hex Wargame!");
        AddToDamageLog($"🗺️ Map: {_game.Map.MapName}");
        UpdateUI();
    }

    private void SetupEventHandlers()
    {
        // Game events
        _game.TeamChanged += OnTeamChanged;
        _game.AttackExecuted += OnAttackExecuted;
        _game.UnitMoved += OnUnitMoved;
        _game.GameEnded += OnGameEnded;
        _game.StateChanged += OnStateChanged;
        _game.AbilityExecuted += OnAbilityExecuted; // Make sure this is included
        
        // UI events - CRITICAL: These were missing!
        hexGridControl.UnitSelected += OnUnitSelected;
        hexGridControl.HexClicked += OnHexClicked;
        
        // Button events
        newGameButton.Click += OnNewGameClicked;
        newMapButton.Click += OnNewMapClicked;
        endTurnButton.Click += OnEndTurnClicked;
        helpButton.Click += OnHelpClicked;
        abilityButton.Click += OnAbilityButtonClicked; // Wire up ability button
        
        // Keyboard events
        this.KeyPreview = true;
        this.KeyDown += OnKeyDown; // Wire up keyboard handler
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
            
            var abilityInfo = unit.CanUseAbility() ? 
                $"Ability: {unit.Ability.Type} (Ready)" : 
                unit.Ability != null ? 
                    $"Ability: {unit.Ability.Type} (Cooldown: {unit.Ability.CurrentCooldown})" : 
                    "Ability: None";
            
            unitStatsLabel.Text = $"HP: {unit.CurrentHp}/{unit.MaxHp}\n" +
                                 $"Movement: {unit.Movement}\n" +
                                 $"Range: {unit.AttackRange}\n" +
                                 $"Power: {unit.AttackPower}\n" +
                                 $"Can Move: {(unit.CanMove ? "Yes" : "No")}\n" +
                                 $"Can Attack: {(unit.CanAttack ? "Yes" : "No")}\n" +
                                 abilityInfo;
                                 
            // Enable ability button if unit can use ability and it's player's turn
            abilityButton.Enabled = unit.CanUseAbility() && 
                                  _game.State == GameState.PlayerTurn && 
                                  unit.Team == _game.CurrentTeam;
        }
        else
        {
            selectedUnitLabel.Text = "Selected: None";
            unitStatsLabel.Text = "";
            abilityButton.Enabled = false;
        }

        // Update button states
        endTurnButton.Enabled = !_game.GameOver && _game.State == GameState.PlayerTurn;
        
        // Change team label color
        currentTeamLabel.ForeColor = _game.CurrentTeam == Team.Red ? Color.Red : Color.Blue;
    }

    private async void OnTeamChanged(object? sender, Team newTeam)
    {
        UpdateUI();
        
        // Show round transition when Red team starts (new round)
        if (newTeam == Team.Red && _game.TurnNumber > 1)
        {
            ShowRoundTransition();
        }
        
        // If it's AI's turn, execute AI moves
        if (newTeam == Team.Blue && !_game.GameOver)
        {
            endTurnButton.Enabled = false;
            
            AddToDamageLog("🤖 AI Turn Starting...");
            await Task.Delay(1000); // Brief pause before AI starts
            await _game.ExecuteAITurn();
            
            AddToDamageLog("🤖 AI Turn Complete");
            UpdateUI();
        }
    }

    private void ShowRoundTransition()
    {
        var stats = _game.GetGameStats();
        var redUnits = (int)stats["RedUnitsAlive"];
        var blueUnits = (int)stats["BlueUnitsAlive"];
        var accuracy = (double)stats["AttackAccuracy"];
        
        var message = $@"ROUND {_game.TurnNumber} STARTING

CURRENT STATUS:
Red Team: {redUnits} units remaining
Blue Team: {blueUnits} units remaining

BATTLE STATISTICS:
Total Attacks: {stats["TotalAttacks"]}
Hit Accuracy: {accuracy:F1}%
Units Lost: {stats["UnitsKilled"]} total

Map: {stats["MapName"]}

Good luck, Commander!";

        MessageBox.Show(message, $"Round {_game.TurnNumber}", MessageBoxButtons.OK, MessageBoxIcon.Information);
        AddToDamageLog($"⚔️ Round {_game.TurnNumber} - Fight!");
    }

    private void OnAttackExecuted(object? sender, AttackResult result)
    {
        string logEntry;
        if (result.Hit)
        {
            logEntry = $"💥 Hit! {result.Damage} damage";
            if (result.TargetKilled)
                logEntry += " - ELIMINATED!";
        }
        else
        {
            logEntry = $"❌ Attack missed!";
        }

        AddToDamageLog(logEntry);
        UpdateUI();
    }

    private void AddToDamageLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _damageLog.Add($"[{timestamp}] {message}");
        
        // Keep only last 20 entries
        if (_damageLog.Count > 20)
        {
            _damageLog.RemoveAt(0);
        }
        
        // Update the display
        damageLogTextBox.Text = string.Join(Environment.NewLine, _damageLog);
        
        // Auto-scroll to bottom
        damageLogTextBox.SelectionStart = damageLogTextBox.Text.Length;
        damageLogTextBox.ScrollToCaret();
    }

    private void ClearDamageLog()
    {
        _damageLog.Clear();
        damageLogTextBox.Text = "";
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
                    ClearDamageLog();
                    AddToDamageLog("🎮 New game started!");
                    UpdateUI();
                    break;
                    
                case "NewMap":
                    _game.Reset(); // This generates a new random map
                    hexGridControl.ClearSelection();
                    hexGridControl.CenterView();
                    ClearDamageLog();
                    AddToDamageLog($"🗺️ New map: {_game.Map.MapName}");
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
        // Add some debugging to see if this is being called
        AddToDamageLog($"🎯 Unit selected: {unit.UnitType} at {unit.Position}");
        UpdateUI();
    }

    private void OnHexClicked(object? sender, HexCoord coord)
    {
        // This could be used for additional hex click handling
    }

    /// <summary>
    /// Handles unit movement events from the game engine.
    /// </summary>
    private void OnUnitMoved(object? sender, Unit unit)
    {
        // Add movement info to the damage log and update UI
        AddToDamageLog($"🚶 {unit.GetUnitChar()} {unit.UnitType} moved to {unit.Position}");
        UpdateUI();
    }

    private void OnNewGameClicked(object? sender, EventArgs e)
    {
        var result = MessageBox.Show("Start a new game?", "New Game", 
                                   MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        
        if (result == DialogResult.Yes)
        {
            _game.Reset();
            hexGridControl.ClearSelection();
            hexGridControl.CenterView();
            ClearDamageLog();
            AddToDamageLog("🎮 New game started!");
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
            ClearDamageLog();
            AddToDamageLog($"🗺️ New map: {_game.Map.MapName}");
            UpdateUI();
        }
    }

    private void OnEndTurnClicked(object? sender, EventArgs e)
    {
        if (!_game.GameOver && _game.State == GameState.PlayerTurn)
        {
            hexGridControl.ClearSelection();
            _game.NextTurn();
        }
    }

    /// <summary>
    /// Handle ability button click - toggle ability mode for selected unit
    /// </summary>
    private void OnAbilityButtonClicked(object? sender, EventArgs e)
    {
        AddToDamageLog($"🔧 Ability button clicked. Selected unit: {hexGridControl.SelectedUnit?.UnitType.ToString() ?? "None"}");
        
        if (hexGridControl.SelectedUnit != null && 
            hexGridControl.SelectedUnit.Team == Team.Red && 
            hexGridControl.SelectedUnit.CanUseAbility())
        {
            hexGridControl.ToggleAbilityMode();
            UpdateUI();
            
            // Add visual feedback to combat log
            var unit = hexGridControl.SelectedUnit;
            var message = hexGridControl.IsInAbilityMode ? 
                $"⚡ {unit.GetUnitChar()} {unit.UnitType} ability mode activated. Select target for {unit.Ability.Type}." :
                $"❌ {unit.GetUnitChar()} {unit.UnitType} ability mode cancelled.";
            AddToDamageLog(message);
        }
        else if (hexGridControl.SelectedUnit == null)
        {
            AddToDamageLog("⚠️ No unit selected. Select a unit first to use abilities.");
        }
        else if (hexGridControl.SelectedUnit.Team != Team.Red)
        {
            AddToDamageLog("⚠️ You can only use abilities for your own units (Red team).");
        }
        else if (!hexGridControl.SelectedUnit.CanUseAbility())
        {
            var unit = hexGridControl.SelectedUnit;
            var cooldownText = unit.Ability?.CurrentCooldown > 0 ? 
                $" (Cooldown: {unit.Ability.CurrentCooldown} turns)" : "";
            AddToDamageLog($"⚠️ {unit.UnitType} ability not ready{cooldownText}.");
        }
    }

    /// <summary>
    /// Handle keyboard shortcuts for game actions
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Space:
            case Keys.Enter:
                // End turn shortcut
                if (_game.CurrentTeam == Team.Red && !_game.GameOver)
                {
                    OnEndTurnClicked(sender, e);
                    e.Handled = true;
                }
                break;
                
            case Keys.A:
            case Keys.Q:
                // Ability shortcut (A or Q key)
                OnAbilityButtonClicked(sender, e);
                e.Handled = true;
                break;
                
            case Keys.Escape:
                // Cancel ability mode or deselect unit
                if (hexGridControl.IsInAbilityMode)
                {
                    hexGridControl.ExitAbilityMode();
                    AddToDamageLog("❌ Ability mode cancelled.");
                    UpdateUI();
                }
                else if (hexGridControl.SelectedUnit != null)
                {
                    hexGridControl.ClearSelection();
                    AddToDamageLog("🔄 Unit deselected.");
                    UpdateUI();
                }
                e.Handled = true;
                break;
                
            case Keys.H:
            case Keys.F1:
                // Help shortcut
                OnHelpClicked(sender, e);
                e.Handled = true;
                break;
                
            case Keys.N:
                // New game shortcut (Ctrl+N)
                if (e.Control)
                {
                    OnNewGameClicked(sender, e);
                    e.Handled = true;
                }
                break;
                
            case Keys.M:
                // New map shortcut (Ctrl+M)
                if (e.Control)
                {
                    OnNewMapClicked(sender, e);
                    e.Handled = true;
                }
                break;
        }
    }

    /// <summary>
    /// Handle ability execution results from the game engine
    /// </summary>
    private void OnAbilityExecuted(object? sender, AbilityResult result)
    {
        // Show ability effect in the combat log with detailed information
        var user = result.User;
        var abilityName = GetAbilityDisplayName(result.AbilityType);
        
        string message = $"⚡ {user.GetUnitChar()} {user.Team} {user.UnitType} used {abilityName}";
        
        switch (result.AbilityType)
        {
            case AbilityType.MedicHeal:
                var target = _game.Map.GetUnitAt(result.TargetCoord);
                if (target != null)
                {
                    message += $" to heal {target.GetUnitChar()} {target.UnitType} for 30 HP ({target.CurrentHp}/{target.MaxHp} HP)";
                }
                break;
                
            case AbilityType.SniperOverwatch:
                message += " and is now on overwatch. Will intercept enemy movement.";
                break;
                
            case AbilityType.HeavySuppression:
                var suppressedEnemies = _game.Map.Units.Values
                    .Count(u => u.Team != user.Team && 
                               u.IsAlive && 
                               u.Position.DistanceTo(result.TargetCoord) <= 2);
                message += $" to suppress {suppressedEnemies} enemy unit(s). Their movement is reduced.";
                break;
                
            case AbilityType.InfantryRush:
                message += " to sprint forward. Movement increased this turn.";
                break;
                
            default:
                message += ".";
                break;
        }
        
        AddToDamageLog(message);
        
        // Don't call ExitAbilityMode here - it's handled in HexGridControl
        // Just update the UI to reflect any changes
        UpdateUI();
    }

    /// <summary>
    /// Get user-friendly display name for abilities
    /// </summary>
    private string GetAbilityDisplayName(AbilityType abilityType)
    {
        return abilityType switch
        {
            AbilityType.MedicHeal => "Field Medic",
            AbilityType.SniperOverwatch => "Overwatch",
            AbilityType.HeavySuppression => "Suppression Fire",
            AbilityType.InfantryRush => "Sprint",
            _ => abilityType.ToString()
        };
    }

    /// <summary>
    /// Handle game state changes
    /// </summary>
    private void OnStateChanged(object? sender, GameState newState)
    {
        UpdateUI();
        
        // Add state change information to the log
        switch (newState)
        {
            case GameState.PlayerTurn:
                AddToDamageLog("🎮 Your turn!");
                break;
            case GameState.AITurn:
                AddToDamageLog("🤖 AI thinking...");
                break;
            case GameState.GameOver:
                AddToDamageLog("🏁 Game Over!");
                break;
        }
    }

    /// <summary>
    /// Enhanced help dialog with ability information
    /// </summary>
    private void OnHelpClicked(object? sender, EventArgs e)
    {
        var helpText = @"=== HEX WARGAME CONTROLS ===

MOUSE CONTROLS:
• Click to select units (Red team only)
• Click empty hex to move selected unit
• Click enemy unit to attack
• Click ability target when in ability mode

KEYBOARD SHORTCUTS:
• SPACE/ENTER - End turn
• A or Q - Use selected unit's ability
• ESC - Cancel ability mode or deselect
• H or F1 - Show this help
• Ctrl+N - New game
• Ctrl+M - Generate new map

UNIT ABILITIES:
• Medic (🏥) - Field Medic: Heal friendly units for 30 HP (Range: 2)
• Sniper (🎯) - Overwatch: Intercept enemy movement (Cooldown: 3)
• Heavy (🛡️) - Suppression: Reduce enemy movement (Area: 2 hexes)
• Infantry (⚔️) - Sprint: +2 movement this turn (Cooldown: 4)

TERRAIN EFFECTS:
• Forest/Hills - Defensive bonus, slower movement
• Buildings - Strong defensive bonus
• Roads - Faster movement
• Water - Impassable

COMBAT MECHANICS:
• Base 70% hit chance modified by terrain defense
• Line-of-sight required for attacks
• Flanking attacks deal bonus damage
• Units have cooldowns after actions

VICTORY:
Eliminate all enemy units to win!";

        MessageBox.Show(helpText, "Game Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
