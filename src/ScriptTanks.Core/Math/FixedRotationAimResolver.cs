namespace ScriptTanks.Core.Math;



/// <summary>

/// Resolves a direction <see cref="FixedVec2"/> to a deterministic turret turn-fraction rotation.

/// </summary>

/// <remarks>

/// Zero vectors return <see cref="FixedRotationAimResolution.Unresolved"/>. Cardinal axis-aligned

/// directions resolve to exact quarter-turn anchors: +X → 0, +Y → 1/4, -X → 1/2, -Y → 3/4.

/// Non-cardinal directions resolve via <see cref="FixedRotationInverseLookup"/> against the committed

/// forward lookup table. Does not use floating-point trigonometry or runtime angle math. Does not

/// mutate match state, construct fire requests, or integrate scripting pipelines.

/// </remarks>

public static class FixedRotationAimResolver

{

    public static FixedRotationAimResolution ResolveFromDirection(FixedVec2 direction)

    {

        if (direction.X == Fixed.Zero && direction.Y == Fixed.Zero)

        {

            return FixedRotationAimResolution.Unresolved();

        }



        if (direction.Y == Fixed.Zero)

        {

            if (direction.X > Fixed.Zero)

            {

                return FixedRotationAimResolution.Resolved(Fixed.Zero);

            }



            if (direction.X < Fixed.Zero)

            {

                return FixedRotationAimResolution.Resolved(Fixed.FromRatio(1, 2));

            }

        }



        if (direction.X == Fixed.Zero)

        {

            if (direction.Y > Fixed.Zero)

            {

                return FixedRotationAimResolution.Resolved(Fixed.FromRatio(1, 4));

            }



            if (direction.Y < Fixed.Zero)

            {

                return FixedRotationAimResolution.Resolved(Fixed.FromRatio(3, 4));

            }

        }



        return FixedRotationInverseLookup.ResolveFromDirection(direction);

    }

}


