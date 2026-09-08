using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.Gameplay.Towers;

namespace Project.Scripts.System.Save
{
    public sealed class WorldService : IWorldService
    {
        private const int CurrentVersion = 1;

        private readonly WorldSaveService _saveService;
        private readonly RunConfig _runConfig;
        private WorldSaveData _data;

        public int Gold => _data.gold;
        public int Gems => _data.gems;
        public int MaxBaseHealth => _data.maxBaseHealth;
        public int MaxEnergy => _data.maxEnergy;
        public IReadOnlyList<WorldTowerSaveData> Towers => _data.towers;
        public IReadOnlyList<SpellProgressSaveData> Spells => _data.spells;
        public bool HasPersistedData { get; private set; }

        public event Action<int> GoldChanged;
        public event Action<int> GemsChanged;
        public event Action<int> MaxBaseHealthChanged;
        public event Action<int> MaxEnergyChanged;
        public event Action TowersChanged;
        public event Action SpellsChanged;

        public WorldService(WorldSaveService saveService, RunConfig runConfig)
        {
            _saveService = saveService;
            _runConfig = runConfig;
            HasPersistedData = _saveService.TryLoad(out _data);

            if (!HasPersistedData)
                _data = CreateDefaults();

            Normalize();

            if (HasPersistedData)
                _saveService.Save(_data);
        }

        public bool CanSpendGold(int amount) => amount > 0 && Gold >= amount;

        public bool TrySpendGold(int amount)
        {
            if (!CanSpendGold(amount))
                return false;

            _data.gold -= amount;
            Save();
            GoldChanged?.Invoke(Gold);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            _data.gold = AddClamped(_data.gold, amount);
            Save();
            GoldChanged?.Invoke(Gold);
        }

        public void AddGems(int amount)
        {
            if (amount <= 0)
                return;

            _data.gems = AddClamped(_data.gems, amount);
            Save();
            GemsChanged?.Invoke(Gems);
        }

        public bool TrySpendGems(int amount)
        {
            if (amount <= 0 || Gems < amount)
                return false;

            _data.gems -= amount;
            Save();
            GemsChanged?.Invoke(Gems);
            return true;
        }

        public void SetMaxBaseHealth(int value)
        {
            var safeValue = Math.Max(1, value);
            if (_data.maxBaseHealth == safeValue)
                return;

            _data.maxBaseHealth = safeValue;
            Save();
            MaxBaseHealthChanged?.Invoke(MaxBaseHealth);
        }

        public void SetMaxEnergy(int value)
        {
            var safeValue = Math.Max(1, value);
            if (_data.maxEnergy == safeValue)
                return;

            _data.maxEnergy = safeValue;
            Save();
            MaxEnergyChanged?.Invoke(MaxEnergy);
        }

        public void SetTower(string slotId, int towerLevel, ETowerType towerType)
        {
            if (string.IsNullOrWhiteSpace(slotId))
                throw new ArgumentException("A persistent slot id is required.", nameof(slotId));

            var safeLevel = Math.Max(1, towerLevel);
            var tower = FindTower(slotId);

            if (tower == null)
            {
                _data.towers.Add(new WorldTowerSaveData(slotId, safeLevel, towerType));
            }
            else
            {
                if (tower.towerLevel == safeLevel && tower.towerType == towerType)
                    return;

                tower.towerLevel = safeLevel;
                tower.towerType = towerType;
            }

            Save();
            TowersChanged?.Invoke();
        }

        public void RemoveTower(string slotId)
        {
            var tower = FindTower(slotId);
            if (tower == null)
                return;

            _data.towers.Remove(tower);
            Save();
            TowersChanged?.Invoke();
        }

        public void MoveTower(
            string sourceSlotId,
            string targetSlotId,
            int towerLevel,
            ETowerType towerType)
        {
            if (string.IsNullOrWhiteSpace(sourceSlotId))
                throw new ArgumentException("A source slot id is required.", nameof(sourceSlotId));

            if (string.IsNullOrWhiteSpace(targetSlotId))
                throw new ArgumentException("A target slot id is required.", nameof(targetSlotId));

            var sourceTower = FindTower(sourceSlotId);
            if (sourceTower != null)
                _data.towers.Remove(sourceTower);

            var targetTower = FindTower(targetSlotId);
            if (targetTower == null)
            {
                _data.towers.Add(new WorldTowerSaveData(
                    targetSlotId,
                    Math.Max(1, towerLevel),
                    towerType));
            }
            else
            {
                targetTower.towerLevel = Math.Max(1, towerLevel);
                targetTower.towerType = towerType;
            }

            Save();
            TowersChanged?.Invoke();
        }

        public bool IsSpellUnlocked(string spellId)
        {
            var spell = FindSpell(spellId);
            return spell != null && spell.isUnlocked;
        }

        public int GetSpellLevel(string spellId)
        {
            var spell = FindSpell(spellId);
            return spell != null && spell.isUnlocked ? spell.level : 0;
        }

