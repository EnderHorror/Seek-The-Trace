using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class BeeGroup : MonoBehaviour
{
    public Transform target;
    public float speed = 10f;
    
    private Transform[] bees;
    private Rigidbody2D _rigidbody2D;
    void Start()
    {
        bees = GetComponentsInChildren<Transform>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var i in bees)
        {
            i.localScale = new Vector3(Random.Range(0.99f,1.09f),Random.Range(0.99f,1.09f));
        }
        
        if(target!= null)
            RunOff(target);
            
    }

    void RunOff(Transform target)
    {
        Vector3 dir = target.position - transform.position;
        _rigidbody2D.MovePosition(transform.position + (dir.normalized * Time.deltaTime * speed));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && target == other.transform)
        {
            CharacterController2D.Insyance.PlayerDead();
        }
    }
}
