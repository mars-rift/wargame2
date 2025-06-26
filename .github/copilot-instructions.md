# Copilot Instructions

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Overview
This is a graphical hex-based tactical wargame built with .NET Windows Forms. The game features:
- Visual hexagonal grid battlefield with mouse controls
- Two opposing squads battling in a small town setting
- Turn-based combat mechanics with smooth animations
- AI opponent with visual feedback
- Modern Windows GUI interface

## Code Style Guidelines
- Use C# naming conventions (PascalCase for public members, camelCase for private)
- Follow .NET coding standards and best practices
- Include XML documentation for all public classes and methods
- Use async/await for any UI operations that might block
- Keep UI logic separate from game logic
- Use proper disposal patterns for graphics resources

## Architecture Principles
- Separate game logic from UI presentation (Model-View pattern)
- Use custom controls for complex UI elements (hex grid)
- Implement proper event handling for user interactions
- Maintain clean separation between game state and rendering
- Use dependency injection where appropriate

## Graphics and UI Guidelines
- Use double buffering for smooth rendering
- Implement proper hit testing for hex grid mouse interactions
- Provide visual feedback for user actions (highlighting, animations)
- Ensure accessible color schemes and clear visual indicators
- Optimize drawing operations for smooth performance
