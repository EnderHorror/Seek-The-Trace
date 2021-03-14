using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class WaterFall : MonoBehaviour
{
    private Transform branch;
    private Renderer _renderer;
    void Start()
    {
        _renderer = GetComponent<Renderer>();
        branch = transform.GetChild(0);
        BranchMove();
    }

    void BranchMove()
    {
        branch.position = new Vector2(branch.position.x,_renderer.bounds.max.y);
        var tweener = branch.DOMoveY(_renderer.bounds.min.y,6);
        tweener.SetEase(Ease.Linear);
        tweener.onComplete += () =>
        {
            BranchMove();
        };
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
