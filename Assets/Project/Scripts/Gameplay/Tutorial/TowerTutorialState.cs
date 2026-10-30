using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.System.Save;

namespace Project.Scripts.Gameplay.Tutorial
{
    public enum TowerTutorialStep
    {
        BuyFirst = 0,
        BuySecond = 1,
        Merge = 2,
        StartWave = 3,
        Completed = 4,
        BuyThird = 5
    }

    [Serializable]
    public sealed class TowerTutorialSaveData
    {
        public int version = 1;
        public TowerTutorialStep step;
        public int towerLevel = 1;

        public static TowerTutorialSaveData ForExistingPlayer() => new() { step = TowerTutorialStep.Completed };

        public void Normalize()
        {
            if (version != 1 || step < TowerTutorialStep.BuyFirst || step > TowerTutorialStep.BuyThird)
                step = TowerTutorialStep.Completed;
            towerLevel = Math.Max(1, towerLevel);
        }
    }

    public sealed class TowerTutorialInput
    {
        public bool Active { get; private set; }
        public bool IsDragging { get; set; }
        public TowerTutorialStep Step { get; private set; }
        public int TowerLevel { get; private set; }
        private string _firstSlot;
        private string _secondSlot;

        public bool CanStartWave => !Active || Step == TowerTutorialStep.StartWave;

        public void Set(TowerTutorialStep step, int level, string firstSlot, string secondSlot)
        {
            Active = true;
            Step = step;
            TowerLevel = level;
            _firstSlot = firstSlot;
            _secondSlot = secondSlot;
        }

        public void Clear()
        {
            Active = false;
            IsDragging = false;
        }

        public bool AllowsPurchase(ETowerType type, int level) => !Active
            || ((Step == TowerTutorialStep.BuyFirst || Step == TowerTutorialStep.BuySecond || Step == TowerTutorialStep.BuyThird)
                && type == ETowerType.Combat && level == TowerLevel);

        public bool AllowsDrag(string slot) => !Active
            || (Step == TowerTutorialStep.Merge && (slot == _firstSlot || slot == _secondSlot));

        public bool AllowsDrop(string source, string target) => !Active
            || (Step == TowerTutorialStep.Merge && source != target && AllowsDrag(source) && AllowsDrag(target));
    }

    public static class TowerTutorialRules
    {
        public static TowerTutorialStep Reconcile(TowerTutorialSaveData progress, IReadOnlyList<WorldTowerSaveData> towers)
        {
            if (progress.step == TowerTutorialStep.StartWave || progress.step == TowerTutorialStep.Completed)
                return progress.step;

            var count = 0;
            foreach (var tower in towers)
            {
                if (tower.towerType != ETowerType.Combat)
                    continue;
                if (progress.step == TowerTutorialStep.Merge && tower.towerLevel > progress.towerLevel)
                    return TowerTutorialStep.StartWave;
                if (tower.towerLevel == progress.towerLevel)
                    count++;
            }

            return count >= 3 ? TowerTutorialStep.Merge
                : count == 2 ? TowerTutorialStep.BuyThird
                : count == 1 ? TowerTutorialStep.BuySecond : TowerTutorialStep.BuyFirst;
        }
    }
}
