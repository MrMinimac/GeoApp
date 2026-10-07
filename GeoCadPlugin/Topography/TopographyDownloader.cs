using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore.Services;
using GeoAppCore.Topography;
using Newtonsoft.Json;
using System.IO;

namespace GeoCadPlugin.Topography
{
    public sealed class DemCacheDatabase
    {
        public List<DemCacheInfo> Dems { get; set; } = new();
    }

    public sealed class DemCacheInfo
    {
        public string DemType { get; set; } = "SRTMGL1";

        public double South { get; set; }
        public double North { get; set; }

        public double West { get; set; }
        public double East { get; set; }

        public string FileName { get; set; } = "";
    }

    public readonly record struct DemBounds(double South, double North, double West, double East);

    internal class TopographyDownloader
    {
        private static OpenTopographyDemProvider? _demProvider;

        public static async Task<string?> Download(PromptSelectionResult selection)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            if (selection == null || selection.Status != PromptStatus.OK || selection.Value == null)
                return null;

            // --------------------------------------------------------
            // 2. Общая область всех кругов
            // --------------------------------------------------------
            Extents3d extents;
            try
            {
                extents = GetCirclesExtents(doc.Database, selection.Value);
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nОшибка получения области: {ex.Message}");
                return null;
            }

            // --------------------------------------------------------
            // 3. Добавляем запас
            // --------------------------------------------------------
            const double margin = 100.0;

            double minX = extents.MinPoint.X - margin;
            double maxX = extents.MaxPoint.X + margin;

            double minY = extents.MinPoint.Y - margin;
            double maxY = extents.MaxPoint.Y + margin;

            // --------------------------------------------------------
            // 4. Четыре угла в Гаусс-Крюгере (в DWG: X = Восток, Y = Север)
            // --------------------------------------------------------
            var corners = new[]
            {
                (X: minX, Y: minY),
                (X: minX, Y: maxY),
                (X: maxX, Y: minY),
                (X: maxX, Y: maxY)
            };

            // --------------------------------------------------------
            // 5. Гаусс-Крюгер → геодезические координаты
            // --------------------------------------------------------
            var geoPoints = corners
                .Select(p =>
                    GaussKrugerConverter.GKToGeodetic(
                        p.Y, // GK X — север
                        p.X  // GK Y — восток
                    ))
                .ToList();

            // --------------------------------------------------------
            // 6. Получаем WGS84 BoundingBox
            // --------------------------------------------------------
            double south = geoPoints.Min(p => p.Latitude);
            double north = geoPoints.Max(p => p.Latitude);
            double west = geoPoints.Min(p => p.Longitude);
            double east = geoPoints.Max(p => p.Longitude);

            DemBounds requestedBounds =
                new DemBounds(
                    south,
                    north,
                    west,
                    east);

            // --------------------------------------------------------
            // 7. Скачать DEM
            // --------------------------------------------------------
            string outputDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GeoCadPlugin",
                "DEM");

            Directory.CreateDirectory(outputDirectory);

            string? existingFile =
                DemCache.FindCoveringDem(
                    outputDirectory,
                    requestedBounds);

            if (existingFile != null)
            {
                ed.WriteMessage($"\nDEM получен из кэша");
                return existingFile;
            }

