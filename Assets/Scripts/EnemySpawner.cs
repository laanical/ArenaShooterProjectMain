using UnityEngine;
using System.Collections; // Required for using Coroutines

// This script handles spawning different types of enemies at various spawn points.
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("A list of different enemy prefabs to spawn.")]
    public GameObject[] enemyPrefabs; // Array to hold Melee, Ranged, etc.

    [Tooltip("A list of locations where enemies can be spawned.")]
    public Transform[] spawnPoints;

    [Tooltip("The time in seconds between each spawn.")]
    public float spawnInterval = 3.0f;

    [Tooltip("The maximum number of enemies allowed in the scene at one time.")]
    public int maxEnemies = 10;

    // --- Private Fields ---
    private int currentEnemyCount = 0; // Tracks the number of active enemies.

    // This is a static reference to the spawner itself, so other scripts can easily find it.
    public static EnemySpawner instance;

    void Awake()
    {
        // --- Singleton Pattern ---
        // This ensures there is only ever one instance of the EnemySpawner.
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // If another spawner already exists, destroy this one.
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Check if prefabs or spawn points are assigned.
        if (enemyPrefabs.Length == 0)
        {
            Debug.LogError("Enemy Spawner: No enemy prefabs assigned!", this);
            return;
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogError("Enemy Spawner: No spawn points assigned!", this);
            return;
        }

        // Start the spawning process.
        StartCoroutine(SpawnEnemies());
    }

    // This is the main spawning loop.
    private IEnumerator SpawnEnemies()
    {
        // This loop will run forever as long as the script is active.
        while (true)
        {
            // Wait for the specified interval before trying to spawn a new enemy.
            yield return new WaitForSeconds(spawnInterval);

            // Check if we are below the maximum enemy limit.
            if (currentEnemyCount < maxEnemies)
            {
                SpawnSingleEnemy();
            }
        }
    }

    private void SpawnSingleEnemy()
    {
        // 1. Pick a random enemy prefab from our list.
        int randomEnemyIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject enemyToSpawn = enemyPrefabs[randomEnemyIndex];

        // 2. Pick a random spawn point from our list.
        int randomSpawnIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnLocation = spawnPoints[randomSpawnIndex];

        // 3. Instantiate the chosen enemy at the chosen spawn point.
        Instantiate(enemyToSpawn, spawnLocation.position, spawnLocation.rotation);

        // 4. Increment our count of active enemies.
        currentEnemyCount++;
        Debug.Log($"Spawned an enemy. Current count: {currentEnemyCount}/{maxEnemies}");
    }

    // This public method can be called by other scripts (like EnemyHealth) when an enemy is defeated.
    public void EnemyDefeated()
    {
        // Decrement the enemy count.
        currentEnemyCount--;
        if (currentEnemyCount < 0) currentEnemyCount = 0; // Prevent it from going below zero.
        Debug.Log($"An enemy was defeated. Current count: {currentEnemyCount}/{maxEnemies}");
    }
}