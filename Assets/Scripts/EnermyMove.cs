using System;
using System.Collections;
using System.Collections.Generic;
using Bolt;
using Ludiq;
using UnityEngine;

public class EnermyMove : MonoBehaviour
{
    public float maxSpeed;
    public float trueTime = 0.5f;


    private Variables _variables;
    private Rigidbody2D _rigidbody2D;
    private bool canMove = true;
    void Start()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _variables = GetComponent<Variables>();
    }
    
    void PreventSlid(bool isFind)
    {
        _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (!isFind) _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeAll;

    }
    // Update is called once per frame
    void Update()
    {
        PreventSlid(Variables.Object(gameObject).Get<bool>("isFind"));
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            CharacterController2D.Insyance.PlayerDead();
        }
    }

    public void Move(float dir)
    {
        if (Mathf.Sign(dir) != Mathf.Sign(_rigidbody2D.velocity.x) && _rigidbody2D.velocity.x != 0 && canMove)
        {
            StartCoroutine(Turing(trueTime));
        }
        if (canMove)
        {
            _rigidbody2D.velocity += new Vector2(dir,0);
            _rigidbody2D.velocity = Vector2.ClampMagnitude(new Vector2(_rigidbody2D.velocity.x, 0), maxSpeed)+new Vector2(0, _rigidbody2D.velocity.y);
        }
        
    }

    IEnumerator Turing(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }
}
