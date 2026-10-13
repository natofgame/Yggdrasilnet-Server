using System.Numerics;
using Yggdrasilnet.Maths.Collision;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components.Collision;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Core.Systems.Physics.Collision;

public sealed class CollisionSystem : IWorldSystem {
    private const int SolverIterations = 3;
    private const float ContactSkin = 0.005f;
    private const float MinDisplacementSq = 1e-10f;
    private readonly List<(Entity Entity, CollisionComponent Collider)> _colliders = [];
    private readonly List<bool> _dynamic = [];

    private readonly CollisionSpatialHash _hash = new();
    private readonly List<BoundingBoxes> _swept = [];

    public string Id => "collision";
    public WorldSystemPhase Phase => WorldSystemPhase.Physics;

    public void Update(Dimension dimension, float dt) {
        _colliders.Clear();
        _swept.Clear();
        _dynamic.Clear();

        var maxExtent = 0f;
        foreach (var entity in dimension.Query<CollisionComponent>()) {
            var collider = entity.Get<CollisionComponent>();
            _colliders.Add((entity, collider));
            _swept.Add(collider.GetSweptWorldAabb(entity.PreviousPosition, entity.Position));
            _dynamic.Add(entity.Has<MovementComponent>());
            maxExtent = MathF.Max(maxExtent, MathF.Max(collider.X, collider.Z));
        }

        var count = _colliders.Count;
        if (count == 0) {
            return;
        }

        var cellSize = MathF.Max(1f, maxExtent);
        for (var iteration = 0; iteration < SolverIterations; iteration++) {
            _hash.Rebuild(_swept, cellSize);
            var moved = false;
            for (var i = 0; i < count; i++) {
                var neighbors = _hash.Query(_swept[i]);
                for (var n = 0; n < neighbors.Count; n++) {
                    var j = neighbors[n];
                    if (j <= i) {
                        continue;
                    }

                    if (!_dynamic[i] && !_dynamic[j]) {
                        continue;
                    }

                    var colliderA = _colliders[i].Collider;
                    var colliderB = _colliders[j].Collider;
                    if (!colliderA.CanCollide(colliderB) || colliderA.IsTrigger || colliderB.IsTrigger) {
                        continue;
                    }

                    moved |= TryResolvePair(i, j);
                }
            }

            if (!moved) {
                break;
            }
        }
    }

    private bool TryResolvePair(int i, int j) {
        var entityA = _colliders[i].Entity;
        var entityB = _colliders[j].Entity;
        var colliderA = _colliders[i].Collider;
        var colliderB = _colliders[j].Collider;

        var startA = entityA.PreviousPosition;
        var startB = entityB.PreviousPosition;
        var destA = entityA.Position;
        var destB = entityB.Position;
        var dispA = destA - startA;
        var dispB = destB - startB;
        var relative = dispA - dispB;

        var boxStartA = colliderA.GetWorldAabb(startA);
        var boxStartB = colliderB.GetWorldAabb(startB);

        var hit = AabbSweep.TryGetTimeOfImpact(boxStartA, relative, boxStartB, out var time, out var normal);
        if (hit && time > 0f && relative.LengthSquared() > MinDisplacementSq) {
            var clamped = MathF.Max(0f, time - ContactSkin);
            if (_dynamic[i]) {
                destA = startA + dispA * clamped;
            }

            if (_dynamic[j]) {
                destB = startB + dispB * clamped;
            }

            SlideRemaining(ref destA, ref destB, dispA, dispB, time, normal, _dynamic[i], _dynamic[j]);
        }

        var boxA = colliderA.GetWorldAabb(destA);
        var boxB = colliderB.GetWorldAabb(destB);
        if (AabbSweep.TryComputeMinimumTranslation(boxA, boxB, out var mtv)) {
            Separate(ref destA, ref destB, mtv, _dynamic[i], _dynamic[j]);
        }

        var changed = false;
        if (_dynamic[i] && destA != entityA.Position) {
            entityA.Position = destA;
            _swept[i] = colliderA.GetSweptWorldAabb(entityA.PreviousPosition, destA);
            changed = true;
        }

        if (_dynamic[j] && destB != entityB.Position) {
            entityB.Position = destB;
            _swept[j] = colliderB.GetSweptWorldAabb(entityB.PreviousPosition, destB);
            changed = true;
        }

        return changed;
    }

