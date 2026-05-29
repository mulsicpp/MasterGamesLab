namespace Map.Blueprint
{
    // public enum VisualState : byte
    // {
    //     None,
    //     RouteSelected,
    //     RouteSuggested,
    //     RouteCompleted,
    //     Planned, // objects is planned for building
    //     Hologram, // when hovering over a tile in build mode
    //     Overlapping, // object is placed over already built object
    //     Invalid, // object is not valid
    // }

    public enum VisualState : byte
    {
        None,
        Valid, // structure can be built
        Invalid, // structure cannot be built
        Overlapping // structure overlaps with a structure of the same type
    }
}