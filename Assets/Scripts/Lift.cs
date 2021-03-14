using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using DG.Tweening;

public class Lift : MonoBehaviour
{
    public Vector2 target;

    private Vector2 originPos;
    private Rigidbody2D _rigidbody2D;
    private Tweener _tweener;
    void Start()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        originPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawCube(target,Vector3.one);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == PlayerInput.Instance.gameObject)
        {
            if (_tweener != null) _tweener.Pause();
            _tweener =  _rigidbody2D.DOMove(target, 2f);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == PlayerInput.Instance.gameObject)
        {
            if (_tweener != null) _tweener.Pause();
            _tweener =  _rigidbody2D.DOMove(originPos, 0.5f);
        }
    }


}
