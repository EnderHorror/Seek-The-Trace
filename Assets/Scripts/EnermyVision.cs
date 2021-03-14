using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bolt;
using Ludiq;

public class EnermyVision : MonoBehaviour
{
    public GameObject target;

    public bool isFind = false;
    void Start()
    {
        CustomEvent.Trigger(gameObject,"FindPTarget");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool FindPTarget()
    {
        return isFind;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == target)
        {
            isFind = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == target)
        {
            isFind = false;
        }
    }
}
