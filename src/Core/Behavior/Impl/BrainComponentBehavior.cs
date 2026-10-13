using System.Numerics;
using Yggdrasilnet.Server.Core.Behavior.Core;
using Yggdrasilnet.Server.Core.Systems.Ai;
using Yggdrasilnet.Server.Core.Systems.Ai.Behaviors;
using Yggdrasilnet.Server.Core.World.Entities.Components;

namespace Yggdrasilnet.Server.Core.Behavior.Impl;

public sealed class BrainComponentBehavior : ComponentBehavior<BrainComponent> {
    public float Prudence { get; set; }
    public float Impulsivity { get; set; }
    public float Aggressivity { get; set; }
    public float Courage { get; set; }
    public float Curiosity { get; set; }
    public float Discipline { get; set; }

    public float SightRange { get; set; } = 20f;
    public float PerceptionInterval { get; set; } = 0.2f;
    public float SurprisedDuration { get; set; } = 0.4f;
    public float FearDuration { get; set; } = 2.5f;
    public float FearRadius { get; set; } = 8f;

    public AiBehavior[] Actions { get; set; } = [
        new FleeBehavior(),
        new AttackBehavior(),
        new RepositionBehavior(),
        new HoldBehavior(),
        new FlankBehavior(),
        new WanderBehavior()
    ];

    protected override BrainComponent GetComponent(Vector3 spawnPosition) {
        BehaviorValidation.EnsureFinite(
            (Prudence, "brain.prudence"),
            (Impulsivity, "brain.impulsivity"),
            (Aggressivity, "brain.aggressivity"),
            (Courage, "brain.courage"),
            (Curiosity, "brain.curiosity"),
            (Discipline, "brain.discipline"),
            (SightRange, "brain.sightRange"),
            (PerceptionInterval, "brain.perceptionInterval"),
            (SurprisedDuration, "brain.surprisedDuration"),
            (FearDuration, "brain.fearDuration"),
            (FearRadius, "brain.fearRadius")
        );

        BehaviorValidation.EnsureNonNegative(
            (SightRange, "brain.sightRange"),
            (SurprisedDuration, "brain.surprisedDuration"),
            (FearDuration, "brain.fearDuration"),
            (FearRadius, "brain.fearRadius")
        );

        BehaviorValidation.EnsurePositive(PerceptionInterval, "brain.perceptionInterval");

        return new BrainComponent {
            Prudence = Prudence,
            Impulsivity = Impulsivity,
            Aggressivity = Aggressivity,
            Courage = Courage,
            Curiosity = Curiosity,
            Discipline = Discipline,
            SightRange = SightRange,
            PerceptionInterval = PerceptionInterval,
            PerceptionTimer = 0f,
            SurprisedDuration = SurprisedDuration,
            FearDuration = FearDuration,
            FearRadius = FearRadius,
            Actions = Actions is { Length: > 0 } ? [.. Actions] : []
        };
    }
}
