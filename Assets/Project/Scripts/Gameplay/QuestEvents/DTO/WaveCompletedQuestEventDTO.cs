namespace Project.Scripts.Gameplay.QuestEvents
{
    public readonly struct WaveCompletedQuestEventDTO
    {
        public readonly int WaveNumber;
        public readonly bool IsLastWave;

        public WaveCompletedQuestEventDTO(int waveNumber, bool isLastWave)
        {
            WaveNumber = waveNumber;
            IsLastWave = isLastWave;
        }
    }
}