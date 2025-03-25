using UnityEngine;

public class FollowCamera : MonoBehaviour
{

    [SerializeField]
    public GameObject player;

    private Vector2 offset;

    void Start()
    {
        offset = transform.position - player.transform.position;
    }

    void LateUpdate()
    {
        transform.position = new Vector2(player.transform.position.x, player.transform.position.y) + offset;
    }
}