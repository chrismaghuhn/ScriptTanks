using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Tests.Geometry;

public sealed class CollisionChecksTests
{
    private static FixedRect UnitTenRect()
        => new FixedRect(FixedVec2.FromInts(0, 0), FixedVec2.FromInts(10, 10));

    // ----- CirclesOverlap ---------------------------------------------------

    [Fact]
    public void CirclesOverlap_OverlappingCircles()
    {
        CircleShape a = new CircleShape(FixedVec2.FromInts(0, 0), Fixed.FromInt(3));
        CircleShape b = new CircleShape(FixedVec2.FromInts(2, 0), Fixed.FromInt(3));

        Assert.True(CollisionChecks.CirclesOverlap(a, b));
    }

    [Fact]
    public void CirclesOverlap_TangentCircles_AreOverlap()
    {
        // distance² = 16 == (2 + 2)² = 16 -> tangent counts as overlap.
        CircleShape a = new CircleShape(FixedVec2.FromInts(0, 0), Fixed.FromInt(2));
        CircleShape b = new CircleShape(FixedVec2.FromInts(4, 0), Fixed.FromInt(2));

        Assert.True(CollisionChecks.CirclesOverlap(a, b));
    }

    [Fact]
    public void CirclesOverlap_SeparatedCircles()
    {
        // distance² = 25 > (2 + 2)² = 16 -> separated.
        CircleShape a = new CircleShape(FixedVec2.FromInts(0, 0), Fixed.FromInt(2));
        CircleShape b = new CircleShape(FixedVec2.FromInts(5, 0), Fixed.FromInt(2));

        Assert.False(CollisionChecks.CirclesOverlap(a, b));
    }

    [Fact]
    public void CirclesOverlap_IsCommutative()
    {
        CircleShape a = new CircleShape(FixedVec2.FromInts(0, 0), Fixed.FromInt(2));
        CircleShape b = new CircleShape(FixedVec2.FromInts(4, 0), Fixed.FromInt(2));

        Assert.Equal(
            CollisionChecks.CirclesOverlap(a, b),
            CollisionChecks.CirclesOverlap(b, a));
    }

    [Fact]
    public void CirclesOverlap_OneFullyInsideOther()
    {
        CircleShape outer = new CircleShape(FixedVec2.FromInts(0, 0), Fixed.FromInt(10));
        CircleShape inner = new CircleShape(FixedVec2.FromInts(1, 1), Fixed.FromInt(2));

        Assert.True(CollisionChecks.CirclesOverlap(outer, inner));
        Assert.True(CollisionChecks.CirclesOverlap(inner, outer));
    }

    // ----- CircleIntersectsRect --------------------------------------------

    [Fact]
    public void CircleIntersectsRect_CenterInside()
    {
        CircleShape circle = new CircleShape(FixedVec2.FromInts(5, 5), Fixed.FromInt(1));
        FixedRect rect = UnitTenRect();

        Assert.True(CollisionChecks.CircleIntersectsRect(circle, rect));
    }

    [Fact]
    public void CircleIntersectsRect_OverlapsLeftEdge()
    {
        // Center at (-1, 5), r = 2 -> overlaps the left edge by 1 unit.
        CircleShape circle = new CircleShape(FixedVec2.FromInts(-1, 5), Fixed.FromInt(2));
        FixedRect rect = UnitTenRect();

        Assert.True(CollisionChecks.CircleIntersectsRect(circle, rect));
    }

    [Fact]
    public void CircleIntersectsRect_TangentToLeftEdge()
    {
        // Center at (-2, 5), r = 2 -> rim exactly touches x = 0 (closest = (0, 5)).
        CircleShape circle = new CircleShape(FixedVec2.FromInts(-2, 5), Fixed.FromInt(2));
        FixedRect rect = UnitTenRect();

        Assert.True(CollisionChecks.CircleIntersectsRect(circle, rect));
    }

    [Fact]
    public void CircleIntersectsRect_SeparatedFromLeftEdge()
    {
        // Center at (-3, 5), r = 2 -> distance to closest = 3 > radius = 2.
        CircleShape circle = new CircleShape(FixedVec2.FromInts(-3, 5), Fixed.FromInt(2));
        FixedRect rect = UnitTenRect();

        Assert.False(CollisionChecks.CircleIntersectsRect(circle, rect));
    }

    [Fact]
    public void CircleIntersectsRect_TangentToTopEdge()
    {
        // Center at (5, 12), r = 2 -> closest = (5, 10), distance² = 4 == r².
        CircleShape circle = new CircleShape(FixedVec2.FromInts(5, 12), Fixed.FromInt(2));
        FixedRect rect = UnitTenRect();

        Assert.True(CollisionChecks.CircleIntersectsRect(circle, rect));
    }

    [Fact]
    public void CircleIntersectsRect_SeparatedFromRightEdge()
    {
        // Center at (13, 5), r = 2 -> closest = (10, 5), distance = 3 > 2.
        CircleShape circle = new CircleShape(FixedVec2.FromInts(13, 5), Fixed.FromInt(2));
        FixedRect rect = UnitTenRect();

        Assert.False(CollisionChecks.CircleIntersectsRect(circle, rect));
    }

    [Fact]
    public void CircleIntersectsRect_TangentToCorner()
    {
        // Center at (-3, -4), r = 5 -> closest = (0, 0), distance² = 25 == r² = 25.
        // Exact deterministic corner-tangent test, no approximated radii needed.
        CircleShape circle = new CircleShape(FixedVec2.FromInts(-3, -4), Fixed.FromInt(5));
        FixedRect rect = UnitTenRect();

        Assert.True(CollisionChecks.CircleIntersectsRect(circle, rect));
    }

    [Fact]
    public void CircleIntersectsRect_SeparatedFromCorner()
    {
        // Center at (-3, -4), r = 4 -> distance² = 25 > r² = 16.
        CircleShape circle = new CircleShape(FixedVec2.FromInts(-3, -4), Fixed.FromInt(4));
        FixedRect rect = UnitTenRect();

        Assert.False(CollisionChecks.CircleIntersectsRect(circle, rect));
    }

    [Fact]
    public void CircleIntersectsRect_FullyInsideRect()
    {
        // Even when the circle never touches the rectangle, center-inside means
        // closest == center, distance² = 0, which is <= radius² for any positive radius.
        CircleShape circle = new CircleShape(FixedVec2.FromInts(5, 5), Fixed.FromInt(1));
        FixedRect rect = new FixedRect(FixedVec2.FromInts(0, 0), FixedVec2.FromInts(100, 100));

        Assert.True(CollisionChecks.CircleIntersectsRect(circle, rect));
    }
}
