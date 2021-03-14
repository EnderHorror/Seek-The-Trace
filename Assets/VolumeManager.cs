using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public class VolumeManager : MonoBehaviour
{
    public int[] colorTemp = new int[4];
    
    private Volume _volume;
    private VolumeComponent whiteBalance;
    private SeasonManager _seasonManager;
    private int temper = 0;
    private Tweener tempColorTweener;
    void Start()
    {
        _seasonManager = SeasonManager.Instance;
        _volume = GetComponent<Volume>();
        whiteBalance = _volume.profile.components[1];

        _seasonManager.OnChangeToSpring += ChangeToSpring;
        _seasonManager.OnChangeToSummer += ChangeToSummer;
        _seasonManager.OnChangeToFall += ChangeToFall;
        _seasonManager.OnChangeToWinter += ChangeToWinter;
    }

    // Update is called once per frame
    void Update()
    {
        if(tempColorTweener != null)
            ChangeWhiteBalance(temper);
    }

    void ChangeToSpring()
    {
        tempColorTweener = DOTween.To(() => temper,(s) => temper =s, colorTemp[0], SeasonManager.TransitionTime);
    }

    void ChangeToSummer()
    {
        tempColorTweener = DOTween.To(() => temper,(s) => temper =s, colorTemp[1], SeasonManager.TransitionTime);

    }

    void ChangeToFall()
    {
        tempColorTweener = DOTween.To(() => temper,(s) => temper =s, colorTemp[2], SeasonManager.TransitionTime);

    }

    void ChangeToWinter()
    {
        tempColorTweener = DOTween.To(() => temper,(s) => temper =s, colorTemp[3], SeasonManager.TransitionTime);

    }

    void ChangeWhiteBalance(int value)
    {
        whiteBalance.parameters[0].SetValue( new ClampedFloatParameter(value,-50,50));

    }
}
