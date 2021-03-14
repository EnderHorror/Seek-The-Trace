using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeeAttactRange : MonoBehaviour
{
    private BeeGroup _beeGroup;
    void Start()
    {
        _beeGroup = GameObject.FindObjectOfType<BeeGroup>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && _beeGroup.target == null)
        {
            _beeGroup.target = other.transform;
        }
    }
}
