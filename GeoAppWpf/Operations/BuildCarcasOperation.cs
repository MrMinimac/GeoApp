using GeoAppWpf.Interfaces;
using GeoAppWpf.Models;
using GeoAppWpf.Services;
using HelixToolkit.Wpf;
using netDxf;
using netDxf.Entities;
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
        private readonly List<DrawerObject> _selectedVisuals;
        private DrawerObject? _modelObject;

        public BuildCarcasOperation(DxfDocument document, ViewportController viewport)
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
                BuildCarcas();

            // 2. Сохраняем в DXF документ
            _document?.Entities.Add(_createdEntities);

            // 3. Добавляем линии каркаса на экран
            foreach (var visual in _createdVisuals)
                _viewport.Add(visual);

            // 3. Добавляем визуал каркаса на экран
            _viewport.Add(_modelObject);

            // 4. Снимаем выделения
            _viewport.UnselectAll();
        }

        public void Undo()
        {
            // 1. Удаляем из документа сущности
            _document?.Entities.Remove(_createdEntities);

            // 2. Удаляем линии каркаса с экрана
            foreach (var visual in _createdVisuals)
                _viewport.Remove(visual);

            // 3. Удаляем визуал каркаса с экрана
            _viewport.Remove(_modelObject);

            // 5. Возвращаем выделения
            _viewport.SelectRange(_selectedVisuals);
        }

        public void BuildCarcas()
        {
            if (_selectedVisuals.Count() == 0)
                throw new Exception("Нет выделеных объектов.");

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
                var solidModel = CarcasMeshBuilder.BuildFromFaces(
                    faces,
                    Colors.Orange.ChangeAlpha(150)
                );

                var modelVisual = new ModelVisual3D { Content = solidModel };
                _modelObject = new DrawerObject(modelVisual);
                _modelObject.Color = Colors.Orange.ChangeAlpha(150);
            }
        }
    }
}
