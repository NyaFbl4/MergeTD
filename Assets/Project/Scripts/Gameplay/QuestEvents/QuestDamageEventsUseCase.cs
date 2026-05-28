using System;
using MessagePipe;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Enemies;
using Project.Scripts.Gameplay.QuestEvents;
using VContainer.Unity;

namespace Project.Scripts.System.UseCases
{
    public class QuestDamageEventsUseCase : IInitializable, IDisposable
    {
        private readonly IPublisher<DamageDealtQuestEventDTO> _damagePublisher;

        public QuestDamageEventsUseCase(IPublisher<DamageDealtQuestEventDTO> damagePublisher)
        {
            _damagePublisher = damagePublisher;
        }

        public void Initialize()
        {
            EnemyHealth.DamageTaken += OnDamageTaken;
        }

        public void Dispose()
        {
            EnemyHealth.DamageTaken -= OnDamageTaken;
        }

        private void OnDamageTaken(EnemyConfig config, int damage, bool isCritical)
        {
            _damagePublisher.Publish(new DamageDealtQuestEventDTO(
                damage,
                isCritical,
                config.EnemyType));
        }
    }
}