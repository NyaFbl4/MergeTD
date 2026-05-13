using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Systems.UI
{
    [Serializable]
    [CreateAssetMenu(fileName = "Layouts Repository", menuName = "Configs/Repository")]
    public class LayoutsRepository : ScriptableObject, ILayoutRepository
    {
        [SerializeField] private List<LayoutViewBase> _layoutViews;
        public List<LayoutViewBase> Views => _layoutViews;
    }
}
