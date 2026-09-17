using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Gameplay.Run.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Run Catalog", fileName = "Run Catalog")]
    public sealed class RunCatalog : ScriptableObject
    {
        [SerializeField] private List<RunConfig> _runs = new();

        public IReadOnlyList<RunConfig> Runs => _runs;
    }
}
