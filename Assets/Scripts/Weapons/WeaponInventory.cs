using System.Collections.Generic;
using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Weapons
{

    public class WeaponInventory : MonoBehaviour
    {
        public const int MaxWeapons = 2;

        public KeyCode previousWeaponKey = KeyCode.Q;
        public KeyCode nextWeaponKey = KeyCode.E;

        private readonly LinkedList<WeaponInstance> _weapons = new LinkedList<WeaponInstance>();
        private LinkedListNode<WeaponInstance> _equipped;

        private readonly PlayerWeaponStats _stats = new PlayerWeaponStats();

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
