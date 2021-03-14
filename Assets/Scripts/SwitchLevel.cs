using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class SwitchLevel : LoadSceneBase
{
    // Start is called before the first frame update
    
    public float switchDruation = 0.5f;
    public float switchDelay = 2f;

    private Image switchGround;

    private void Start()
    {
        switchGround = GameObject.FindGameObjectWithTag("End").transform.GetChild(0).GetComponent<Image>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
         SwitchSceneWithAniation(index);
    }

    public void ReloadScene()
    {
        SwitchSceneWithAniation(index - 1);
    }

    public void SwitchSceneWithAniation(int _index)
    {
        DontDestroyOnLoad(gameObject);
        switchGround.gameObject.SetActive(true);
       var start = switchGround.DOColor(Color.black, switchDruation);
       start.onComplete += () =>
       {
           var startEndDelay = switchGround.DOColor(Color.black, switchDelay);
           startEndDelay.onComplete += () =>
           {
               LoadScene(_index);
               var endStartDelay = switchGround.DOColor(Color.black, switchDelay);
               endStartDelay.onComplete += () =>
               {
                   var end = switchGround.DOColor(Color.clear, switchDruation);
                   end.onComplete += () =>
                   {
                       switchGround.gameObject.SetActive(false);
                       Destroy(gameObject);
                   };
               };

           };
          

       };
    }
}
