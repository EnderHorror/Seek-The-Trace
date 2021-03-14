using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class LightSeasonControl : MonoBehaviour
{
    private Light2D _light2D;
    private SeasonManager _manager;
    private Tweener _tweener;
    private float intensity = 0;
    
    void Start()
    {
        _light2D = GetComponent<Light2D>();
        intensity = _light2D.intensity;
        _manager = SeasonManager.Instance;
        
        _manager.OnChangeToSpring += ChangeToSpring;
        _manager.OnChangeToSummer += ChangeToSummer;
        _manager.OnChangeToFall += ChangeToFall;
        _manager.OnChangeToWinter += ChangeToWinter;
    }

    // Update is called once per frame
    void Update()
    {
        if (_tweener != null)
            _light2D.intensity = intensity;

    }

    void ChangeToSpring()
    {
        _tweener = DOTween.To(() => intensity, (s) => intensity = s, 1, SeasonManager.TransitionTime);
    }

    void ChangeToSummer()
    {
        _tweener = DOTween.To(() => intensity, (s) => intensity = s, 1, SeasonManager.TransitionTime);
    }
    void ChangeToFall()
    {
        _tweener = DOTween.To(() => intensity, (s) => intensity = s, 0.8f, SeasonManager.TransitionTime);
    }
    void ChangeToWinter()
    {
        _tweener = DOTween.To(() => intensity, (s) => intensity = s, 0, SeasonManager.TransitionTime);

    }


}
