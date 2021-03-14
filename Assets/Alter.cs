using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 祭坛
/// </summary>
public class Alter : MonoBehaviour,Iinteract
{
    public Season targetSeason;
    public bool canReUse = false;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    public bool IsInteract { get; set; } = true;
    public void PressInteract()
    {
        SeasonManager.Instance.SwitchSeason(targetSeason);
        if(!canReUse)
            IsInteract = false;
    }
}
