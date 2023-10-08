using Unity.Mathematics;
using UnityEngine;
using NaughtyAttributes;

[RequireComponent(typeof(LineRenderer))]
[AddComponentMenu("Eclipse/BodyOrbit", order: 0)]
public sealed class BodyOrbit : MonoBehaviour
{
    public Transform orbitCenterT, orbitAxisT;

    [Range(0.01f, int.MaxValue)] public float radius = 0;

    public float orbitingAngularSpeed = 0;

    public LineRenderer orbitRender;
    public float orbitRenderResolution = 0.1f;

    [Button("Update Orbit Renderer")]
    private void UpdateOrbitRenderer()
    {
        if (orbitRender == null)
        {
            orbitRender = GetComponent<LineRenderer>();
        }

        var orbitCenter = orbitCenterT.position;
        var orbitAxis = orbitAxisT.up;

        var count = (int)Mathf.Floor(orbitRenderResolution * 2 * Mathf.PI * radius);
        orbitRender.positionCount = count;
        orbitRender.loop = true;
        var positions = new Vector3[count];
        var angle = 0f;
        for (int i = 0; i < count; i++)
        {
            angle = (float)i / count * 360;
            var pos = (Quaternion.AngleAxis(angle, orbitAxis) * Vector3.forward) * radius + orbitCenter;
            positions[i] = pos;
        }

        orbitRender.SetPositions(positions);
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateOrbitRenderer();
    }

    // Update is called once per frame
    void Update()
    {
        return;
        var orbitCenter = orbitCenterT.position;
        var orbitAxis = orbitAxisT.up;
        var newAngularPosition = transform.position.RotateAround(orbitCenter, orbitAxis,
            orbitingAngularSpeed * Time.deltaTime);

        /*if (math.dot(newAngularPosition, orbitAxis) != 0)
        {
            newAngularPosition = Vector3.ProjectOnPlane(newAngularPosition, orbitAxis);
        }

        newAngularPosition = (newAngularPosition - orbitCenter).normalized * radius + orbitCenter;*/
        newAngularPosition = GetNearestOrbitPosition(newAngularPosition);

        transform.position = newAngularPosition;
    }

    public Vector3 GetNearestOrbitPosition(Vector3 worldPos)
    {
        var orbitCenter = orbitCenterT.position;
        var orbitAxis = orbitAxisT.up;

        var orbitPos = worldPos - orbitCenter;

        if (math.dot(orbitPos, orbitAxis) != 0)
        {
            orbitPos = Vector3.ProjectOnPlane(orbitPos, orbitAxis);
        }

        worldPos = orbitPos.normalized * radius + orbitCenter;

        return worldPos;
    }
}