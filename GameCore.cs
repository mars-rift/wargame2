using System.Drawing;

namespace HexWargame.Core
{
    /// <summary>
    /// Represents a hexagonal coordinate using axial coordinate system (flat-top orientation)
    /// </summary>
    public struct HexCoord : IEquatable<HexCoord>
    {
        public int Q { get; }  // Column
        public int R { get; }  // Row

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        /// <summary>
        /// Calculate hex distance between two coordinates
        /// </summary>
        public int DistanceTo(HexCoord other)
        {
            return (Math.Abs(Q - other.Q) + 
                    Math.Abs(Q + R - other.Q - other.R) + 
                    Math.Abs(R - other.R)) / 2;
        }

        /// <summary>
        /// Get all neighboring hex coordinates (flat-top hexes)
        /// </summary>
        public IEnumerable<HexCoord> GetNeighbors()
        {
            // Flat-top hex neighbors: E, NE, NW, W, SW, SE
            var directions = new[] { (1, 0), (0, -1), (-1, -1), (-1, 0), (-1, 1), (0, 1) };
            var currentQ = Q;
            var currentR = R;
            return directions.Select(d => new HexCoord(currentQ + d.Item1, currentR + d.Item2));
        }

        /// <summary>
        /// Convert hex coordinate to pixel position for rendering (flat-top hexes)
        /// </summary>
        public PointF ToPixel(float hexSize)
        {
            var x = hexSize * (Math.Sqrt(3.0f) * Q + Math.Sqrt(3.0f) / 2.0f * R);
            var y = hexSize * (3.0f / 2.0f * R);
            return new PointF((float)x, (float)y);
        }

        /// <summary>
        /// Convert pixel position to hex coordinate (flat-top hexes)
        /// </summary>
        public static HexCoord FromPixel(PointF pixel, float hexSize)
        {
            var q = (Math.Sqrt(3.0f) / 3.0f * pixel.X - 1.0f / 3.0f * pixel.Y) / hexSize;
            var r = (2.0f / 3.0f * pixel.Y) / hexSize;
            return HexRound(q, r);
        }

        public static HexCoord HexRound(double q, double r)
        {
            var s = -q - r;
            var rq = Math.Round(q);
            var rr = Math.Round(r);
            var rs = Math.Round(s);

            var qDiff = Math.Abs(rq - q);
            var rDiff = Math.Abs(rr - r);
            var sDiff = Math.Abs(rs - s);

            if (qDiff > rDiff && qDiff > sDiff)
                rq = -rr - rs;
            else if (rDiff > sDiff)
                rr = -rq - rs;

            return new HexCoord((int)rq, (int)rr);
        }

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
        public override bool Equals(object? obj) => obj is HexCoord other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Q, R);
        public override string ToString() => $"({Q},{R})";

