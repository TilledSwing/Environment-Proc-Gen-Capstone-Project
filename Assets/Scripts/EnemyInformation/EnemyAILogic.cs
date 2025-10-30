using FishNet.Demo.AdditiveScenes;
using FishNet.Object;
using GameKit.Dependencies.Utilities;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAILogic : NetworkBehaviour
{
    private Transform target;
    private float attackRange = 2f;
    private float wanderInterval = 2f;
    private float wanderRadius = 30f;
    private float minWanderDistance = 15f;
    private float thinkRate = .2f;
    private NavMeshAgent agent;
    private EnemyAIMovement enemyMovement;
    private EnemyAnimation enemyAnimation;
    private float wanderTime = 0f;
    private bool isAttacking = false;

    public void Awake()
    {
        enemyMovement = GetComponent<EnemyAIMovement>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (IsServerStarted)
            InvokeRepeating(nameof(ServerThink), 0f, thinkRate);
    }

    /// <summary>
    /// The server "thinks" about what the agent should do next. This includes starting attacks,
    /// wandering and moving towards its target
    /// </summary>
    [Server]
    void ServerThink()
    {
        //Dont interupt the attack animation
        if (isAttacking)
            return;

        //Wander if we don't have a target
        if (target != null)
            HandleChaseOrAttack();
        else
            GetClosestTarget();
            HandleWander();
        
        //No matter what update speed as this changes based on terrain and position
        UpdateEnemySpeed();
    }

    /// <summary>
    /// If a target has been assigned, then the enemy will make their way to the target.
    /// If the enemy is in range of the target they will attack them
    /// </summary>
    [Server]
    private void HandleChaseOrAttack()
    {
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        // if (distanceToTarget > loseTargetRadius)
        // {
        //     target = null;
        //     return;
        // }

        if (distanceToTarget <= attackRange)
        {
            StartCoroutine(AttackTarget());
        }
        else
            enemyMovement.MoveTo(target.position);
    }

    [Server]
    private void GetClosestTarget()
    {
        var enemies = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        Transform closestTarget = null;
        float closestDist = Mathf.Infinity;
        foreach (PlayerController play in enemies)
        {
            float dist = Vector3.Distance(transform.position, play.transform.position);
            if (dist < closestDist)
            {
                closestTarget = play.transform;
                closestDist = dist;
            }
        }

        if (closestTarget != null)
        {
            target = closestTarget;
        }
            
    }

    /// <summary>
    /// If the enemy is in range this will select one of the attack animations and attack the enemy
    /// </summary>
    /// <returns></returns>
    [Server]
    private IEnumerator AttackTarget()
    {
        isAttacking = true;
        enemyMovement.StopMovement();

        int attackIdx = Random.Range(1, 4);

        //Server
        enemyAnimation.Trigger("Attacking");
        enemyAnimation.SetInt("AttackIdx", attackIdx);

        //Client
        RPCTrigger("Attacking");
        RPCSetInt("AttackIdx", attackIdx);

        yield return new WaitForSeconds(2.333f);
        isAttacking = false;
    }

    /// <summary>
    /// If a target hasn't been assigned we want the enemy to wander around the map. This updates the 
    /// wander destination location every wanderInterval seconds
    /// </summary>
    [Server]
    private void HandleWander()
    {
        //Checks if the agent is attacking for our purposes
        if (Time.time < wanderTime && agent.remainingDistance > 0.5f)
            return;

        Vector3 newPos = RandomNavMeshLoaction(wanderRadius);
        enemyMovement.MoveTo(newPos);
        wanderTime = Time.time + wanderInterval;
    }

    /// <summary>
    /// Updates the enemy animation speed to ensure that they are in the correct movement state idle/walk/run
    /// </summary>
    [Server]
    private void UpdateEnemySpeed()
    {
        float speed = agent.velocity.magnitude;
        enemyAnimation.SetFloat("Speed", speed);
        RPCUpdateEnemySpeed(speed);
    }

    /// ##### Client methods for updates ##### /// 

    /// <summary>
    /// Updates the client AI enemy speed animation
    /// </summary>
    /// <param name="speed"></param>
    [ObserversRpc]
    private void RPCUpdateEnemySpeed(float speed)
    {
        enemyAnimation.SetFloat("Speed", speed);
    }

    /// <summary>
    /// Triggers the clients specific trigger
    /// </summary>
    /// <param name="param"></param>
    [ObserversRpc]
    private void RPCTrigger(string param)
    {
        enemyAnimation.Trigger(param);
    }

    /// <summary>
    /// Sets the clients int animation paramater
    /// </summary>
    /// <param name="param"></param>
    /// <param name="attackIdx"></param>
    [ObserversRpc]
    private void RPCSetInt(string param, int attackIdx)
    {
        enemyAnimation.SetInt(param, attackIdx);
    }

    ///##### Helper Methods ##### ///

    /// <summary>
    /// Finds a random wander location. Enemies appear to wander when this is called after t seconds 
    /// </summary>
    /// <param name="wanderRadius"></param>
    /// <returns></returns>
    private Vector3 RandomNavMeshLoaction(float wanderRadius)
    {
        int attempts = 10;
        while (attempts > 0)
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += agent.transform.position;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(randomDirection, out navHit, 5f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(agent.transform.position, navHit.position) >= minWanderDistance)
                    return navHit.position;
            }
        }
        return agent.transform.position;
    }
}