namespace LegendDesignWpf.WinApi
{
    public enum CornerPreference
    {
        Default = 0,     // по умолчанию (зависит от темы Windows)
        DoNotRound = 1,  // без скругления (прямые)
        Round = 2,       // обычные скругления
        SmallRound = 3   // маленькие скругления
    }
}
