using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AI;

public class CharacterMovement: MonoBehaviour {
    public Animator animator;
    public NavMeshAgent agent;
    public float inputHoldDelay = 0.5f;
    public float turnSpeedThreshold = 0.5f;
    public float speedDampTime = 0.1f;
    public float slowingSpeed = 0.175f;
    public float turnSmoothing = 15f;

    private WaitForSeconds inputHoldWait;
    private Vector3 destinationPosition;
    private GameObject currentInteractable;
    private bool handleInput = true;

    private const float stopDistanceProportion = 0.1f;
    private const float navMeshSampleDistance = 4f;

    private readonly int hashSpeedParam = Animator.StringToHash("Speed");
    private readonly int hashLocomotionTag = Animator.StringToHash("Locomotion");

    private void Start() {
        agent.updateRotation = false;
        inputHoldWait = new WaitForSeconds(inputHoldDelay);
        destinationPosition = transform.position;
    }

    private void OnAnimatorMove() {
        agent.velocity = animator.deltaPosition / Time.deltaTime;
    }

    private void Update() {
        if (agent.pathPending) {
            return;
        }

        float speed = agent.desiredVelocity.magnitude;

        if (agent.remainingDistance <= agent.stoppingDistance * stopDistanceProportion) {
            Stopping(out speed);

        } else if (agent.remainingDistance <= agent.stoppingDistance) {
            Slowing(out speed, agent.remainingDistance);

        } else if (speed > turnSpeedThreshold) {
            Moving();
        }

        animator.SetFloat(hashSpeedParam, speed, speedDampTime, Time.deltaTime);

    }

    private void Stopping(out float speed) {
        agent.isStopped = true;
        transform.position = destinationPosition;
        speed = 0f;

        if (currentInteractable) {
            transform.rotation = currentInteractable.transform.localRotation;//.interactionLocation.rotation;
            //currentInteractable.Interact();
            currentInteractable = null;
        }
        
        StartCoroutine(WaitForInteraction());
    }

    private void Slowing(out float speed, float distanceToDestination) {
        agent.isStopped = true;
        transform.position = 
            Vector3.MoveTowards(transform.position, destinationPosition, slowingSpeed * Time.deltaTime);
        float proportionalDistance = 1f - distanceToDestination / agent.stoppingDistance;
        speed = Mathf.Lerp(slowingSpeed, 0f, proportionalDistance);

        Quaternion targetRotation = currentInteractable
            ? currentInteractable.transform.localRotation// interactionLocation.rotation
            : transform.rotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, proportionalDistance);
    }

    // normal case, handled by the animator, here we just rotate
    private void Moving() {
        Quaternion targetRotation = Quaternion.LookRotation(agent.desiredVelocity);
        transform.rotation = 
            Quaternion.Lerp(transform.rotation, targetRotation, turnSmoothing * Time.deltaTime);
    }

    private IEnumerator WaitForInteraction() {
        handleInput = false;

        yield return inputHoldWait;

        while (animator.GetCurrentAnimatorStateInfo(0).tagHash != hashLocomotionTag) {
            yield return null;
        }

        handleInput = true;
    }
}
