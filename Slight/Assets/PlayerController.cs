﻿/// This script handles the player controls and main interactions


using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;



public class PlayerController : MonoBehaviour {

    // Custom vector3 manipulation functions
    public Vector3 MultiplyVector3(Vector3 firstVector, Vector3 secondVector) {
        return new Vector3(firstVector.x * secondVector.x, firstVector.y * secondVector.y, firstVector.z * secondVector.z);
    }

    public Vector3 AddVector3(Vector3 firstVector, Vector3 secondVector)
    {
        return new Vector3(firstVector.x + secondVector.x, firstVector.y + secondVector.y, firstVector.z + secondVector.z);
    }

    // Variables
    // Player components
    public GameObject player;
    public Rigidbody rb;

    // Movement variables
    public Vector3 velMove;
    public float oldYVel;
    public float moveHorizontal;
    public float moveVertical;
    public Vector3 localVelocity;

    // Movement constants
    public float movespeed = 20f;
    public float movespeedLimit = 60f;
    public Vector3 midairModifier = new Vector3(4f, 0f, 4f);
    public Vector3 groundedModifier = new Vector3(1f, 0f, 1f);
    public float playerDynamicFriction = 0.6f;
    public float playerStaticFriction = 0.2f;
    public float jetpackPower = 125f;
    public float jetpackMeter;
    public float jetpackMeterLimit = 50f;
    public float jetpackRecoveryRate = 0.15f;
    public bool isGrounded;
    public bool isSkiing;

    // UI variables
    public Slider powerSlider;
    public GameObject powerSliderObject;
    public GameObject HUDCanvas;
    public Text debugText;
    public GameObject debugTextBox;

    // Gun variables
    public float weaponRange = 100f;
    public float maxAmmoCount = 8f;
    public float ammoCount;
    public GameObject ammoTextBox;
    public Text ammoText;
    public GameObject explosionPrefab;

    // Old bullet variables (projectiles)
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletSpeed = 150f;
    public float bulletTime = 0.8f;
    public Vector3 bulletRotation;

    // Sword variables
    public SwordController swordControllerScript;
    public GameObject swordImagePrefab;
    public bool isSlashing;
    public float swordDuration = 0.4f;

    // Grind Variables
    public Collider railBox;
    public bool isGrinding;
    public Transform railTransform;
    public RailBoxController railBoxScript;

    // Other variables
    public EnemySpawnerHandlerController enemySpawnerHandlerScript;
    public AudioManager audioManager;
    public bool jetpacking;
    public bool jSoundPlaying;
    public AudioSource windSound;
    public GameObject dryfireEmitterPrefab;
    public GameObject tinyExplosionPrefab;


    // Initialization
    void Start () {
        Debug.Log("Player spawned.");
        isGrounded = false;
        isSkiing = false;
        jetpackMeter = jetpackMeterLimit;
        player = GameObject.Find("Player(Clone)");
        player.GetComponent<Collider>().material.dynamicFriction = playerDynamicFriction;
        player.GetComponent<Collider>().material.staticFriction = playerStaticFriction;
        rb = GetComponent("Rigidbody") as Rigidbody;
        HUDCanvas = GameObject.Find("HUDCanvas");
        powerSliderObject = GameObject.Find("PowerSlider");
        powerSlider = powerSliderObject.GetComponent("Slider") as Slider;
        debugTextBox = GameObject.Find("DebugTextBox");
        debugText = debugTextBox.GetComponent<Text>();
        bulletSpawn = GameObject.Find("BulletSpawn").GetComponent<Transform>();
        bulletPrefab = Resources.Load("prefabs/Bullet") as GameObject;
        ammoTextBox = GameObject.Find("AmmoTextBox");
        ammoText = ammoTextBox.GetComponent<Text>();
        ammoCount = maxAmmoCount;
        UpdateAmmo();
        swordControllerScript = GameObject.Find("SwordBox").GetComponent<SwordController>();
        swordImagePrefab = Resources.Load("prefabs/SlashImage") as GameObject;
        isSlashing = false;
        explosionPrefab = Resources.Load("prefabs/Explosion") as GameObject;
        enemySpawnerHandlerScript = GameObject.Find("EnemySpawnerHandler").GetComponent<EnemySpawnerHandlerController>();
        audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        jetpacking = false;
        jSoundPlaying = false;
        railBox = GameObject.Find("RailBox").GetComponent<Collider>();
        railBoxScript = GameObject.Find("RailBox").GetComponent<RailBoxController>();
        windSound = this.gameObject.GetComponent<AudioSource>();
        dryfireEmitterPrefab = Resources.Load("prefabs/Dryfire") as GameObject;
        tinyExplosionPrefab = Resources.Load("prefabs/TinyExplosion") as GameObject;
    }
	

