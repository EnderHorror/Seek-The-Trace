using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorRequir : MonoBehaviour
{
    public string[] requirs;
    public GameObject[] prefabs;
    void Start()
    {
        for (int i = 0; i < requirs.Length; i++)
        {
            if (GameObject.Find(requirs[i]) == null)
            {
                Instantiate(prefabs[i]);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
