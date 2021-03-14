using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 需要按E交互的物品接口
/// </summary>
public interface Iinteract
{
    bool IsInteract { get; set; }
    void PressInteract();
}


/// <summary>
/// 交换提示标签
/// </summary>
public class InteractLable : MonoBehaviour
{
    public static InteractLable Instance { get; private set; }
    
    public LayerMask mask;
    public List<Collider2D> colliderList = new List<Collider2D>();
    private GameObject text;
    private BoxCollider2D _collider2D;
    private ContactFilter2D _filter2D = new ContactFilter2D();

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        text = transform.Find("Sign").gameObject;
        _collider2D = GetComponent<BoxCollider2D>();
        _filter2D.layerMask = mask;
        _filter2D.useLayerMask = true;
        _filter2D.useTriggers = true;
    }

    // Update is called once per frame
    void Update()
    {
        transform.localScale = transform.parent.localScale;
        _collider2D.OverlapCollider(_filter2D, colliderList);

        text.SetActive(false);

        foreach (var item in colliderList)
        {
            Iinteract temp;
            if (item.TryGetComponent<Iinteract>(out temp))
            {
                if (temp.IsInteract)
                {
                    text.SetActive(true);
                    break;
                }
            }
        }
        


        Iinteract target;

        if (Input.GetKeyDown(KeyCode.E))
        {
            foreach (var item in colliderList)
            {
                if (item.TryGetComponent<Iinteract>(out target))
                {
                    if (target.IsInteract)
                    {
                        target.PressInteract();
                        break;
                    }
                    continue;
                }
            }
        }
        

        
    }
    
}