	void Update () {
        // Rotate along global y axis to match camera rotation
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, Camera.main.transform.rotation.eulerAngles.y, transform.eulerAngles.z);

        // Firing main weapon
        if (Input.GetButtonDown("Fire1"))
        {
            if (swordControllerScript.attack <= 0f)
            {
                if (ammoCount > 0f)
                {
                    FireMain();
                    ammoCount -= 1f;
                    UpdateAmmo();
                    if (ammoCount <= 0f)
                    {
                        ammoText.color = new Color(1, 0, 0, 1);
                    }
                } else
                {
                    // Play dryfire sound effect
                    audioManager.PlayOneShot("Dryfire");
                }
            }
        }

        // Using sword
        if (Input.GetButton("Fire2"))
        {
            swordControllerScript.attack = swordDuration;
            if (!isSlashing)
            {
                // Add animation
                Instantiate(
                    swordImagePrefab,
                    HUDCanvas.transform.position,
                    HUDCanvas.transform.rotation,
                    HUDCanvas.transform);

                // Play sound effect
                audioManager.Play("Slash");
            }
            isSlashing = true;
        }

        // Toggle skiing
        if (Input.GetButtonDown("Modifier"))
        {
            if (isSkiing)
            {
                // Set friction
                player.GetComponent<Collider>().material.dynamicFriction = playerDynamicFriction;
                player.GetComponent<Collider>().material.staticFriction = playerStaticFriction;
                player.GetComponent<Collider>().material.frictionCombine = PhysicMaterialCombine.Average;

                // Update tracking variable
                isSkiing = false;

                // Play sound effect
                audioManager.PlayOneShot("Land");
            } else
            {
                // Set friction
                player.GetComponent<Collider>().material.dynamicFriction = 0f;
                player.GetComponent<Collider>().material.staticFriction = 0f;
                player.GetComponent<Collider>().material.frictionCombine = PhysicMaterialCombine.Minimum;

                // Update tracking variable
                isSkiing = true;

                // Play sound effect
                audioManager.PlayOneShot("Ski");
            }
        }

        // Reloading
        if (Input.GetButtonDown("Reload"))
        {
            ammoCount = maxAmmoCount;
            UpdateAmmo();
            ammoText.color = new Color(0, 0, 0, 1);

            // Play sound effect
            audioManager.PlayOneShot("Reload");
        }

