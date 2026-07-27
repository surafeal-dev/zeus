using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space health bar for an Entity. Builds its own Canvas and images at
/// runtime, so it needs no setup beyond dropping it on the entity.
/// </summary>
[DisallowMultipleComponent]
public class HealthBar : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to use the Entity on this GameObject or a parent.")]
    [SerializeField] private Entity target;

    [Header("Layout (pixels at 1920x1080)")]
    [SerializeField] private Vector2 size = new Vector2(280, 26);
    [SerializeField] private Vector2 margin = new Vector2(28, 28);
    [SerializeField] private float borderThickness = 3;

    [Header("Colours")]
    [SerializeField] private Color borderColor = new Color(.07f, .07f, .09f, 1);
    [SerializeField] private Color backgroundColor = new Color(.28f, .09f, .10f, 1);
    [SerializeField] private Color fillColor = new Color(.85f, .18f, .22f, 1);

    [Header("Behaviour")]
    [Tooltip("Fraction of the full bar drained per second. 0 snaps instantly.")]
    [SerializeField] private float drainSpeed = 1.5f;

    private RectTransform fillRect;
    private float targetFill = 1;
    private float shownFill = 1;

    private void Start()
    {
        if (target == null)
            target = GetComponentInParent<Entity>();

        if (target == null)
        {
            Debug.LogError($"{nameof(HealthBar)} on '{name}' found no Entity to track.", this);
            enabled = false;
            return;
        }

        BuildUI();

        // Entity.Awake fires OnHealthChanged before this Start runs, so read the
        // current value directly rather than waiting for the next change.
        target.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(target.CurrentHealth, target.MaxHealth);

        shownFill = targetFill;
        ApplyFill();
    }

    private void OnDestroy()
    {
        if (target != null)
            target.OnHealthChanged -= HandleHealthChanged;
    }

    private void Update()
    {
        if (Mathf.Approximately(shownFill, targetFill))
            return;

        shownFill = Mathf.MoveTowards(shownFill, targetFill, drainSpeed * Time.deltaTime);
        ApplyFill();
    }

    private void HandleHealthChanged(int current, int max)
    {
        targetFill = max > 0 ? Mathf.Clamp01((float)current / max) : 0;

        if (drainSpeed <= 0)
        {
            shownFill = targetFill;
            ApplyFill();
        }
    }

    private void ApplyFill()
    {
        if (fillRect != null)
            fillRect.anchorMax = new Vector2(shownFill, 1);
    }

    private void BuildUI()
    {
        RectTransform border = CreatePanel(GetOrCreateCanvas().transform, "PlayerHealthBar", borderColor);
        border.anchorMin = new Vector2(0, 1);
        border.anchorMax = new Vector2(0, 1);
        border.pivot = new Vector2(0, 1);
        border.sizeDelta = size;
        border.anchoredPosition = new Vector2(margin.x, -margin.y);

        RectTransform background = CreatePanel(border, "Background", backgroundColor);
        Stretch(background, borderThickness);

        // The fill is stretched to its parent, then its right anchor is moved
        // between 0 and 1 to show the current health fraction.
        fillRect = CreatePanel(background, "Fill", fillColor);
        Stretch(fillRect, 0);
    }

    private static RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static Canvas GetOrCreateCanvas()
    {
        foreach (Canvas existing in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (existing.renderMode == RenderMode.ScreenSpaceOverlay)
                return existing;
        }

        GameObject go = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler));

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;

        return canvas;
    }
}
