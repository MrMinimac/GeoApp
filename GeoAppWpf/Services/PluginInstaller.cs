using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Windows;

namespace GeoAppWpf.Services
{
    public static class PluginInstaller
    {
        private const string BundleName = "GeoCadPlugin.bundle";

        private const string ResourcePrefix = "GeoAppWpf.Resources.";

        private static readonly string[] ResourceFiles =
        [
            "GeoCadPlugin.dll",
            "LegendDesignWpf.dll",
            "PackageContents.xml",
        ];

        /// <summary>
        /// Путь к установленному bundle.
        /// </summary>
        public static string InstallDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Autodesk",
                "ApplicationPlugins",
                BundleName
            );

        /// <summary>
        /// Устанавливает или обновляет файлы плагина.
        /// </summary>
        public static void Install()
        {
            string bundleDirectory = InstallDirectory;
            Directory.CreateDirectory(bundleDirectory);

            Assembly assembly = Assembly.GetExecutingAssembly();

            try
            {
                foreach (string fileName in ResourceFiles)
                {
                    string resourceName = ResourcePrefix + fileName;

                    string destinationPath = Path.Combine(bundleDirectory, fileName);

                    using Stream? resource = assembly.GetManifestResourceStream(resourceName);

                    if (resource == null)
                    {
                        throw new InvalidOperationException(
                            $"Встроенный ресурс не найден: {resourceName}"
                        );
                    }

                    using var output = new FileStream(
                        destinationPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None
                    );

                    resource.CopyTo(output);
                }
            }
            catch
            {
                // Не скрываем исключение: вызывающий код
                // должен показать пользователю причину ошибки.
                throw;
            }
        }

        /// <summary>
        /// Проверяет наличие основных файлов установки.
        /// </summary>
        public static bool IsInstalled()
        {
            return File.Exists(Path.Combine(InstallDirectory, "PackageContents.xml"))
                && File.Exists(Path.Combine(InstallDirectory, "GeoCadPlugin.dll"))
                && File.Exists(Path.Combine(InstallDirectory, "LegendDesignWpf.dll"));
        }

        public static Version? GetInstalledVersion()
        {
            string path = Path.Combine(InstallDirectory, "GeoCadPlugin.dll");
            return GetVersionFromFile(path);
        }

        public static Version? GetEmbeddedVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using Stream? resource = assembly.GetManifestResourceStream(
                "GeoAppWpf.Resources.GeoCadPlugin.dll"
            );

            if (resource == null)
                throw new InvalidOperationException(
                    "Встроенный ресурс GeoCadPlugin.dll не найден."
                );

            using var memory = new MemoryStream();
            resource.CopyTo(memory);
            memory.Position = 0;

            using var peReader = new PEReader(memory);

            if (!peReader.HasMetadata)
                throw new InvalidOperationException("GeoCadPlugin.dll не содержит .NET metadata.");

            var metadata = peReader.GetMetadataReader();
            var assemblyDefinition = metadata.GetAssemblyDefinition();

            return assemblyDefinition.Version;
        }

        private static Version? GetVersionFromFile(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(path);
                return assemblyName.Version;
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// Удаляет установленный плагин.
        /// AutoCAD должен быть закрыт.
        /// </summary>
        /// <returns>
        /// true, если плагин удалён или уже отсутствовал;
        /// false, если удаление не выполнено.
        /// </returns>
        public static bool Uninstall()
        {
            // Проверяем, запущен ли AutoCAD.
            Process[] processes = Process.GetProcessesByName("acad");

            if (processes.Length > 0)
            {
                MessageBox.Show(
                    "Невозможно удалить плагин, пока запущен AutoCAD.\n\n" +
                    "Закройте все окна AutoCAD и повторите попытку.",
                    "Удаление GeoCadPlugin",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            try
            {
                if (Directory.Exists(InstallDirectory))
                {
                    Directory.Delete(InstallDirectory, recursive: true);
                }

                MessageBox.Show(
                    "Плагин GeoCadPlugin успешно удалён.",
                    "Удаление GeoCadPlugin",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                MessageBox.Show(
                    $"Не удалось удалить плагин.\n\n{ex.Message}",
                    "Ошибка удаления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }
    }
}
