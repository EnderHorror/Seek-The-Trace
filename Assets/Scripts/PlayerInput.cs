using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{

    public static PlayerInput Instance { get; private set; }
    
    public Vector2 inputDir;
    public bool jump = false;
    private CharacterController2D _controller2D;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _controller2D = GetComponent<CharacterController2D>();
    }

    // Update is called once per frame
    void Update()
    {
        inputDir.x = Input.GetAxis("Horizontal");
        inputDir.y = Input.GetAxis("Vertical");

        jump = false;

        if (Input.GetKeyDown(KeyCode.Space))
            jump = true;

    }

    private void OnDisable()
    {
        inputDir = Vector2.zero;
        jump = false;
    }
}
