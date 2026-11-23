
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public TowerHealth target;
    public float damage = 10f;
    public float attackInterval = 1f;
    public float attackRange = 1.05f;

    private float timer = 0f;

    void Start()
    {
        // Автоматически находим башню
        if (target == null)
        {
            target = FindFirstObjectByType<TowerHealth>();

            if (target == null)
                Debug.LogError("EnemyAttack: Tower not found!");
        }
    }

    void Update()
    {
        if (target == null)
            return;

        float distance = Vector2.Distance(
            transform.position,
            target.transform.position
        );

        if (distance > attackRange)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;

        if (timer >= attackInterval)
        {
            timer = 0f;
            target.TakeDamage(damage);
        }
    }
}
