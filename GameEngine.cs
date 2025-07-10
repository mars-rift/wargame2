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

        // Add properties for attacker and target units
        public Unit Attacker { get; set; } = null!;
        public Unit Target { get; set; } = null!;
        public bool IsOverwatchAttack { get; set; } // Indicates if this was an overwatch attack
    }

    /// <summary>
    /// Main game engine managing state and rules
    /// </summary>
    public class Game
    {
        public GameMap Map { get; }
        public Team CurrentTeam { get; private set; }
        public GameState State { get; private set; }
        public int TurnNumber { get; private set; }
        public bool GameOver { get; private set; }
        public Team? Winner { get; private set; }

        // Game statistics tracking
        public int TotalAttacks { get; private set; }
        public int SuccessfulAttacks { get; private set; }
        public int UnitsKilled { get; private set; }
        public int RedUnitsLost { get; private set; }
        public int BlueUnitsLost { get; private set; }
        public DateTime GameStartTime { get; private set; }

        private readonly Random _random;

        public event EventHandler<Team>? TeamChanged;
        public event EventHandler<AttackResult>? AttackExecuted;
        public event EventHandler<Unit>? UnitMoved;
        public event EventHandler<GameState>? StateChanged;
        public event EventHandler? GameEnded;
        public event EventHandler<AbilityResult>? AbilityExecuted;

        public Game()
        {
            Map = new GameMap();
            CurrentTeam = Team.Red;
            State = GameState.PlayerTurn;
            TurnNumber = 1;
            _random = new Random();
            GameStartTime = DateTime.Now;
            SetupSquads();
        }

        /// <summary>
        /// Change the game state and notify listeners
        /// </summary>
        private void ChangeState(GameState newState)
        {
            if (State != newState)
            {
                State = newState;
                StateChanged?.Invoke(this, newState);
            }
        }

        private void SetupSquads()
        {
            // Calculate actual map edges dynamically
            var mapEdge = Map.Height / 2; // For 30x20 map, this is 10
            
            // Larger squads for the bigger map - 8 units per team
            // Red team (top side) - spread across more positions
            var redPositions = new[]
            {
                new HexCoord(-4, -mapEdge), new HexCoord(-3, -mapEdge), new HexCoord(-2, -mapEdge), 
                new HexCoord(-1, -mapEdge), new HexCoord(0, -mapEdge), new HexCoord(1, -mapEdge),
                new HexCoord(2, -mapEdge), new HexCoord(3, -mapEdge)
            };
            var redUnits = new[] { 
                UnitType.Infantry, UnitType.Infantry, UnitType.Sniper, UnitType.Sniper,
                UnitType.Heavy, UnitType.Heavy, UnitType.Medic, UnitType.Medic 
            };

            // Blue team (bottom side) - spread across more positions
            var bluePositions = new[]
            {
                new HexCoord(-4, mapEdge), new HexCoord(-3, mapEdge), new HexCoord(-2, mapEdge), 
                new HexCoord(-1, mapEdge), new HexCoord(0, mapEdge), new HexCoord(1, mapEdge),
                new HexCoord(2, mapEdge), new HexCoord(3, mapEdge)
            };
            var blueUnits = new[] { 
                UnitType.Infantry, UnitType.Infantry, UnitType.Sniper, UnitType.Sniper,
                UnitType.Heavy, UnitType.Heavy, UnitType.Medic, UnitType.Medic 
            };

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

            var maxMovement = unit.GetEffectiveMovement();
            var validMoves = new List<HexCoord>();
            var visited = new Dictionary<HexCoord, int> { { unit.Position, 0 } };
            var queue = new Queue<(HexCoord coord, int movementUsed)>();
            queue.Enqueue((unit.Position, 0));

            while (queue.Count > 0)
            {
                var (current, movementUsed) = queue.Dequeue();

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (!Map.IsPassable(neighbor)) continue;

                    // Calculate movement cost including terrain
                    var terrainCost = Map.Terrain.TryGetValue(neighbor, out var terrain) 
                        ? Map.GetMovementCost(terrain) 
                        : 1;
                    var newMovementUsed = movementUsed + terrainCost;

                    if (newMovementUsed <= maxMovement)
                    {
                        // If we haven't visited this hex or found a cheaper path
                        if (!visited.ContainsKey(neighbor) || visited[neighbor] > newMovementUsed)
                        {
                            visited[neighbor] = newMovementUsed;
                            
                            if (neighbor != unit.Position)
                            {
                                validMoves.Add(neighbor);
                            }
                            
                            queue.Enqueue((neighbor, newMovementUsed));
                        }
                    }
                }
            }

            return validMoves.Distinct().ToList();
        }

        /// <summary>
        /// Get optimal movement path using A* algorithm
        /// </summary>
        public List<HexCoord> GetOptimalMovementPath(Unit unit, HexCoord destination)
        {
            if (!unit.CanMove) return new List<HexCoord>();
            
            var maxMovement = unit.GetEffectiveMovement();
            return Map.FindOptimalPath(unit.Position, destination, maxMovement);
        }

        public List<Unit> GetAttackTargets(Unit unit)
        {
            if (!unit.CanAttack) return new List<Unit>();

            return Map.Units.Values
                .Where(target => target.Team != unit.Team && 
                               target.IsAlive && 
                               unit.Position.DistanceTo(target.Position) <= unit.AttackRange &&
                               Map.HasLineOfSight(unit.Position, target.Position))
                .ToList();
        }

        public bool MoveUnit(Unit unit, HexCoord targetCoord)
        {
            var originalPosition = unit.Position;
            
            if (Map.PlaceUnit(unit, targetCoord))
            {
                // Check for overwatch attacks from enemy units
                CheckOverwatchAttacks(unit, originalPosition, targetCoord);
                
                unit.HasMoved = true;
                UnitMoved?.Invoke(this, unit);
                return true;
            }
            
            return false;
        }

        private void CheckOverwatchAttacks(Unit movingUnit, HexCoord fromCoord, HexCoord toCoord)
        {
            // Find enemy units on overwatch that have line of sight to the movement path
            var overwatchUnits = Map.Units.Values
                .Where(u => u.Team != movingUnit.Team 
                    && u.IsAlive 
                    && u.IsOnOverwatch
                    && u.UnitType == UnitType.Sniper)
                .ToList();
                
            foreach (var sniper in overwatchUnits)
            {
                // Check if movement path is visible to sniper
                if (Map.HasLineOfSight(sniper.Position, toCoord) &&
                    toCoord.DistanceTo(sniper.Position) <= sniper.AttackRange)
                {
                    // Execute overwatch attack using simplified damage calculation
                    var baseDamage = sniper.AttackPower;
                    var damageVariance = _random.Next(-10, 11);
                    var damage = Math.Max(10, baseDamage + damageVariance);
                    movingUnit.TakeDamage(damage);
                    
                    // Notify attack occurred
                    AttackExecuted?.Invoke(this, new AttackResult
                    {
                        Attacker = sniper,
                        Target = movingUnit,
                        Damage = damage,
                        Success = true,
                        Hit = true,
                        IsOverwatchAttack = true
                    });
                    
                    // Overwatch is used up after firing
                    sniper.IsOnOverwatch = false;
                }
            }
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

            // Track attack statistics
            TotalAttacks++;
            
            if (hit)
            {
                SuccessfulAttacks++;
                
                // Calculate damage
                var baseDamage = attacker.AttackPower;
                var damageVariance = _random.Next(-10, 11);
                var damage = Math.Max(10, baseDamage + damageVariance);

                target.TakeDamage(damage);
                result.Damage = damage;
                result.TargetKilled = !target.IsAlive;

                if (!target.IsAlive)
                {
                    UnitsKilled++;
                    if (target.Team == Team.Red)
                        RedUnitsLost++;
                    else
                        BlueUnitsLost++;
                        
                    Map.RemoveUnit(target.Position);
                }
            }

            attacker.HasAttacked = true;
            AttackExecuted?.Invoke(this, result);
            return result;
        }

        /// <summary>
        /// Advance to the next turn
        /// </summary>
        public void NextTurn()
        {
            // Reset unit abilities for new turn
            foreach (var unit in Map.Units.Values)
            {
                unit.ResetTurn();
            }

            // Switch teams
            CurrentTeam = CurrentTeam == Team.Red ? Team.Blue : Team.Red;
            
            // Increment turn counter when it's red team's turn again
            if (CurrentTeam == Team.Red)
            {
                TurnNumber++;
            }

            TeamChanged?.Invoke(this, CurrentTeam);

            // Check for game end conditions
            CheckGameOver();
        }

        /// <summary>
        /// Execute a unit's special ability on a target
        /// </summary>
        public bool ExecuteAbility(Unit unit, HexCoord targetCoord)
        {
            if (!unit.CanUseAbility())
                return false;
                
            var ability = unit.Ability;
            bool success = false;
            
            // Check if target is in range
            if (unit.Position.DistanceTo(targetCoord) > ability.Range && ability.Range > 0)
                return false;
                
            switch (ability.Type)
            {
                case AbilityType.MedicHeal:
                    success = ExecuteMedicHeal(unit, targetCoord);
                    break;
                    
                case AbilityType.SniperOverwatch:
                    success = ExecuteSniperOverwatch(unit);
                    break;
                    
                case AbilityType.HeavySuppression:
                    success = ExecuteHeavySuppression(unit);
                    break;
                    
                case AbilityType.InfantryRush:
                    success = ExecuteInfantryRush(unit);
                    break;
            }
            
            if (success)
            {
                ability.Use();
                
                // Fire ability used event
                AbilityExecuted?.Invoke(this, new AbilityResult
                {
                    User = unit,
                    AbilityType = ability.Type,
                    TargetCoord = targetCoord
                });
            }
            
            return success;
        }

        private bool ExecuteMedicHeal(Unit medic, HexCoord targetCoord)
        {
            var targetUnit = Map.GetUnitAt(targetCoord);
            
            // Check if target is valid (friendly unit that needs healing)
            if (targetUnit == null || targetUnit.Team != medic.Team || targetUnit.CurrentHp == targetUnit.MaxHp)
                return false;
                
            // Heal the unit for 30 HP
            targetUnit.Heal(30);
            return true;
        }

        private bool ExecuteSniperOverwatch(Unit sniper)
        {
            // Activate overwatch mode - will be checked during movement phase
            sniper.IsOnOverwatch = true;
            return true;
        }

        private bool ExecuteHeavySuppression(Unit heavy)
        {
            // Get all enemies in 2-hex radius
            var enemiesInRange = Map.Units.Values
                .Where(u => u.Team != heavy.Team && u.IsAlive && u.Position.DistanceTo(heavy.Position) <= 2)
                .ToList();
                
            if (!enemiesInRange.Any())
                return false;
                
            // Apply suppression to all enemies in range
            foreach (var enemy in enemiesInRange)
            {
                enemy.ApplySuppression(2);
            }
            
            return true;
        }

        private bool ExecuteInfantryRush(Unit infantry)
        {
            // Ability is passive - it's applied in GetEffectiveMovement()
            // Just mark as used and the extra movement will be available
            return true;
        }

        /// <summary>
        /// Get all valid targets for a unit's ability
        /// </summary>
        public List<HexCoord> GetValidAbilityTargets(Unit unit)
        {
            if (!unit.CanUseAbility())
                return new List<HexCoord>();
                
            var targets = new List<HexCoord>();
            var ability = unit.Ability;
            
            switch (ability.Type)
            {
                case AbilityType.MedicHeal:
                    // Valid targets are friendly units in range that aren't at full health
                    targets = Map.Units.Values
                        .Where(u => u.Team == unit.Team 
                            && u.IsAlive 
                            && u.Position.DistanceTo(unit.Position) <= ability.Range
                            && u.CurrentHp < u.MaxHp)
                        .Select(u => u.Position)
                        .ToList();
                    break;
                    
                case AbilityType.SniperOverwatch:
                    // Overwatch is self-targeted
                    targets.Add(unit.Position);
                    break;
                    
                case AbilityType.HeavySuppression:
                    // If there are enemies in range, target self (area effect)
                    var enemiesInRange = Map.Units.Values
                        .Any(u => u.Team != unit.Team 
                            && u.IsAlive 
                            && u.Position.DistanceTo(unit.Position) <= 2);
                            
                    if (enemiesInRange)
                        targets.Add(unit.Position);
                    break;
                    
                case AbilityType.InfantryRush:
                    // Self-targeted movement boost
                    targets.Add(unit.Position);
                    break;
            }
            
            return targets;
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

            ChangeState(GameState.AITurn);
            var aiUnits = GetTeamUnits(Team.Blue);
            var redUnits = GetTeamUnits(Team.Red);
            
            if (redUnits.Count == 0) return; // Safety check
            
            var isEndgame = redUnits.Count <= 3 || aiUnits.Count <= 3;
            
            // Coordinate focus fire - prioritize targets multiple units can attack
            var focusTargets = GetFocusFireTargets(aiUnits, redUnits);
            
            // Organize units by tactical roles for better coordination
            var tacticalGroups = OrganizeUnitsIntoTacticalGroups(aiUnits);
            
            // Execute actions by priority: Snipers -> Heavy -> Infantry -> Medics
            foreach (var group in tacticalGroups.OrderBy(g => GetGroupPriority(g.Key)))
            {
                foreach (var unit in group.Value.ToList())
                {
                    if (!unit.IsAlive) continue;

                    // Add delay for better visualization
                    await Task.Delay(400);

                    // Phase 1: Consider using abilities first
                    await ExecuteAbilityPhase(unit, isEndgame);

                    // Phase 2: Try to attack (with focus fire coordination)
                    await ExecuteAttackPhase(unit, isEndgame, focusTargets);

                    // Phase 3: Strategic movement (recalculate enemies in case one was eliminated)
                    var currentEnemies = GetTeamUnits(Team.Red);
                    if (currentEnemies.Count > 0)
                    {
                        await ExecuteMovementPhase(unit, currentEnemies, isEndgame);
                    }
                    
                    // Phase 4: Attack again if moved into range
                    if (unit.CanAttack)
                    {
                        await ExecuteAttackPhase(unit, isEndgame, focusTargets);
                    }
                }
            }

            // End AI turn
            await Task.Delay(300);
            ChangeState(GameState.PlayerTurn);
            NextTurn();
        }

        private Dictionary<UnitType, List<Unit>> OrganizeUnitsIntoTacticalGroups(List<Unit> units)
        {
            return units.GroupBy(u => u.UnitType)
                       .ToDictionary(g => g.Key, g => g.ToList());
        }

        private int GetGroupPriority(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Sniper => 1,    // Attack first from range
                UnitType.Heavy => 2,     // Tank damage and attack
                UnitType.Infantry => 3,  // Versatile support
                UnitType.Medic => 4,     // Support and heal
                _ => 5
            };
        }

        private Dictionary<Unit, int> GetFocusFireTargets(List<Unit> aiUnits, List<Unit> enemies)
        {
            var targetPriority = new Dictionary<Unit, int>();
            
            foreach (var enemy in enemies)
            {
                var unitsInRange = aiUnits.Count(ai => ai.Position.DistanceTo(enemy.Position) <= ai.AttackRange && ai.CanAttack);
                targetPriority[enemy] = unitsInRange;
            }
            
            return targetPriority;
        }

        private async Task ExecuteAttackPhase(Unit unit, bool isEndgame, Dictionary<Unit, int>? focusTargets = null)
        {
            var targets = GetAttackTargets(unit);
            if (targets.Count == 0 || !unit.CanAttack) return;

            Unit? target = SelectBestTarget(unit, targets, isEndgame, focusTargets);
            
            if (target != null)
            {
                AttackUnit(unit, target);
                await Task.Delay(1000);
            }
        }

        private Unit SelectBestTarget(Unit attacker, List<Unit> targets, bool isEndgame, Dictionary<Unit, int>? focusTargets = null)
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
                    // Prefer targets that multiple units can focus fire on
                    if (focusTargets != null)
                    {
                        var bestFocusTarget = killableTargets
                            .Where(t => focusTargets.ContainsKey(t))
                            .OrderByDescending(t => focusTargets[t])
                            .ThenByDescending(t => t.AttackPower)
                            .FirstOrDefault();
                        
                        if (bestFocusTarget != null)
                            return bestFocusTarget;
                    }
                    
                    return killableTargets
                        .OrderByDescending(t => t.AttackPower) // Kill strongest attacker first
                        .ThenBy(t => t.CurrentHp)
                        .First();
                }

                // Priority 2: Highest threat units (with focus fire consideration)
                if (focusTargets != null)
                {
                    var focusTarget = targets
                        .Where(t => focusTargets.ContainsKey(t) && focusTargets[t] > 1)
                        .OrderByDescending(t => focusTargets[t])
                        .ThenByDescending(t => CalculateThreatLevel(attacker, t))
                        .FirstOrDefault();
                    
                    if (focusTarget != null)
                        return focusTarget;
                }
                
                return targets
                    .OrderByDescending(t => CalculateThreatLevel(attacker, t))
                    .First();
            }
            else
            {
                // Early/mid game: Balanced targeting with coordination
                if (focusTargets != null)
                {
                    var coordinatedTarget = targets
                        .Where(t => focusTargets.ContainsKey(t) && focusTargets[t] > 1)
                        .OrderByDescending(t => focusTargets[t])
                        .ThenByDescending(t => CalculateTargetPriority(attacker, t))
                        .FirstOrDefault();
                    
                    if (coordinatedTarget != null)
                        return coordinatedTarget;
                }
                
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

            // Line-of-sight and attack opportunity score
            var attackableEnemies = enemies.Count(e => 
                position.DistanceTo(e.Position) <= unit.AttackRange &&
                Map.HasLineOfSight(position, e.Position));
            score += attackableEnemies * (isEndgame ? 100 : 50);

            // Flanking bonus - prefer positions that allow flanking attacks
            var flankingOpportunities = enemies.Count(e => 
                position.DistanceTo(e.Position) <= unit.AttackRange &&
                Map.HasLineOfSight(position, e.Position) &&
                CanFlankFromPosition(position, e));
            score += flankingOpportunities * 25;

            // Defense bonus from terrain
            score += Map.GetDefenseBonus(position);

            // High ground advantage for snipers
            if (unit.UnitType == UnitType.Sniper && Map.Terrain.TryGetValue(position, out var terrain) && terrain == TerrainType.Hill)
            {
                score += 30; // Snipers love high ground
            }

            // Unit type specific positioning
            score += GetUnitTypePositionBonus(unit, position, enemies);

            // Formation and coordination bonus for larger maps
            var friendlyUnits = GetTeamUnits(unit.Team);
            score += GetFormationBonus(unit, position, friendlyUnits);

            // Penalty for being in enemy attack range (but only if they have LOS)
            var enemiesInRange = enemies.Count(e => 
                position.DistanceTo(e.Position) <= e.AttackRange && 
                e.CanAttack &&
                Map.HasLineOfSight(e.Position, position));
            score -= enemiesInRange * (isEndgame ? 30 : 20);

            // Distance consideration (closer is generally better, but not too close for ranged units)
            var closestEnemy = enemies.OrderBy(e => position.DistanceTo(e.Position)).First();
            var distanceToClosest = position.DistanceTo(closestEnemy.Position);
            
            if (unit.UnitType == UnitType.Sniper)
            {
                // Snipers prefer medium range with line of sight
                var idealRange = unit.AttackRange - 1;
                score += Math.Max(0, 20 - Math.Abs(distanceToClosest - idealRange) * 5);
                
                // Extra bonus if position provides LOS to multiple enemies
                var visibleEnemies = enemies.Count(e => Map.HasLineOfSight(position, e.Position));
                score += visibleEnemies * 10;
            }
            else
            {
                // Other units prefer to be closer
                score += Math.Max(0, 30 - distanceToClosest * 3);
            }

            return score;
        }

        private bool CanFlankFromPosition(HexCoord attackerPos, Unit target)
        {
            // Simple flanking check - attacking from sides based on target's facing
            // For AI purposes, assume units face towards the center of enemy forces
            var enemyTeam = target.Team == Team.Red ? Team.Blue : Team.Red;
            var enemyUnits = GetTeamUnits(enemyTeam);
            
            if (!enemyUnits.Any()) return false;
            
            // Calculate average enemy position as "facing direction"
            var avgEnemyQ = enemyUnits.Average(u => u.Position.Q);
            var avgEnemyR = enemyUnits.Average(u => u.Position.R);
            var enemyCenter = new HexCoord((int)Math.Round(avgEnemyQ), (int)Math.Round(avgEnemyR));
            
            return Map.CanFlank(attackerPos, target.Position, enemyCenter);
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

        private double GetFormationBonus(Unit unit, HexCoord position, List<Unit> friendlyUnits)
        {
            var bonus = 0.0;
            var nearbyFriendlies = friendlyUnits
                .Where(f => f != unit && f.IsAlive && position.DistanceTo(f.Position) <= 3)
                .ToList();

            // Bonus for staying near friendlies (mutual support)
            bonus += nearbyFriendlies.Count * 5;

            // Special formations based on unit types
            switch (unit.UnitType)
            {
                case UnitType.Medic:
                    // Medics get bonus for being central to formation
                    var centralBonus = nearbyFriendlies.Count >= 2 ? 15 : 0;
                    bonus += centralBonus;
                    break;

                case UnitType.Heavy:
                    // Heavy units get bonus for being on the front line
                    var frontLineBonus = nearbyFriendlies.Any(f => f.UnitType == UnitType.Infantry) ? 10 : 0;
                    bonus += frontLineBonus;
                    break;

                case UnitType.Sniper:
                    // Snipers prefer positions with support but not crowded
                    if (nearbyFriendlies.Count == 1 || nearbyFriendlies.Count == 2)
                        bonus += 10;
                    else if (nearbyFriendlies.Count > 3)
                        bonus -= 5; // Too crowded for snipers
                    break;

                case UnitType.Infantry:
                    // Infantry are flexible and support others
                    bonus += nearbyFriendlies.Count * 3;
                    break;
            }

            return bonus;
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
            
            // Reset statistics
            TotalAttacks = 0;
            SuccessfulAttacks = 0;
            UnitsKilled = 0;
            RedUnitsLost = 0;
            BlueUnitsLost = 0;
            GameStartTime = DateTime.Now;
            
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
            var gameDuration = DateTime.Now - GameStartTime;

            return new Dictionary<string, object>
            {
                ["TurnNumber"] = TurnNumber,
                ["CurrentTeam"] = CurrentTeam.ToString(),
                ["RedUnitsAlive"] = redUnits.Count,
                ["BlueUnitsAlive"] = blueUnits.Count,
                ["GameOver"] = GameOver,
                ["Winner"] = Winner?.ToString() ?? "None",
                ["MapName"] = Map.MapName,
                ["TotalAttacks"] = TotalAttacks,
                ["SuccessfulAttacks"] = SuccessfulAttacks,
                ["AttackAccuracy"] = TotalAttacks > 0 ? (double)SuccessfulAttacks / TotalAttacks * 100 : 0,
                ["UnitsKilled"] = UnitsKilled,
                ["RedUnitsLost"] = RedUnitsLost,
                ["BlueUnitsLost"] = BlueUnitsLost,
                ["GameDuration"] = gameDuration,
                ["GameStartTime"] = GameStartTime
            };
        }

        /// <summary>
        /// Use a unit's special ability
        /// </summary>
        public bool UseUnitAbility(Unit unit, HexCoord? targetPos = null, Unit? targetUnit = null)
        {
            if (!unit.CanUseAbility()) return false;

            ChangeState(GameState.AbilityPhase);

            switch (unit.Ability.Type)
            {
                case AbilityType.MedicHeal:
                    return UseMedicHeal(unit, targetUnit);
                    
                case AbilityType.SniperOverwatch:
                    return UseSniperOverwatch(unit);
                    
                case AbilityType.HeavySuppression:
                    return UseHeavySuppressionFire(unit, targetPos ?? unit.Position);
                    
                case AbilityType.InfantryRush:
                    return UseInfantryRush(unit);
                    
                default:
                    return false;
            }
        }

        private bool UseMedicHeal(Unit medic, Unit? target)
        {
            if (target == null || target.Team != medic.Team || !target.IsAlive)
                return false;
                
            if (medic.Position.DistanceTo(target.Position) > medic.Ability.Range)
                return false;
                
            target.Heal(30);
            medic.Ability.Use();
            return true;
        }

        private bool UseSniperOverwatch(Unit sniper)
        {
            sniper.IsOnOverwatch = true;
            sniper.Ability.Use();
            return true;
        }

        private bool UseHeavySuppressionFire(Unit heavy, HexCoord targetArea)
        {
            var affectedUnits = Map.Units.Values
                .Where(u => u.Team != heavy.Team && 
                           u.IsAlive && 
                           targetArea.DistanceTo(u.Position) <= 2)
                .ToList();
                
            foreach (var unit in affectedUnits)
            {
                unit.ApplySuppression(2);
            }
            
            heavy.Ability.Use();
            return true;
        }

        private bool UseInfantryRush(Unit infantry)
        {
            // Sprint ability - extra movement is handled in GetEffectiveMovement
            infantry.Ability.Use();
            return true;
        }

        private async Task ExecuteAbilityPhase(Unit unit, bool isEndgame)
        {
            if (!unit.CanUseAbility()) return;

            switch (unit.UnitType)
            {
                case UnitType.Medic:
                    await ConsiderMedicHealing(unit);
                    break;
                    
                case UnitType.Sniper:
                    await ConsiderSniperOverwatch(unit, isEndgame);
                    break;
                    
                case UnitType.Heavy:
                    await ConsiderHeavySuppression(unit);
                    break;
                    
                case UnitType.Infantry:
                    await ConsiderInfantryRush(unit, isEndgame);
                    break;
            }
        }

        private async Task ConsiderMedicHealing(Unit medic)
        {
            var friendlyUnits = GetTeamUnits(medic.Team)
                .Where(u => u != medic && u.IsAlive && u.CurrentHp < u.MaxHp * 0.6f)
                .Where(u => medic.Position.DistanceTo(u.Position) <= medic.Ability.Range)
                .OrderBy(u => (float)u.CurrentHp / u.MaxHp)
                .ToList();

            if (friendlyUnits.Any())
            {
                UseUnitAbility(medic, targetUnit: friendlyUnits.First());
                await Task.Delay(1000);
            }
        }

        private async Task ConsiderSniperOverwatch(Unit sniper, bool isEndgame)
        {
            var enemies = GetTeamUnits(Team.Red);
            var threatsNearby = enemies.Count(e => sniper.Position.DistanceTo(e.Position) <= sniper.AttackRange + 2);
            
            // Use overwatch if enemies are approaching and it's not endgame (need to be aggressive in endgame)
            if (threatsNearby >= 2 && !isEndgame)
            {
                UseUnitAbility(sniper);
                await Task.Delay(800);
            }
        }

        private async Task ConsiderHeavySuppression(Unit heavy)
        {
            var enemies = GetTeamUnits(Team.Red);
            var targetArea = GetBestSuppressionTarget(heavy, enemies);
            
            if (targetArea.HasValue)
            {
                UseUnitAbility(heavy, targetPos: targetArea.Value);
                await Task.Delay(1000);
            }
        }

        private async Task ConsiderInfantryRush(Unit infantry, bool isEndgame)
        {
            // Use sprint in endgame or when need to close distance quickly
            if (isEndgame)
            {
                var enemies = GetTeamUnits(Team.Red);
                var closestEnemy = enemies.OrderBy(e => infantry.Position.DistanceTo(e.Position)).FirstOrDefault();
                
                if (closestEnemy != null && infantry.Position.DistanceTo(closestEnemy.Position) > 4)
                {
                    UseUnitAbility(infantry);
                    await Task.Delay(600);
                }
            }
        }

        private HexCoord? GetBestSuppressionTarget(Unit heavy, List<Unit> enemies)
        {
            var bestScore = 0;
            HexCoord? bestTarget = null;

            foreach (var enemy in enemies)
            {
                if (heavy.Position.DistanceTo(enemy.Position) > heavy.Ability.Range) continue;
                
                var affectedCount = enemies.Count(e => enemy.Position.DistanceTo(e.Position) <= 2);
                if (affectedCount > bestScore)
                {
                    bestScore = affectedCount;
                    bestTarget = enemy.Position;
                }
            }

            return bestScore >= 2 ? bestTarget : null;
        }

        // Add this to your AI turn execution
        private async Task ExecuteAbilityPhase()
        {
            foreach (var unit in GetTeamUnits(Team.Blue).Where(u => u.CanUseAbility()))
            {
                await Task.Delay(400); // Slight delay for visual feedback
                
                switch (unit.UnitType)
                {
                    case UnitType.Medic:
                        ExecuteAIMedicAbility(unit);
                        break;
                        
                    case UnitType.Sniper:
                        ExecuteAISniperAbility(unit);
                        break;
                        
                    case UnitType.Heavy:
                        ExecuteAIHeavyAbility(unit);
                        break;
                        
                    case UnitType.Infantry:
                        ExecuteAIInfantryAbility(unit);
                        break;
                }
            }
        }

        private void ExecuteAIMedicAbility(Unit medic)
        {
            // Find the most wounded friendly unit in range
            var woundedAllies = Map.Units.Values
                .Where(u => u.Team == medic.Team 
                    && u.IsAlive 
                    && u.Position.DistanceTo(medic.Position) <= medic.Ability.Range
                    && u.CurrentHp < u.MaxHp)
                .OrderBy(u => (float)u.CurrentHp / u.MaxHp) // Most wounded first
                .ToList();
                
            if (woundedAllies.Any())
            {
                ExecuteAbility(medic, woundedAllies.First().Position);
            }
        }

        private void ExecuteAISniperAbility(Unit sniper)
        {
            // Use overwatch if enemies are approaching but not yet in attack range
            var nearbyEnemies = Map.Units.Values
                .Where(u => u.Team != sniper.Team && u.IsAlive)
                .ToList();
                
            var enemiesApproaching = nearbyEnemies.Any(e => 
                e.Position.DistanceTo(sniper.Position) > sniper.AttackRange &&
                e.Position.DistanceTo(sniper.Position) <= sniper.AttackRange + 2);
                
            if (enemiesApproaching)
            {
                ExecuteAbility(sniper, sniper.Position);
            }
        }

        private void ExecuteAIHeavyAbility(Unit heavy)
        {
            // Use suppression if multiple enemies are clustered
            var enemyClusters = Map.Units.Values
                .Where(u => u.Team != heavy.Team && u.IsAlive)
                .GroupBy(e => e.Position)
                .Where(g => g.Count() >= 2 || g.Key.DistanceTo(heavy.Position) <= 2)
                .ToList();
                
            if (enemyClusters.Any())
            {
                ExecuteAbility(heavy, heavy.Position);
            }
        }

        private void ExecuteAIInfantryAbility(Unit infantry)
        {
            // Use sprint if there are enemies to chase or objectives to reach
            var nearestEnemy = Map.Units.Values
                .Where(u => u.Team != infantry.Team && u.IsAlive)
                .OrderBy(e => e.Position.DistanceTo(infantry.Position))
                .FirstOrDefault();
                
            if (nearestEnemy != null && 
                nearestEnemy.Position.DistanceTo(infantry.Position) > infantry.AttackRange &&
                nearestEnemy.Position.DistanceTo(infantry.Position) <= infantry.AttackRange + 5)
            {
                ExecuteAbility(infantry, infantry.Position);
            }
        }
    }

    /// <summary>
    /// Holds the result of an ability execution
    /// </summary>
    public class AbilityResult
    {
        public Unit User { get; set; } = null!;
        public AbilityType AbilityType { get; set; }
        public HexCoord TargetCoord { get; set; }
    }
}
