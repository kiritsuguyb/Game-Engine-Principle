using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collideWithPlayer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody.gameObject.tag.Equals("Player"))
        {
            Debug.Log("Hit Player");
            FindObjectOfType<Player>().score += 1;
            Destroy(gameObject.transform.parent.parent.gameObject);
        }
    }
}
