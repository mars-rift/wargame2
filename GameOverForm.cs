using System.Drawing;
using System.Windows.Forms;
using HexWargame.Core;

namespace HexWargame.UI
{
    /// <summary>
    /// Game over dialog showing detailed game statistics
    /// </summary>
    public partial class GameOverForm : Form
    {
        public GameOverForm(Dictionary<string, object> gameStats, Team winner)
        {
            InitializeComponent();
            DisplayGameStats(gameStats, winner);
        }

        private void DisplayGameStats(Dictionary<string, object> gameStats, Team winner)
        {
            // Set window title and winner message
            this.Text = "Game Over - Hex Wargame";
            
            var winnerColor = winner == Team.Red ? Color.Red : Color.Blue;
            winnerLabel.Text = $"{winner} Team Wins!";
            winnerLabel.ForeColor = winnerColor;

            // Calculate derived statistics
            var gameDuration = (TimeSpan)gameStats["GameDuration"];
            var accuracy = (double)gameStats["AttackAccuracy"];
            var totalUnitsStarted = (int)gameStats["RedUnitsLost"] + (int)gameStats["BlueUnitsLost"] + 
                                   (int)gameStats["RedUnitsAlive"] + (int)gameStats["BlueUnitsAlive"];

            // Display detailed statistics
            var statsText = $@"GAME STATISTICS

Map: {gameStats["MapName"]}
Duration: {gameDuration:mm\:ss}
Total Turns: {gameStats["TurnNumber"]}
Winner: {winner} Team

FINAL UNIT COUNT
Red Team: {gameStats["RedUnitsAlive"]} units remaining
Blue Team: {gameStats["BlueUnitsAlive"]} units remaining

COMBAT STATISTICS
Total Attacks: {gameStats["TotalAttacks"]}
Successful Hits: {gameStats["SuccessfulAttacks"]}
Accuracy: {accuracy:F1}%
Units Eliminated: {gameStats["UnitsKilled"]} of {totalUnitsStarted}

LOSSES BY TEAM
Red Team Lost: {gameStats["RedUnitsLost"]} units
Blue Team Lost: {gameStats["BlueUnitsLost"]} units

PERFORMANCE RATING
{GetPerformanceRating(gameStats, winner)}

Thank you for playing!";

            statsTextBox.Text = statsText;
        }

        private string GetPerformanceRating(Dictionary<string, object> gameStats, Team winner)
        {
            var turns = (int)gameStats["TurnNumber"];
            var isPlayerWinner = winner == Team.Red;
            
            if (isPlayerWinner)
            {
                return turns switch
                {
                    <= 5 => "★★★ EXCEPTIONAL! Lightning Victory",
                    <= 10 => "★★★ EXCELLENT! Quick Victory", 
                    <= 15 => "★★☆ GOOD! Tactical Victory",
                    <= 20 => "★☆☆ DECENT! Hard-fought Victory",
                    _ => "☆☆☆ PYRRHIC! Costly Victory"
                };
            }
            else
            {
                return turns switch
                {
                    <= 5 => "The AI crushed you quickly! Try different tactics.",
                    <= 10 => "The AI outmaneuvered you. Study unit positioning.",
                    <= 15 => "Close battle! You're improving.",
                    <= 20 => "Long battle. Focus on aggressive play.",
                    _ => "Marathon battle! Consider faster strategies."
                };
            }
        }
    }
}
