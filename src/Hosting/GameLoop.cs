using System.Diagnostics;
using Yggdrasilnet.Server.Network;
using Yggdrasilnet.Server.Runtime;

namespace Yggdrasilnet.Server.Hosting;

public sealed class GameLoop(ServerTransport server, SimulationEngine simulation, int tickRate) {
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
