using UnityEngine;
using UnityEngine.Assertions.Comparers;

public static class RectUtilsExt
{
    /// <summary>
    /// Converts RectTransform.rect's local coordinates to world space
    /// Usage example RectTransformExt.GetWorldRect(myRect, Vector2.one);
    /// </summary>
    /// <returns>The world rect.</returns>
    /// <param name="rt">RectangleTransform we want to convert to world coordinates.</param>
    /// <param name="scale">Optional scale pulled from the CanvasScaler. Default to using Vector2.one.</param>
    internal static Rect GetWorldRect(this RectTransform rt, Vector2 scale)
    {
        // Convert the rectangle to world corners and grab the top left
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 topLeft = corners[0];

        // Rescale the size appropriately based on the current Canvas scale
        Vector2 scaledSize = new Vector2(scale.x * rt.rect.size.x, scale.y * rt.rect.size.y);

        return new Rect(topLeft, scaledSize);
    }

    private static float Round(float num)
    {
        return (int)(num * 1000.0f) / 1000.0f;
    }

    private static bool ApproximatelyRounded(float a, float b)
    {
        return Mathf.Approximately(Round(a), Round(b));
    }

    /// <summary>
    /// Same as Unity's Contains, but inclusive
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    internal static bool ContainsInclusive(this Rect rect, Vector2 point)
    {
        if (ApproximatelyRounded(point.x, rect.xMin))
        {
            return (point.y > rect.yMin || ApproximatelyRounded(point.y, rect.yMin)) && (point.y < rect.yMax || ApproximatelyRounded(point.y, rect.yMax));
        }

        if (ApproximatelyRounded(point.x, rect.xMax))
        {
            return (point.y > rect.yMin || ApproximatelyRounded(point.y, rect.yMin)) && (point.y < rect.yMax || ApproximatelyRounded(point.y, rect.yMax));
        }

        if (ApproximatelyRounded(point.y, rect.yMin))
        {
            return (point.x > rect.xMin || ApproximatelyRounded(point.x, rect.xMin)) && (point.x < rect.xMax || ApproximatelyRounded(point.x, rect.xMax));
        }

        if (ApproximatelyRounded(point.y, rect.yMax))
        {
            return (point.x > rect.xMin || ApproximatelyRounded(point.x, rect.xMin)) && (point.x < rect.xMax || ApproximatelyRounded(point.x, rect.xMax));
        }

        if (point.x >= (double)rect.xMin && (double)point.x <= rect.xMax && point.y >= (double)rect.yMin)
            return point.y <= (double)rect.yMax;
        return false;
    }
}