using HexWargame.Core;

namespace HexWargame.Core
{
    /// <summary>
    /// Result of an attack action
    /// </summary>
    public class AttackResult
    {
        public bool Success { get; set; }
        public bool Hit { get; set; }
        public int Roll { get; set; }
        public int HitChance { get; set; }
        public int Damage { get; set; }
        public bool TargetKilled { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Main game engine managing state and rules
    /// </summary>
    public class Game
    {
        public GameMap Map { get; }
        public Team CurrentTeam { get; private set; }
        public int TurnNumber { get; private set; }
        public bool GameOver { get; private set; }
        public Team? Winner { get; private set; }

        private readonly Random _random;

        public event EventHandler<Team>? TeamChanged;
        public event EventHandler<AttackResult>? AttackExecuted;
        public event EventHandler<Unit>? UnitMoved;
        public event EventHandler? GameEnded;

        public Game()
        {
            Map = new GameMap();
            CurrentTeam = Team.Red;
            TurnNumber = 1;
            _random = new Random();
            SetupSquads();
        }

        private void SetupSquads()
        {
            // Red team (top side)
            var redPositions = new[]
            {
                new HexCoord(-2, -4), new HexCoord(-1, -4), 
                new HexCoord(0, -4), new HexCoord(1, -4)
            };
            var redUnits = new[] { UnitType.Infantry, UnitType.Sniper, UnitType.Heavy, UnitType.Medic };

            // Blue team (bottom side)
            var bluePositions = new[]
            {
                new HexCoord(-2, 4), new HexCoord(-1, 4), 
                new HexCoord(0, 4), new HexCoord(1, 4)
            };
            var blueUnits = new[] { UnitType.Infantry, UnitType.Sniper, UnitType.Heavy, UnitType.Medic };

            // Place red team
            for (int i = 0; i < redPositions.Length && i < redUnits.Length; i++)
            {
                if (Map.IsValidPosition(redPositions[i]))
                {
                    var unit = new Unit(redUnits[i], Team.Red, redPositions[i]);
                    Map.PlaceUnit(unit, redPositions[i]);
                }
            }

            // Place blue team
            for (int i = 0; i < bluePositions.Length && i < blueUnits.Length; i++)
            {
                if (Map.IsValidPosition(bluePositions[i]))
                {
                    var unit = new Unit(blueUnits[i], Team.Blue, bluePositions[i]);
                    Map.PlaceUnit(unit, bluePositions[i]);
                }
            }
        }

        public List<Unit> GetTeamUnits(Team team)
        {
            return Map.Units.Values
                .Where(unit => unit.Team == team && unit.IsAlive)
                .ToList();
        }

        public List<HexCoord> GetValidMoves(Unit unit)
        {
            if (!unit.CanMove) return new List<HexCoord>();

            var validMoves = new List<HexCoord>();
            var visited = new HashSet<HexCoord> { unit.Position };
            var queue = new Queue<(HexCoord coord, int distance)>();
            queue.Enqueue((unit.Position, 0));

            while (queue.Count > 0)
            {
                var (current, distance) = queue.Dequeue();

                if (distance < unit.Movement)
                {
                    foreach (var neighbor in current.GetNeighbors())
                    {
                        if (!visited.Contains(neighbor) && 
                            Map.IsPassable(neighbor) && 
                            distance + 1 <= unit.Movement)
                        {
                            validMoves.Add(neighbor);
                            visited.Add(neighbor);
                            queue.Enqueue((neighbor, distance + 1));
                        }
                    }
                }
            }

            return validMoves;
        }

        public List<Unit> GetAttackTargets(Unit unit)
        {
            if (!unit.CanAttack) return new List<Unit>();

            return Map.Units.Values
                .Where(target => target.Team != unit.Team && 
                               target.IsAlive && 
                               unit.Position.DistanceTo(target.Position) <= unit.AttackRange)
                .ToList();
        }

        public bool MoveUnit(Unit unit, HexCoord targetPos)
        {
            var validMoves = GetValidMoves(unit);
            if (!validMoves.Contains(targetPos)) return false;

            Map.PlaceUnit(unit, targetPos);
            unit.HasMoved = true;
            UnitMoved?.Invoke(this, unit);
            return true;
        }

        public AttackResult AttackUnit(Unit attacker, Unit target)
        {
            var validTargets = GetAttackTargets(attacker);
            if (!validTargets.Contains(target))
            {
                return new AttackResult 
                { 
                    Success = false, 
                    Message = "Invalid target" 
                };
            }

            // Calculate hit chance (base 70% + modifiers)
            var hitChance = 70;
            var defenseBonus = Map.GetDefenseBonus(target.Position);
            hitChance -= defenseBonus;

            // Ensure minimum and maximum hit chances
            hitChance = Math.Max(10, Math.Min(95, hitChance));

            // Roll for hit
            var roll = _random.Next(1, 101);
            var hit = roll <= hitChance;

            var result = new AttackResult
            {
                Success = true,
                Hit = hit,
                Roll = roll,
                HitChance = hitChance,
                Damage = 0,
                TargetKilled = false
            };

            if (hit)
            {
                // Calculate damage
                var baseDamage = attacker.AttackPower;
                var damageVariance = _random.Next(-10, 11);
                var damage = Math.Max(10, baseDamage + damageVariance);

                target.TakeDamage(damage);
                result.Damage = damage;
                result.TargetKilled = !target.IsAlive;

                if (!target.IsAlive)
                {
                    Map.RemoveUnit(target.Position);
                }
            }

            attacker.HasAttacked = true;
            AttackExecuted?.Invoke(this, result);
            return result;
        }

        public void NextTurn()
        {
            // Reset all units of current team
            foreach (var unit in GetTeamUnits(CurrentTeam))
            {
                unit.ResetTurn();
            }

            // Switch teams
            CurrentTeam = CurrentTeam == Team.Red ? Team.Blue : Team.Red;
            
            if (CurrentTeam == Team.Red)
                TurnNumber++;

            TeamChanged?.Invoke(this, CurrentTeam);

            // Check for game over
            CheckGameOver();
        }

        private void CheckGameOver()
        {
            var redUnits = GetTeamUnits(Team.Red);
            var blueUnits = GetTeamUnits(Team.Blue);

            if (redUnits.Count == 0)
            {
                GameOver = true;
                Winner = Team.Blue;
                GameEnded?.Invoke(this, EventArgs.Empty);
            }
            else if (blueUnits.Count == 0)
            {
                GameOver = true;
                Winner = Team.Red;
                GameEnded?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Intelligent AI for computer player with endgame tactics
        /// </summary>
        public async Task ExecuteAITurn()
        {
            if (CurrentTeam != Team.Blue || GameOver) return;

            var aiUnits = GetTeamUnits(Team.Blue);
            var redUnits = GetTeamUnits(Team.Red);
            var isEndgame = redUnits.Count <= 2 || aiUnits.Count <= 2;
            
            foreach (var unit in aiUnits)
            {
                if (!unit.IsAlive) continue;

                // Add delay for better visualization
                await Task.Delay(500);

                // Phase 1: Try to attack first
                await ExecuteAttackPhase(unit, redUnits, isEndgame);

                // Phase 2: Strategic movement
                await ExecuteMovementPhase(unit, redUnits, isEndgame);
            }

            // End AI turn
            await Task.Delay(500);
            NextTurn();
        }

        private async Task ExecuteAttackPhase(Unit unit, List<Unit> enemies, bool isEndgame)
        {
            var targets = GetAttackTargets(unit);
            if (targets.Count == 0 || !unit.CanAttack) return;

            Unit? target = null;

            if (isEndgame)
            {
                // Endgame: Focus on eliminating threats
                // Priority: Low HP units that can still fight
                target = targets
                    .Where(t => t.CurrentHp <= unit.AttackPower + 15) // Can likely kill
                    .OrderBy(t => t.CurrentHp)
                    .FirstOrDefault();

                // If no killable target, target highest damage dealer
                target ??= targets
                    .OrderByDescending(t => t.AttackPower)
                    .ThenBy(t => t.CurrentHp)
                    .First();
            }
            else
            {
                // Early/mid game: Target tactically
                target = targets
                    .OrderBy(t => t.CurrentHp) // Weakest first
                    .ThenByDescending(t => t.AttackPower) // Then strongest
                    .First();
            }

            AttackUnit(unit, target);
            await Task.Delay(1000);
        }

        private async Task ExecuteMovementPhase(Unit unit, List<Unit> enemies, bool isEndgame)
        {
            if (!unit.CanMove || enemies.Count == 0) return;

            var validMoves = GetValidMoves(unit);
            if (validMoves.Count == 0) return;

            HexCoord bestMove;

            if (isEndgame)
            {
                // Endgame strategy: Aggressive positioning
                bestMove = GetAggressivePosition(unit, enemies, validMoves);
            }
            else
            {
                // Normal strategy: Balanced approach
                bestMove = GetTacticalPosition(unit, enemies, validMoves);
            }

            MoveUnit(unit, bestMove);
            await Task.Delay(500);
        }

        private HexCoord GetAggressivePosition(Unit unit, List<Unit> enemies, List<HexCoord> validMoves)
        {
            // Find position that maximizes attack opportunities next turn
            var scoredMoves = validMoves.Select(pos => new
            {
                Position = pos,
                AttackOpportunities = enemies.Count(e => pos.DistanceTo(e.Position) <= unit.AttackRange),
                DistanceToClosest = enemies.Min(e => pos.DistanceTo(e.Position)),
                DefenseBonus = Map.GetDefenseBonus(pos)
            }).ToList();

            // Prioritize positions that enable attacks
            return scoredMoves
                .OrderByDescending(m => m.AttackOpportunities)
                .ThenBy(m => m.DistanceToClosest)
                .ThenByDescending(m => m.DefenseBonus)
                .First().Position;
        }

        private HexCoord GetTacticalPosition(Unit unit, List<Unit> enemies, List<HexCoord> validMoves)
        {
            // Balanced positioning considering attack range and defense
            var closestEnemy = enemies.OrderBy(e => unit.Position.DistanceTo(e.Position)).First();
            
            var scoredMoves = validMoves.Select(pos => new
            {
                Position = pos,
                DistanceToEnemy = pos.DistanceTo(closestEnemy.Position),
                DefenseBonus = Map.GetDefenseBonus(pos),
                CanAttackNext = pos.DistanceTo(closestEnemy.Position) <= unit.AttackRange
            }).ToList();

            // Move to attack range if possible, otherwise get closer while seeking cover
            return scoredMoves
                .OrderByDescending(m => m.CanAttackNext ? 1 : 0)
                .ThenBy(m => m.DistanceToEnemy)
                .ThenByDescending(m => m.DefenseBonus)
                .First().Position;
        }

        /// <summary>
        /// Reset the game to initial state with a new random map
        /// </summary>
        public void Reset()
        {
            Map.Units.Clear();
            Map.Terrain.Clear();
            Map.GenerateRandomMap();
            
            CurrentTeam = Team.Red;
            TurnNumber = 1;
            GameOver = false;
            Winner = null;
            
            SetupSquads();
        }

        /// <summary>
        /// Reset the game with a specific map layout
        /// </summary>
        public void ResetWithMap(Action mapGenerator)
        {
            Map.Units.Clear();
            Map.Terrain.Clear();
            mapGenerator();
            
            CurrentTeam = Team.Red;
            TurnNumber = 1;
            GameOver = false;
            Winner = null;
            
            SetupSquads();
        }

        /// <summary>
        /// Get game statistics
        /// </summary>
        public Dictionary<string, object> GetGameStats()
        {
            var redUnits = GetTeamUnits(Team.Red);
            var blueUnits = GetTeamUnits(Team.Blue);

            return new Dictionary<string, object>
            {
                ["TurnNumber"] = TurnNumber,
                ["CurrentTeam"] = CurrentTeam.ToString(),
                ["RedUnitsAlive"] = redUnits.Count,
                ["BlueUnitsAlive"] = blueUnits.Count,
                ["GameOver"] = GameOver,
                ["Winner"] = Winner?.ToString() ?? "None",
                ["MapName"] = Map.MapName
            };
        }
    }
}
