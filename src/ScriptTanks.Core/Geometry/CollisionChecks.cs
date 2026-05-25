using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Geometry;

/// <summary>
/// Pure deterministic geometry checks that operate on <see cref="CircleShape"/>
/// and <see cref="FixedRect"/>. All tests use squared distance, so no
/// <c>Math.Sqrt</c> call is involved. Rim and edge contact (tangents)
/// are treated as overlap, matching the inclusive boundary policy of the
/// underlying primitives. No collision response, sweep, raycast, or
/// rotated-shape support is provided here.
/// </summary>
public static class CollisionChecks
{
    public static bool CirclesOverlap(CircleShape a, CircleShape b)
    {
        Fixed sum = a.Radius + b.Radius;
        return a.Center.DistanceSquaredTo(b.Center) <= sum * sum;
    }

    public static bool CircleIntersectsRect(CircleShape circle, FixedRect rect)
    {
        Fixed closestX = ClampLocal(circle.Center.X, rect.Min.X, rect.Max.X);
        Fixed closestY = ClampLocal(circle.Center.Y, rect.Min.Y, rect.Max.Y);
        FixedVec2 closest = new FixedVec2(closestX, closestY);
        return circle.Center.DistanceSquaredTo(closest) <= circle.Radius * circle.Radius;
    }

    private static Fixed ClampLocal(Fixed value, Fixed min, Fixed max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
