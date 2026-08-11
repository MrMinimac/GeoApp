using GeoAppWpf.Interfaces;
using GeoAppWpf.Models;
using GeoAppWpf.Services;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GeoAppWpf.Operations
{
    public static class CarcasLayerNameController
    {
        public static int Counter = 1;

        public static string GetNewName(string category)
        {
            return $"{Counter}-{category}";
        }

        public static void PlusOne()
        {
            Counter++;
        }

        public static void MinusOne()
        {
            Counter--;
        }
    }

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
        private Carcas3D? _createdCarcas;      // Сохраняем ссылку на созданный каркас
        private Carcas3D? _parentOuterCarcas;  // Сохраняем ссылку на родительский каркас

        private bool _extrapolate;
        private bool _isInitialized;           // Флаг первичного выполнения

        public BuildCarcasOperation(DxfDocument document, ViewportController viewport, bool extrapolate)
        {
            _document = document;
            _viewport = viewport;
            _extrapolate = extrapolate;

            _selectedVisuals = _viewport.Visuals
                .Where(x => x.IsSelected)
                .ToList();
        }

        public void Execute()
        {
            // 1. Первичное выполнение: один раз строим каркас и регистрируем геометрию
            if (!_isInitialized)
            {
                BuildCarcas(_extrapolate);
                _isInitialized = true;
            }

            // 2. Восстанавливаем иерархическую связь (Parent <-> Child)
            if (_parentOuterCarcas != null && _createdCarcas != null)
            {
                _parentOuterCarcas.AddChild(_createdCarcas);
            }

            // 3. Сохраняем сущности в DXF документ
            _document?.Entities.Add(_createdEntities);
            _document?.Entities.Add(_createdExtropolatesEntities);

            // 4. Возвращаем 3D-модель и экстраполированные линии в Viewport
            if (_modelObject != null)
            {
                _viewport.Add(_modelObject);
            }

            foreach (var visual in _createdExtropolates)
            {
                _viewport.Add(visual);
            }

            // 5. Снимаем выделение
            _viewport.UnselectAll();

            CarcasLayerNameController.PlusOne();
        }

        public void Undo()
        {
            // 1. Разрываем иерархическую связь, чтобы "дочерний" каркас
            // перестал учитываться как препятствие (Obstacle) для других
            if (_parentOuterCarcas != null && _createdCarcas != null)
            {
                _parentOuterCarcas.RemoveChild(_createdCarcas);
            }

            // 2. Удаляем сущности из DXF документа
            _document?.Entities.Remove(_createdEntities);
            _document?.Entities.Remove(_createdExtropolatesEntities);

            // 3. Удаляем визуальные объекты из Viewport
            if (_modelObject != null)
            {
                _viewport.Remove(_modelObject);
            }

            foreach (var visual in _createdExtropolates)
            {
                _viewport.Remove(visual);
            }

            // 4. Восстанавливаем выделение исходных полилиний
            _viewport.SelectRange(_selectedVisuals);

            CarcasLayerNameController.MinusOne();
        }

        public void BuildCarcas(bool extrapolate)
        {
            if (!_selectedVisuals.Any())
                throw new InvalidOperationException("Нет выделенных объектов.");

            var selectedEntities = _selectedVisuals
                .Where(x => x.Entity is Polyline3D)
                .Select(x => (Polyline3D)x.Entity)
                .ToList();

            if (selectedEntities.Count == 0 || (!extrapolate && selectedEntities.Count <= 1))
                throw new InvalidOperationException("Выделенные объекты не подходят для построения каркаса.");

            // 1. Ищем внешний каркас-контейнер и запоминаем ссылку
            _parentOuterCarcas = FindBoundingOuterCarcas(selectedEntities);

            List<Polyline3D> allContours = selectedEntities;
            List<Polyline3D>? extrapolateEntities = null;

            // 2. Экстраполяция (создаем торцы)
            if (extrapolate)
            {
                // Передаем родительский каркас. 
                // Ссылка _currentChildCarcas равна null, так как каркас еще не построен
                var extrapolator = new Extrapolator(selectedEntities, new MorphToFitStrategy(), _parentOuterCarcas)
                {
                    Scale = 0.2,
                    Distance = 25
                };

                extrapolateEntities = extrapolator.Extrapolate().ToList();
                allContours = selectedEntities.Concat(extrapolateEntities).ToList();
            }

            if (allContours.Count < 2) return;

            // 3. Строим каркас
            var category = allContours.Count <= 3 ? "C2" : "С1";
            var layer = new Layer(CarcasLayerNameController.GetNewName(category));
            _createdCarcas = new Carcas3D(allContours, layer).Build();

            if (_createdCarcas == null || !_createdCarcas.MeshTriangles.Any())
                throw new InvalidOperationException("Не удалось построить каркас.");

            _createdEntities.AddRange(_createdCarcas.MeshTriangles);

            if (extrapolateEntities != null)
            {
                _createdExtropolatesEntities.AddRange(extrapolateEntities);

                foreach (var exropolate in extrapolateEntities)
                    _createdExtropolates.Add(new DrawerObject(exropolate));
            }

            foreach (var face in _createdCarcas.MeshTriangles)
                _createdVisuals.Add(new DrawerObject(face));

            if (_createdCarcas.MeshTriangles.Count != 0)
            {
                var acicolor = selectedEntities[0]!.Color;
                var color = Color.FromArgb(150, acicolor.R, acicolor.G, acicolor.B);
                var solidModel = CarcasMeshBuilder.BuildFromFaces(_createdCarcas.MeshTriangles, color);

                var modelVisual = new ModelVisual3D { Content = solidModel };
                _modelObject = new DrawerObject(modelVisual)
                {
                    Color = color,
                    Tag = _createdCarcas
                };
            }
        }

        private Carcas3D? FindBoundingOuterCarcas(List<Polyline3D> innerContours)
        {
            var existingCarcasses = _viewport.Visuals
                .Concat(_viewport.HiddenVisuals)
                .Where(v => v.Tag is Carcas3D)
                .Select(v => (Carcas3D)v.Tag)
                .ToList();

            if (existingCarcasses.Count == 0) return null;

            var firstInner = innerContours.First();
            double innerCenterX = firstInner.Vertexes.Average(v => v.X);

            foreach (var carcas in existingCarcasses)
            {
                double minX = carcas.XPositions.Min();
                double maxX = carcas.XPositions.Max();

                if (innerCenterX >= minX && innerCenterX <= maxX)
                {
                    return carcas;
                }
            }

            return null;
        }
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

            var extrapolator = new Extrapolator(selectedEntities, new MorphToFitStrategy())
            {
                Scale = 0.2,
                Distance = 25,
            };

            var extrapolateEntities = extrapolator.Extrapolate().ToList();
            _createdEntities.AddRange(extrapolateEntities);

            foreach (var entity in extrapolateEntities)
                _createdVisuals.Add(new DrawerObject(entity));
        }
    }
}
