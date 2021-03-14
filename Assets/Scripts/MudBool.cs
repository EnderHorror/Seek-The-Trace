using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MudBool : MonoBehaviour
{

    public float deadDelay = 4f;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerInput.Instance.enabled = false;
            other.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            other.GetComponent<Rigidbody2D>().velocity = new Vector2(0,-0.5f);
            StartCoroutine(PlayerDie());

        }
    }

    IEnumerator PlayerDie()
    {
        yield return new WaitForSeconds(deadDelay);
        CharacterController2D.Insyance.PlayerDead();
    }

}
