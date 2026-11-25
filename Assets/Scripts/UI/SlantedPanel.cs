using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI graphic that fills its rect but cuts the bottom-right corner, giving a slanted right edge.
/// Optional vertical gradient via topTint / bottomTint (multiplied with Color).
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class SlantedPanel : MaskableGraphic
{
    [Tooltip("How far (in canvas units) the bottom-right corner is pulled left.")]
    [SerializeField] private float slant = 115f;
    [SerializeField] private Color topTint = Color.white;
    [SerializeField] private Color bottomTint = Color.white;

    public float Slant
    {
        get => slant;
        set { slant = value; SetVerticesDirty(); }
    }

    public void SetGradient(Color top, Color bottom)
    {
        topTint = top;
        bottomTint = bottom;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        float s = Mathf.Clamp(slant, 0f, r.width);

        Color top = color * topTint;
        Color bottom = color * bottomTint;

        vh.AddVert(new Vector3(r.xMin, r.yMin), bottom, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin, r.yMax), top, Vector2.up);
        vh.AddVert(new Vector3(r.xMax, r.yMax), top, Vector2.one);
        vh.AddVert(new Vector3(r.xMax - s, r.yMin), bottom, Vector2.right);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
