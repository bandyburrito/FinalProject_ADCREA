using UnityEngine;
using ADCREA.Algorithms;

namespace ADCREA.Dungeon
{
    /// <summary>
    /// Creates a DungeonGenerator automatically when the scene contains a room template
    /// but nobody added a generator object yet. This keeps the project playable straight
    /// from version control: press Play and the floor generates with default settings.
    ///
    /// A hand-placed DungeonGenerator always wins - the bootstrap backs off, so the
    /// inspector can still be used to tune seeds, room counts and spacing.
    /// </summary>
    public static class DungeonBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGeneratorExists()
        {
            if (Object.FindAnyObjectByType<DungeonGenerator>() != null)
            {
                return;
            }

            // No room template means this is not the dungeon scene (for example a pure
            // pathfinding test scene) - generating there would only produce errors.
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