        public static bool operator ==(HexCoord left, HexCoord right) => left.Equals(right);
        public static bool operator !=(HexCoord left, HexCoord right) => !left.Equals(right);
    }

    public enum UnitType
    {
        Infantry,
        Sniper,
        Heavy,
        Medic
    }

    public enum Team
    {
        Red,
        Blue
    }

    public enum TerrainType
    {
        Open,
        Building,
        Cover,
        Water,
        Forest,     // Provides cover but slows movement
        Hill,       // Provides elevation advantage and good visibility
        Road,       // Allows faster movement
        Ruins       // Partial cover with some defensive bonus
    }

    /// <summary>
    /// Represents a game unit with stats and position
    /// </summary>
    public class Unit
    {
        public UnitType UnitType { get; }
        public Team Team { get; }
        public HexCoord Position { get; set; }
        public int MaxHp { get; }
        public int CurrentHp { get; set; }
        public int Movement { get; }
        public int AttackRange { get; }
        public int AttackPower { get; }
        public bool HasMoved { get; set; }
        public bool HasAttacked { get; set; }
        public UnitAbility Ability { get; }
        
        // Status effects
        public bool IsOnOverwatch { get; set; }
        public bool IsSuppressed { get; set; }
        public int SuppressionTurns { get; set; }

        public Unit(UnitType unitType, Team team, HexCoord position)
        {
            UnitType = unitType;
            Team = team;
            Position = position;
            MaxHp = GetMaxHp(unitType);
            CurrentHp = MaxHp;
            Movement = GetMovement(unitType);
            AttackRange = GetAttackRange(unitType);
            AttackPower = GetAttackPower(unitType);
            Ability = GetUnitAbility(unitType);
        }

        private static int GetMaxHp(UnitType unitType) => unitType switch
        {
            UnitType.Infantry => 100,
            UnitType.Sniper => 80,
            UnitType.Heavy => 150,
            UnitType.Medic => 90,
            _ => 100
        };

        private static int GetMovement(UnitType unitType) => unitType switch
        {
            UnitType.Infantry => 3,
            UnitType.Sniper => 2,
            UnitType.Heavy => 2,
            UnitType.Medic => 3,
            _ => 2
        };

        private static int GetAttackRange(UnitType unitType) => unitType switch
        {
            UnitType.Infantry => 2,
            UnitType.Sniper => 4,
            UnitType.Heavy => 1,
            UnitType.Medic => 1,
            _ => 1
        };

        private static int GetAttackPower(UnitType unitType) => unitType switch
        {
            UnitType.Infantry => 35,
            UnitType.Sniper => 50,
            UnitType.Heavy => 45,
            UnitType.Medic => 20,
            _ => 30
        };

        private static UnitAbility GetUnitAbility(UnitType unitType) => unitType switch
        {
            UnitType.Medic => new UnitAbility(AbilityType.MedicHeal, "Field Medic", 
                "Heal a friendly unit for 30 HP", 2, 2),
            UnitType.Sniper => new UnitAbility(AbilityType.SniperOverwatch, "Overwatch", 
                "Enter overwatch mode - attack enemies that move in range", 0, 3),
            UnitType.Heavy => new UnitAbility(AbilityType.HeavySuppression, "Suppression Fire", 
                "Suppress enemies in a 2-hex radius, reducing their movement", 3, 3),
            UnitType.Infantry => new UnitAbility(AbilityType.InfantryRush, "Sprint", 
                "Move an extra 2 hexes this turn", 0, 4),
            _ => new UnitAbility(AbilityType.None, "None", "No special ability", 0, 0)
        };

        public bool IsAlive => CurrentHp > 0;
        public bool CanMove => !HasMoved && IsAlive;
        public bool CanAttack => !HasAttacked && IsAlive;

        public void TakeDamage(int damage)
        {
            CurrentHp = Math.Max(0, CurrentHp - damage);
        }

        public void Heal(int amount)
        {
            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
        }

        public void ResetTurn()
        {
            HasMoved = false;
            HasAttacked = false;
            Ability.ReduceCooldown();
            
            // Update status effects
            if (SuppressionTurns > 0)
            {
                SuppressionTurns--;
                if (SuppressionTurns <= 0)
                    IsSuppressed = false;
            }
        }

        /// <summary>
        /// Apply suppression effect to this unit
        /// </summary>
        public void ApplySuppression(int turns = 2)
        {
            IsSuppressed = true;
            SuppressionTurns = Math.Max(SuppressionTurns, turns);
        }

        /// <summary>
        /// Check if unit can use its ability
        /// </summary>
        public bool CanUseAbility()
        {
            return IsAlive && Ability.IsAvailable && !IsSuppressed;
        }

        /// <summary>
        /// Get effective movement considering status effects
        /// </summary>
        public int GetEffectiveMovement()
        {
            int baseMovement = Movement;
            
            // Suppression reduces movement
            if (IsSuppressed)
                baseMovement = Math.Max(1, baseMovement - 1);
                
            // Infantry sprint ability
            if (UnitType == UnitType.Infantry && Ability.Type == AbilityType.InfantryRush 
                && Ability.CurrentCooldown == Ability.Cooldown) // Just used
                baseMovement += 2;
                
            return baseMovement;
        }

        /// <summary>
        /// Get the color for this unit based on team
        /// </summary>
        public Color GetTeamColor() => Team switch
        {
            Team.Red => Color.Red,
            Team.Blue => Color.Blue,
            _ => Color.Gray
        };

        /// <summary>
        /// Get display character for this unit type
        /// </summary>
        public char GetUnitChar() => UnitType switch
        {
            UnitType.Infantry => 'I',
            UnitType.Sniper => 'S',
            UnitType.Heavy => 'H',
            UnitType.Medic => 'M',
            _ => '?'
        };
    }

    /// <summary>
    /// Represents the game map with terrain and units
    /// </summary>
    public class GameMap
    {
        public int Width { get; }
        public int Height { get; }
        public Dictionary<HexCoord, TerrainType> Terrain { get; }
        public Dictionary<HexCoord, Unit> Units { get; }
        public string MapName { get; private set; } = "Unknown";

        private readonly Random _random;

        public GameMap(int width = 30, int height = 20, int? seed = null)
        {
            Width = width;
            Height = height;
            Terrain = new Dictionary<HexCoord, TerrainType>();
            Units = new Dictionary<HexCoord, Unit>();
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            GenerateRandomMap();
        }

        public void GenerateRandomMap()
        {
            // Choose a random map layout
            var mapLayouts = new Action[]
            {
                GenerateSmallTownMap,
                GenerateForestBattlefield,
                GenerateUrbanWarfare,
                GenerateRiverCrossing,
                GenerateHillsAndValleys
            };

            var selectedLayout = mapLayouts[_random.Next(mapLayouts.Length)];
            selectedLayout();
        }

        public void GenerateSmallTownMap()
        {
            MapName = "Small Town";
            
            // Initialize with open terrain
            InitializeOpenTerrain();

            // Add buildings in the center (town square) - much larger town
            var buildings = new[]
            {
                // Central town square
                new HexCoord(-1, 0), new HexCoord(0, 0), new HexCoord(1, 0),
                new HexCoord(-1, 1), new HexCoord(1, -1),
                new HexCoord(0, 2), new HexCoord(0, -2),
                new HexCoord(-2, -1), new HexCoord(2, 1),
                new HexCoord(-1, -2), new HexCoord(1, 2),
                // Additional buildings spread out
                new HexCoord(-3, 0), new HexCoord(3, 0),
                new HexCoord(-2, 2), new HexCoord(2, -2),
                new HexCoord(-4, 1), new HexCoord(4, -1),
                new HexCoord(-3, -3), new HexCoord(3, 3),
                new HexCoord(-5, 0), new HexCoord(5, 0),
                new HexCoord(0, -5), new HexCoord(0, 5)
            };

            foreach (var building in buildings)
            {
                if (Terrain.ContainsKey(building))
                    Terrain[building] = TerrainType.Building;
            }

            // Add much more cover positions spread throughout the larger map
            var coverPositions = new[]
            {
                new HexCoord(-3, 1), new HexCoord(3, -1),
                new HexCoord(-2, -2), new HexCoord(2, 2),
                new HexCoord(-4, 0), new HexCoord(4, 0),
                new HexCoord(-5, -1), new HexCoord(5, 1),
                new HexCoord(-3, 3), new HexCoord(3, -3),
                new HexCoord(-6, 0), new HexCoord(6, 0),
                new HexCoord(0, -4), new HexCoord(0, 4),
                // Additional cover for larger map
                new HexCoord(-7, 2), new HexCoord(7, -2),
                new HexCoord(-6, -3), new HexCoord(6, 3),
                new HexCoord(-8, 0), new HexCoord(8, 0),
                new HexCoord(-4, 4), new HexCoord(4, -4),
                new HexCoord(-5, -5), new HexCoord(5, 5),
                new HexCoord(-2, 6), new HexCoord(2, -6),
                new HexCoord(-9, 1), new HexCoord(9, -1),
                new HexCoord(-1, 7), new HexCoord(1, -7)
            };

            foreach (var cover in coverPositions)
            {
                if (Terrain.ContainsKey(cover))
                    Terrain[cover] = TerrainType.Cover;
            }

            // Add water obstacle - extended but away from spawn areas
            var waterPositions = new[]
            {
                new HexCoord(-2, -3), new HexCoord(-1, -3), new HexCoord(0, -3), 
                new HexCoord(1, -3), new HexCoord(2, -3)
            };

            foreach (var water in waterPositions)
            {
                if (Terrain.ContainsKey(water))
                    Terrain[water] = TerrainType.Water;
            }
        }

        public void GenerateForestBattlefield()
        {
            MapName = "Deep Forest";
            InitializeOpenTerrain();

            // Dense forest areas - much more for larger map
            var forestCount = _random.Next(40, 60);
            var placedTerrain = new HashSet<HexCoord>();

            // Place forest clusters
            for (int i = 0; i < forestCount; i++)
            {
                var attempts = 0;
                HexCoord pos;
                do
                {
                    var q = _random.Next(-Width / 2, Width / 2 + 1);
                    var r = _random.Next(-Height / 2, Height / 2 + 1);
                    pos = new HexCoord(q, r);
                    attempts++;
                } while ((placedTerrain.Contains(pos) || !Terrain.ContainsKey(pos) || 
                         IsNearSpawnArea(pos)) && attempts < 50);

                if (attempts < 50 && Terrain.ContainsKey(pos))
                {
                    Terrain[pos] = TerrainType.Forest;
                    placedTerrain.Add(pos);
                }
            }

            // Scattered natural cover (bushes, rocks)
            var coverCount = _random.Next(25, 35);
            for (int i = 0; i < coverCount; i++)
            {
                var attempts = 0;
                HexCoord pos;
                do
                {
                    var q = _random.Next(-Width / 2, Width / 2 + 1);
                    var r = _random.Next(-Height / 2, Height / 2 + 1);
                    pos = new HexCoord(q, r);
                    attempts++;
                } while ((placedTerrain.Contains(pos) || !Terrain.ContainsKey(pos) || 
                         IsNearSpawnArea(pos)) && attempts < 50);

                if (attempts < 50 && Terrain.ContainsKey(pos))
                {
                    Terrain[pos] = TerrainType.Cover;
                    placedTerrain.Add(pos);
                }
            }

            // Add some hills for elevation
            var hillCount = _random.Next(8, 12);
            for (int i = 0; i < hillCount; i++)
            {
                var attempts = 0;
                HexCoord pos;
                do
                {
                    var q = _random.Next(-Width / 2, Width / 2 + 1);
                    var r = _random.Next(-Height / 2, Height / 2 + 1);
                    pos = new HexCoord(q, r);
                    attempts++;
                } while ((placedTerrain.Contains(pos) || !Terrain.ContainsKey(pos) || 
                         IsNearSpawnArea(pos)) && attempts < 50);

                if (attempts < 50 && Terrain.ContainsKey(pos))
                {
                    Terrain[pos] = TerrainType.Hill;
                    placedTerrain.Add(pos);
                }
            }

            // Forest stream
            var centerWater = new[]
            {
                new HexCoord(-1, 0), new HexCoord(0, 0), new HexCoord(1, 0),
                new HexCoord(0, 1), new HexCoord(0, -1), new HexCoord(-2, 0), new HexCoord(2, 0)
            };

            foreach (var water in centerWater)
            {
                if (Terrain.ContainsKey(water) && !placedTerrain.Contains(water))
                {
                    Terrain[water] = TerrainType.Water;
                    placedTerrain.Add(water);
                }
            }
        }

        public void GenerateUrbanWarfare()
        {
            MapName = "Urban Ruins";
            InitializeOpenTerrain();

            // Multiple building clusters - greatly expanded for larger map
            var buildingClusters = new[]
            {
                // Original clusters
                new[] { new HexCoord(-2, -1), new HexCoord(-1, -1), new HexCoord(-2, 0) },
                new[] { new HexCoord(2, 1), new HexCoord(1, 1), new HexCoord(2, 0) },
                new[] { new HexCoord(-1, 2), new HexCoord(0, 2), new HexCoord(1, 2) },
                new[] { new HexCoord(-1, -2), new HexCoord(0, -2), new HexCoord(1, -2) },
                // Additional clusters for larger map
                new[] { new HexCoord(-4, -2), new HexCoord(-3, -2), new HexCoord(-4, -1) },
                new[] { new HexCoord(4, 2), new HexCoord(3, 2), new HexCoord(4, 1) },
                new[] { new HexCoord(-5, 1), new HexCoord(-4, 1), new HexCoord(-5, 0) },
                new[] { new HexCoord(5, -1), new HexCoord(4, -1), new HexCoord(5, 0) },
                // More clusters to fill the 30x20 map
                new[] { new HexCoord(-7, 3), new HexCoord(-6, 3), new HexCoord(-7, 2) },
                new[] { new HexCoord(7, -3), new HexCoord(6, -3), new HexCoord(7, -2) },
                new[] { new HexCoord(-3, 5), new HexCoord(-2, 5), new HexCoord(-3, 4) },
                new[] { new HexCoord(3, -5), new HexCoord(2, -5), new HexCoord(3, -4) },
                new[] { new HexCoord(-8, 0), new HexCoord(-7, 0), new HexCoord(-8, 1) },
                new[] { new HexCoord(8, 0), new HexCoord(7, 0), new HexCoord(8, -1) },
                new[] { new HexCoord(0, 6), new HexCoord(1, 6), new HexCoord(0, 5) },
                new[] { new HexCoord(0, -6), new HexCoord(-1, -6), new HexCoord(0, -5) }
            };

            foreach (var cluster in buildingClusters)
            {
                foreach (var building in cluster)
                {
                    if (Terrain.ContainsKey(building))
                    {
                        // Mix of intact buildings and ruins
                        Terrain[building] = _random.Next(100) < 70 ? TerrainType.Building : TerrainType.Ruins;
                    }
                }
            }

            // Add roads connecting building clusters
            var roadPositions = new[]
            {
                // Main cross roads
                new HexCoord(0, -3), new HexCoord(0, -2), new HexCoord(0, -1), 
                new HexCoord(0, 1), new HexCoord(0, 3),
                new HexCoord(-3, 0), new HexCoord(-2, 0), new HexCoord(-1, 0), 
                new HexCoord(1, 0), new HexCoord(2, 0), new HexCoord(3, 0),
                // Additional connecting roads
                new HexCoord(-4, -3), new HexCoord(-3, -3), new HexCoord(3, 3), new HexCoord(4, 3),
                new HexCoord(-6, -1), new HexCoord(-5, -1), new HexCoord(5, 1), new HexCoord(6, 1)
            };

            foreach (var road in roadPositions)
            {
                if (Terrain.ContainsKey(road) && Terrain[road] == TerrainType.Open)
                    Terrain[road] = TerrainType.Road;
            }

            // Random rubble and ruins scattered throughout
            var debrisCount = _random.Next(20, 30);
            var placedDebris = new HashSet<HexCoord>();

            for (int i = 0; i < debrisCount; i++)
            {
                var attempts = 0;
                HexCoord pos;
                do
                {
                    var q = _random.Next(-Width / 2, Width / 2 + 1);
                    var r = _random.Next(-Height / 2, Height / 2 + 1);
                    pos = new HexCoord(q, r);
                    attempts++;
                } while ((placedDebris.Contains(pos) || !Terrain.ContainsKey(pos) || 
                         Terrain[pos] != TerrainType.Open || IsNearSpawnArea(pos)) && attempts < 50);

                if (attempts < 50 && Terrain.ContainsKey(pos))
                {
                    Terrain[pos] = _random.Next(100) < 60 ? TerrainType.Cover : TerrainType.Ruins;
                    placedDebris.Add(pos);
                }
            }
        }

        public void GenerateRiverCrossing()
        {
            MapName = "River Crossing";
            InitializeOpenTerrain();

            // River running through the middle (3 rows wide)
            for (int q = -Width / 2; q <= Width / 2; q++)
            {
                var riverCoords = new[]
                {
                    new HexCoord(q, -1),
                    new HexCoord(q, 0),
                    new HexCoord(q, 1)
                };

                foreach (var coord in riverCoords)
                {
                    if (Terrain.ContainsKey(coord))
                        Terrain[coord] = TerrainType.Water;
                }
            }

            // Bridges (clear passages through water) - strategically placed
            var bridges = new[]
            {
                new HexCoord(-2, -1), new HexCoord(-2, 0), new HexCoord(-2, 1), // Left bridge
                new HexCoord(2, -1), new HexCoord(2, 0), new HexCoord(2, 1)     // Right bridge
            };

            foreach (var bridge in bridges)
            {
                if (Terrain.ContainsKey(bridge))
                    Terrain[bridge] = TerrainType.Open;
            }

            // Cover positions near river banks for tactical positioning
            var coverPositions = new[]
            {
                new HexCoord(-3, -2), new HexCoord(3, 2), new HexCoord(-1, -3),
                new HexCoord(1, 3), new HexCoord(-4, -1), new HexCoord(4, 1),
                new HexCoord(0, -3), new HexCoord(0, 3), new HexCoord(-3, 0),
                new HexCoord(3, 0), new HexCoord(-1, 2), new HexCoord(1, -2)
            };

            foreach (var cover in coverPositions)
            {
                if (Terrain.ContainsKey(cover))
                    Terrain[cover] = TerrainType.Cover;
            }

            // Add a few buildings near bridges for strategic importance
            var bridgeBuildings = new[]
            {
                new HexCoord(-3, 1), new HexCoord(3, -1)
            };

            foreach (var building in bridgeBuildings)
            {
                if (Terrain.ContainsKey(building))
                    Terrain[building] = TerrainType.Building;
            }
        }

        public void GenerateHillsAndValleys()
        {
            MapName = "Rocky Hills";
            InitializeOpenTerrain();

            // Central valley with cover on hills - expanded for larger map
            var hillPositions = new[]
            {
                new HexCoord(-3, -2), new HexCoord(-2, -3), new HexCoord(-1, -3),
                new HexCoord(3, 2), new HexCoord(2, 3), new HexCoord(1, 3),
                new HexCoord(-4, 1), new HexCoord(-3, 2), new HexCoord(4, -1),
                new HexCoord(3, -2), new HexCoord(-2, 1), new HexCoord(2, -1),
                // Additional hills for larger map
                new HexCoord(-6, -1), new HexCoord(-5, -2), new HexCoord(-4, -3),
                new HexCoord(6, 1), new HexCoord(5, 2), new HexCoord(4, 3),
                new HexCoord(-7, 0), new HexCoord(-6, 1), new HexCoord(-5, 2),
                new HexCoord(7, 0), new HexCoord(6, -1), new HexCoord(5, -2),
                new HexCoord(-3, -4), new HexCoord(3, 4), new HexCoord(-4, 3),
                new HexCoord(4, -3), new HexCoord(-8, 1), new HexCoord(8, -1)
            };

            foreach (var hill in hillPositions)
            {
                if (Terrain.ContainsKey(hill))
                    Terrain[hill] = TerrainType.Cover;
            }

            // Small building outpost - more spread out
            var outposts = new[]
            {
                new HexCoord(-1, 0), new HexCoord(1, 0),
                new HexCoord(-3, 0), new HexCoord(3, 0)  // Additional outposts
            };

            foreach (var outpost in outposts)
            {
                if (Terrain.ContainsKey(outpost))
                    Terrain[outpost] = TerrainType.Building;
            }

            // Mountain lake - larger
            var lakePositions = new[]
            {
                new HexCoord(0, -1), new HexCoord(0, 1),
                new HexCoord(-1, 1), new HexCoord(1, -1)  // Expanded lake
            };

            foreach (var lake in lakePositions)
            {
                if (Terrain.ContainsKey(lake))
                    Terrain[lake] = TerrainType.Water;
            }
        }

        private void InitializeOpenTerrain()
        {
            Terrain.Clear();
            for (int q = -Width / 2; q <= Width / 2; q++)
            {
                for (int r = -Height / 2; r <= Height / 2; r++)
                {
                    var coord = new HexCoord(q, r);
                    Terrain[coord] = TerrainType.Open;
                }
            }
        }

        private bool IsNearSpawnArea(HexCoord coord)
        {
            // Keep spawn areas clear (top and bottom of map)
            return Math.Abs(coord.R) >= Height / 2 - 1;
        }

        public bool IsValidPosition(HexCoord coord) => Terrain.ContainsKey(coord);

        public bool IsPassable(HexCoord coord)
        {
            if (!IsValidPosition(coord)) return false;
            if (Units.ContainsKey(coord)) return false;
            return Terrain[coord] != TerrainType.Water;
        }

        public bool PlaceUnit(Unit unit, HexCoord coord)
        {
            if (!IsPassable(coord)) return false;

            if (Units.ContainsKey(unit.Position))
                Units.Remove(unit.Position);

            Units[coord] = unit;
            unit.Position = coord;
            return true;
        }

        public void RemoveUnit(HexCoord coord)
        {
            Units.Remove(coord);
        }

        public Unit? GetUnitAt(HexCoord coord)
        {
            return Units.TryGetValue(coord, out var unit) ? unit : null;
        }

        public int GetDefenseBonus(HexCoord coord)
        {
            if (!Terrain.TryGetValue(coord, out var terrain))
                return 0;

            return terrain switch
            {
                TerrainType.Building => 20,    // Strong cover
                TerrainType.Cover => 15,       // Good cover
                TerrainType.Forest => 12,      // Natural cover
                TerrainType.Hill => 10,        // Elevation advantage
                TerrainType.Ruins => 8,        // Partial cover
                TerrainType.Water => -10,      // Vulnerable position
                TerrainType.Road => 0,         // No defensive value
                TerrainType.Open => 0,         // No cover
                _ => 0
            };
        }

        /// <summary>
        /// Get color for terrain type
        /// </summary>
        public Color GetTerrainColor(TerrainType terrain) => terrain switch
        {
            TerrainType.Open => Color.LightGreen,
            TerrainType.Building => Color.Gray,
            TerrainType.Cover => Color.SaddleBrown,
            TerrainType.Water => Color.LightBlue,
            TerrainType.Forest => Color.DarkGreen,
            TerrainType.Hill => Color.Tan,
            TerrainType.Road => Color.DarkGray,
            TerrainType.Ruins => Color.DimGray,
            _ => Color.White
        };

        /// <summary>
        /// Get movement cost for terrain type
        /// </summary>
        public int GetMovementCost(TerrainType terrain) => terrain switch
        {
            TerrainType.Road => 1,        // Fast movement
            TerrainType.Open => 1,        // Normal movement
            TerrainType.Hill => 2,        // Slower uphill
            TerrainType.Cover => 1,       // Normal through cover
            TerrainType.Forest => 2,      // Slower through trees
            TerrainType.Ruins => 1,       // Normal through ruins
            TerrainType.Building => 1,    // Normal through buildings
            TerrainType.Water => 999,     // Impassable
            _ => 1
        };

        /// <summary>
        /// Check if there's a clear line of sight between two positions
        /// </summary>
        public bool HasLineOfSight(HexCoord from, HexCoord to)
        {
            if (from == to) return true;
            
            var distance = from.DistanceTo(to);
            if (distance <= 1) return true; // Adjacent hexes always have LOS
            
            // Use hex line algorithm to check intermediate hexes
            var line = GetHexLine(from, to);
            
            foreach (var hex in line.Skip(1).Take(line.Count - 2)) // Skip start and end
            {
                if (!Terrain.TryGetValue(hex, out var terrain)) continue;
                
                // Blocking terrain types
                if (terrain == TerrainType.Building || terrain == TerrainType.Forest)
                    return false;
                    
                // Units block line of sight
                if (Units.ContainsKey(hex))
                    return false;
            }
            
            return true;
        }

        /// <summary>
        /// Get hex line between two coordinates using linear interpolation
        /// </summary>
        private List<HexCoord> GetHexLine(HexCoord from, HexCoord to)
        {
            var distance = from.DistanceTo(to);
            var results = new List<HexCoord>();
            
            if (distance == 0)
            {
                results.Add(from);
                return results;
            }
            
            for (int i = 0; i <= distance; i++)
            {
                var t = i / (float)distance;
                var lerpQ = from.Q + (to.Q - from.Q) * t;
                var lerpR = from.R + (to.R - from.R) * t;
                
                results.Add(HexCoord.HexRound(lerpQ, lerpR));
            }
            
            return results;
        }

        /// <summary>
        /// Find optimal path using A* algorithm considering terrain costs
        /// </summary>
        public List<HexCoord> FindOptimalPath(HexCoord start, HexCoord goal, int maxMovement)
        {
            var openSet = new SortedSet<PathNode>(new PathNodeComparer());
            var closedSet = new HashSet<HexCoord>();
            var cameFrom = new Dictionary<HexCoord, HexCoord>();
            var gScore = new Dictionary<HexCoord, int> { [start] = 0 };
            var fScore = new Dictionary<HexCoord, int> { [start] = start.DistanceTo(goal) };
            
            openSet.Add(new PathNode(start, fScore[start]));
            
            while (openSet.Count > 0)
            {
                var currentNode = openSet.Min!;
                var current = currentNode.Coord;
                openSet.Remove(currentNode);
                
                if (current == goal)
                {
                    return ReconstructPath(cameFrom, current);
                }
                
                closedSet.Add(current);
                
                foreach (var neighbor in current.GetNeighbors())
                {
                    if (!IsPassable(neighbor) || closedSet.Contains(neighbor))
                        continue;
                    
                    var terrainCost = Terrain.TryGetValue(neighbor, out var terrain) 
                        ? GetMovementCost(terrain) 
                        : 1;
                    var tentativeGScore = gScore[current] + terrainCost;
                    
                    // Don't exceed movement range
                    if (tentativeGScore > maxMovement)
                        continue;
                    
                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + neighbor.DistanceTo(goal);
                        
                        var neighborNode = new PathNode(neighbor, fScore[neighbor]);
                        if (!openSet.Contains(neighborNode))
                        {
                            openSet.Add(neighborNode);
                        }
                    }
                }
            }
            
            return new List<HexCoord>(); // No path found
        }

        private List<HexCoord> ReconstructPath(Dictionary<HexCoord, HexCoord> cameFrom, HexCoord current)
        {
            var path = new List<HexCoord> { current };
            
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            
            return path;
        }

        /// <summary>
        /// Check if a unit can flank another unit (attack from sides/rear)
        /// </summary>
        public bool CanFlank(HexCoord attackerPos, HexCoord targetPos, HexCoord targetFacing)
        {
            // Simplified flanking: attacking from sides or rear relative to target's last movement
            var attackVector = new HexCoord(targetPos.Q - attackerPos.Q, targetPos.R - attackerPos.R);
            var facingVector = new HexCoord(targetFacing.Q - targetPos.Q, targetFacing.R - targetPos.R);
            
            // If vectors are opposite or perpendicular, it's a flank
            var dotProduct = attackVector.Q * facingVector.Q + attackVector.R * facingVector.R;
            return dotProduct <= 0; // Side or rear attack
        }
    }

    /// <summary>
    /// Represents the current phase of the game
    /// </summary>
    public enum GameState
    {
        PlayerTurn,     // Player is actively taking actions
        AITurn,         // AI is processing its turn
        Animating,      // Animations or effects are playing
        GameOver,       // Game has ended
        UnitSelection,  // Player is selecting a unit
        MovementPhase,  // Unit movement is being executed
        AttackPhase,    // Attack is being executed
        AbilityPhase    // Special ability is being used
    }

    /// <summary>
    /// Represents different unit abilities
    /// </summary>
    public enum AbilityType
    {
        None,
        MedicHeal,      // Medic healing ability
        SniperOverwatch, // Sniper overwatch mode
        HeavySuppression, // Heavy suppression fire
        InfantryRush    // Infantry sprint ability
    }

    /// <summary>
    /// Represents a unit's special ability
    /// </summary>
    public class UnitAbility
    {
        public AbilityType Type { get; }
        public string Name { get; }
        public string Description { get; }
        public int Range { get; }
        public int Cooldown { get; }
        public int CurrentCooldown { get; set; }
        public bool IsAvailable => CurrentCooldown <= 0;

        public UnitAbility(AbilityType type, string name, string description, int range, int cooldown)
        {
            Type = type;
            Name = name;
            Description = description;
            Range = range;
            Cooldown = cooldown;
            CurrentCooldown = 0;
        }

        public void Use()
        {
            CurrentCooldown = Cooldown;
        }

        public void ReduceCooldown()
        {
            if (CurrentCooldown > 0)
                CurrentCooldown--;
        }
    }

    /// <summary>
    /// Node for A* pathfinding algorithm
    /// </summary>
    public class PathNode
    {
        public HexCoord Coord { get; }
        public int FScore { get; }

        public PathNode(HexCoord coord, int fScore)
        {
            Coord = coord;
            FScore = fScore;
        }

        public override bool Equals(object? obj)
        {
            return obj is PathNode other && Coord.Equals(other.Coord);
        }

        public override int GetHashCode()
        {
            return Coord.GetHashCode();
        }
    }

    /// <summary>
    /// Comparer for PathNode to use in SortedSet for A*
    /// </summary>
    public class PathNodeComparer : IComparer<PathNode>
    {
        public int Compare(PathNode? x, PathNode? y)
        {
            if (x == null || y == null) return 0;
            
            var result = x.FScore.CompareTo(y.FScore);
            if (result == 0)
            {
                // If F-scores are equal, compare coordinates to maintain uniqueness
                result = x.Coord.Q.CompareTo(y.Coord.Q);
                if (result == 0)
                    result = x.Coord.R.CompareTo(y.Coord.R);
            }
            return result;
        }
    }
}
