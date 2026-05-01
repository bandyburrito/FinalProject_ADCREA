using UnityEngine;
using System.Collections.Generic;   
using System;

public class EnemyStatHolder : MonoBehaviour
{
    public string enemyName;
    public int maxHealth;
    public int enemyDamage;
    public float movementSpeed;
    public float attackRange;
    public Sprite enemySprite;
    
    public enum enemyType
    {
        Melee,
        Ranged,
        Boss
    }

    public class EnemyCreator
    {
        public string enemyName;
        public int maxHealth;
        public int enemyDamage;
        public float movementSpeed;
        public float attackRange;
        public Sprite enemySprite;
        public enemyType type;
        public float enemyAttackSpeed;

        public EnemyCreator(string name, int health, int damage, float speed, float range, Sprite sprite, enemyType enemyType, float attackSpeed = 1.0f)
        {
            this.enemyName = name;
            this.maxHealth = health;
            this.enemyDamage = damage;
            this.movementSpeed = speed;
            this.attackRange = range;
            this.enemySprite = sprite;
            this.type = enemyType;
            this.enemyAttackSpeed = attackSpeed;
        }
    }
        


}
    


