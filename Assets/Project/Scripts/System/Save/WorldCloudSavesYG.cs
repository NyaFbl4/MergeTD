using Project.Scripts.System.Save;

namespace YG
{
    public partial class SavesYG
    {
        public WorldSaveData world;

        // Kept only to migrate saves written before the world became a nested object.
        public string worldJson = string.Empty;
    }
}
