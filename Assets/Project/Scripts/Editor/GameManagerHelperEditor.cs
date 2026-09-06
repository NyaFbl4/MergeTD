using Project.Scripts.GameManager;
using UnityEditor;
using UnityEngine;

namespace Project.Scripts.Editor
{
    [CustomEditor(typeof(GameManagerHelper))]
    public class GameManagerHelperEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Game Manager Actions", EditorStyles.boldLabel);

            var helper = (GameManagerHelper)target;

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Start Game"))
                    helper.StartGame();

                if (GUILayout.Button("Start Wave"))
                    helper.StartWave();

                if (GUILayout.Button("Finish Game"))
                    helper.FinishGame();

                if (GUILayout.Button("Pause Game"))
                    helper.PauseGame();

                if (GUILayout.Button("Resume Game"))
                    helper.ResumeGame();

                if (GUILayout.Button("Spawn Tower To Slot"))
                    helper.SpawnTowerToSpawnSlot();

                if (GUILayout.Button("Move Spawned Tower To Active Slot"))
                    helper.MoveSpawnedTowerToActiveSlot();
            }

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("Auto Detect Slot Types By Name"))
                helper.AutoDetectSlotTypesByName();
        }
    }
}
