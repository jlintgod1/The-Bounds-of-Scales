using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicEnemySpawner : MonoBehaviour
{
    // As the game progresses, harder enemies will spawn based on slight random chance and how many levels have been played.
    // RNG can give you an extra 5%, while completing 1.5 level loops in a row gives you 100%. If eligible, going over 100%
    // hard enemy chance can start spawning extra enemies (guaranteed 1 extra per extra 100%.)
    public List<GameObject> EasyEnemies;
    public List<GameObject> HardEnemies;
    public bool EligibleForDuplicates;
    [Tooltip("")]
    public int ThemeDependency = -1;
    // Start is called before the first frame update
    void Start()
    {
        int DuplicateChance = EligibleForDuplicates ? Mathf.FloorToInt(Random.Range(0f, Mathf.Max(GameManager.Instance.GlobalDifficulty - 1f, 0.01f))) : 1;
        for (int i = 0; i < DuplicateChance; i++)
        {
            float DifficultyChance = Random.value;
            GameObject newEnemy;
            if (ThemeDependency > -1)
                newEnemy = GameManager.Instance.CurrentLevelTheme.ThemeEnemies[ThemeDependency];
            else if (DifficultyChance <= Random.Range(0, 0.05f) + GameManager.Instance.GlobalDifficulty)
                newEnemy = HardEnemies[Random.Range(0, HardEnemies.Count)];
            else
                newEnemy = EasyEnemies[Random.Range(0, EasyEnemies.Count)];

            newEnemy = Instantiate(newEnemy, transform.position, transform.rotation, transform.parent);
        }
        Destroy(gameObject);
    }
}
