using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneBase : MonoBehaviour
{
    public int index;

    public void LoadScene(int _index)
    {
        SceneManager.LoadScene(_index);
    }
}
