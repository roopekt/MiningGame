using UnityEngine;

//make the gameobject this is attached to follow the cursor
public class CursorFollower : MonoBehaviour
{
    void Update()
    {
        transform.position = Input.mousePosition;
    }
}
