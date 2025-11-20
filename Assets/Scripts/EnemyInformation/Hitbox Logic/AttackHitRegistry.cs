using System.Collections.Generic;
using UnityEngine;

public class AttackHitRegistry : MonoBehaviour
{
    /// <summary>
    /// Stores which targets have already been hit during the current attack.
    /// Shared across all hitboxes (left arm, right arm, etc.)
    /// </summary>
    public HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

    /// <summary>
    /// Clears the registry for the next attack.
    /// Called at the start of an attack animation.
    /// </summary>
    public void ResetHits()
    {
        alreadyHit.Clear();
    }
}
