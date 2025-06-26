# Hex Wargame - .NET Windows Forms Edition

A turn-based tactical strategy game where two squads battle in a small town setting. Built with .NET 8 and Windows Forms using a hexagonal grid system for authentic tactical gameplay with a modern graphical interface.

## Features

- **Visual Hexagonal Grid**: Interactive hex battlefield with mouse controls
- **Graphical Unit Representation**: Clear visual units with team colors
- **Four Unit Types**: Each with unique abilities and tactical roles
  - **Infantry (I)**: Balanced unit with good movement and range
  - **Sniper (S)**: Long-range specialist with precision attacks  
  - **Heavy (H)**: Tough unit with high damage but limited mobility
  - **Medic (M)**: Support unit for healing and utility
- **Interactive Terrain**: Buildings, cover, and obstacles with visual effects
- **Turn-Based Strategy**: Plan your moves with visual feedback
- **AI Opponent**: Challenge against computer-controlled blue team
- **Modern UI**: Windows Forms interface with status panels and controls

## Game Rules

### Victory Conditions
- Eliminate all enemy units to win
- Game tracks turns and displays statistics

### Unit Capabilities
Each unit can **move** and **attack** once per turn:

| Unit Type | HP  | Movement | Attack Range | Damage |
|-----------|-----|----------|--------------|--------|
| Infantry  | 100 | 3 hexes  | 2 hexes      | 35     |
| Sniper    | 80  | 2 hexes  | 4 hexes      | 50     |
| Heavy     | 150 | 2 hexes  | 1 hex        | 45     |
| Medic     | 90  | 3 hexes  | 1 hex        | 20     |

### Terrain Effects
- **Open Ground**: Light green, no modifiers
- **Buildings**: Gray, +20 defense bonus
- **Cover**: Brown, +15 defense bonus  
- **Water**: Blue, impassable to units

### Combat System
- Base hit chance: 70%
- Terrain provides defensive bonuses
- Damage has random variance (±10)
- Minimum damage: 10 points

## Installation & Setup

### Requirements
- .NET 8.0 or higher
- Windows operating system
- Visual Studio Code (recommended) or Visual Studio

### Running the Game

1. **Clone or download** this repository
2. **Open terminal** in the project directory
3. **Build and run**:
   ```bash
   dotnet run --project HexWargame.csproj
   ```

### VS Code Setup
If using VS Code:
1. Open the project folder in VS Code
2. Ensure C# extension is installed
3. Use `Ctrl+Shift+P` → "Tasks: Run Task" → "Run Hex Wargame"
4. Or use `Ctrl+F5` to run without debugging

### Building
```bash
dotnet build HexWargame.csproj
```

## How to Play

### Game Interface
- **Left Panel**: Hexagonal battlefield with visual units
- **Right Panel**: Game status, unit information, and controls
- **Mouse Controls**: Click to select units and targets
- **Buttons**: New Game, End Turn, Help

### Visual Elements
- **Red Units**: Player controlled (uppercase letters I, S, H, M)
- **Blue Units**: AI controlled (lowercase letters i, s, h, m)
- **Hex Grid**: Visual hexagonal battlefield
- **Terrain Colors**: 
  - Light Green: Open ground
  - Gray: Buildings (+20 defense)
  - Brown: Cover (+15 defense)
  - Blue: Water (impassable)

### Controls
- **Left Click**: Select friendly unit or target for attack/movement
- **Right Click**: Context menu (future feature)
- **New Game Button**: Start a new battle
- **End Turn Button**: End current player's turn
- **Help Button**: Show game instructions

### Strategy Tips
1. **Use terrain wisely** - Position units in buildings or cover for defense bonuses
2. **Coordinate attacks** - Focus fire on single targets to eliminate them quickly
3. **Protect your Sniper** - Use them for long-range support from safe positions
4. **Control key positions** - Buildings in the town center provide excellent defensive positions
5. **Plan movement carefully** - Units can only move and attack once per turn

## Project Structure

```
wargame2/
├── GameCore.cs         # Core game logic and data structures
├── GameEngine.cs       # Game state management and rules
├── HexGridControl.cs   # Custom hex grid UI control
├── Form1.cs           # Main form logic
├── Form1.Designer.cs  # UI layout and components
├── Program.cs         # Application entry point
├── HexWargame.csproj  # Project file
├── README.md          # This file
└── .github/
    └── copilot-instructions.md  # Development guidelines
```

## Technical Details

### Architecture
- **Model-View Pattern**: Clean separation of game logic and UI
- **Custom Controls**: HexGridControl for hex battlefield rendering
- **Object-Oriented Design**: Well-structured classes for units, map, and game state
- **Event-Driven UI**: Responsive Windows Forms interface
- **Type Safety**: Modern C# with nullable reference types

### Key Classes
- `Game`: Main game controller and state management
- `GameMap`: Handles terrain and unit positioning  
- `Unit`: Individual unit properties and actions
- `HexGridControl`: Custom control for hex grid visualization
- `MainForm`: Windows Forms interface
- `HexCoord`: Hexagonal coordinate mathematics

### Graphics Features
- **Double Buffering**: Smooth rendering without flicker
- **Custom Drawing**: Hand-drawn hex grid with proper geometry
- **Color Coding**: Team colors and terrain visualization
- **Mouse Hit Testing**: Accurate hex coordinate detection

## Development

### Code Style
- Follows .NET coding standards
- XML documentation for public APIs
- Modern C# features and patterns
- Separation of concerns between UI and logic

### Building from Source
```bash
# Clone repository
git clone <repository-url>
cd wargame2

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project HexWargame.csproj
```

### VS Code Tasks
- `Ctrl+Shift+P` → "Tasks: Run Task" → "Build Hex Wargame"
- `Ctrl+Shift+P` → "Tasks: Run Task" → "Run Hex Wargame"

### Future Enhancements
Potential improvements for the game:
- [ ] Enhanced graphics with sprites and animations
- [ ] Sound effects and music
- [ ] Multiplayer network support
- [ ] More unit types and special abilities
- [ ] Larger maps with varied terrain
- [ ] Campaign mode with multiple battles
- [ ] Enhanced AI with difficulty levels
- [ ] Save/load game functionality
- [ ] Unit experience and progression
- [ ] Map editor

## License

This project is open source and available under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

---

**Command your squad to victory in this tactical hex-based wargame!** 🎯⚔️
