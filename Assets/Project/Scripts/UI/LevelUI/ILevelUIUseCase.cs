using Project.Scripts.Configs;
using UnityEngine;

namespace Project.Scripts.UI.LevelUI
{
    public interface ILevelUIUseCase
    {
        void TryBuyTower();
        void OpenShop();
        TowerConfig GetSelectedTowerConfig();
    }
}