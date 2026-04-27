using System.Collections.Generic;
using UnityEngine;

// Tracks the player's performance using a Dictionary<string, float>.
// A Dictionary is the right structure here because the score is a named set
// of independent metrics (time, damageTaken) that need O(1) read/write access
// and can grow if more metrics are added without restructuring the scoring logic.
public class GameManager : MonoBehaviour
{
    private Dictionary<string, float> scoreData;
    private float levelStartTime;
    private bool levelIsActive;

    private const string KeyTime = "time";
    private const string KeyDamageTaken = "damageTaken";

    void Start()
    {
        scoreData = new Dictionary<string, float>();
        scoreData[KeyTime] = 0f;
        scoreData[KeyDamageTaken] = 0f;

        levelStartTime = Time.time;
        levelIsActive = true;

        Debug.Log("Level started. Timer running.");
    }

    void Update()
    {
        if (!levelIsActive)
        {
            return;
        }

        scoreData[KeyTime] = Time.time - levelStartTime;
    }

    // Called by player health scripts when damage is received
    public void RecordDamage(float damageAmount)
    {
        if (!levelIsActive)
        {
            return;
        }

        scoreData[KeyDamageTaken] += damageAmount;
        Debug.Log("Damage recorded. Total damage: " + scoreData[KeyDamageTaken]);
    }

    public void CompleteLevel()
    {
        if (!levelIsActive)
        {
            return;
        }

        levelIsActive = false;
        scoreData[KeyTime] = Time.time - levelStartTime;

        float grade = CalculateGrade();
        DisplayScore(grade);
    }

    // Grade formula: 100 base, minus time penalty (0.5 per second) and damage penalty (10 per hit-point).
    // Penalties are weighted so a slow but undamaged run scores roughly the same
    // as a fast but reckless one — both aspects of skill are tested.
    private float CalculateGrade()
    {
        float baseScore = 100f;
        float timePenalty = scoreData[KeyTime] * 0.5f;
        float damagePenalty = scoreData[KeyDamageTaken] * 10f;

        float grade = baseScore - timePenalty - damagePenalty;

        if (grade < 0f)
        {
            grade = 0f;
        }

        return grade;
    }

    private void DisplayScore(float grade)
    {
        string gradeLabel = GetGradeLabel(grade);

        Debug.Log("=== LEVEL COMPLETE ===");
        Debug.Log("Time:         " + scoreData[KeyTime].ToString("F1") + "s");
        Debug.Log("Damage taken: " + scoreData[KeyDamageTaken].ToString("F1"));
        Debug.Log("Score:        " + grade.ToString("F0") + " / 100  (" + gradeLabel + ")");
    }

    private string GetGradeLabel(float grade)
    {
        if (grade >= 90f)
        {
            return "S";
        }

        if (grade >= 75f)
        {
            return "A";
        }

        if (grade >= 60f)
        {
            return "B";
        }

        if (grade >= 40f)
        {
            return "C";
        }

        return "D";
    }

    public Dictionary<string, float> GetScoreData()
    {
        return scoreData;
    }

    public bool IsLevelActive()
    {
        return levelIsActive;
    }
}
