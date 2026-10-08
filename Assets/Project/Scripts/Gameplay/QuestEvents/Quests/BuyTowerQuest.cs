using System;
using MessagePipe;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.System.UseCases;
using Project.Scripts.System.Save;

namespace Project.Scripts.Gameplay.Quests
{
    public class BuyTowerQuest : QuestRuntimeBase<BuyTowerQuestConfig>
    {
        private readonly IDisposable _subscription;

        public BuyTowerQuest(
            BuyTowerQuestConfig config,
            IPlayerStatsUseCase playerStats,
            IWorldService world,
            ISubscriber<TowerBoughtQuestEventDTO> subscriber,
            EQuestCategory category,
            int targetValue,
            int rewardGold,
            int rewardGems) : base(config, playerStats, world, category, targetValue, rewardGold, rewardGems)
        {
            _subscription = subscriber.Subscribe(OnTowerBought);
        }

        private void OnTowerBought(TowerBoughtQuestEventDTO dto)
        {
            if (dto.TowerLevel < _config.MinTowerLevel)
                return;

            AddProgress(1);
        }

        public override void Dispose()
        {
            _subscription.Dispose();
        }
    }
}
