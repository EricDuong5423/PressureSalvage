using UnityEngine;

public class EnemySpawnZone : MonoBehaviour
{
    public enum Shape
    {
        Circle,
        Box
    }
    
    [SerializeField] private Shape shape =  Shape.Circle;
    [SerializeField, Min(0.1f)]
    private float radius = 10f;
    [SerializeField]
    private Vector2 boxSize = new(20f, 20f);
    [SerializeField, Min(0.1f)]
    private float navMeshSampleDistance = 2f;

    public bool TryGetRandomNavMeshPoint(
        int navMeshAreaMask,
        out Vector3 position)
    {
        Vector2 localPoint = shape == Shape.Circle
            ? Random.insideUnitCircle * radius
            : new Vector2(
                Random.Range(-boxSize.x * 0.5f, boxSize.x * 0.5f),
                Random.Range(-boxSize.y * 0.5f, boxSize.y * 0.5f));

        Vector3 candidate = transform.TransformPoint(
            new Vector3(localPoint.x, 0f, localPoint.y));

        if (UnityEngine.AI.NavMesh.SamplePosition(
                candidate,
                out UnityEngine.AI.NavMeshHit hit,
                navMeshSampleDistance,
                navMeshAreaMask) &&
            Contains(hit.position))
        {
            position = hit.position;
            return true;
        }

        position = default;
        return false;
    }

    private bool Contains(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);

        return shape == Shape.Circle
            ? local.x * local.x + local.z * local.z <= radius * radius
            : Mathf.Abs(local.x) <= boxSize.x * 0.5f &&
              Mathf.Abs(local.z) <= boxSize.y * 0.5f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (shape == Shape.Circle)
        {
            Gizmos.DrawWireSphere(Vector3.zero, radius);
        }
        else
        {
            Gizmos.DrawWireCube(
                Vector3.zero,
                new Vector3(boxSize.x, 0.2f, boxSize.y));
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
