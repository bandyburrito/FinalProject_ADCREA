using System.Collections.Generic;
using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Enemies
{
    /// <summary>
    /// Marks an enemy as a boss so the HUD can draw its Isaac-style health bar. Keeps a
    /// static registry instead of making the HUD search the scene every OnGUI call -
    /// bosses register on enable and silently drop out when they die.
    /// </summary>
    public class BossTag : MonoBehaviour
    {
        private static readonly List<BossTag> ActiveList = new List<BossTag>();

        public static IReadOnlyList<BossTag> Active
        {
            get { return ActiveList; }
        }

        public EnemyHealth Health;
        public DungeonRoom Room;
        public SpriteRenderer Icon;

        private void OnEnable()
        {
            ActiveList.Add(this);
        }

        private void OnDisable()
        {
            ActiveList.Remove(this);
        }
    }
}
