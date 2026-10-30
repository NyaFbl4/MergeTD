using System;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Towers;
using UnityEngine;

namespace Project.Scripts.Gameplay.Tutorial
{
    public enum SpellTutorialStep
    {
        WaitingForPreparation = 0,
        BuyGenerator = 1,
        StartWave = 2,
        AccumulateEnergy = 3,
        SelectSpell = 4,
        SelectTarget = 5,
        Completed = 6
    }

    [Serializable]
    public sealed class SpellTutorialSaveData
    {
        public int version = 1;
        public SpellTutorialStep step;

        public static SpellTutorialSaveData ForExistingPlayer() => new() { step = SpellTutorialStep.Completed };

        public void Normalize()
        {
            if (version != 1 || step < SpellTutorialStep.WaitingForPreparation || step > SpellTutorialStep.Completed)
                step = SpellTutorialStep.Completed;
        }
    }

    public sealed class SpellTutorialInput
    {
        public bool Active { get; private set; }
        public SpellTutorialStep Step { get; private set; }
        private Vector2 _target;
        private float _targetRadius;

        public bool CanStartWave => !Active || Step == SpellTutorialStep.StartWave;

        public void Set(SpellTutorialStep step, Vector2 target = default, float targetRadius = 0)
        {
            Active = true;
            Step = step;
            _target = target;
            _targetRadius = Mathf.Max(0, targetRadius);
        }

        public void Clear() => Active = false;

        public bool AllowsPurchase(ETowerType type, int level) => !Active
            || (Step == SpellTutorialStep.BuyGenerator && type == ETowerType.Generator && level == 1);

        public bool AllowsTarget(Vector2 center) => !Active
            || (Step == SpellTutorialStep.SelectTarget && (center - _target).sqrMagnitude <= _targetRadius * _targetRadius);
    }

    public static class SpellTutorialRules
    {
        public static bool CanBegin(TowerTutorialStep towerStep, string runId, int wave, ERunPhase phase) =>
            towerStep == TowerTutorialStep.Completed && runId == "default-run-1"
            && wave >= 2 && phase == ERunPhase.Preparation;

        public static SpellTutorialStep PreparationStep(bool hasGenerator) =>
            hasGenerator ? SpellTutorialStep.StartWave : SpellTutorialStep.BuyGenerator;

        public static bool CanPrompt(int energy, int cost, bool spellReady, bool hasEnemy, bool running) =>
            cost >= 0 && energy >= cost && spellReady && hasEnemy && running;

        public static bool CanCountCast(SpellTutorialStep step, ERunPhase phase) =>
            phase == ERunPhase.Wave && step >= SpellTutorialStep.AccumulateEnergy && step < SpellTutorialStep.Completed;

        public static bool IsTargetInView(Vector3 viewport) => viewport.z > 0
            && viewport.x > .06f && viewport.x < .94f && viewport.y > .64f && viewport.y < .9f;
    }
}