        // Resetting Game
        if (Input.GetButtonUp("Reset"))
        {
            // Reload scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Grinding
        if (Input.GetButton("Modifier2"))
        {
            Collider[] hitColliders = Physics.OverlapBox(railBox.gameObject.transform.position, railBox.gameObject.transform.localScale / 2, Quaternion.identity);
            foreach (Collider collider in hitColliders)
            {
                // If it is an enemy, kill it and add vertical velocity to the player
                if (collider.name == "Enemy(Clone)")
                {
                    // Kill enemy and add velocity
                    enemySpawnerHandlerScript.RemoveEnemyStomp(collider.gameObject, rb);
                    audioManager.PlayOneShot("SkateKick");
                }
                // If it is a rail, begin grind
                if (collider.name.StartsWith("Rail") && !(collider.name == "RailBox"))
                {
                    if (!isGrinding)
                    {
                        // CHANGE VELOCITY RELATIVE TO THE RAIL; GET RAIL TRANSFORM... CLAMP?
                        audioManager.PlayOneShot("SkateHit");
                        audioManager.Play("SkateGrind");
                        railTransform = collider.gameObject.transform;
                        transform.position = collider.ClosestPoint(transform.position + Vector3.down / 2) + Vector3.up / 2;
                        //Vector3 localizedVelocity = railTransform.InverseTransformVector(rb.velocity);
                        //localizedVelocity.x = 0f;
                        //localizedVelocity.z = 0f;
                        //rb.velocity = railTransform.TransformVector(rb.velocity);
                        isGrinding = true;
                    }
                }
            }
        }
    }

    // Function to update ammo display
    void UpdateAmmo()
    {
        ammoText.text = String.Format("{0} / {1}", ammoCount, maxAmmoCount);
    }

