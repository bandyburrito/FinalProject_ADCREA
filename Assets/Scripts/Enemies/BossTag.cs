using System.Collections.Generic;
using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Enemies
{

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
