using System.Runtime.CompilerServices;
using Yggdrasilnet.Maths.Collision;

namespace Yggdrasilnet.Server.Core.Systems.Physics.Collision;

/// <summary>
///  A spatial hash for collision detection.
/// </summary>
internal sealed class CollisionSpatialHash {
    private const float DefaultCellSize = 2f;
    private readonly Stack<List<int>> _bucketPool = new();
    private readonly Dictionary<long, List<int>> _cells = new();
    private readonly List<int> _queryBuffer = [];
    private int _stamp;
    private int[] _stamps = [];

    /// <summary>
    ///  The size of each cell in the spatial hash.
    /// </summary>
    private float CellSize { get; set; } = DefaultCellSize;

    /// <summary>
    ///  Rebuilds the spatial hash with the given bounding boxes.
    /// </summary>
    /// <param name="boxes"></param>
    /// <param name="cellSize"></param>
    public void Rebuild(IReadOnlyList<BoundingBoxes> boxes, float cellSize) {
        Recycle();
        CellSize = cellSize > 0.1f ? cellSize : DefaultCellSize;
        EnsureStamps(boxes.Count);

        for (var i = 0; i < boxes.Count; i++) {
            Insert(i, boxes[i]);
        }
    }

    /// <summary>
    ///  Queries the spatial hash for entities within the given bounding box.
    /// </summary>
    /// <param name="box"></param>
    /// <returns></returns>
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

            foreach (var index in bucket) {
                if (_stamps[index] == _stamp) {
                    continue;
                }

                _stamps[index] = _stamp;
                _queryBuffer.Add(index);
            }
        }

        return _queryBuffer;
    }

    /// <summary>
    ///  Inserts a bounding box into the spatial hash.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="box"></param>
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

    /// <summary>
    ///  Recycles all buckets and cells.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Recycle() {
        foreach (var bucket in _cells.Values) {
            bucket.Clear();
            _bucketPool.Push(bucket);
        }

        _cells.Clear();
    }

    /// <summary>
    ///  Ensures that the stamps array has at least the given size.
    /// </summary>
    /// <param name="count"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureStamps(int count) {
        if (_stamps.Length >= count) {
            return;
        }

        Array.Resize(ref _stamps, Math.Max(count, _stamps.Length * 2 + 8));
    }
    
    /// <summary>
    ///  Returns the floor of the given value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Floor(float value) {
        return (int)MathF.Floor(value / CellSize);
    }

    /// <summary>
    ///  Packs the given coordinates into a single long.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Pack(int x, int z) {
        return ((long)x << 32) | (uint)z;
    }
}