    // Function to do the firing of the main weapon
    void FireMain()
    {
        // Set origin for raycast
        Vector3 rayOrigin = Camera.main.ViewportToWorldPoint(new Vector3(.5f, .5f, 0));

        // Do the raycast
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Camera.main.transform.forward, out hit, weaponRange))
        {
            // debugText.text = (hit.collider.gameObject.name);
            
            if (hit.collider.name == "Enemy(Clone)")
            {
                // Damage and launch enemy that was hit
                hit.collider.gameObject.GetComponent<EnemyHealth>().DamageEnemy();
                hit.collider.gameObject.transform.LookAt(hit.point);
                hit.collider.gameObject.GetComponent<Rigidbody>().velocity = hit.collider.gameObject.transform.forward * -50f;
            }
            else
            {
                // Create explosion at hit
                var explosion = (GameObject)Instantiate(
                    explosionPrefab,
                    hit.point,
                    Quaternion.identity);
                Instantiate(
                    tinyExplosionPrefab,
                    hit.point,
                    Quaternion.identity);
            }
        }
        else
        {
            // Create Dryfire emitter
            var dryfireEmitter = (GameObject)Instantiate(
                dryfireEmitterPrefab,
                AddVector3(Camera.main.transform.position, Camera.main.transform.TransformDirection(new Vector3(0f, 0f, weaponRange))),
                Camera.main.transform.rotation);

            /*
            // Create explosion at max range
            var explosion = (GameObject)Instantiate(
                explosionPrefab,
                AddVector3(Camera.main.transform.position, Camera.main.transform.TransformDirection(new Vector3(0f, 0f, weaponRange))),
                Quaternion.identity);
                */
        }

        // Play sound effect
        audioManager.PlayOneShot("Fire");

        // SLOW BULLETS, PROJECTILES VVVVVVVVVVVVVVVVVVVVVVVVVVVV
        /*
        // Create the Bullet from the Bullet Prefab
        bulletRotation = new Vector3(Camera.main.transform.rotation.eulerAngles.x, bulletSpawn.rotation.eulerAngles.y, bulletSpawn.rotation.eulerAngles.z);
        var bullet = (GameObject)Instantiate(
            bulletPrefab,
            bulletSpawn.position,
            Quaternion.Euler(bulletRotation));

        // Add velocity to the bullet
        bullet.GetComponent<Rigidbody>().velocity = AddVector3(bullet.transform.forward * bulletSpeed, rb.velocity);

        // Destroy the bullet after 2 seconds
        Destroy(bullet, bulletTime);
        */
    }
    
    // Collisions and grounding
    private void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger && other.gameObject.name != "Invisible walls" && !other.gameObject.name.StartsWith("Rail"))
        {
            isGrounded = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.isTrigger && other.gameObject.name != "Invisible walls" && !other.gameObject.name.StartsWith("Rail"))
        {
            isGrounded = false;
        }
    }


    // FixedUpdate is updated based on time, in sync with the physics engine
    void FixedUpdate() {
        // MOVING THE PLAYER
        moveHorizontal = Input.GetAxis("Horizontal");
        moveVertical = Input.GetAxis("Vertical");

        if (isGrounded && !isSkiing)
        {
            // Ground movement
            velMove = transform.TransformVector(MultiplyVector3((new Vector3(moveHorizontal, 0f, moveVertical) * movespeed), groundedModifier));
            oldYVel = rb.velocity.y;
            if (oldYVel > 0)
            {
                oldYVel = 0;
            }
            rb.velocity = new Vector3(velMove.x, oldYVel, velMove.z);
        }
        else
        {
            // Limit velocity and multiply in the movespeed

            localVelocity = transform.InverseTransformVector(rb.velocity);
            if (Math.Abs(localVelocity.x) > movespeedLimit && Math.Abs((moveHorizontal * movespeed * midairModifier.x) + localVelocity.x) > Math.Abs(localVelocity.x) && Math.Sign((moveHorizontal * movespeed * midairModifier.x) + localVelocity.x) == Math.Sign(localVelocity.x))
            {
                moveHorizontal = 0f;
            }
            else
            {
                moveHorizontal = Mathf.Clamp(moveHorizontal * movespeed, -movespeedLimit, movespeedLimit);
            }
            
            if (Math.Abs(localVelocity.z) > movespeedLimit && Math.Abs((moveVertical * movespeed * midairModifier.z) + localVelocity.z) > Math.Abs(localVelocity.z) && Math.Sign((moveVertical * movespeed * midairModifier.z) + localVelocity.z) == Math.Sign(localVelocity.z))
            {
                moveVertical = 0f;
            }
            else
            {
                moveVertical = Mathf.Clamp(moveVertical * movespeed, -movespeedLimit, movespeedLimit);
            }

            // Add the force
            rb.AddRelativeForce(MultiplyVector3(new Vector3(moveHorizontal, 0f, moveVertical), midairModifier));
        }
        
        // Jumping and jetpack
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Joystick1Button1))
        {
            if (isGrinding)
            {
                railBoxScript.StopGrinding();
            }
            else if (isGrounded)
            {
                // Set vertical velocity
                rb.velocity = new Vector3(rb.velocity.x, 12.0f, rb.velocity.z);

                // Play jump sound effect
                //audioManager.Play("Jump");
            }
            else if (jetpackMeter > 0)
            {
                // Add the force
                rb.AddForce(new Vector3(0f, jetpackPower, 0f));

                jetpacking = true;

                // Reduce the power meter
                jetpackMeter -= 0.75f;
                powerSlider.value = 100f * (jetpackMeter / jetpackMeterLimit);
                if (jetpackMeter <= 0f)
                {
                    jetpackMeter = 0f;
                    jetpacking = false;
                }
            }
        } else
        {
            // Refill jetpack meter
            if (jetpackMeter < jetpackMeterLimit)
            {
                if (isGrounded)
                {
                    jetpackMeter += jetpackRecoveryRate * 3f;
                }
                else
                {
                    jetpackMeter += jetpackRecoveryRate;
                }
                powerSlider.value = 100f * (jetpackMeter / jetpackMeterLimit);
            }

            // Ensure jetpacking is stopped
            if (jetpacking)
            {
                jetpacking = false;
            }
        }

        if (jetpacking && !jSoundPlaying)
        {
            // Play jetpack sound effect
            audioManager.Play("Jetpack");
            jSoundPlaying = true;
        } else if (!jetpacking && jSoundPlaying)
        {
            // Stop jetpack sound effect
            audioManager.Stop("Jetpack");
            jSoundPlaying = false;
        }

        // Wind
        windSound.volume = Mathf.Clamp01((Math.Abs(rb.velocity.x) + Math.Abs(rb.velocity.y / 2) + Math.Abs(rb.velocity.z)) / 250f);
    }
}

