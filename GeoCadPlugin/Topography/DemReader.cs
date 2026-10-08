using BitMiracle.LibTiff.Classic;

namespace GeoCadPlugin.Topography
{
    public sealed class DemGrid
    {
        public int Width { get; }

        public int Height { get; }

        public double[,] Elevation { get; }

        /// <summary>
        /// Longitude левого верхнего угла DEM.
        /// </summary>
        public double OriginLongitude { get; }

        /// <summary>
        /// Latitude левого верхнего угла DEM.
        /// </summary>
        public double OriginLatitude { get; }

        /// <summary>
        /// Размер пикселя по Longitude.
        /// </summary>
        public double PixelWidth { get; }

        /// <summary>
        /// Размер пикселя по Latitude.
        /// Обычно положительный размер, поэтому координату
        /// строки считаем как OriginLatitude - row * PixelHeight.
        /// </summary>
        public double PixelHeight { get; }

        public DemGrid(
            int width,
            int height,
            double[,] elevation,
            double originLongitude,
            double originLatitude,
            double pixelWidth,
            double pixelHeight
        )
        {
            Width = width;
            Height = height;

            Elevation = elevation;

            OriginLongitude = originLongitude;
            OriginLatitude = originLatitude;

            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
        }

        public (double Longitude, double Latitude) GetCoordinate(double row, double column)
        {
            double longitude = OriginLongitude + column * PixelWidth;

            double latitude = OriginLatitude - row * PixelHeight;

            return (longitude, latitude);
        }

        public (double Longitude, double Latitude) GetCoordinate(int row, int column)
        {
            return GetCoordinate((double)row, (double)column);
        }
    }

    internal static class DemReader
    {
        public static DemGrid Read(string filePath)
        {
            using Tiff? tif = Tiff.Open(filePath, "r");

            if (tif == null)
            {
                throw new InvalidOperationException($"Не удалось открыть TIFF: {filePath}");
            }

            // --------------------------------------------------------
            // Размер
            // --------------------------------------------------------

            int width = GetIntField(tif, TiffTag.IMAGEWIDTH);

            int height = GetIntField(tif, TiffTag.IMAGELENGTH);

            // --------------------------------------------------------
            // Формат высот
            // --------------------------------------------------------

            FieldValue[]? bitsField = tif.GetField(TiffTag.BITSPERSAMPLE);

            if (bitsField == null || bitsField.Length == 0)
            {
                throw new InvalidOperationException("В TIFF отсутствует BitsPerSample.");
            }

            int bitsPerSample = bitsField[0].ToInt();

            if (bitsPerSample != 16)
            {
                throw new NotSupportedException(
                    $"Ожидался 16-битный DEM. " + $"BitsPerSample={bitsPerSample}"
                );
            }

            // --------------------------------------------------------
            // Количество каналов
            // --------------------------------------------------------

            FieldValue[]? samplesField = tif.GetField(TiffTag.SAMPLESPERPIXEL);

            int samplesPerPixel = 1;

            if (samplesField != null && samplesField.Length > 0)
                samplesPerPixel = samplesField[0].ToInt();

            if (samplesPerPixel != 1)
            {
                throw new NotSupportedException(
                    $"Ожидался одноканальный DEM. " + $"SamplesPerPixel={samplesPerPixel}"
                );
            }

            // --------------------------------------------------------
            // Геопривязка
            // --------------------------------------------------------

            double[] pixelScale = ReadPixelScale(tif);

            double[] tiePoint = ReadTiePoint(tif);

            if (pixelScale.Length < 2)
            {
                throw new InvalidOperationException("Некорректный ModelPixelScaleTag.");
            }

            if (tiePoint.Length < 6)
            {
                throw new InvalidOperationException("Некорректный ModelTiepointTag.");
            }

            double pixelWidth = pixelScale[0];

            double pixelHeight = pixelScale[1];

            double originLongitude = tiePoint[3];

            double originLatitude = tiePoint[4];

            // --------------------------------------------------------
            // Raster
            // --------------------------------------------------------

            double[,] elevation = new double[height, width];

            ReadRaster(tif, width, height, elevation);

            return new DemGrid(
                width,
                height,
                elevation,
                originLongitude,
                originLatitude,
                pixelWidth,
                pixelHeight
            );
        }

        private static int GetIntField(Tiff tif, TiffTag tag)
        {
            FieldValue[]? field = tif.GetField(tag);

            if (field == null || field.Length == 0)
            {
                throw new InvalidOperationException($"В TIFF отсутствует тег {tag}.");
            }

            return field[0].ToInt();
        }

