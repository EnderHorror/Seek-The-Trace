using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 力场
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ForceField : MonoBehaviour
{
    // Start is called before the first frame update
    public Vector3 force;

    private Rigidbody2D _rigidbody2D;
    private BoxCollider2D _collider2D;
    void Start()
    {
        _collider2D = GetComponent<BoxCollider2D>();
        if (!_collider2D.isTrigger) _collider2D.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        _rigidbody2D = other.GetComponent<Rigidbody2D>();
    }

    private void OnDrawGizmos()
    {
        _collider2D = GetComponent<BoxCollider2D>();
        Gizmos.DrawLine(transform.position,transform.position + Vector3.ClampMagnitude(force,10));
        Gizmos.DrawWireCube(transform.position,_collider2D.size);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        _rigidbody2D.AddForce(force);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
