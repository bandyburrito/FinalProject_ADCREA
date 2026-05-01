using UnityEngine;

public class EnemyList : EnemyStatHolder
{

    public EnemyCreator[] enemyList;

    //add enemies to the list
    void Start()
    {
        enemyList = new EnemyCreator[]
        {
            new EnemyCreator("Goblin", 100, 20, 2.0f, 3.0f, Resources.Load<Sprite>("Sprites/Goblin"), enemyType.Melee),
            new EnemyCreator("Orc", 150, 30, 1.5f, 4.0f, Resources.Load<Sprite>("Sprites/Orc"), enemyType.Melee),
            new EnemyCreator("Dragon", 300, 50, 1.0f, 6.0f, Resources.Load<Sprite>("Sprites/Dragon"), enemyType.Boss, 0.5f)
        };
    }





}

