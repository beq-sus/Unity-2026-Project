using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Один замок на карте уровней.
// Состояния: закрыт (фиолетовый с замком), открыт, текущий (флаг + светящееся кольцо), пройден (звёзды).
public class LevelNode : MonoBehaviour
{
    [Header("Level")]
    public int levelNumber = 1;

    [Header("Visuals")]
    public Button button;
    public Image towerImage;
    public Sprite openSprite;
    public Sprite lockedSprite;

    [Header("Stars")]
    public GameObject starsRoot;
    public Image[] stars;
    public Sprite starFull;
    public Sprite starEmpty;

    [Header("Current level markers")]
    public GameObject flag;
    public GameObject glowRing;

    [Header("Animation")]
    public float ringPulseSpeed = 3f;
    public float ringPulseAmount = 0.06f;

    public event Action<LevelNode> Clicked;

    public bool IsUnlocked { get; private set; }

    RectTransform rect;
    Vector2 basePosition;
    Coroutine shakeRoutine;

    void Awake()
    {
        rect = (RectTransform)transform;
        basePosition = rect.anchoredPosition;

        if (button != null)
            button.onClick.AddListener(() => Clicked?.Invoke(this));
    }

    public void Refresh(bool unlocked, int starCount, bool isCurrent)
    {
        IsUnlocked = unlocked;

        if (towerImage != null)
            towerImage.sprite = unlocked ? openSprite : lockedSprite;

        bool completed = starCount > 0;

        if (starsRoot != null)
            starsRoot.SetActive(completed);

        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] != null)
                    stars[i].sprite = i < starCount ? starFull : starEmpty;
            }
        }

        if (flag != null) flag.SetActive(isCurrent && !completed);
        if (glowRing != null) glowRing.SetActive(isCurrent);
    }

    void Update()
    {
        if (glowRing != null && glowRing.activeSelf)
        {
            float s = 1f + Mathf.Sin(Time.unscaledTime * ringPulseSpeed) * ringPulseAmount;
            glowRing.transform.localScale = new Vector3(s, s, 1f);
        }
    }

    // Небольшая встряска, когда кликают по закрытому уровню.
    public void PlayLockedFeedback()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        const float duration = 0.3f;
        const float strength = 10f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float damper = 1f - t / duration;
            float offset = Mathf.Sin(t * 60f) * strength * damper;
            rect.anchoredPosition = basePosition + new Vector2(offset, 0f);
            yield return null;
        }

        rect.anchoredPosition = basePosition;
        shakeRoutine = null;
    }
}
