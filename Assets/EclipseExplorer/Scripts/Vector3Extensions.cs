using UnityEngine;

public static class Vector3Extensions
{
    public static Vector3 RotateAround(this Vector3 position, in Vector3 pivot, in Vector3 axis,
        in float deltaDegrees) =>
        (Quaternion.AngleAxis(deltaDegrees, axis) * (position - pivot)) + pivot;
}