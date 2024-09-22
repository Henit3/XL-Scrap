using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XLScrapApi.Models;

namespace XLScrapApi.Util;

public static class XLSpawner
{
    private const float MaxSpawnDiff = 20f;
    private const int MaxFailedCorrectionAttempts = 3;
    // Cannot make this a constant while still referring to StartOfRound.Instance
    private static int HolderLinecastMask => StartOfRound.Instance.collidersAndRoomMaskAndDefault ^ 2048;

    public static bool CorrectToValidPosition(XLMainItem xlMain, Vector3? location = null)
    {
        var xlItem = new XLSimpleMainItem(xlMain);
        if (location.HasValue)
        {
            xlItem.Position = location.Value;
            xlItem.HolderPositions = XLPositionUtils.GetHolderPositionsAt(xlItem.Anchors, location.Value);
        }

        for (var failedCorrectionAttempts = 0;
            failedCorrectionAttempts < MaxFailedCorrectionAttempts;
            failedCorrectionAttempts++)
        {
            var realAnchors = xlItem.Anchors.Select(a => a + xlItem.Position).ToList();

            if (CheckValidPosition(xlItem, realAnchors, out var correction))
            {
                // Set correct position only on success
                xlMain.SetPositionWithHolders(xlItem.Position);

                Plugin.Logger.LogDebug($"Valid position at {xlItem.Position}");
                return true;
            }
            Plugin.Logger.LogDebug($"Invalid position at {xlItem.Position}: {correction}");
            switch (correction)
            {
                case CorrectionType.Wall:
                    CorrectValidWallPosition(xlItem, realAnchors);
                    break;
                case CorrectionType.Floor:
                    CorrectValidFloorPosition(xlItem, realAnchors);
                    break;
                default:
                    Plugin.Logger.LogWarning("Invalid position without correction type specified!");
                    break;
            }
        }
        return false;
    }

    private static void CorrectValidWallPosition(XLSimpleMainItem xlItem, List<Vector3> realAnchors)
    {
        // Use intersection point to determine wall normal and hitpoint
        RaycastHit hitInfo = new();
        bool foundHit = false;
        for (var i = 0; i < realAnchors.Count; i++)
        {
            for (var j = i + 1; j < realAnchors.Count; j++)
            {
                if (Physics.Linecast(realAnchors[i], realAnchors[j], out hitInfo,
                    HolderLinecastMask))
                {
                    foundHit = true;
                    break;
                }
            }
            if (foundHit) break;
        }
        if (!foundHit)
        {
            Plugin.Logger.LogDebug($"Correcting wall (NO WALL)");
            return;
        }

        // Get the furthest point away and shift by furthest in _other_ direction (min movement)
        var maxPosDotDistance = 0f;
        var maxNegDotDistance = 0f;
        for (var i = 0; i < realAnchors.Count; i++)
        {
            var toPoint = hitInfo.point - realAnchors[i];
            var dotDistance = Vector3.Dot(hitInfo.normal, toPoint);
            if (dotDistance > maxPosDotDistance) maxPosDotDistance = dotDistance;
            if (dotDistance < maxNegDotDistance) maxNegDotDistance = dotDistance;
        }

        if (maxPosDotDistance == 0 && maxNegDotDistance == 0)
        {
            Plugin.Logger.LogDebug($"Correcting wall (NO DISTANCES)");
            return;
        }

        var shiftDotDistance = (System.Math.Abs(maxPosDotDistance), System.Math.Abs(maxNegDotDistance)) switch
        {
            (0, 0) => 0, // Should never happen due to preceding check
            (0, _) => maxNegDotDistance,
            (_, 0) => maxPosDotDistance,
            (var pos, var neg) => pos > neg ? neg : pos
        };

        var shiftVector = shiftDotDistance * hitInfo.normal;
        xlItem.Position += shiftVector;
        xlItem.HolderPositions = XLPositionUtils.GetHoldersPositionsOnShift(xlItem.HolderPositions, shiftVector);
    }

