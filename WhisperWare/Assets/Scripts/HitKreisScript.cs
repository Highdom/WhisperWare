using System;
using UnityEngine;

public class HitKreisScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Dreieck"))
            {
                Console.WriteLine("Quadrat hat das Dreieck getroffen!");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {

            Console.WriteLine("Test");
        }
}
