using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Event.Types;

/// <summary>
///  Event triggered when a dimension event occurs.
/// </summary>
public class DimensionEvent : WorldEvent {
    /// <summary>
    ///  The dimension that the event occurred in.
    /// </summary>
    public IDimension Dimension { get; private set; }
    
    protected DimensionEvent(IDimension dimension) : base(dimension.World) {
        Dimension = dimension;   
    }
}