    private static void CorrectValidFloorPosition(XLSimpleMainItem xlItem, List<Vector3> realAnchors)
    {
        // Average floored anchors' differences from the main point for shift direction
        var nullableFlooredAnchors = realAnchors.Select(x => x.FloorVector()).ToList();

        // Shift by a bit more than the maxAnchorDistance in random vector if no floor around the item
        if (nullableFlooredAnchors.All(x => x == null))
        {
            var maxAnchorDistance = xlItem.Anchors.Max(a => a.magnitude);

            var randomShiftVector = (Vector3)Random.insideUnitCircle.normalized * (maxAnchorDistance + 1f);
            xlItem.Position += randomShiftVector;
            xlItem.HolderPositions = XLPositionUtils.GetHoldersPositionsOnShift(xlItem.HolderPositions, randomShiftVector);
            Plugin.Logger.LogDebug($"Correcting floor (RANDOM END): {xlItem.Position}");
            return;
        }

        Vector3 floorDirection = Vector3.zero;
        for (var i = 0; i < nullableFlooredAnchors.Count; i++)
        {
            var flooredAnchor = nullableFlooredAnchors[i];
            if (flooredAnchor == null) continue;

            floorDirection += xlItem.Anchors[i];
        }
        floorDirection.Normalize();

        // Assumes floor on one side, gap on the other
        // (Dot closest floor - Dot furthest non-floor) for minimum shift
        var maxNonFloorDot = float.MinValue;
        var minFloorDot = float.MaxValue;
        for (var i = 0; i < realAnchors.Count; i++)
        {
            var anchor = realAnchors[i];
            var floor = nullableFlooredAnchors[i];

            var anchorMain = anchor - xlItem.Position;
            var anchorFloorDot = Vector3.Dot(floorDirection, anchorMain);
            if (floor == null)
            {
                if (anchorFloorDot < maxNonFloorDot) maxNonFloorDot = anchorFloorDot;
            }
            else
            {
                if (anchorFloorDot < minFloorDot) minFloorDot = anchorFloorDot;
            }
        }
        // Reset to 0 if unchanged; should never be invoked but here for safety/sanity
        if (maxNonFloorDot == float.MinValue) maxNonFloorDot = 0;
        if (minFloorDot == float.MaxValue) minFloorDot = 0;

        var shiftMagnitude = System.Math.Abs(maxNonFloorDot) + System.Math.Abs(minFloorDot);
        var shiftVector = floorDirection * shiftMagnitude;
        xlItem.Position += shiftVector;
        xlItem.HolderPositions = XLPositionUtils.GetHoldersPositionsOnShift(xlItem.HolderPositions, floorDirection * shiftMagnitude);
    }

    private static bool CheckValidPosition(XLSimpleMainItem xlItem, List<Vector3> realAnchors, out CorrectionType? correction)
    {
        if (!CheckAnchorIntersections(xlItem, realAnchors, out correction)
            || !CheckFloorPositions(xlItem, realAnchors, out correction)) return false;

        return true;
    }

    private static bool CheckAnchorIntersections(XLSimpleMainItem xlItem, List<Vector3> realAnchors, out CorrectionType? correction)
    {
        correction = null;

        for (var i = 0; i < realAnchors.Count; i++)
        {
            for (var j = i + 1; j < realAnchors.Count; j++)
            {
                if (Physics.Linecast(realAnchors[i], realAnchors[j],
                    HolderLinecastMask))
                {
                    Plugin.Logger.LogDebug($"Wall between anchors: {realAnchors[i]}, {realAnchors[j]}");
                    correction = CorrectionType.Wall;
                    return false;
                }
            }
        }

        return true;
    }

    private static bool CheckFloorPositions(XLSimpleMainItem xlItem, List<Vector3> realAnchors, out CorrectionType? correction)
    {
        correction = null;

        var floorPosition = xlItem.Position.FloorVector();
        if (floorPosition == null
            || Vector3.Distance(floorPosition.Value, xlItem.Position) > MaxSpawnDiff)
        {
            Plugin.Logger.LogDebug($"Main floor not good: {xlItem.Position} -> {floorPosition}");
            correction = CorrectionType.Floor;
            return false;
        }

        // Get floor points with down raycasts at the anchor points
        var nullableFlooredAnchors = realAnchors.Select(x => x.FloorVector()).ToList();

        if (nullableFlooredAnchors.Any(a => a == null))
        {
            Plugin.Logger.LogDebug($"Anchor floor not good: {string.Join(',', nullableFlooredAnchors)}");
            correction = CorrectionType.Floor;
            return false;
        }
        var flooredAnchors = nullableFlooredAnchors.Select(a => a.Value).ToList();

        var newPosition = XLPositionUtils.GetPositionFromHolders(xlItem.Anchors, flooredAnchors);

        if (Vector3.Distance(newPosition, xlItem.Position) > MaxSpawnDiff)
        {
            Plugin.Logger.LogDebug($"New position too far away: {xlItem.Position} -> {newPosition}");
            correction = CorrectionType.Floor;
            return false;
        }

        // Check if resulting anchor points are valid relative to main object
        for (var i = 0; i < flooredAnchors.Count; i++)
        {
            var dist = Vector3.Distance(newPosition, flooredAnchors[i]);
            if (dist > xlItem.Anchors[i].magnitude + (Plugin.Config.MaxHolderRadius.Value / 2))
            {
                Plugin.Logger.LogDebug($"Floored anchor too far away ({dist}): {newPosition} <-> {flooredAnchors[i]}");
                correction = CorrectionType.Floor;
                return false;
            }
        }

        return true;
    }
}
