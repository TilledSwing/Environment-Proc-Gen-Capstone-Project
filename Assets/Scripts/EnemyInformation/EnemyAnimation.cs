using FishNet.Connection;
using FishNet.Example.ColliderRollbacks;
using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Play(string state)
    {
        animator.Play(state);
    }

    public void SetBool(string param, bool state)
    {
        animator.SetBool(param, state);
    }

    public void Trigger(string trigger)
    {
        animator.SetTrigger(trigger);
    }

    /// <summary>
    /// This updates the agents movement animation from idle/walk/run
    /// </summary>
    /// <param name="speed"></param>
    public void SetFloat(string param, float speed)
    {
        animator.SetFloat(param, speed);
    }
    
    public void SetInt(string param, int target)
    {
        animator.SetInteger(param, target);
    }
 
}