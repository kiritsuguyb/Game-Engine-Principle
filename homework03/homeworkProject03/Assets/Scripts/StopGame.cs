using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StopGame : MonoBehaviour
{
    public bool win = false;
    public bool lost = false;
    public Text gameoverText;
    // Start is called before the first frame update
    void Start()
    {
        gameoverText.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Escape)) Application.Quit();
        if (!win && !lost) return;
        Time.timeScale = 0;
        if (lost) gameoverText.text = "Score:" + 0;
        else gameoverText.text = "Score:" + FindObjectOfType<Player>().score;
        gameoverText.gameObject.SetActive(true);

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody.gameObject.tag.Equals("Player"))
        {
            win = true;
        }
    }
}