        private static double[] ReadPixelScale(Tiff tif)
        {
            FieldValue[]? field = tif.GetField((TiffTag)33550);

            if (field == null || field.Length < 2)
            {
                throw new InvalidOperationException(
                    "В TIFF отсутствует или некорректен ModelPixelScaleTag."
                );
            }

            double[]? result = field[1].ToDoubleArray();

            if (result == null || result.Length < 2)
            {
                throw new InvalidOperationException(
                    "Не удалось прочитать значения ModelPixelScaleTag."
                );
            }

            return result;
        }

        private static double[] ReadTiePoint(Tiff tif)
        {
            FieldValue[]? field = tif.GetField((TiffTag)33922);

            if (field == null || field.Length < 2)
            {
                throw new InvalidOperationException(
                    "В TIFF отсутствует или некорректен ModelTiepointTag."
                );
            }

            double[]? result = field[1].ToDoubleArray();

            if (result == null || result.Length < 6)
            {
                throw new InvalidOperationException(
                    "Не удалось прочитать значения ModelTiepointTag."
                );
            }

            return result;
        }

        private static void ReadRaster(Tiff tif, int width, int height, double[,] elevation)
        {
            if (tif.IsTiled())
            {
                ReadTiledRaster(tif, width, height, elevation);

                return;
            }

            ReadStripRaster(tif, width, height, elevation);
        }

        private static void ReadStripRaster(Tiff tif, int width, int height, double[,] elevation)
        {
            int rowsPerStrip = height;

            FieldValue[]? rpsField = tif.GetField(TiffTag.ROWSPERSTRIP);

            if (rpsField != null && rpsField.Length > 0)
            {
                int rps = rpsField[0].ToInt();

                // Значение 2^32-1 (или переполнение в int) означает "весь растр одной полосой"
                if (rps > 0 && rps < height)
                    rowsPerStrip = rps;
            }

            int rowSize = tif.ScanlineSize();
            int stripCount = tif.NumberOfStrips();

            byte[] buffer = new byte[tif.StripSize()];

            for (int strip = 0; strip < stripCount; strip++)
            {
                int bytes = tif.ReadEncodedStrip(strip, buffer, 0, -1);

                if (bytes < 0)
                {
                    throw new InvalidOperationException(
                        $"Не удалось прочитать полосу TIFF: {strip}"
                    );
                }

                int firstRow = strip * rowsPerStrip;
                int rowsInBuffer = bytes / rowSize;

                for (int r = 0; r < rowsInBuffer; r++)
                {
                    int row = firstRow + r;

                    if (row >= height)
                        break;

                    int rowOffset = r * rowSize;

                    for (int column = 0; column < width; column++)
                    {
                        elevation[row, column] = BitConverter.ToInt16(
                            buffer,
                            rowOffset + column * sizeof(short)
                        );
                    }
                }
            }
        }

        private static void ReadTiledRaster(Tiff tif, int width, int height, double[,] elevation)
        {
            FieldValue[]? tileWidthField = tif.GetField(TiffTag.TILEWIDTH);

            FieldValue[]? tileHeightField = tif.GetField(TiffTag.TILELENGTH);

            if (
                tileWidthField == null
                || tileWidthField.Length == 0
                || tileHeightField == null
                || tileHeightField.Length == 0
            )
            {
                throw new InvalidOperationException(
                    "TIFF помечен как tiled, но отсутствует TILEWIDTH/TILELENGTH."
                );
            }

            int tileWidth = tileWidthField[0].ToInt();

            int tileHeight = tileHeightField[0].ToInt();

            int tileSize = tif.TileSize();

            int tileRowSize = tif.TileRowSize();

            byte[] buffer = new byte[tileSize];

            for (int tileY = 0; tileY < height; tileY += tileHeight)
            {
                for (int tileX = 0; tileX < width; tileX += tileWidth)
                {
                    int bytesRead = tif.ReadTile(buffer, 0, tileX, tileY, 0, 0);

                    if (bytesRead < 0)
                    {
                        throw new InvalidOperationException(
                            $"Не удалось прочитать TIFF tile " + $"X={tileX}, Y={tileY}."
                        );
                    }

                    int actualWidth = Math.Min(tileWidth, width - tileX);

                    int actualHeight = Math.Min(tileHeight, height - tileY);

                    for (int row = 0; row < actualHeight; row++)
                    {
                        for (int column = 0; column < actualWidth; column++)
                        {
                            int offset = row * tileRowSize + column * sizeof(short);

                            short value = BitConverter.ToInt16(buffer, offset);

                            elevation[tileY + row, tileX + column] = value;
                        }
                    }
                }
            }
        }
    }
}
