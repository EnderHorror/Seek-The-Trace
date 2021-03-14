using System;
using System.Collections;
using System.Collections.Generic;
using Bolt;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 河流
/// </summary>
public class River : SeasonChangeBase
{
    private BoxCollider2D _collider2D;
    private Renderer _renderer;
    private AudioSource _source;
    void Start()
    {
        _renderer = GetComponent<Renderer>();
        _collider2D = GetComponent<BoxCollider2D>();
        _source = GetComponent<AudioSource>();
        Init();

    }

    protected override void ChangeToWinter()
    {
        _renderer.material.DOFloat(1, "_Frozen", SeasonManager.TransitionTime);
        _source.DOFade(0, SeasonManager.TransitionTime);
        base.ChangeToWinter();
        _collider2D.enabled = true;
    }

    protected override void ChangeToFall()
    {
        _renderer.material.DOFloat(0, "_Frozen", SeasonManager.TransitionTime);
        _source.DOFade(1, SeasonManager.TransitionTime);
        base.ChangeToFall();
        _collider2D.enabled = false;
    }

    protected override void ChangeToSummer()
    {
        _renderer.material.DOFloat(0, "_Frozen", SeasonManager.TransitionTime);
        _source.DOFade(1, SeasonManager.TransitionTime);
        base.ChangeToSummer();
        _collider2D.enabled = false;
    }

    protected override void ChangeToSpring()
    {
        base.ChangeToSpring();
        _source.DOFade(1, SeasonManager.TransitionTime);
        _renderer.material.DOFloat(0, "_Frozen", SeasonManager.TransitionTime);
        _collider2D.enabled = false;
    }
    
    
}
