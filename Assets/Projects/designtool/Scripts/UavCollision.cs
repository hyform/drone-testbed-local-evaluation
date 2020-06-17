using UnityEngine;

/// <summary>
/// Detects if the vehicle hits the test stand
/// </summary>
public class UavCollision : MonoBehaviour
{

    /// <summary>
    /// static variable identying a collision
    /// </summary>
    public static bool hit = false;

    /// <summary>
    /// Unity Start method
    /// </summary>
    void Start() { }

    /// <summary>
    /// Unity Update method
    /// </summary>
    void Update() { }

    /// <summary>
    /// fires for a collision event
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        hit = true;
    }

}
