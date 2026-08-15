using GeoAppWpf.Interfaces;
using GeoAppWpf.Models;
using GeoAppWpf.Services;
using netDxf;
using netDxf.Entities;

namespace GeoAppWpf.Operations
{
    public class ExtrapolateOperation : IUndoableCommand
    {
        private readonly DxfDocument _document;
        private readonly ViewportController _viewport;
        private readonly List<EntityObject> _createdEntities = new();
        private readonly List<DrawerObject> _createdVisuals = new();
        private readonly List<DrawerObject> _selectedVisuals;

        private double _distance;

        public ExtrapolateOperation(DxfDocument document, ViewportController viewport, double distance)
        {
            _document = document;
            _viewport = viewport;
            _distance = distance;

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
                Distance = _distance,
            };

            var extrapolateEntities = extrapolator.Extrapolate().ToList();
            _createdEntities.AddRange(extrapolateEntities);

            foreach (var entity in extrapolateEntities)
                _createdVisuals.Add(new DrawerObject(entity));
        }
    }
}
