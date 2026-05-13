using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Systems.UI
{
    public class LayoutsManager
    {
        private readonly LayoutsRepository _repository;
        private readonly Transform _parent;
        private readonly List<LayoutViewBase> _instances = new();

        public IReadOnlyList<LayoutViewBase> Instances => _instances;

        public LayoutsManager(LayoutsRepository repository, Transform parent)
        {
            _repository = repository;
            _parent = parent;
        }

        public void Initialize()
        {
            if (_repository == null || _repository.Views == null)
                return;

            foreach (var prefab in _repository.Views)
            {
                if (prefab == null)
                    continue;

                var instance = Object.Instantiate(prefab, _parent);
                _instances.Add(instance);
            }
        }
    }
}
