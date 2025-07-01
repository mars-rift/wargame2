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
            
            if (redUnits.Count == 0) return; // Safety check
            
            var isEndgame = redUnits.Count <= 2 || aiUnits.Count <= 2;
            
            foreach (var unit in aiUnits.ToList()) // ToList to avoid collection modification issues
            {
                if (!unit.IsAlive) continue;

                // Add delay for better visualization
                await Task.Delay(500);

                // Phase 1: Try to attack first
                await ExecuteAttackPhase(unit, isEndgame);

                // Phase 2: Strategic movement (recalculate enemies in case one was eliminated)
                var currentEnemies = GetTeamUnits(Team.Red);
                if (currentEnemies.Count > 0)
                {
                    await ExecuteMovementPhase(unit, currentEnemies, isEndgame);
                }
                
                // Phase 3: Attack again if moved into range
                if (unit.CanAttack)
                {
                    await ExecuteAttackPhase(unit, isEndgame);
                }
            }

            // End AI turn
            await Task.Delay(500);
            NextTurn();
        }

        private async Task ExecuteAttackPhase(Unit unit, bool isEndgame)
        {
            var targets = GetAttackTargets(unit);
            if (targets.Count == 0 || !unit.CanAttack) return;

            Unit? target = SelectBestTarget(unit, targets, isEndgame);
            
            if (target != null)
            {
                AttackUnit(unit, target);
                await Task.Delay(1000);
            }
        }

        private Unit SelectBestTarget(Unit attacker, List<Unit> targets, bool isEndgame)
        {
            if (targets.Count == 0) return null!;

            if (isEndgame)
            {
                // Endgame: Focus on eliminating threats
                // Priority 1: Units we can kill this turn
                var killableTargets = targets
                    .Where(t => t.CurrentHp <= attacker.AttackPower + 20) // Buffer for damage variance
                    .ToList();

                if (killableTargets.Count > 0)
                {
                    return killableTargets
                        .OrderByDescending(t => t.AttackPower) // Kill strongest attacker first
                        .ThenBy(t => t.CurrentHp)
                        .First();
                }

                // Priority 2: Highest threat units
                return targets
                    .OrderByDescending(t => CalculateThreatLevel(attacker, t))
                    .First();
            }
            else
            {
                // Early/mid game: Balanced targeting
                return targets
                    .OrderByDescending(t => CalculateTargetPriority(attacker, t))
                    .First();
            }
        }

        private double CalculateThreatLevel(Unit attacker, Unit target)
        {
            var hpRatio = (double)target.CurrentHp / target.MaxHp;
            var attackPowerScore = target.AttackPower;
            var rangeScore = target.AttackRange;
            var canCounterAttack = target.Position.DistanceTo(attacker.Position) <= target.AttackRange ? 2.0 : 1.0;

            return (attackPowerScore * 2 + rangeScore) * canCounterAttack / hpRatio;
        }

        private double CalculateTargetPriority(Unit attacker, Unit target)
        {
            var hpScore = (target.MaxHp - target.CurrentHp) / (double)target.MaxHp; // Prefer damaged units
            var threatScore = target.AttackPower / 50.0; // Normalize attack power
            var rangeScore = target.AttackRange / 4.0; // Normalize range
            
            return hpScore * 2 + threatScore + rangeScore;
        }

        private async Task ExecuteMovementPhase(Unit unit, List<Unit> enemies, bool isEndgame)
        {
            if (!unit.CanMove || enemies.Count == 0) return;

            var validMoves = GetValidMoves(unit);
            if (validMoves.Count == 0) return;

            HexCoord bestMove = GetOptimalPosition(unit, enemies, validMoves, isEndgame);

            if (bestMove != unit.Position) // Only move if it's actually different
            {
                MoveUnit(unit, bestMove);
                await Task.Delay(500);
            }
        }

        private HexCoord GetOptimalPosition(Unit unit, List<Unit> enemies, List<HexCoord> validMoves, bool isEndgame)
        {
            var scoredMoves = validMoves.Select(pos => new
            {
                Position = pos,
                Score = CalculatePositionScore(unit, pos, enemies, isEndgame)
            }).ToList();

            return scoredMoves
                .OrderByDescending(m => m.Score)
                .First().Position;
        }

        private double CalculatePositionScore(Unit unit, HexCoord position, List<Unit> enemies, bool isEndgame)
        {
            double score = 0;

            // Attack opportunity score
            var attackableEnemies = enemies.Count(e => position.DistanceTo(e.Position) <= unit.AttackRange);
            score += attackableEnemies * (isEndgame ? 100 : 50);

            // Defense bonus from terrain
            score += Map.GetDefenseBonus(position);

            // Unit type specific positioning
            score += GetUnitTypePositionBonus(unit, position, enemies);

            // Penalty for being in enemy attack range
            var enemiesInRange = enemies.Count(e => 
                position.DistanceTo(e.Position) <= e.AttackRange && e.CanAttack);
            score -= enemiesInRange * (isEndgame ? 30 : 20);

            // Distance consideration (closer is generally better, but not too close for ranged units)
            var closestEnemy = enemies.OrderBy(e => position.DistanceTo(e.Position)).First();
            var distanceToClosest = position.DistanceTo(closestEnemy.Position);
            
            if (unit.UnitType == UnitType.Sniper)
            {
                // Snipers prefer medium range
                var idealRange = unit.AttackRange - 1;
                score += Math.Max(0, 20 - Math.Abs(distanceToClosest - idealRange) * 5);
            }
            else
            {
                // Other units prefer to be closer
                score += Math.Max(0, 30 - distanceToClosest * 3);
            }

            return score;
        }

        private double GetUnitTypePositionBonus(Unit unit, HexCoord position, List<Unit> enemies)
        {
            return unit.UnitType switch
            {
                UnitType.Sniper => GetSniperPositionBonus(position, enemies),
                UnitType.Heavy => GetHeavyPositionBonus(position),
                UnitType.Medic => GetMedicPositionBonus(position, unit.Team),
                UnitType.Infantry => GetInfantryPositionBonus(position, enemies),
                _ => 0
            };
        }

        private double GetSniperPositionBonus(HexCoord position, List<Unit> enemies)
        {
            // Snipers prefer cover and good sight lines
            var coverBonus = Map.GetDefenseBonus(position) > 0 ? 15 : 0;
            var sightLineBonus = enemies.Count(e => position.DistanceTo(e.Position) <= 4) * 5;
            return coverBonus + sightLineBonus;
        }

        private double GetHeavyPositionBonus(HexCoord position)
        {
            // Heavy units prefer buildings for defense
            return Map.Terrain.TryGetValue(position, out var terrain) && terrain == TerrainType.Building ? 20 : 0;
        }

        private double GetMedicPositionBonus(HexCoord position, Team team)
        {
            // Medics prefer to be near friendly units
            var friendlyUnits = GetTeamUnits(team);
            var nearbyFriendlies = friendlyUnits.Count(u => position.DistanceTo(u.Position) <= 2);
            return nearbyFriendlies * 10;
        }

        private double GetInfantryPositionBonus(HexCoord position, List<Unit> enemies)
        {
            // Infantry are flexible, slight preference for cover
            return Map.GetDefenseBonus(position) * 0.5;
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
