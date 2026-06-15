using UnityEngine;
using ADCREA.Algorithms;

namespace ADCREA.Dungeon
{

    public static class DungeonBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGeneratorExists()
        {
            if (Object.FindAnyObjectByType<DungeonGenerator>() != null)
            {
                return;
            }

            if (Object.FindAnyObjectByType<RoomGrid>() == null)
            {
                return;
            }

            var host = new GameObject("Dungeon (auto-created)");
            host.AddComponent<DungeonGenerator>();
            Debug.Log("DungeonBootstrap: no DungeonGenerator in the scene - created one with default settings. "
                + "Add your own 'Dungeon' GameObject with a DungeonGenerator component to tune it in the inspector.");
        }
    }
}
