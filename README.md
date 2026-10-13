# Yggdrasil Server

The authoritative simulation engine for Yggdrasilnet.

## Overview

The server is responsible for maintaining the world state, executing entity logic, and synchronizing data across all connected clients. It operates on an authoritative model to ensure game integrity.

## Technical Specifications

- **Target Framework**: .NET 10.0
- **Networking**: Powered by `LiteNetLib` (Authoritative model with client prediction).
- **Tick Rate**: Constant 20 TPS (configurable in `SimulationRuntime`).
- **Core Architecture**: ECS-like component management system.

## Project Structure

- `src/Core`: Core simulation logic, ECS components, and world management.
- `src/Network`: Network transport layer, packet handling, and session management.
- `src/Hosting`: Server hosting infrastructure and game loop execution.
- `src/Runtime`: Simulation services and snapshot broadcasting logic.

## Usage

### Build

```bash
dotnet build Yggdrasilnet.Server.csproj
```

### Run

```bash
dotnet run --project Yggdrasilnet.Server.csproj
```

The server listens on UDP port `9050` by default.

## Dependencies

- `Yggdrasilnet.NetworkV4`: Protocol and serialization.
- `Yggdrasilnet.Maths`: Physics and collision utilities.
- `Yggdrasilnet.Gameplay`: Shared game logic.
