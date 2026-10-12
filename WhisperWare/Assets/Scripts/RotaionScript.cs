using System;
using UnityEngine;

public class RotaionScript : MonoBehaviour
{
    [SerializeField]  float rationsspeed;
    [SerializeField] private Vector3 axis = Vector3.forward;

    // Update is called once per frame
    void Update()
    {
        float angle = rationsspeed * Time.deltaTime;

        transform.RotateAround(axis, angle);

    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Kreis")
        {
            Console.WriteLine("Test123");
        }

        if (other.gameObject.CompareTag("Kreis"))
        {
            Console.WriteLine("123Test");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Console.WriteLine("test67");
    }
}
