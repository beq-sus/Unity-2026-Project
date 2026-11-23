
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject[] enemyPrefabs;

    [Header("Wave Settings")]
    public int waveCount = 3;
    public int firstWaveEnemies = 6;
    public int extraEnemiesPerWave = 2;
    public float spawnInterval = 4f;
    public float timeBetweenWaves = 3f;

    [Header("UI")]
    public TMP_Text waveText;

    private int previousEnemyType = -1;

    private List<GameObject> waveEnemies =
        new List<GameObject>();

    private bool GameEnded()
    {
        return GameManager.Instance != null &&
               GameManager.Instance.IsGameEnded;
    }

    IEnumerator Start()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length != 3)
        {
            Debug.LogError("Assign 3 enemy prefabs!");
            yield break;
        }

        foreach (GameObject prefab in enemyPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogError("Missing enemy prefab!");
                yield break;
            }
        }

        for (int wave = 1; wave <= waveCount; wave++)
        {
            if (GameEnded())
                yield break;

            if (waveText != null)
                waveText.text = "WAVE " + wave;

            Debug.Log("WAVE " + wave + " STARTED!");

            waveEnemies.Clear();
            previousEnemyType = -1;

            int enemyCount =
                firstWaveEnemies +
                (wave - 1) * extraEnemiesPerWave;

            for (int i = 0; i < enemyCount; i++)
            {
                if (GameEnded())
                    yield break;

                SpawnEnemy(wave, i);

                if (i < enemyCount - 1)
                    yield return new WaitForSeconds(spawnInterval);
            }

            // Wait until all enemies are destroyed
            // or the game has ended.
            yield return new WaitUntil(
                () => GameEnded() ||
                waveEnemies.TrueForAll(enemy => enemy == null)
            );

            if (GameEnded())
                yield break;

            Debug.Log("WAVE " + wave + " COMPLETED!");

            if (wave < waveCount)
            {
                yield return new WaitForSeconds(
                    timeBetweenWaves
                );
            }
        }

        if (GameEnded())
            yield break;

        if (waveText != null)
            waveText.text = "WAVES CLEARED!";

        Debug.Log("ALL WAVES COMPLETED!");

        // Automatic victory
        if (GameManager.Instance != null)
        {
            GameManager.Instance.WinGame();
        }
    }

    void SpawnEnemy(int wave, int index)
    {
        int type = ChooseEnemyType(wave, index);
        GameObject prefab = enemyPrefabs[type];

        float x = Random.value < 0.5f ? -5f : 5f;
        float y = Random.Range(-2f, 2f);

        Vector3 position = new Vector3(x, y, 0f);

        GameObject enemy = Instantiate(
            prefab,
            position,
            Quaternion.identity
        );

        waveEnemies.Add(enemy);
        previousEnemyType = type;
    }

    int ChooseEnemyType(int wave, int index)
    {
        if (index == 0)
            return 0;

        float normalChance;
        float fastChance;

        if (wave == 1)
        {
            normalChance = 60f;
            fastChance = 30f;
        }
        else if (wave == 2)
        {
            normalChance = 45f;
            fastChance = 35f;
        }
        else
        {
            normalChance = 30f;
            fastChance = 35f;
        }

        if (previousEnemyType == 2)
        {
            float safeRoll = Random.Range(
                0f,
                normalChance + fastChance
            );

            return safeRoll < normalChance ? 0 : 1;
        }

        float roll = Random.Range(0f, 100f);

        if (roll < normalChance)
            return 0;

        if (roll < normalChance + fastChance)
            return 1;

        return 2;
    }
}
