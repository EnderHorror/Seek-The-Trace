using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 切换季节的基类
/// </summary>
public class SeasonChangeBase : MonoBehaviour
{
    
    protected void  Init()
    {

        SeasonManager.Instance.OnChangeToSpring +=ChangeToSpring;
        SeasonManager.Instance.OnChangeToSummer +=ChangeToSummer;
        SeasonManager.Instance.OnChangeToFall +=ChangeToFall;
        SeasonManager.Instance.OnChangeToWinter +=ChangeToWinter;
        
    }
    

    protected virtual void ChangeToSpring()
    {
        
    }
    protected virtual void ChangeToSummer()
    {
        
    }
    protected virtual void ChangeToFall()
    {
        
    }
    protected virtual void ChangeToWinter()
    {
       
    }
    protected virtual void OnDisable()
    {
        SeasonManager.Instance.OnChangeToSpring -=ChangeToSpring;
        SeasonManager.Instance.OnChangeToSummer -=ChangeToSummer;
        SeasonManager.Instance.OnChangeToFall -=ChangeToFall;
        SeasonManager.Instance.OnChangeToWinter -=ChangeToWinter;
    }
}
