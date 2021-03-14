using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BounceArea : MonoBehaviour
{
    public float force = 2000;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var rg = other.gameObject.GetComponent<Rigidbody2D>();
            if (rg.velocity.y < -5)
            {
                rg.velocity = new Vector2(rg.velocity.x,0);
                rg.AddForce(Vector2.up * force);
            }
        }
    }
}
