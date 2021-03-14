using System;
using System.Collections;
using System.Collections.Generic;
using Bolt;
using DG.Tweening;
using UnityEngine;

public class Hole : SeasonChangeBase,Iinteract
{
    public bool IsInteract { get; set; } = true;

    public GameObject flower;
    public GameObject seed;
    public GameObject bee;

    private BeeGroup _beeGroup;
    void Start()
    {
        _beeGroup = bee.GetComponent<BeeGroup>();
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    

    public void PressInteract()
    {
        SeasonManager.Instance.OnChangeToSpring += Grow;
        seed.SetActive(true);

        IsInteract = false;
    }

    protected override void OnDisable()
    {
        SeasonManager.Instance.OnChangeToSpring -= Grow;

    }

    void Grow()
    {
        seed.GetComponent<SpriteRenderer>().DOColor(Color.clear, SeasonManager.TransitionTime);
        flower.SetActive(true);
        flower.GetComponent<SpriteRenderer>().DOColor(Color.white, SeasonManager.TransitionTime);
        _beeGroup.target = flower.transform;
    }
}
