using System.Drawing;

namespace HexWargame.Core
{
    /// <summary>
    /// Represents a hexagonal coordinate using axial coordinate system
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
        /// Get all neighboring hex coordinates
        /// </summary>
        public IEnumerable<HexCoord> GetNeighbors()
        {
            var directions = new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };
            var currentQ = Q;
            var currentR = R;
            return directions.Select(d => new HexCoord(currentQ + d.Item1, currentR + d.Item2));
        }

        /// <summary>
        /// Convert hex coordinate to pixel position for rendering
        /// </summary>
        public PointF ToPixel(float hexSize)
        {
            var x = hexSize * (3.0f / 2.0f * Q);
            var y = hexSize * (Math.Sqrt(3.0f) / 2.0f * Q + Math.Sqrt(3.0f) * R);
            return new PointF((float)x, (float)y);
        }

        /// <summary>
        /// Convert pixel position to hex coordinate
        /// </summary>
        public static HexCoord FromPixel(PointF pixel, float hexSize)
        {
            var q = (2.0f / 3.0f * pixel.X) / hexSize;
            var r = (-1.0f / 3.0f * pixel.X + Math.Sqrt(3.0f) / 3.0f * pixel.Y) / hexSize;
            return HexRound(q, r);
        }

        private static HexCoord HexRound(double q, double r)
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
        Water
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

        public GameMap(int width = 12, int height = 10)
        {
            Width = width;
            Height = height;
            Terrain = new Dictionary<HexCoord, TerrainType>();
            Units = new Dictionary<HexCoord, Unit>();
            GenerateSmallTownMap();
        }

        public void GenerateSmallTownMap()
        {
            // Initialize with open terrain
            for (int q = -Width / 2; q <= Width / 2; q++)
            {
                for (int r = -Height / 2; r <= Height / 2; r++)
                {
                    var coord = new HexCoord(q, r);
                    Terrain[coord] = TerrainType.Open;
                }
            }

            // Add buildings in the center (town square)
            var buildings = new[]
            {
                new HexCoord(-1, 0), new HexCoord(0, 0), new HexCoord(1, 0),
                new HexCoord(-1, 1), new HexCoord(1, -1),
                new HexCoord(0, 2), new HexCoord(0, -2)
            };

            foreach (var building in buildings)
            {
                if (Terrain.ContainsKey(building))
                    Terrain[building] = TerrainType.Building;
            }

            // Add some cover positions
            var coverPositions = new[]
            {
                new HexCoord(-3, 1), new HexCoord(3, -1),
                new HexCoord(-2, -2), new HexCoord(2, 2),
                new HexCoord(-4, 0), new HexCoord(4, 0)
            };

            foreach (var cover in coverPositions)
            {
                if (Terrain.ContainsKey(cover))
                    Terrain[cover] = TerrainType.Cover;
            }

            // Add water obstacle
            var waterPositions = new[]
            {
                new HexCoord(-1, -3), new HexCoord(0, -3), new HexCoord(1, -3)
            };

            foreach (var water in waterPositions)
            {
                if (Terrain.ContainsKey(water))
                    Terrain[water] = TerrainType.Water;
            }
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
                TerrainType.Open => 0,
                TerrainType.Building => 20,
                TerrainType.Cover => 15,
                TerrainType.Water => 0,
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
            _ => Color.White
        };
    }
}
