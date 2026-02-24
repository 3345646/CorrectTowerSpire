using UnityEngine;

public class DungeonPiece : MonoBehaviour
{
    public GameObject ExitPoint;
    public Transform player;

    public enum ExitDir
    {
        Up,
        Left,
        Right,
        UpLeft,
        UpRight,
        RightUp,
        LeftUp,
    }

    public ExitDir Dir;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.name.Contains("(Clone)"))
        {
            float DistanceToPlayer = player.position.y - transform.position.y;

            if (DistanceToPlayer > 500)
            {
                Destroy(gameObject);
            }
        }
    }
}
