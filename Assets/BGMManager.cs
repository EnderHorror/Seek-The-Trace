using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BGMManager : SeasonChangeBase
{
    public AudioClip[] AudioClips;

    private AudioSource _source;
    
    void Start()
    {
        Init();

        _source = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected override void ChangeToSpring()
    {
        base.ChangeToSpring();

        AudioTrsition(AudioClips[0]);
    }

    protected override void ChangeToSummer()
    {
        base.ChangeToSummer();
        AudioTrsition(AudioClips[1]);
    }

    protected override void ChangeToFall()
    {
        base.ChangeToFall();
        AudioTrsition(AudioClips[2]);
    }

    protected override void ChangeToWinter()
    {
        base.ChangeToWinter();
        AudioTrsition(AudioClips[3]);
    }

    void AudioTrsition(AudioClip clip)
    {
        var start = _source.DOFade(0, SeasonManager.TransitionTime);
        start.onComplete += () =>
        {
            _source.clip = clip;
            _source.Play();
            _source.DOFade(1, SeasonManager.TransitionTime);
        };

    }
}
