using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XLScrapApi.Util;

public static class XLPositionUtils
{
    public static Vector3 GetPositionFromHolders(IList<Vector3> anchors, IList<Vector3> holdersPos)
    {
        return Enumerable.Range(0, holdersPos.Count)
            .Select(i => holdersPos[i] - anchors[i])
            .Average();
    }

    public static Vector3[] GetHolderPositionsAt(IList<Vector3> anchors, Vector3 destVector)
    {
        return anchors.Select(x => x + destVector).ToArray();
    }

    public static Vector3[] GetHoldersPositionsOnShift(IList<Vector3> holdersPos, Vector3 shiftVector)
    {
        if (holdersPos == null) return null;

        return holdersPos.Select(x => x + shiftVector).ToArray();
    }
}