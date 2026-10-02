namespace GeoAppCore.Hatch
{
    public record HatchConfig(IReadOnlyList<HatchElement> Elements, System.Drawing.Color? Color = null, 
        System.Drawing.Color? BackgroundColor = null, double MaxClippedFraction = 0.5);

    public record HatchElement(
        GeometryKey Geometry,
        int StepX,
        int StepY,
        bool RandomOffset = false,
        double Scale = 1.0,
        int PlacementAttempts = 8,
        bool RandomRotation = false,
        bool IngnoreInrersections = false);
}
