using System.Drawing;
using System.Drawing.Drawing2D;
using HexWargame.Core;

namespace HexWargame.UI
{
    /// <summary>
    /// Custom control for rendering and interacting with hex grid
    /// </summary>
    public partial class HexGridControl : UserControl
    {
        private Game? _game;
        private Unit? _selectedUnit;
        private List<HexCoord> _validMoves = new();
        private List<Unit> _validTargets = new();
        
        // Rendering settings
        private const float HexSize = 30f;
        private const float HexWidth = HexSize * 2f;
        private static readonly float HexHeight = (float)(Math.Sqrt(3) * HexSize);
        
        // Colors
        private readonly Color _gridColor = Color.Black;
        private readonly Color _selectedColor = Color.Yellow;
        private readonly Color _validMoveColor = Color.LightGreen;
        private readonly Color _validAttackColor = Color.LightCoral;
        
        // Center offset for rendering
        private PointF _offset = new(200, 200);

        public Game? Game
        {
            get => _game;
            set
            {
                if (_game != null)
                {
                    // Unsubscribe from old game events
                    _game.UnitMoved -= OnUnitMoved;
                    _game.AttackExecuted -= OnAttackExecuted;
                    _game.TeamChanged -= OnTeamChanged;
                }
                
                _game = value;
                
                if (_game != null)
                {
                    // Subscribe to new game events
                    _game.UnitMoved += OnUnitMoved;
                    _game.AttackExecuted += OnAttackExecuted;
                    _game.TeamChanged += OnTeamChanged;
                }
                
                ClearSelection();
                Invalidate();
            }
        }

        public Unit? SelectedUnit
        {
            get => _selectedUnit;
            private set
            {
                _selectedUnit = value;
                UpdateValidActions();
                Invalidate();
            }
        }

        public event EventHandler<Unit>? UnitSelected;
        public event EventHandler<HexCoord>? HexClicked;

        public HexGridControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint | 
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // HexGridControl
            // 
            BackColor = Color.White;
            Name = "HexGridControl";
            Size = new Size(600, 500);
            MouseClick += HexGridControl_MouseClick;
            ResumeLayout(false);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (_game?.Map == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw terrain hexagons
            DrawTerrain(g);
            
            // Draw valid moves and attacks
            DrawValidActions(g);
            
            // Draw units
            DrawUnits(g);
            
            // Draw grid lines
            DrawGrid(g);
        }

        private void DrawTerrain(Graphics g)
        {
            if (_game?.Map == null) return;

            foreach (var kvp in _game.Map.Terrain)
            {
                var coord = kvp.Key;
                var terrain = kvp.Value;
                var center = GetHexCenter(coord);
                var hexPath = CreateHexPath(center);
                
                using var brush = new SolidBrush(_game.Map.GetTerrainColor(terrain));
                g.FillPath(brush, hexPath);
            }
        }

        private void DrawValidActions(Graphics g)
        {
            // Draw valid moves
            foreach (var coord in _validMoves)
            {
                var center = GetHexCenter(coord);
                var hexPath = CreateHexPath(center);
                
                using var brush = new SolidBrush(Color.FromArgb(128, _validMoveColor));
                g.FillPath(brush, hexPath);
            }

            // Draw valid attack targets
            foreach (var target in _validTargets)
            {
                var center = GetHexCenter(target.Position);
                var hexPath = CreateHexPath(center);
                
                using var brush = new SolidBrush(Color.FromArgb(128, _validAttackColor));
                g.FillPath(brush, hexPath);
            }
        }

