using UnityEngine;
public class Rotator : MonoBehaviour
{
    void Update()
    {
        // Spin slowly on the Y axis
        transform.Rotate(0, 50 * Time.deltaTime, 0);
    }
}