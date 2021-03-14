using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCDIalogSystem : MonoBehaviour
{
    public bool lockPlayerMove = true;
    
    private bool isFirst = true;
    private GameObject canvas;
    void Start()
    {
        canvas = transform.GetChild(0).gameObject;
        canvas.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isFirst)
        {
            canvas.SetActive(true);
            isFirst = false;

            if (lockPlayerMove)
            {
                PlayerInput.Instance.enabled = false;
                canvas.GetComponent<Dialog>().OnFinish += () =>
                {
                    PlayerInput.Instance.enabled = true;
                };
            }
        }
    }
}
