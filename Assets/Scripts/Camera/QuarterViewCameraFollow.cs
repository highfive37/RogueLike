using UnityEngine;

public class QuarterViewCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -6f);

    public Vector3 Offset => offset;

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        transform.position = player.position + offset;
        transform.LookAt(player);
    }
}
