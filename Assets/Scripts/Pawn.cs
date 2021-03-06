﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Pawn : MonoBehaviour {

    // Set in editor
    public float height; // Float distance above planet
    public float moveSpeed;
    public float attackSpeed;
    public float attackRange; // Range we need to be at to physically attack
    public float trackingRange; // Range at which we'll autoattack any enemies
    public int startingHealth;
    public int damage;
    public GameObject bloodPS;
    public GameObject ragdollPrefab;

    public PlayerNum owner;// { get; private set; }
    private Planet planet;
    private Rigidbody rb;
    public Vector3 targetPosition; // debug - make private
    public Pawn trackingOpponent; // Debug - make private
    private Pawn attackingOpponent;
    private float attackTimer;
    public int healthRemaining { get; private set; }
    private LayerMask targetingLayerMask;
    private AudioSource audioSource;
    private float _height;

	// Use this for initialization
	void Start () {
        planet = FindObjectOfType<Planet>(); // TODO: remove FindObjectOfType
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

	// Update is called once per frame
	void Update () {
        _height -= Mathf.Abs(_height - height) * Time.deltaTime * 15;

        HandleAttacking();
        SnapToPlanet();

        Debug.DrawRay(transform.position + transform.up * 1.2f + transform.right, -transform.right * 2 * ((float)healthRemaining / startingHealth), PlayerMethods.GetPlayerColor(owner));
	}

    public void Init(PlayerNum owner, GameManager gameManager) {
        this.owner = owner;
        SetColor(PlayerMethods.GetPlayerColor(owner), gameManager);
        healthRemaining = startingHealth; // Need to do this here instead of start so that if someone hits us as we spawn we don't insta-die
        
        // Ugly hack - could be cleaned up
        if (owner == PlayerNum.One) {
            targetingLayerMask = PlayerMethods.allButP1;
            gameObject.layer = LayerMask.NameToLayer("Pawn1");
        } else if (owner == PlayerNum.Two) {
            targetingLayerMask = PlayerMethods.allButP2;
            gameObject.layer = LayerMask.NameToLayer("Pawn2");
        } else if (owner == PlayerNum.Three) {
            targetingLayerMask = PlayerMethods.allButP3;
            gameObject.layer = LayerMask.NameToLayer("Pawn3");
        } else if (owner == PlayerNum.Four) {
            targetingLayerMask = PlayerMethods.allButP4;
            gameObject.layer = LayerMask.NameToLayer("Pawn4");
        }

        _height = 100;
    }

    public void SetTargetPos(Vector3 target) {
        targetPosition = target;
    }

    public void TakeDamage(int amount, Vector3 sourceLocation) {
        healthRemaining -= amount;
        ParticleSystem blood = Instantiate(bloodPS, transform.position, Quaternion.LookRotation(transform.up)).GetComponent<ParticleSystem>();
        blood.startColor = PlayerMethods.GetPlayerColor(owner);
        if (healthRemaining < 0) {
            for (int i = 0; i < 2; i++) {
                blood = Instantiate(bloodPS, transform.position, Quaternion.LookRotation(transform.up)).GetComponent<ParticleSystem>();
                blood.startColor = PlayerMethods.GetPlayerColor(owner);
                blood.gameObject.transform.localScale *= 2;

                AudioSource source = blood.GetComponent<AudioSource>();
                source.pitch = Random.Range(2f, 2.5f);
                source.Play();
            }

            GameObject guts = Instantiate(ragdollPrefab, transform.position, Random.rotation);
            guts.GetComponentInChildren<Renderer>().sharedMaterial = GetComponentInChildren<Renderer>().sharedMaterial;
            Vector3 planetToPawn = (transform.position - planet.transform.position).normalized;
            Vector3 sourceToPawn = (transform.position - sourceLocation).normalized;
            guts.GetComponent<Rigidbody>().velocity = planetToPawn * 15 + sourceToPawn * 20 + Random.onUnitSphere * 2;
            guts.GetComponent<Rigidbody>().angularVelocity = Random.onUnitSphere * 3;

            Destroy(gameObject);
        } else {
            audioSource.pitch = Random.Range(2f, 2.5f);
            audioSource.Play();
        }

    }

    public void SetColor(Color color, GameManager gameManager) {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) {
            renderer.sharedMaterial = gameManager.GetPlayerSharedMat(color);
        }
    }

    private void HandleAttacking() {
        // Check if our targets are still good
        if (trackingOpponent != null && Vector3.Distance(trackingOpponent.transform.position, targetPosition) > trackingRange) {
            trackingOpponent = null;
        }
        if (attackingOpponent != null && Vector3.Distance(attackingOpponent.transform.position, transform.position) > attackRange) {
            attackingOpponent = null;
        }

        // Find new targets if necessary
        if (attackingOpponent == null) {
            foreach (Collider col in Physics.OverlapSphere(transform.position, attackRange, targetingLayerMask)) { // Only seek opponents that are within range of our target position
                attackingOpponent = col.GetComponent<Pawn>();
                if (Vector3.Distance(attackingOpponent.transform.position, targetPosition) < trackingRange) {
                    trackingOpponent = attackingOpponent;
                }
            }
        }
        if (trackingOpponent == null) {
            foreach (Collider col in Physics.OverlapSphere(targetPosition, trackingRange, targetingLayerMask)) { // Only seek opponents that are within range of our target position
                trackingOpponent = col.GetComponent<Pawn>();
            }
        }
        
        // Do the attack
        attackTimer += Time.deltaTime;
        if (attackTimer > attackSpeed && attackingOpponent != null) {
            attackingOpponent.TakeDamage(damage, transform.position);
            attackTimer = 0;
        }

    }

    private void SnapToPlanet() {
        Vector3 toSurface = planet.toSurface(transform.position);
        transform.position += toSurface.normalized * (toSurface.magnitude - _height);
        Vector3 newForward = Vector3.ProjectOnPlane(transform.forward, -toSurface);
        transform.rotation = Quaternion.LookRotation(newForward, -toSurface);
        if (trackingOpponent != null) {
            rb.velocity = Vector3.ProjectOnPlane(trackingOpponent.transform.position - transform.position, -toSurface) * moveSpeed;
        } else if (targetPosition != Vector3.zero) {
            rb.velocity = Vector3.ProjectOnPlane(targetPosition - transform.position, -toSurface) * moveSpeed;
            Debug.DrawRay(transform.position, rb.velocity, Color.cyan);
            Debug.DrawLine(transform.position, targetPosition, Color.black);
        }
    }

    public void PlayAudio(AudioClip clip) {
        audioSource.pitch = Random.Range(1.5f, 2.5f);
        audioSource.PlayOneShot(clip);
    }
}
