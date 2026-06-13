using System.Collections.Generic;
using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Weapons
{
    /// <summary>
    /// The player's weapons, stored in a LinkedList and hard-capped at two: the starting
    /// pick and the one unlocked by beating the first boss.
    ///
    /// A LinkedList still earns its place at this size: the equipped weapon is a cursor
    /// (a LinkedListNode), swapping with Q/E follows Previous/Next pointers, and the
    /// second weapon is inserted in O(1) without touching the cursor - the structure is
    /// the same whether the cap is two or twenty.
    /// </summary>
    public class WeaponInventory : MonoBehaviour
    {
        public const int MaxWeapons = 2;

        public KeyCode previousWeaponKey = KeyCode.Q;
        public KeyCode nextWeaponKey = KeyCode.E;

        private readonly LinkedList<WeaponInstance> _weapons = new LinkedList<WeaponInstance>();
        private LinkedListNode<WeaponInstance> _equipped;

        // Upgrades belong to the player, not to one weapon: this single shared object is
        // handed to every WeaponInstance, so a buff applies across the whole arsenal.
        private readonly PlayerWeaponStats _stats = new PlayerWeaponStats();

        /// <summary>The player's run-wide weapon upgrades, applied to every weapon held.</summary>
        public PlayerWeaponStats Stats
        {
            get { return _stats; }
        }

        public LinkedListNode<WeaponInstance> FirstNode
        {
            get { return _weapons.First; }
        }

        public LinkedListNode<WeaponInstance> EquippedNode
        {
            get { return _equipped; }
        }

        public WeaponInstance Equipped
        {
            get
            {
                if (_equipped == null)
                {
                    return null;
                }
                return _equipped.Value;
            }
        }

        public int Count
        {
            get { return _weapons.Count; }
        }

        private void Update()
        {
            // Menus and choice screens must not react to weapon-swap keys.
            if (!GameSession.IsPlaying)
            {
                return;
            }

            if (Input.GetKeyDown(nextWeaponKey))
            {
                CycleNext();
            }
            if (Input.GetKeyDown(previousWeaponKey))
            {
                CyclePrevious();
            }
        }

        public bool Owns(WeaponDefinition definition)
        {
            LinkedListNode<WeaponInstance> node = _weapons.First;
            while (node != null)
            {
                if (node.Value.Definition == definition)
                {
                    return true;
                }
                node = node.Next;
            }
            return false;
        }

        /// <summary>Adds a freshly chosen weapon and equips it. Refuses beyond the cap.</summary>
        public void AddWeapon(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return;
            }
            if (_weapons.Count >= MaxWeapons)
            {
                Debug.LogWarning("WeaponInventory: already carrying " + MaxWeapons + " weapons - ignoring " + definition.DisplayName);
                return;
            }

            _equipped = _weapons.AddLast(new WeaponInstance(definition, _stats));
            Debug.Log("Weapon acquired: " + definition.DisplayName);
        }

        /// <summary>
        /// Drops the currently equipped weapon and puts the new one in its place in the
        /// LinkedList. The run's upgrades carry over untouched - they belong to the player,
        /// not the weapon. Used by the post-boss choice once both slots are full.
        /// </summary>
        public void ReplaceEquipped(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return;
            }
            if (_equipped == null)
            {
                AddWeapon(definition);
                return;
            }

            string dropped = _equipped.Value.Definition.DisplayName;
            _equipped.Value = new WeaponInstance(definition, _stats);
            Debug.Log("Dropped " + dropped + " for " + definition.DisplayName);
        }

        /// <summary>
        /// Roguelike death rule: the next run starts with nothing - no weapons and no
        /// upgrades. The opening weapon choice fills the inventory again.
        /// </summary>
        public void ResetToEmpty()
        {
            _weapons.Clear();
            _equipped = null;
            _stats.Reset();
        }

        private void CycleNext()
        {
            if (_equipped == null || _weapons.Count < 2)
            {
                return;
            }

            if (_equipped.Next != null)
            {
                _equipped = _equipped.Next;
            }
            else
            {
                // Walk off the tail, reappear at the head - the list behaves like a ring
                // for the player even though the structure itself is linear.
                _equipped = _weapons.First;
            }
        }

        private void CyclePrevious()
        {
            if (_equipped == null || _weapons.Count < 2)
            {
                return;
            }

            if (_equipped.Previous != null)
            {
                _equipped = _equipped.Previous;
            }
            else
            {
                _equipped = _weapons.Last;
            }
        }
    }
}
