namespace Map.Blueprint
{
    public enum VisualState : byte
    {
        Preview, // preview of structure before placing it
        PreviewOverlapping, // preview of structure overlaps with a structure of the same type
        Valid, // structure can be built
        Invalid, // structure cannot be built
        Overlapping // structure overlaps with a structure of the same type
    }
}