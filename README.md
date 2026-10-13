# Yggdrasil Server

## Description
Yggdrasil Server est le moteur de simulation autoritaire pour le projet Yggdrasil. Il gère l'état du monde, la logique des entités et la synchronisation réseau.

## Caractéristiques Techniques
- **Framework** : .NET 10.0
- **Networking** : Basé sur `LiteNetLib` (Modèle serveur autoritaire avec prédiction client).
- **Tick Rate** : Simulation constante à 20 TPS (défini dans `SimulationRuntime`).
- **Architecture** : Utilise un système de gestion de composants (ECS-like) via `ComponentManager`.

## Structure du Projet
- `src/Core` : Logique de simulation, monde et entités.
- `src/Network` : Couche de transport et gestion des sessions.
- `src/Hosting` : Runtime du serveur et boucle de jeu.
- `src/Runtime` : Services de simulation et diffusion de snapshots.

## Configuration et Build
Pour compiler le serveur :
```bash
dotnet build Yggdrasilnet.Server.csproj
```

Pour lancer le serveur :
```bash
dotnet run --project Yggdrasilnet.Server.csproj
```
Le port par défaut est `9050`.

## Dépendances
Le serveur dépend des bibliothèques partagées suivantes :
- `Yggdrasilnet.NetworkV4` (Protocole et Sérialisation)
- `Yggdrasilnet.Maths` (Utilitaires mathématiques et collisions)
- `Yggdrasilnet.Gameplay` (Logique de jeu partagée)