            try
            {
                ed.WriteMessage($"\nПодождите, скачивание DEM...");

                string fileId =
                    $"{south:F6}_{west:F6}_{north:F6}_{east:F6}"
                        .Replace('.', '_')
                        .Replace('-', 'm');

                string outputFile =
                    Path.Combine(
                        outputDirectory,
                        $"SRTM_{fileId}.tif");

                _demProvider ??= new OpenTopographyDemProvider();

                // Здесь выполнение приостанавливается, а потом возобновляется в фоновом потоке
                await _demProvider.DownloadSrtm30Async(
                    south,
                    north,
                    west,
                    east,
                    outputFile);

                ed.WriteMessage($"\nDEM успешно скачан:\n{outputFile}");

                var cacheInfo = new DemCacheInfo
                {
                    DemType = "SRTMGL1",

                    South = south,
                    North = north,

                    West = west,
                    East = east,

                    FileName = Path.GetFileName(outputFile)
                };

                DemCache.Add(outputDirectory, cacheInfo);

                return outputFile;
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nОшибка загрузки DEM: {ex.Message}");
                return null;
            }
        }

        private static Extents3d GetCirclesExtents(Database db, SelectionSet selection)
        {
            Extents3d result = new Extents3d();
            bool initialized = false;

            using Transaction tr = db.TransactionManager.StartTransaction();

            foreach (SelectedObject selected in selection)
            {
                if (selected == null)
                    continue;

                Circle? circle = tr.GetObject(selected.ObjectId, OpenMode.ForRead) as Circle;
                if (circle == null)
                    continue;

                var min = new Point3d(
                    circle.Center.X - circle.Radius,
                    circle.Center.Y - circle.Radius,
                    0);

                var max = new Point3d(
                    circle.Center.X + circle.Radius,
                    circle.Center.Y + circle.Radius,
                    0);

                var circleExtents = new Extents3d(min, max);

                if (!initialized)
                {
                    result = circleExtents;
                    initialized = true;
                }
                else
                {
                    result.AddExtents(circleExtents);
                }
            }

            tr.Commit();

            if (!initialized)
                throw new InvalidOperationException("Круги не найдены.");

            return result;
        }
    }

    internal static class DemCache
    {
        private const string CacheFileName = "dem-cache.json";

        public static string? FindCoveringDem(
            string directory,
            DemBounds requested)
        {
            if (!Directory.Exists(directory))
                return null;

            string cacheFile =
                Path.Combine(directory, CacheFileName);

            if (!File.Exists(cacheFile))
                return null;

            try
            {
                string json =
                    File.ReadAllText(cacheFile);

                DemCacheDatabase? database =
                    JsonConvert.DeserializeObject<DemCacheDatabase>(json);

                if (database == null)
                    return null;

                bool changed = false;

                foreach (DemCacheInfo info in database.Dems.ToList())
                {
                    if (!string.Equals(
                            info.DemType,
                            "SRTMGL1",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string tifPath =
                        Path.Combine(
                            directory,
                            info.FileName);

                    // Запись в JSON есть, а TIFF уже удалён.
                    if (!File.Exists(tifPath))
                    {
                        database.Dems.Remove(info);
                        changed = true;

                        continue;
                    }

                    DemBounds dem =
                        new DemBounds(
                            info.South,
                            info.North,
                            info.West,
                            info.East);

                    if (!Contains(dem, requested))
                        continue;

                    // Нашли подходящий существующий DEM.
                    if (changed)
                        SaveDatabase(cacheFile, database);

                    return tifPath;
                }

                // Например, были удалены отсутствующие TIFF.
                if (changed)
                    SaveDatabase(cacheFile, database);
            }
            catch
            {
                // Повреждённый cache JSON.
                // В этом случае считаем, что кэш пустой.
            }

            return null;
        }

        public static void Add(string directory, DemCacheInfo info)
        {
            Directory.CreateDirectory(directory);

            string cacheFile = Path.Combine(directory, CacheFileName);

            DemCacheDatabase database = LoadDatabase(cacheFile);

            // Если такой файл уже зарегистрирован,
            // обновляем запись вместо создания дубликата.
            DemCacheInfo? existing =
                database.Dems.FirstOrDefault(x =>
                    string.Equals(
                        x.FileName,
                        info.FileName,
                        StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.DemType = info.DemType;

                existing.South = info.South;
                existing.North = info.North;

                existing.West = info.West;
                existing.East = info.East;
            }
            else
            {
                database.Dems.Add(info);
            }

            SaveDatabase(cacheFile, database);
        }

        private static DemCacheDatabase LoadDatabase(string cacheFile)
        {
            if (!File.Exists(cacheFile))
                return new DemCacheDatabase();

            try
            {
                string json = File.ReadAllText(cacheFile);

                return
                    JsonConvert.DeserializeObject<DemCacheDatabase>(json)
                    ?? new DemCacheDatabase();
            }
            catch
            {
                return new DemCacheDatabase();
            }
        }

        private static void SaveDatabase(string cacheFile, DemCacheDatabase database)
        {
            string json =
                JsonConvert.SerializeObject(
                    database,
                    Formatting.Indented);

            File.WriteAllText(
                cacheFile,
                json);
        }

        private static bool Contains(DemBounds dem, DemBounds requested)
        {
            return
                dem.South <= requested.South &&
                dem.North >= requested.North &&
                dem.West <= requested.West &&
                dem.East >= requested.East;
        }
    }
}