        private void DrawUnits(Graphics g)
        {
            if (_game?.Map == null) return;

            foreach (var unit in _game.Map.Units.Values)
            {
                if (!unit.IsAlive) continue;

                var center = GetHexCenter(unit.Position);
                var unitRect = new RectangleF(
                    center.X - HexSize * 0.6f, 
                    center.Y - HexSize * 0.6f,
                    HexSize * 1.2f, 
                    HexSize * 1.2f);

                // Draw unit background
                var isSelected = unit == _selectedUnit;
                var backgroundColor = isSelected ? _selectedColor : unit.GetTeamColor();
                
                using (var brush = new SolidBrush(Color.FromArgb(isSelected ? 255 : 180, backgroundColor)))
                {
                    g.FillEllipse(brush, unitRect);
                }

                // Draw unit border
                using (var pen = new Pen(Color.Black, isSelected ? 3f : 1f))
                {
                    g.DrawEllipse(pen, unitRect);
                }

                // Draw unit type character
                var unitChar = unit.GetUnitChar().ToString();
                using (var font = new Font("Arial", 14, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    var textSize = g.MeasureString(unitChar, font);
                    var textPos = new PointF(
                        center.X - textSize.Width / 2,
                        center.Y - textSize.Height / 2);
                    g.DrawString(unitChar, font, brush, textPos);
                }

                // Draw health bar
                DrawHealthBar(g, unit, center);
            }
        }

        private void DrawHealthBar(Graphics g, Unit unit, PointF center)
        {
            var barWidth = HexSize * 1.2f;
            var barHeight = 4f;
            var barY = center.Y + HexSize * 0.8f;
            
            var healthPercent = (float)unit.CurrentHp / unit.MaxHp;
            var healthColor = healthPercent > 0.6f ? Color.Green :
                            healthPercent > 0.3f ? Color.Yellow : Color.Red;

            // Background
            using (var brush = new SolidBrush(Color.Gray))
            {
                g.FillRectangle(brush, center.X - barWidth / 2, barY, barWidth, barHeight);
            }

            // Health
            using (var brush = new SolidBrush(healthColor))
            {
                g.FillRectangle(brush, center.X - barWidth / 2, barY, barWidth * healthPercent, barHeight);
            }
        }

        private void DrawGrid(Graphics g)
        {
            if (_game?.Map == null) return;

            using var pen = new Pen(_gridColor, 1f);
            
            foreach (var coord in _game.Map.Terrain.Keys)
            {
                var center = GetHexCenter(coord);
                var hexPath = CreateHexPath(center);
                g.DrawPath(pen, hexPath);
            }
        }

        private PointF GetHexCenter(HexCoord coord)
        {
            var pixel = coord.ToPixel(HexSize);
            return new PointF(pixel.X + _offset.X, pixel.Y + _offset.Y);
        }

        private GraphicsPath CreateHexPath(PointF center)
        {
            var path = new GraphicsPath();
            var points = new PointF[6];
            
            for (int i = 0; i < 6; i++)
            {
                var angle = Math.PI / 3 * i;
                points[i] = new PointF(
                    center.X + (float)(HexSize * Math.Cos(angle)),
                    center.Y + (float)(HexSize * Math.Sin(angle)));
            }
            
            path.AddPolygon(points);
            return path;
        }

        private HexCoord? GetHexAt(Point mousePos)
        {
            if (_game?.Map == null) return null;

            var adjustedPos = new PointF(mousePos.X - _offset.X, mousePos.Y - _offset.Y);
            var coord = HexCoord.FromPixel(adjustedPos, HexSize);
            
            return _game.Map.IsValidPosition(coord) ? coord : null;
        }

        private void HexGridControl_MouseClick(object? sender, MouseEventArgs e)
        {
            if (_game == null || _game.GameOver) return;

            var clickedHex = GetHexAt(e.Location);
            if (!clickedHex.HasValue) return;

            var coord = clickedHex.Value;
            
            // If we have a selected unit and clicked on a valid move
            if (_selectedUnit != null && _validMoves.Contains(coord))
            {
                _game.MoveUnit(_selectedUnit, coord);
                return;
            }

            // If we have a selected unit and clicked on a valid target
            if (_selectedUnit != null)
            {
                var target = _validTargets.FirstOrDefault(t => t.Position == coord);
                if (target != null)
                {
                    _game.AttackUnit(_selectedUnit, target);
                    return;
                }
            }

            // Try to select a unit at clicked position
            var unit = _game.Map.GetUnitAt(coord);
            if (unit != null && unit.Team == _game.CurrentTeam)
            {
                SelectedUnit = unit;
                UnitSelected?.Invoke(this, unit);
            }
            else
            {
                ClearSelection();
            }

            HexClicked?.Invoke(this, coord);
        }

        private void UpdateValidActions()
        {
            _validMoves.Clear();
            _validTargets.Clear();

            if (_selectedUnit != null && _game != null)
            {
                _validMoves = _game.GetValidMoves(_selectedUnit);
                _validTargets = _game.GetAttackTargets(_selectedUnit);
            }
        }

        public void ClearSelection()
        {
            SelectedUnit = null;
        }

        private void OnUnitMoved(object? sender, Unit unit)
        {
            if (unit == _selectedUnit)
            {
                UpdateValidActions();
            }
            Invalidate();
        }

        private void OnAttackExecuted(object? sender, AttackResult result)
        {
            Invalidate();
        }

        private void OnTeamChanged(object? sender, Team team)
        {
            ClearSelection();
        }

        /// <summary>
        /// Center the view on the map
        /// </summary>
        public void CenterView()
        {
            _offset = new PointF(Width / 2f, Height / 2f);
            Invalidate();
        }

        /// <summary>
        /// Zoom the view in or out
        /// </summary>
        public void Zoom(float factor)
        {
            // This could be implemented for zoom functionality
            // For now, we'll keep it simple with fixed zoom
        }
    }
}
