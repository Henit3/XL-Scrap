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

    public static Vector3? FloorVector(this Vector3 vector)
        => Physics.Raycast(vector, Vector3.down, out var hit, 1000f,
                    StartOfRound.Instance.collidersAndRoomMaskAndDefault)
                ? hit.point
                : null;
}