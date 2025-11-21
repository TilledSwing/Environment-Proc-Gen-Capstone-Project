using FishNet.Demo.AdditiveScenes;
using FishNet.Object;
using GameKit.Dependencies.Utilities;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SocialPlatforms.GameCenter;

public class LandEnemyAILogic : NetworkBehaviour
{
    private GameObject target;
    private float attackRange = 3f;
    private float wanderRadius = 50f;
    private float minWanderDistance = 15f;

    private float thinkRate = .2f;
    private NavMeshAgent agent;
    private EnemyAIMovement enemyMovement;
    private EnemyAnimation enemyAnimation;
    private float wanderTime = 0f;
    private float wanderDuration = 5f;
    private bool isAttacking = false;
    public bool isFrozen = false;
    private float freezeDuration = 5f;
    private float frozenTime = 0f;
    private float totalFrozenTime = 0f;
    private float totalFrozenDeathTimer = 15f;
    private bool isDead = false;
    private List<GameObject> targetsInRange = new List<GameObject>();
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
        if (isDead)
            return;

        if (isFrozen)
        {
            frozenTime -= thinkRate;
            if (frozenTime <= 0f)
            {
                SetFrozen(false);
                return;
            }

            totalFrozenTime += thinkRate;
            if (totalFrozenTime >= totalFrozenDeathTimer)
            {
                StartCoroutine(KillEnemy());
                return;
            }

            return;
        }
        //Dont interupt the attack animation
        if (isAttacking)
            return;

        //Wander if we don't have a target
        if (target != null)
            HandleChaseOrAttack();
        else
            HandleWander();
    }

    /// <summary>
    /// If a target has been assigned, then the enemy will make their way to the target.
    /// If the enemy is in range of the target they will attack them
    /// </summary>
    [Server]
    private void HandleChaseOrAttack()
    {
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        if (distanceToTarget <= attackRange)
        {
            StartCoroutine(AttackTarget());
        }
        else
        {
            UpdateEnemySpeed();
            enemyMovement.MoveTo(target.transform.position);
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
        StopMovement();

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
    /// Kills the enemy
    /// </summary>
    /// <returns></returns>
    [Server]
    private IEnumerator KillEnemy()
    {
        Debug.Log("Calling kill enemy script");
        isDead = true;

        StopMovement();
        CancelInvoke(nameof(ServerThink));

        //Server
        enemyAnimation.Trigger("Die");

        //Client
        RPCTrigger("Die");

        yield return new WaitForSeconds(1.1f);
        ServerManager.Despawn(gameObject);
    }
    /// <summary>
    /// If a target hasn't been assigned we want the enemy to wander around the map. This updates the 
    /// wander destination location every wanderInterval seconds
    /// </summary>
    [Server]
    private void HandleWander()
    {
        //Checks if the agent is attacking for our purposes
        if (wanderTime > 0 && agent.remainingDistance > 0.5f)
        {
            wanderTime -= thinkRate;
            UpdateEnemySpeed();
            return;
        }
        Vector3 newPos = RandomNavMeshLoaction();
        enemyMovement.MoveTo(newPos);
        wanderTime = wanderDuration;
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

    [Server]
    public void SetFrozen(bool frozen)
    {
        if (frozen)
        {
            if (isAttacking)
                isAttacking = false;

            if (!isFrozen)
            {
                StopMovement();
                enemyAnimation.Trigger("GetHit");
                RPCTrigger("GetHit");
                enemyAnimation.SetBool("IsStunned", frozen);
                RPCSetBool("IsStunned", frozen);
            }
            isFrozen = true;
            frozenTime = freezeDuration;
        }
        else
        {
            isFrozen = false;
            enemyAnimation.SetBool("IsStunned", frozen);
            RPCSetBool("IsStunned", frozen);
        }
    }

    /// <summary>
    /// If a player enters the enemies specified range, this is triggered and a target is assigned
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServerInitialized) return;

        if (!other.CompareTag("Player")) return;

        //SoundManager.Instance.PlayMusic("ChaseMusic");
        if (target == null)
            target = other.gameObject;
        targetsInRange.Add(other.gameObject);
    }

    /// <summary>
    /// If a player exits the enemies specified range, this is triggered and the target is unnasigned
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerExit(Collider other)
    {

        if (!IsServerInitialized) return;
        if (!other.CompareTag("Player")) return;

        //SoundManager.Instance.PlayMusic("GameTheme");
        if (targetsInRange.Contains(other.gameObject))
            targetsInRange.Remove(other.gameObject);

        if (other.gameObject == target)
        {
            target = null;  
            if (targetsInRange.Count > 0)
            {
                if (targetsInRange.Count == 1)
                {
                    target = targetsInRange[0];
                }
                else
                {
                    float nearestPlayer = 100000000;
                    float playerDistance;
                    GameObject nextTarget = null;
                    GameObject potentialTarget;

                    for (int i = 0; i < targetsInRange.Count; i++)
                    {
                        potentialTarget = targetsInRange[i];
                        playerDistance = Vector3.Distance(transform.position, potentialTarget.transform.position);
                        if (playerDistance < nearestPlayer)
                        {
                            nextTarget = potentialTarget;
                            nearestPlayer = playerDistance;
                        }
                    }
                    target = nextTarget;
                }
            }
        }
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

    /// <summary>
    /// Sets the clients int animation paramater
    /// </summary>
    /// <param name="param"></param>
    /// <param name="attackIdx"></param>
    [ObserversRpc]
    private void RPCSetBool(string param, bool value)
    {
        enemyAnimation.SetBool(param, value);
    }
    ///##### Helper Methods ##### ///

    private void StopMovement()
    {
        enemyMovement.StopMovement();
        enemyAnimation.SetFloat("Speed", 0);
        RPCUpdateEnemySpeed(0);
    }
    /// <summary>
    /// Finds a random wander location. Enemies appear to wander when this is called after t seconds 
    /// </summary>
    /// <param name="wanderRadius"></param>
    /// <returns></returns>
    private Vector3 RandomNavMeshLoaction()
    {
        int attempts = 30;
        while (attempts > 0)
        {
            Vector3 randomDirection = agent.transform.position + Random.insideUnitSphere * wanderRadius;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(randomDirection, out navHit, 5f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(agent.transform.position, navHit.position) >= minWanderDistance)
                    return navHit.position;
            }
            attempts--;
        }
        return agent.transform.position;
    }
}