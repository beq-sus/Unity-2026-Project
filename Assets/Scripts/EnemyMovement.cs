
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public Transform target;
    public float speed = 1.5f;
    public float stoppingDistance = 1f;

    void Start()
    {
        // Автоматически находим башню
        if (target == null)
        {
            TowerHealth tower = FindFirstObjectByType<TowerHealth>();

            if (tower != null)
                target = tower.transform;
            else
                Debug.LogError("EnemyMovement: Tower not found!");
        }
    }

    void Update()
    {
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;
        direction.z = 0f;

        float distance = direction.magnitude;

        if (distance <= stoppingDistance)
            return;

        float step = Mathf.Min(
            speed * Time.deltaTime,
            distance - stoppingDistance
        );

        transform.position += direction.normalized * step;
    }
}
