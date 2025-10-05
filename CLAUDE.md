# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Codex-Tree is a C# console application targeting .NET 9.0. This is currently a minimal/starter project with a standard console application structure.

## Build and Run Commands

```bash
# Build the solution
dotnet build Codex-Tree.sln

# Build in Release mode
dotnet build Codex-Tree.sln -c Release

# Run the application
dotnet run --project Codex-Tree/Codex-Tree.csproj

# Clean build artifacts
dotnet clean
```

## Project Structure

- **Codex-Tree.sln**: Visual Studio solution file
- **Codex-Tree/**: Main project directory
  - **Codex-Tree.csproj**: Project file with .NET 9.0 targeting, nullable reference types enabled
  - **Program.cs**: Application entry point

## Configuration

- Target Framework: .NET 9.0
- Nullable reference types: Enabled
- Implicit usings: Enabled