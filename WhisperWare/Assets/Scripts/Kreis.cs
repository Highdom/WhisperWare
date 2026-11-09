using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Kreis : MonoBehaviour
{
    public int segments = 50; // Je höher, desto runder der Kreis
    public float radius = 3.0f;
    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segments;
        line.useWorldSpace = false; // Der Kreis bewegt sich mit dem Objekt mit

        DrawHollowCircle();
    }

    void DrawHollowCircle()
    {
        float deltaAngle = (2f * Mathf.PI) / segments;
        float angle = 0f;

        for (int i = 0; i < segments; i++)
        {
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius; // Nutze 'y' statt 'z' für reines 2D

            line.SetPosition(i, new Vector3(x, 0f, z));
            angle += deltaAngle;
        }
    }
}
