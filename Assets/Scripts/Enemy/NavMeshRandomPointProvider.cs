using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public sealed class NavMeshRandomPointProvider
{
    private readonly List<NavMeshTriangle> triangles = new();
    private readonly NavMeshPath reachabilityPath = new();

    private readonly int areaMask;

    private float totalArea;
    private bool hasReachabilityOrigin;
    private Vector3 reachabilityOrigin;

    public bool IsValid =>
        triangles.Count > 0 &&
        totalArea > 0f;

    public bool HasReachabilityOrigin =>
        hasReachabilityOrigin;

    public NavMeshRandomPointProvider(
        int navMeshAreaMask,
        Vector3? origin)
    {
        areaMask = navMeshAreaMask;

        BuildTriangleCache();

        if (origin.HasValue &&
            NavMesh.SamplePosition(
                origin.Value,
                out NavMeshHit hit,
                5f,
                areaMask))
        {
            reachabilityOrigin = hit.position;
            hasReachabilityOrigin = true;
        }
    }

    public bool TryFindPosition(
        Vector3? protectedPosition,
        IReadOnlyList<Vector3> occupiedPositions,
        float minimumProtectedDistance,
        float minimumEnemySeparation,
        int attempts,
        out Vector3 position)
    {
        float protectedDistanceSquared =
            minimumProtectedDistance *
            minimumProtectedDistance;

        float separationSquared =
            minimumEnemySeparation *
            minimumEnemySeparation;

        for (int attempt = 0;
             attempt < Mathf.Max(1, attempts);
             attempt++)
        {
            if (!TryGetRandomNavMeshPoint(
                    out Vector3 candidate))
            {
                break;
            }

            if (protectedPosition.HasValue)
            {
                float distanceSquared =
                    (candidate -
                     protectedPosition.Value)
                    .sqrMagnitude;

                if (distanceSquared <
                    protectedDistanceSquared)
                {
                    continue;
                }
            }

            if (IsTooCloseToOtherEnemy(
                    candidate,
                    occupiedPositions,
                    separationSquared))
            {
                continue;
            }

            if (!CanReachCandidate(candidate))
                continue;

            position = candidate;
            return true;
        }

        position = default;
        return false;
    }

    private void BuildTriangleCache()
    {
        NavMeshTriangulation triangulation =
            NavMesh.CalculateTriangulation();

        int triangleCount =
            triangulation.indices.Length / 3;

        for (int triangleIndex = 0;
             triangleIndex < triangleCount;
             triangleIndex++)
        {
            int area = 0;

            if (triangulation.areas != null &&
                triangleIndex <
                triangulation.areas.Length)
            {
                area =
                    triangulation.areas[
                        triangleIndex];
            }

            if (!ContainsArea(areaMask, area))
                continue;

            int indexOffset =
                triangleIndex * 3;

            Vector3 a =
                triangulation.vertices[
                    triangulation.indices[
                        indexOffset]];

            Vector3 b =
                triangulation.vertices[
                    triangulation.indices[
                        indexOffset + 1]];

            Vector3 c =
                triangulation.vertices[
                    triangulation.indices[
                        indexOffset + 2]];

            float triangleArea =
                Vector3.Cross(
                    b - a,
                    c - a).magnitude * 0.5f;

            if (triangleArea <= Mathf.Epsilon)
                continue;

            totalArea += triangleArea;

            triangles.Add(
                new NavMeshTriangle(
                    a,
                    b,
                    c,
                    totalArea));
        }
    }

    private bool TryGetRandomNavMeshPoint(
        out Vector3 point)
    {
        if (!IsValid)
        {
            point = default;
            return false;
        }

        float roll =
            Random.Range(0f, totalArea);

        NavMeshTriangle selected =
            FindTriangleByArea(roll);

        // Uniform barycentric sampling.
        float root =
            Mathf.Sqrt(Random.value);

        float second =
            Random.value;

        Vector3 candidate =
            (1f - root) * selected.A +
            root * (1f - second) * selected.B +
            root * second * selected.C;

        if (NavMesh.SamplePosition(
                candidate,
                out NavMeshHit hit,
                0.5f,
                areaMask))
        {
            point = hit.position;
            return true;
        }

        point = default;
        return false;
    }

    private NavMeshTriangle FindTriangleByArea(
        float targetArea)
    {
        int low = 0;
        int high = triangles.Count - 1;

        NavMeshTriangle result =
            triangles[high];

        while (low <= high)
        {
            int middle =
                (low + high) / 2;

            NavMeshTriangle triangle =
                triangles[middle];

            if (targetArea <=
                triangle.CumulativeArea)
            {
                result = triangle;
                high = middle - 1;
            }
            else
            {
                low = middle + 1;
            }
        }

        return result;
    }

    private bool CanReachCandidate(
        Vector3 candidate)
    {
        if (!hasReachabilityOrigin)
            return true;

        bool calculated =
            NavMesh.CalculatePath(
                reachabilityOrigin,
                candidate,
                areaMask,
                reachabilityPath);

        return calculated &&
               reachabilityPath.status ==
               NavMeshPathStatus.PathComplete;
    }

    private static bool IsTooCloseToOtherEnemy(
        Vector3 candidate,
        IReadOnlyList<Vector3> occupiedPositions,
        float separationSquared)
    {
        if (occupiedPositions == null)
            return false;

        foreach (Vector3 occupied in occupiedPositions)
        {
            if ((candidate - occupied).sqrMagnitude <
                separationSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsArea(
        int mask,
        int area)
    {
        if (area < 0 || area >= 32)
            return false;

        return (mask & (1 << area)) != 0;
    }

    private readonly struct NavMeshTriangle
    {
        public NavMeshTriangle(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            float cumulativeArea)
        {
            A = a;
            B = b;
            C = c;
            CumulativeArea = cumulativeArea;
        }

        public Vector3 A { get; }
        public Vector3 B { get; }
        public Vector3 C { get; }
        public float CumulativeArea { get; }
    }
}