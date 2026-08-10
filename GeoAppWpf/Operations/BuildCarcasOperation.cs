using GeoAppWpf.Interfaces;
using GeoAppWpf.Models;
using GeoAppWpf.Services;
using netDxf;
using netDxf.Entities;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GeoAppWpf.Operations
{
    public class BuildCarcasOperation : IUndoableCommand
    {
        private readonly DxfDocument _document;
        private readonly ViewportController _viewport;
        private readonly List<EntityObject> _createdEntities = new();
        private readonly List<DrawerObject> _createdVisuals = new();
        private readonly List<DrawerObject> _createdExtropolates = new();
        private readonly List<EntityObject> _createdExtropolatesEntities = new();
        private readonly List<DrawerObject> _selectedVisuals;
        private DrawerObject? _modelObject;

        private bool _extrapolate;

        public BuildCarcasOperation(DxfDocument document, ViewportController viewport, bool extrapolate)
        {
            _document = document;
            _viewport = viewport;
            _extrapolate = extrapolate;

            var selectedVisuals = _viewport.Visuals
                .Where(x => x.IsSelected);

            _selectedVisuals = selectedVisuals.ToList();
        }

        public void Execute()
        {
            // 1. Строим каркас, если команда выполняется первый раз.
            if (_createdEntities.Count == 0)
                BuildCarcas(_extrapolate);

            // 2. Сохраняем в DXF документ
            _document?.Entities.Add(_createdEntities);
            _document?.Entities.Add(_createdExtropolatesEntities);

            // 3. Добавляем линии каркаса на экран
            // foreach (var visual in _createdVisuals)
            //    _viewport.Add(visual);

            // 3. Добавляем визуал каркаса на экран
            _viewport.Add(_modelObject);

            // 3. Добавляем экстрополяцию
            foreach (var visual in _createdExtropolates)
                _viewport.Add(visual);

            // 4. Снимаем выделения
            _viewport.UnselectAll();
        }

        public void Undo()
        {
            // 1. Удаляем из документа сущности
            _document?.Entities.Remove(_createdEntities);
            _document?.Entities.Remove(_createdExtropolatesEntities);

            // 2. Удаляем линии каркаса с экрана
            // foreach (var visual in _createdVisuals)
            //    _viewport.Remove(visual);

            // 3. Удаляем визуал каркаса с экрана
            _viewport.Remove(_modelObject);

            // 3. Удаляем экстрополяцию
            foreach (var visual in _createdExtropolates)
                _viewport.Remove(visual);

            // 5. Возвращаем выделения
            _viewport.SelectRange(_selectedVisuals);
        }

        public void BuildCarcas(bool extrapolate)
        {
            if (_selectedVisuals.Count() == 0)
                throw new Exception("Нет выделенных объектов.");

            var selectedEntities = _selectedVisuals
                .Where(x => x.Entity is Polyline3D)
                .Select(x => (Polyline3D)x.Entity)
                .ToList();

            if (selectedEntities.Count() == 0 || (!extrapolate && selectedEntities.Count() <= 1))
                throw new Exception("Выделенные объекты не подходят для построения каркаса.");

            // 1. Ищем, есть ли в сцене внешний каркас (например, 0.15), в который мы вложены
            // var outerCarcas = FindBoundingOuterCarcas(selectedEntities);

            List<Polyline3D>? allContours = null;
            List<Polyline3D>? extrapolateEntities = null;

            // 2. Экстраполяция (создаем хвосты). Передаем outerCarcas, что бы экстрополяция учитывала ее границы
            if (extrapolate)
            {
                extrapolateEntities = Extrapolator.Extrapolate(selectedEntities, 25, 0.1/*, outerCarcas*/).ToList();
                allContours = selectedEntities.Concat(extrapolateEntities).ToList();
            }

            if (allContours == null || allContours.Count < 2) return;

            // 4. Генерируем каркас (теперь Build возвращает CarcasResult вместо просто List<Face3D>)
            var carcasResult = CarcasBuilder.Build(allContours);
            if (carcasResult == null || !carcasResult.MeshTriangles.Any())
                throw new Exception("Не удалось построить каркас.");

            _createdEntities.AddRange(carcasResult.MeshTriangles);

            if (extrapolateEntities != null)
            {
                _createdExtropolatesEntities.AddRange(extrapolateEntities);

                foreach (var exropolate in extrapolateEntities)
                    _createdExtropolates.Add(new DrawerObject(exropolate));
            }

            foreach (var face in carcasResult.MeshTriangles)
                _createdVisuals.Add(new DrawerObject(face));

            if (carcasResult.MeshTriangles.Count != 0)
            {
                var acicolor = selectedEntities[0]!.Color;
                var color = Color.FromArgb(150, acicolor.R, acicolor.G, acicolor.B);
                var solidModel = CarcasMeshBuilder.BuildFromFaces(carcasResult.MeshTriangles, color);

                var modelVisual = new ModelVisual3D { Content = solidModel };
                _modelObject = new DrawerObject(modelVisual);
                _modelObject.Color = color;

                // НОВОЕ: Сохраняем математику каркаса в Tag, чтобы следующий (внутренний) каркас мог ее найти
                _modelObject.Tag = carcasResult;
            }
        }

        /// <summary>
        /// Ищет в Viewport уже построенный каркас, который пространственно охватывает наши контуры.
        /// </summary>
        private CarcasResult? FindBoundingOuterCarcas(List<Polyline3D> innerContours)
        {
            // Получаем все визуальные объекты, которые являются каркасами (у которых есть сохраненный CarcasResult)
            var existingCarcasses = _viewport.Visuals
                .Concat(_viewport.HiddenVisuals)
                .Where(v => v.Tag is CarcasResult)
                .Select(v => (CarcasResult)v.Tag)
                .ToList();

            if (existingCarcasses.Count == 0)
            {
                Debug.WriteLine("Внешний каркас не нейден.");
                return null;
            }

            // Вычисляем примерный центр текущего выделения
            var firstInner = innerContours.First();
            double innerCenterX = firstInner.Vertexes.Average(v => v.X);
            double innerCenterY = firstInner.Vertexes.Average(v => v.Y);
            double innerCenterZ = firstInner.Vertexes.Average(v => v.Z);
            var innerCenter = new Vector3(innerCenterX, innerCenterY, innerCenterZ);

            // Ищем каркас, внутри которого находится наш центр (упрощенная проверка по BoundingBox)
            foreach (var carcas in existingCarcasses)
            {
                // Здесь можно сделать точную проверку (через Raycast), но обычно достаточно проверить:
                // 1. Попадает ли innerCenter в диапазон X внешнего каркаса
                double minX = carcas.XPositions.Min();
                double maxX = carcas.XPositions.Max();

                if (innerCenterX >= minX && innerCenterX <= maxX)
                {
                    // Для надежности можно проверить 2D вхождение центральной точки в интерполированный контур,
                    // но если каркасы строятся последовательно в одном рудном теле, достаточно вернуть первый подходящий по X
                    Debug.WriteLine("Внешний каркас найден.");
                    return carcas;
                }
            }

            Debug.WriteLine("Внешний каркас не нейден.");
            return null;
        }

        /*
        
        
        public void BuildCarcas()
        {
            if (_selectedVisuals.Count() == 0)
                throw new Exception("Нет выделенных объектов.");

            var selectedEntities = _selectedVisuals
                .Where(x => x.Entity is Polyline3D)
                .Select(x => (Polyline3D)x.Entity)
                .ToList();

            if (selectedEntities.Count() == 0)
                throw new Exception("Выделенные объекты не подходят для построения каркаса.");

            // 1. Экстраполяция
            var extrapolateEntities = Extrapolator.Extrapolate(selectedEntities, 25, 0.1);
            var allContours = selectedEntities.Concat(extrapolateEntities).ToList();

            if (allContours.Count < 2) return;

            // 2. Генерируем каркас (сущности DXF, включая Face3D)
            var faces = CarcasBuilder.Build(allContours)?.ToList();
            if (faces == null || !faces.Any()) throw new Exception("Не удалось построить каркас.");

            _createdEntities.AddRange(faces);

            // 3. Создаем объекты для ViewPort

            foreach (var face in faces)
                _createdVisuals.Add(new DrawerObject(face));

            if (faces.Count != 0)
            {
                var acicolor = selectedEntities[0]!.Color;
                var color = Color.FromArgb(150, acicolor.R, acicolor.G, acicolor.B);
                var solidModel = CarcasMeshBuilder.BuildFromFaces(faces, color);

                var modelVisual = new ModelVisual3D { Content = solidModel };
                _modelObject = new DrawerObject(modelVisual);
                _modelObject.Color = color;
            }
        }

        */

    }

    public class ExtrapolateOperation : IUndoableCommand
    {
        private readonly DxfDocument _document;
        private readonly ViewportController _viewport;
        private readonly List<EntityObject> _createdEntities = new();
        private readonly List<DrawerObject> _createdVisuals = new();
        private readonly List<DrawerObject> _selectedVisuals;

        public ExtrapolateOperation(DxfDocument document, ViewportController viewport)
        {
            _document = document;
            _viewport = viewport;

            var selectedVisuals = _viewport.Visuals
                .Where(x => x.IsSelected);

            _selectedVisuals = selectedVisuals.ToList();
        }

        public void Execute()
        {
            // 1. Строим каркас, если команда выполняется первый раз.
            if (_createdEntities.Count == 0)
                Extrapolate();

            // 2. Сохраняем в DXF документ
            _document?.Entities.Add(_createdEntities);

            // 3. Добавляем экстрополяцию
            foreach (var visual in _createdVisuals)
                _viewport.Add(visual);

            // 4. Снимаем выделения
            _viewport.UnselectAll();
        }

        public void Undo()
        {
            // Удаляем из документа сущности
            _document?.Entities.Remove(_createdEntities);

            // Удаляем с экрана
            foreach (var visual in _createdVisuals)
                _viewport.Remove(visual);

            // Возвращаем выделения
            _viewport.SelectRange(_selectedVisuals);
        }

        public void Extrapolate()
        {
            if (_selectedVisuals.Count() == 0)
                throw new Exception("Нет выделенных объектов.");

            var selectedEntities = _selectedVisuals
                .Where(x => x.Entity is Polyline3D)
                .Select(x => (Polyline3D)x.Entity)
                .ToList();

            if (selectedEntities.Count() == 0)
                throw new Exception("Выделенные объекты не подходят для построения каркаса.");

            var extrapolateEntities = Extrapolator.Extrapolate(selectedEntities, 25, 0.1).ToList();
            _createdEntities.AddRange(extrapolateEntities);

            foreach (var entity in extrapolateEntities)
                _createdVisuals.Add(new DrawerObject(entity));
        }
    }
}
