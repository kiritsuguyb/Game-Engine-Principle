using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ExternalBirdControllingChanger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyUp(KeyCode.C))
        {
            GetComponent<Toggle>().isOn = !GetComponent<Toggle>().isOn;
        }
    }
}
