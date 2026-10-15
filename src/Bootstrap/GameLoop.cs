using System.Diagnostics;
using Yggdrasilnet.Server.Core;
using Yggdrasilnet.Server.Network;

namespace Yggdrasilnet.Server.Bootstrap;

/// <summary>
///     The game loop.
/// </summary>
/// <param name="server"></param>
/// <param name="simulation"></param>
/// <param name="tickRate"></param>
public sealed class GameLoop(ServerTransport server, Simulation simulation, int tickRate) {
    /// <summary>
    ///     Run the game loop.
    /// </summary>
    /// <param name="token"></param>
    public void Run(CancellationToken token) {
        var dt = 1f / tickRate;
        var minDt = dt * 0.5f;
        var maxDt = dt * 3f;
        var tickIntervalMs = 1000L / tickRate;

        var clock = Stopwatch.StartNew();
        var nextTick = clock.ElapsedMilliseconds;
        var previousSimulationTime = nextTick;

        while (!token.IsCancellationRequested) {
            var now = clock.ElapsedMilliseconds;
            if (now >= nextTick) {
                server.Poll();

                var elapsedMs = Math.Max(0L, now - previousSimulationTime);
                var measuredDt = elapsedMs / 1000f;
                var simulationDt = Math.Clamp(measuredDt, minDt, maxDt);
                previousSimulationTime = now;

                simulation.Update(simulationDt);

                nextTick += tickIntervalMs;
                if (clock.ElapsedMilliseconds > nextTick + tickIntervalMs) {
                    nextTick = clock.ElapsedMilliseconds;
                }
            }
            else {
                Thread.Sleep(1);
            }
        }
    }
}