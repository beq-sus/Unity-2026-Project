
using UnityEngine;
using UnityEngine.UI;

public class TowerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public RectTransform hpFill;
    public Image hpFillImage;

    private float currentHealth;
    private float fullWidth;

    void Start()
    {
        currentHealth = maxHealth;

        if (hpFill != null)
        {
            fullWidth = hpFill.rect.width;
        }

        UpdateHealthBar();
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return;

        if (GameManager.Instance != null &&
            GameManager.Instance.IsGameEnded)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Debug.Log("GAME OVER! Tower destroyed!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseGame();
            }
        }
    }

    void UpdateHealthBar()
    {
        if (hpFill == null) return;

        float percentage = currentHealth / maxHealth;

        hpFill.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            fullWidth * percentage
        );

        if (hpFillImage != null)
        {
            if (percentage > 0.5f)
                hpFillImage.color = Color.green;
            else if (percentage > 0.2f)
                hpFillImage.color = Color.yellow;
            else
                hpFillImage.color = Color.red;
        }
    }
}
