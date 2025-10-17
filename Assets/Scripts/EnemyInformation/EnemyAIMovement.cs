using FishNet.Connection;
using FishNet.Example.ColliderRollbacks;
using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIMovement : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;

    /// <summary>
    /// Grabs the movement component of the enemy
    /// </summary>
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// Given a vector3 position sets the agents destination and begins their movement
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        //Always sets to true so that if movement is coming from a static position the agent moves
        agent.isStopped = false;
        agent.SetDestination(destination);
    }


    /// <summary>
    /// Called when a static animation is called. Stops the enemy movement
    /// </summary>
    public void StopMovement()
    {
        agent.isStopped = true;
    }
}