    private static void SlideRemaining(
        ref Vector3 destA,
        ref Vector3 destB,
        Vector3 dispA,
        Vector3 dispB,
        float time,
        Vector3 normal,
        bool dynamicA,
        bool dynamicB
    ) {
        if (normal.LengthSquared() <= MinDisplacementSq || time >= 1f) {
            return;
        }

        var remaining = 1f - time;
        if (dynamicA) {
            destA += RejectInward(dispA * remaining, normal);
        }

        if (dynamicB) {
            destB += RejectInward(dispB * remaining, -normal);
        }
    }

    private static Vector3 RejectInward(Vector3 remaining, Vector3 normal) {
        var intoWall = Vector3.Dot(remaining, normal);
        return intoWall < 0f ? remaining - normal * intoWall : remaining;
    }

    private static void Separate(ref Vector3 positionA, ref Vector3 positionB, Vector3 mtv, bool dynamicA,
        bool dynamicB) {
        if (dynamicA && dynamicB) {
            positionA += mtv * 0.5f;
            positionB -= mtv * 0.5f;
            return;
        }

        if (dynamicA) {
            positionA += mtv;
            return;
        }

        positionB -= mtv;
    }
}

internal sealed class CollisionSpatialHash {
    private const float DefaultCellSize = 2f;
    private readonly Stack<List<int>> _bucketPool = new();
    private readonly Dictionary<long, List<int>> _cells = new();
    private readonly List<int> _queryBuffer = [];
    private int _stamp;
    private int[] _stamps = [];

    public float CellSize { get; private set; } = DefaultCellSize;

    public void Rebuild(IReadOnlyList<BoundingBoxes> boxes, float cellSize) {
        Recycle();
        CellSize = cellSize > 0.1f ? cellSize : DefaultCellSize;
        EnsureStamps(boxes.Count);

        for (var i = 0; i < boxes.Count; i++)
            Insert(i, boxes[i]);
    }

    public List<int> Query(in BoundingBoxes box) {
        _queryBuffer.Clear();
        _stamp++;
        if (_stamp == int.MaxValue) {
            Array.Clear(_stamps);
            _stamp = 1;
        }

        var minX = Floor(box.Min.X);
        var maxX = Floor(box.Max.X);
        var minZ = Floor(box.Min.Z);
        var maxZ = Floor(box.Max.Z);

        for (var z = minZ; z <= maxZ; z++)
        for (var x = minX; x <= maxX; x++) {
            if (!_cells.TryGetValue(Pack(x, z), out var bucket)) {
                continue;
            }

            for (var i = 0; i < bucket.Count; i++) {
                var index = bucket[i];
                if (_stamps[index] == _stamp) {
                    continue;
                }

                _stamps[index] = _stamp;
                _queryBuffer.Add(index);
            }
        }

        return _queryBuffer;
    }

    private void Insert(int index, in BoundingBoxes box) {
        var minX = Floor(box.Min.X);
        var maxX = Floor(box.Max.X);
        var minZ = Floor(box.Min.Z);
        var maxZ = Floor(box.Max.Z);

        for (var z = minZ; z <= maxZ; z++)
        for (var x = minX; x <= maxX; x++) {
            var key = Pack(x, z);
            if (!_cells.TryGetValue(key, out var bucket)) {
                bucket = _bucketPool.TryPop(out var recycled) ? recycled : [];
                _cells[key] = bucket;
            }

            bucket.Add(index);
        }
    }

    private void Recycle() {
        foreach (var bucket in _cells.Values) {
            bucket.Clear();
            _bucketPool.Push(bucket);
        }

        _cells.Clear();
    }

    private void EnsureStamps(int count) {
        if (_stamps.Length >= count) {
            return;
        }

        Array.Resize(ref _stamps, Math.Max(count, _stamps.Length * 2 + 8));
    }

    private int Floor(float value) {
        return (int)MathF.Floor(value / CellSize);
    }

    private static long Pack(int x, int z) {
        return ((long)x << 32) | (uint)z;
    }
}
