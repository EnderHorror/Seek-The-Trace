using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowWait : MonoBehaviour
{
    public float waitTime = 5f;
    public GameObject obj;
    void Start()
    {
        StartCoroutine(Show(waitTime));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator Show(float waitTime)
    {
        obj.SetActive(false);
        yield return new WaitForSeconds(waitTime);
        obj.SetActive(true);
    }
}
