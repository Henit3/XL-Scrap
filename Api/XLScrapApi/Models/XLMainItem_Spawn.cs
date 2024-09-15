using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XLScrapApi.Util;

namespace XLScrapApi.Models;

public partial class XLMainItem : PhysicsProp
{
    private const float MaxSpawnDiff = 20f;
    private const int MaxFailedCorrectionAttempts = 3;
    private static int HolderLinecastMask
    {
        get => StartOfRound.Instance.collidersAndRoomMaskAndDefault;
    }

    public bool CorrectToValidPosition()
    {
        var originalPosition = transform.position;
        for (var failedCorrectionAttempts = 0;
            failedCorrectionAttempts < MaxFailedCorrectionAttempts;
            failedCorrectionAttempts++)
        {
            var realAnchors = Anchors.Select(a => a + transform.position).ToList();

            if (CheckValidPosition(realAnchors, out var correction))
            {
                Plugin.Logger.LogDebug($"Valid spawn at {transform.position}");
                return true;
            }
            Plugin.Logger.LogDebug($"Invalid spawn at {transform.position}: {correction}");
            switch (correction)
            {
                case CorrectionType.Wall:
                    CorrectValidWallPosition(realAnchors);
                    break;
                case CorrectionType.Floor:
                    CorrectValidFloorPosition(realAnchors);
                    break;
                default:
                    Plugin.Logger.LogWarning("Invalid position without correction type specified!");
                    break;
            }
        }
        SetPositionWithHolders(originalPosition);
        return false;
    }

    private void CorrectValidWallPosition(List<Vector3> realAnchors)
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

        ShiftPositionWithHolders(shiftDotDistance * hitInfo.normal);
    }

    private void CorrectValidFloorPosition(List<Vector3> realAnchors)
    {
        // Average floored anchors' differences from the main point for shift direction
        var nullableFlooredAnchors = realAnchors.Select(x => x.FloorVector()).ToList();

        // Shift by a bit more than the maxAnchorDistance in random vector if no floor around the item
        if (nullableFlooredAnchors.All(x => x == null))
        {
            var maxAnchorDistance = Anchors.Max(a => a.magnitude);
            ShiftPositionWithHolders((Vector3)Random.insideUnitCircle.normalized * (maxAnchorDistance + 1f));
            Plugin.Logger.LogDebug($"Correcting floor (RANDOM END): {transform.position}");
            return;
        }

        Vector3 floorDirection = Vector3.zero;
        for (var i = 0; i < nullableFlooredAnchors.Count; i++)
        {
            var flooredAnchor = nullableFlooredAnchors[i];
            if (flooredAnchor == null) continue;

            floorDirection += Anchors[i];
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

            var anchorMain = anchor - transform.position;
            var anchorFloorDot = Vector3.Dot(floorDirection, anchorMain);
            if (floor == null)
            {
                if (anchorFloorDot > maxNonFloorDot) maxNonFloorDot = anchorFloorDot;
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
        ShiftPositionWithHolders(floorDirection * shiftMagnitude);
    }

    private bool CheckValidPosition(List<Vector3> realAnchors, out CorrectionType? correction)
    {
        if (!CheckAnchorIntersections(realAnchors, out correction)
            || !CheckFloorPositions(realAnchors, out correction)) return false;

        return true;
    }

    private bool CheckAnchorIntersections(List<Vector3> realAnchors, out CorrectionType? correction)
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

    private bool CheckFloorPositions(List<Vector3> realAnchors, out CorrectionType? correction)
    {
        correction = null;

        var floorPosition = transform.position.FloorVector();
        if (floorPosition == null
            || Vector3.Distance(floorPosition.Value, transform.position) > MaxSpawnDiff)
        {
            Plugin.Logger.LogDebug($"Main floor not good: {transform.position} -> {floorPosition}");
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

        var newPosition = GetPositionFromHolders(flooredAnchors);

        if (Vector3.Distance(newPosition, transform.position) > MaxSpawnDiff)
        {
            Plugin.Logger.LogDebug($"New position too far away: {transform.position} -> {newPosition}");
            correction = CorrectionType.Floor;
            return false;
        }

        // Check if resulting anchor points are valid relative to main object
        for (var i = 0; i < flooredAnchors.Count; i++)
        {
            var dist = Vector3.Distance(newPosition, flooredAnchors[i]);
            if (dist > Anchors[i].magnitude + (Plugin.Config.MaxHolderRadius.Value / 2))
            {
                Plugin.Logger.LogDebug($"Floored anchor too far away ({dist}): {newPosition} <-> {flooredAnchors[i]}");
                correction = CorrectionType.Floor;
                return false;
            }
        }

        return true;
    }
}
