using UnityEngine;

public enum GraphTrafficColor
{
    White = 0,
    Yellow = 1,
    Orange = 2,
    Red = 3
}

public static class GraphTrafficColorUtility
{
    private const float DefaultColorTolerance = 0.12f;
    private static readonly Color[] ReferenceColors =
    {
        new Color(1f, 1f, 1f, 1f),
        new Color(1f, 0.92f, 0.02f, 1f),
        new Color(1f, 0.5f, 0f, 1f),
        new Color(1f, 0f, 0f, 1f)
    };

    public static GraphTrafficColor Classify(Color color, float tolerance = DefaultColorTolerance)
    {
        var clampedTolerance = Mathf.Max(tolerance, 0f);
        var bestColor = GraphTrafficColor.White;
        var bestDistance = float.MaxValue;
        for (var index = 0; index < ReferenceColors.Length; index++)
        {
            var distance = ColorDistance(color, ReferenceColors[index]);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestColor = (GraphTrafficColor)index;
            }
        }

        return bestDistance <= clampedTolerance ? bestColor : GraphTrafficColor.White;
    }

    public static float CalculateScore(GraphTrafficColor expected, GraphTrafficColor selected)
    {
        var distance = Mathf.Abs((int)expected - (int)selected);
        switch (distance)
        {
            case 0:
                return 2f;
            case 1:
                return 1f;
            case 2:
                return 0.5f;
            default:
                return 0f;
        }
    }

    private static float ColorDistance(Color left, Color right)
    {
        var deltaRed = left.r - right.r;
        var deltaGreen = left.g - right.g;
        var deltaBlue = left.b - right.b;
        return Mathf.Sqrt(deltaRed * deltaRed + deltaGreen * deltaGreen + deltaBlue * deltaBlue);
    }
}
