using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.System.Save;
using UnityEngine;

namespace Project.Scripts.Gameplay.Run
{
    public sealed class RunSelectionService : IRunSelectionService
    {
        private const string SelectedRunIdKey = "merge_td_selected_run_id";

        private readonly RunCatalog _catalog;
        private readonly IWorldService _world;
        private int _selectedIndex;

        public int Count => _catalog.Runs.Count;
        public int SelectedIndex => _selectedIndex;
        public RunConfig SelectedRun => _catalog.Runs[_selectedIndex];
        public RunConfig PreviousRun => _selectedIndex > 0 ? _catalog.Runs[_selectedIndex - 1] : null;
        public bool IsSelectedRunUnlocked
        {
            get
            {
                if (_selectedIndex == 0)
                    return true;

                var previousRunId = PreviousRun.Id;
                foreach (var completedRunId in _world.CompletedRunIds)
                {
                    if (string.Equals(completedRunId, previousRunId, StringComparison.Ordinal))
                        return true;
                }

                return false;
            }
        }

        public event Action<RunConfig> SelectionChanged;

        public RunSelectionService(RunCatalog catalog, IWorldService world)
        {
            _catalog = catalog;
            _world = world;

            if (_catalog.Runs.Count == 0)
                throw new InvalidOperationException("Run Catalog must contain at least one Run Config.");

            ValidateCatalog();
            _selectedIndex = FindSavedRunIndex();
            SaveSelection();
        }

        public void SelectNext()
        {
            Select((_selectedIndex + 1) % Count);
        }

        public void SelectPrevious()
        {
            Select((_selectedIndex - 1 + Count) % Count);
        }

        private void Select(int index)
        {
            if (_selectedIndex == index)
                return;

            _selectedIndex = index;
            SaveSelection();
            SelectionChanged?.Invoke(SelectedRun);
        }

        private int FindSavedRunIndex()
        {
            var savedId = PlayerPrefs.GetString(SelectedRunIdKey, string.Empty);

            for (var i = 0; i < Count; i++)
            {
                if (string.Equals(_catalog.Runs[i].Id, savedId, StringComparison.Ordinal))
                    return i;
            }

            return 0;
        }

        private void SaveSelection()
        {
            PlayerPrefs.SetString(SelectedRunIdKey, SelectedRun.Id);
            PlayerPrefs.Save();
        }

        private void ValidateCatalog()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < Count; i++)
            {
                var run = _catalog.Runs[i];
                if (run == null)
                    throw new InvalidOperationException($"Run Catalog contains an empty entry at index {i}.");

                if (string.IsNullOrWhiteSpace(run.Id))
                    throw new InvalidOperationException($"Run Config '{run.name}' must have a stable ID.");

                if (!ids.Add(run.Id))
                    throw new InvalidOperationException($"Run Config ID '{run.Id}' is duplicated.");
            }
        }
    }
}
