using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerControl : MonoBehaviour
{
    public Transform player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (player.position.y < 0)
        {
            SceneManager.LoadScene(0);
        }

        if (player.position.z > 18)
        {
            SceneManager.LoadScene(0);
        }

        if (player.position.z < -18)
        {
            SceneManager.LoadScene(0);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(0);
        }
    }
}
