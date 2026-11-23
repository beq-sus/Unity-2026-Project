
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Enemy Stats")]
    public int maxHealth = 30;
    public int clickDamage = 10;
    public int rewardCoins = 25;

    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void OnMouseDown()
    {
        if (currentHealth <= 0)
            return;

        currentHealth -= clickDamage;

        Debug.Log("Enemy HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnemyDefeated(rewardCoins);
        }
        else
        {
            Debug.LogError("GameManager not found!");
        }

        Destroy(gameObject);
    }
}
