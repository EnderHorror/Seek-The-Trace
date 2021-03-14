using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 季节枚举
/// </summary>
[Serializable]
public enum Season
{
    Spring,Summer,Fall,Winter
}
/// <summary>
/// 季节管理
/// </summary>
public class SeasonManager : MonoBehaviour
{
    public static float TransitionTime = 1f;
    public Season CurrentSeason
    {
        get { return _season;}
        private set { _season = value; }
    }
    [SerializeField] private Season _season;
    
    public static SeasonManager Instance { get; private set; }
    
    public event  Action OnChangeToSpring;
    public event  Action OnChangeToSummer;
    public event  Action OnChangeToFall;
    public event  Action OnChangeToWinter;
    public event  Action OnChangeSeason;
    

    private void Awake()
    {
        Instance = this;

    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwitchSeason(Season target)
    {
        switch (target)
        {
            case Season.Spring:
                print("Switch Spring");
                OnChangeToSpring?.Invoke();
                break;
            case Season.Summer:
                print("Switch Summer");
                OnChangeToSummer?.Invoke();
                break;
            case Season.Fall:
                print("Switch Fall");
                OnChangeToFall?.Invoke();
                break;
            case Season.Winter:
                print("Switch Winter");
                OnChangeToWinter?.Invoke();
                break;
        }

        CurrentSeason = target;
        OnChangeSeason?.Invoke();
    }
    
}
