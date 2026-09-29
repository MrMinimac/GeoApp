namespace GeoAppWpf.Helpers
{
    public sealed class NaturalNameComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (x is null)
                return -1;

            if (y is null)
                return 1;

            int ix = 0;
            int iy = 0;

            while (ix < x.Length && iy < y.Length)
            {
                if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
                {
                    int startX = ix;
                    int startY = iy;

                    while (ix < x.Length && char.IsDigit(x[ix]))
                        ix++;

                    while (iy < y.Length && char.IsDigit(y[iy]))
                        iy++;

                    var numberX = x[startX..ix];
                    var numberY = y[startY..iy];

                    if (long.TryParse(numberX, out long valueX) && long.TryParse(numberY, out long valueY))
                    {
                        int result = valueX.CompareTo(valueY);

                        if (result != 0)
                            return result;
                    }
                    else
                    {
                        int result = string.Compare(numberX, numberY, StringComparison.OrdinalIgnoreCase);

                        if (result != 0)
                            return result;
                    }
                }
                else
                {
                    int startX = ix;
                    int startY = iy;

                    while (ix < x.Length && !char.IsDigit(x[ix]))
                        ix++;

                    while (iy < y.Length && !char.IsDigit(y[iy]))
                        iy++;

                    var textX = x[startX..ix];
                    var textY = y[startY..iy];

                    int result = string.Compare(textX, textY, StringComparison.CurrentCultureIgnoreCase);

                    if (result != 0)
                        return result;
                }
            }

            return x.Length.CompareTo(y.Length);
        }
    }
}
