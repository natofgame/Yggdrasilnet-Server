<p align="center">
  <img src="assets/banner.png" alt="Yggdrasil Server Banner" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-v0.0.1-ffffff?style=for-the-badge&labelColor=0d0d0d&color=ffffff" alt="Version" />
  <img src="https://img.shields.io/badge/.NET-10.0-ffffff?style=for-the-badge&labelColor=0d0d0d&color=ffffff" alt=".NET 10.0" />
</p>

# 🌳 Yggdrasil Server

The authoritative, data-driven game server of Yggdrasilnet

## 🧭 Overview

The server is responsible for maintaining the world state, executing entity logic, and synchronizing data across all
connected clients. It operates on an authoritative model to ensure game integrity.

## ⚙️ Technical Specifications

- 🎯 **Target Framework**: .NET 10.0
- 📡 **Networking**: Powered by `LiteNetLib`.
- 🧩 **Core Architecture**: ECS-like component management system.

## 🗂️ Project Structure

- 🏁 `src/Bootstrap/`: Application entry, hosted services, and game loop.
- 🔧 `src/Configuration/`: Server configuration models and loader.
- 🧠 `src/Core/`: Simulation logic (behaviors, commands, events, systems, world).
- 🌐 `src/Network/`: Transport, sessions, packet handlers, and snapshot broadcasting.

## 🚀 Usage

### 📥 Setup

The server depends on sibling repositories (`Yggdrasilnet.Maths`, `Yggdrasilnet.Network` and
`Yggdrasilnet.Gameplay`, which holds the behavior packs). Clone or update them all, then build:

```bash
./setup.sh [branch]
```

### 🔨 Build

```bash
dotnet build Yggdrasilnet.Server.csproj
```

### ▶️ Run

```bash
dotnet run --project Yggdrasilnet.Server.csproj
```

The server listens on UDP port `9050` by default.

## 🤝 Contributors

Thanks to the following people who have contributed to this project:

<table align="left">
  <tr>
    <td align="center">
      <a href="https://github.com/natof">
        <img src="https://wsrv.nl/?url=https://avatars.githubusercontent.com/u/80003956&w=120&h=120&fit=cover&mask=roundrect" width="90" alt="Natof" />
      </a>
      <br />
      <b>Natof</b>
    </td>
  </tr>
</table>