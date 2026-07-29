using GeoCadPlugin.Drawers;

namespace GeoCadPlugin
{
    public record GeoTableRow(string Title, string UnitText, List<GeoTableRowValue> Values);
}
