using System;
using MessagePipe;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.System.UseCases;

namespace Project.Scripts.Gameplay.Quests
{
    public class BuyTowerQuest : QuestRuntimeBase<BuyTowerQuestConfig>
    {
        private readonly IDisposable _subscription;

        public BuyTowerQuest(
            BuyTowerQuestConfig config,
            IPlayerStatsUseCase playerStats,
            ISubscriber<TowerBoughtQuestEventDTO> subscriber) : base(config, playerStats)
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