using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XLScrapApi.Util;

public static class VectorExtensions
{
    public static Vector3 Sum(this IEnumerable<Vector3> vectors)
        => vectors.Aggregate(Vector3.zero, (s, v) => s + v);

    public static Vector3 Average(this IEnumerable<Vector3> vectors)
        => vectors.Sum() / vectors.Count();

    // collidersAndRoomMaskAndDefault doesn't work for some reason
    public static Vector3? FloorVector(this Vector3 vector, float maxRange = 5f)
        => Physics.Raycast(vector, Vector3.down, out var hit, maxRange,
                    StartOfRound.Instance.walkableSurfacesMask/* ^ 2048*/)
                ? hit.point
                : null;
}