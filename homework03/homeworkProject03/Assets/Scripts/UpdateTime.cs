using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateTime : MonoBehaviour
{
    float startTime = 0;
    float currentTime = 60;
    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        currentTime =60-(Time.time - startTime);
        GetComponent<Text>().text = "Time Left: " + ((int)currentTime).ToString() + "s";
        if (currentTime < 0) FindObjectOfType<StopGame>().lost=true;
    }
}
