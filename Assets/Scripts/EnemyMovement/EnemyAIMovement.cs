using FishNet.Connection;
using FishNet.Example.ColliderRollbacks;
using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private float wanderRadius = 15f;
    private float wanderInterval = 3f;
    private float timer;
    private float minDistance = 7;
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = wanderInterval;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= wanderInterval)
        {
            Vector3 newPos = RandomNavMeshLoaction(wanderRadius);
            agent.SetDestination(newPos);
            timer = 0f;
        }
    }

    public Vector3 RandomNavMeshLoaction(float wanderRadius)
    {
        int attempts = 10;
        while (attempts > 0)
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += agent.transform.position;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(randomDirection, out navHit, 5f, NavMesh.AllAreas))
            {
                if(Vector3.Distance(agent.transform.position, navHit.position)>= minDistance)
                    return navHit.position;
            }
        }
        return agent.transform.position;
    }

    public void BeginWandering()
    {
        timer = wanderInterval;
    }
}