        public void UnlockSpell(string spellId)
        {
            var spell = GetOrCreateSpell(spellId);
            if (spell.isUnlocked)
                return;

            spell.isUnlocked = true;
            spell.level = Math.Max(1, spell.level);
            Save();
            SpellsChanged?.Invoke();
        }

        public void SetSpellLevel(string spellId, int level)
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level), "Spell level must be at least 1.");

            var spell = GetOrCreateSpell(spellId);
            if (spell.isUnlocked && spell.level == level)
                return;

            spell.isUnlocked = true;
            spell.level = level;
            Save();
            SpellsChanged?.Invoke();
        }

        public void ImportLegacy(
            int gold,
            int maxBaseHealth,
            IReadOnlyList<WorldTowerSaveData> towers)
        {
            if (HasPersistedData)
                return;

            _data.gold = Math.Max(0, gold);
            _data.maxBaseHealth = Math.Max(1, maxBaseHealth);
            _data.towers.Clear();

            if (towers != null)
            {
                for (var i = 0; i < towers.Count; i++)
                {
                    var tower = towers[i];
                    if (tower == null || string.IsNullOrWhiteSpace(tower.slotId))
                        continue;

                    _data.towers.Add(new WorldTowerSaveData(
                        tower.slotId,
                        Math.Max(1, tower.towerLevel),
                        tower.towerType));
                }
            }

            HasPersistedData = true;
            Save();
            GoldChanged?.Invoke(Gold);
            MaxBaseHealthChanged?.Invoke(MaxBaseHealth);
            TowersChanged?.Invoke();
        }

        public void Reset()
        {
            _data = CreateDefaults();
            Save();
            GoldChanged?.Invoke(Gold);
            GemsChanged?.Invoke(Gems);
            MaxBaseHealthChanged?.Invoke(MaxBaseHealth);
            MaxEnergyChanged?.Invoke(MaxEnergy);
            TowersChanged?.Invoke();
            SpellsChanged?.Invoke();
        }

        private WorldSaveData CreateDefaults()
        {
            return new WorldSaveData
            {
                version = CurrentVersion,
                gold = Math.Max(0, _runConfig.StartGold),
                gems = 0,
                maxBaseHealth = Math.Max(1, _runConfig.StartBaseHealth),
                maxEnergy = Math.Max(1, _runConfig.StartMaxEnergy)
            };
        }

        private void Normalize()
        {
            _data.version = CurrentVersion;
            _data.gold = Math.Max(0, _data.gold);
            _data.gems = Math.Max(0, _data.gems);
            _data.maxBaseHealth = _data.maxBaseHealth > 0
                ? _data.maxBaseHealth
                : Math.Max(1, _runConfig.StartBaseHealth);
            _data.maxEnergy = _data.maxEnergy > 0
                ? _data.maxEnergy
                : Math.Max(1, _runConfig.StartMaxEnergy);
            _data.towers ??= new List<WorldTowerSaveData>();
            _data.spells ??= new List<SpellProgressSaveData>();

            var towerSlotIds = new HashSet<string>();
            for (var i = _data.towers.Count - 1; i >= 0; i--)
            {
                var tower = _data.towers[i];
                if (tower == null
                    || string.IsNullOrWhiteSpace(tower.slotId)
                    || tower.towerLevel < 1
                    || !towerSlotIds.Add(tower.slotId))
                {
                    _data.towers.RemoveAt(i);
                }
            }

            var spellIds = new HashSet<string>();
            for (var i = _data.spells.Count - 1; i >= 0; i--)
            {
                var spell = _data.spells[i];
                if (spell == null
                    || string.IsNullOrWhiteSpace(spell.spellId)
                    || !spellIds.Add(spell.spellId))
                {
                    _data.spells.RemoveAt(i);
                    continue;
                }

                if (!spell.isUnlocked)
                    spell.level = 0;
                else
                    spell.level = Math.Max(1, spell.level);
            }
        }

        private WorldTowerSaveData FindTower(string slotId)
        {
            for (var i = 0; i < _data.towers.Count; i++)
            {
                if (_data.towers[i].slotId == slotId)
                    return _data.towers[i];
            }

            return null;
        }

        private SpellProgressSaveData FindSpell(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId))
                throw new ArgumentException("A spell id is required.", nameof(spellId));

            for (var i = 0; i < _data.spells.Count; i++)
            {
                if (_data.spells[i].spellId == spellId)
                    return _data.spells[i];
            }

            return null;
        }

        private SpellProgressSaveData GetOrCreateSpell(string spellId)
        {
            var spell = FindSpell(spellId);
            if (spell != null)
                return spell;

            spell = new SpellProgressSaveData(spellId, false, 0);
            _data.spells.Add(spell);
            return spell;
        }

        private void Save()
        {
            HasPersistedData = true;
            _saveService.Save(_data);
        }

        private static int AddClamped(int current, int amount)
        {
            return current > int.MaxValue - amount ? int.MaxValue : current + amount;
        }
    }
}
