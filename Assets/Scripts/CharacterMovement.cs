﻿using UnityEngine;
using System.Collections;

public class CharacterMovement : MonoBehaviour
{
    // Movement
    public float maxSpeed = 2.0f;
    public bool facingRight = true;
    public float moveSpeedX;
    public float moveSpeedY;

    // Character State
    public int bodyState = 0;
    private const int playerStandingConst = 0;
    private const int playerOnKneesConst = 1;
    private const int playerAsBallConst = 2;
    private const float playerStandingColliderheigthConst = .43f;
    private const float playerOnKneesColliderheigthConst = .33f;
    private const float playerAsBallColliderheigthConst = .0f;

    // Jumps
    public bool doubleJump = false;
    public int maxJumpCount = 5;
    public int jumpCount = 0;
    public float jumpSpeed = 15;

    // Physics
    private Rigidbody rigidbody;
    private CapsuleCollider collider;
    public Transform groundCheck;
    public float groundRadius = 0.0001f;
    public LayerMask whatIsGround;
    public bool grounded = false;
    public bool goingUp = false;


    // Turning
    public float turnWaitTime = .2f;
    public float turnTime = 0;
    public bool turning = false;

    // Shotting
    public float shotSpeed = 600.0f;
    public float shotWaitTime = .5f;
    public float lastShotTime = 0;
    public bool shootingOnAir = false;
    private GameObject currentShotSpawn;
    public Rigidbody shotPrefab;
    Rigidbody clone;
    public int aimingDirection = 0;
    // Estados en que puede estar el cañon
    private const int aimingIdleConst = 0;
    private const int aimingUpConst = 1;
    private const int aimingUpFrontConst = 2;
    private const int aimingFrontConst = 3;
    private const int aimingDownFrontConst = 4;
    private const int aimingDownConst = 5;
    // Weapon spawns:
    GameObject canonIdleSpawn, canonAimingFrontSpawn, canonAimingUpFrontSpawn,
        canonAimingDownFrontSpawn, canonAimingUp;
    GameObject canonIdleSpawnAir, canonAimingFrontSpawnAir, canonAimingUpFrontSpawnAir,
       canonAimingDownFrontSpawnAir, canonAimingUpAir, canonAimingDownSpawnAir;

    //Sounds 
    AudioClip baseShotSound;

    private Animator anim;

    void Awake()
    {
        collider = GetComponent<CapsuleCollider>();
        rigidbody = GetComponent<Rigidbody>();
        // Para que el rigidbody no deje de recibir eventos mientras esta inmovil
        rigidbody.sleepThreshold = 0.0f;

        // Para comprobar cuando se toca el suelo
        groundCheck = GameObject.Find("/Character/GroundCheck").transform;

        // Objeto que manipula las animaciones
        anim = GameObject.Find("/Character/CharacterSprite").GetComponent<Animator>();

        // Inicializando posiciones de cañon
        canonIdleSpawn = GameObject.Find("/Character/CanonAiming/CanonIdleSpawn");
        canonAimingFrontSpawn = GameObject.Find("/Character/CanonAiming/CanonAimingFrontSpawn");
        canonAimingUpFrontSpawn = GameObject.Find("/Character/CanonAiming/CanonAimingUpFrontSpawn");
        canonAimingDownFrontSpawn = GameObject.Find("/Character/CanonAiming/CanonAimingDownFrontSpawn");
        canonAimingUp = GameObject.Find("/Character/CanonAiming/CanonAimingUp");
        canonAimingFrontSpawnAir = GameObject.Find("/Character/CanonAimingAir/CanonAimingFrontSpawn");
        canonAimingUpFrontSpawnAir = GameObject.Find("/Character/CanonAimingAir/CanonAimingUpFrontSpawn");
        canonAimingDownFrontSpawnAir = GameObject.Find("/Character/CanonAimingAir/CanonAimingDownFrontSpawn");
        canonAimingUpAir = GameObject.Find("/Character/CanonAimingAir/CanonAimingUp");
        canonAimingDownSpawnAir = GameObject.Find("/Character/CanonAimingAir/CanonAimingDownSpawn");

        // Cargando sonidos
        baseShotSound = Resources.Load("Sounds/BaseShot", typeof(AudioClip)) as AudioClip;
    }

    void FixedUpdate()
    {
        anim.SetFloat("MoveSpeedX", moveSpeedX);
        anim.SetFloat("MoveSpeedY", moveSpeedY);
        anim.SetBool("GoingUp", goingUp);
        anim.SetInteger("BodyState", bodyState);

        if (bodyState == playerStandingConst && collider.height != playerStandingColliderheigthConst)
            collider.height = playerStandingColliderheigthConst;
        else if (bodyState == playerOnKneesConst && collider.height != playerOnKneesColliderheigthConst)
            collider.height = playerOnKneesColliderheigthConst;
        else if (bodyState == playerAsBallConst && collider.height != playerAsBallColliderheigthConst)
            collider.height = playerAsBallColliderheigthConst;

        HandleAimingDirection();

        // Se chekea si se toca el suelo y se actualizan los estados de la animacion
        grounded =
            Physics2D.OverlapCircle(groundCheck.position, groundRadius, whatIsGround);
        anim.SetBool("Grounded", grounded);
        // Si se esta sobre el suelo se inicializan los estados de salto y disparos en el aire
        if (grounded)
        {
            doubleJump = false;
            jumpCount = 0;
            shootingOnAir = false;
        }
        anim.SetBool("ShootingOnAir", shootingOnAir);

        // En funcion del movimiento se cambia la orientacion del personaje
        if (moveSpeedX > 0.0f && !facingRight) { Flip(); }
        else if (moveSpeedX < 0.0f && facingRight) { Flip(); }

        // Mientras ocurre el giro del personaje no ocurre movimiento
        if (!turning)
        {
            anim.SetBool("Turning", turning);
            rigidbody.velocity =
            new Vector2(moveSpeedX * maxSpeed, rigidbody.velocity.y);
        }
    }


    void HandleAimingDirection()
    {
        if (moveSpeedX == 0 && moveSpeedY == 0)
        {
            aimingDirection = aimingIdleConst;//idle
            currentShotSpawn = canonIdleSpawn;
        }
        else if (moveSpeedX == 0 && moveSpeedY > 0)
        {
            aimingDirection = aimingUpConst;//up
            currentShotSpawn = shootingOnAir ? canonAimingUpAir : canonAimingUp;
        }
        else if (Mathf.Abs(moveSpeedX) > 0 && moveSpeedY > 0)
        {
            aimingDirection = aimingUpFrontConst;//up-right 45 grados
            currentShotSpawn = shootingOnAir ? canonAimingUpFrontSpawnAir : canonAimingUpFrontSpawn;
        }
        else if (Mathf.Abs(moveSpeedX) > 0 && moveSpeedY == 0)
        {
            aimingDirection = aimingFrontConst;//front
            currentShotSpawn = shootingOnAir ? canonAimingFrontSpawnAir : canonAimingFrontSpawn;
        }
        else if (Mathf.Abs(moveSpeedX) > 0 && moveSpeedY < 0)
        {
            aimingDirection = aimingDownFrontConst;//down-right 315 grados 
            currentShotSpawn = shootingOnAir ? canonAimingDownFrontSpawnAir : canonAimingDownFrontSpawn;
        }
        else if (moveSpeedX == 0 && moveSpeedY < 0)
        {
            aimingDirection = aimingDownConst;//down 270 grados
            currentShotSpawn = canonAimingDownSpawnAir;
        }

        // Apuntando arriba-delante
        if (Input.GetKey(KeyCode.R))
        {
            aimingDirection = aimingUpFrontConst;
            currentShotSpawn = shootingOnAir ? canonAimingUpFrontSpawnAir : canonAimingUpFrontSpawn;
        }
        // Apuntando debajo-delante
        if (Input.GetKey(KeyCode.F))
        {
            aimingDirection = aimingDownFrontConst;
            currentShotSpawn = shootingOnAir ? canonAimingDownFrontSpawnAir : canonAimingDownFrontSpawn;
        }

        anim.SetInteger("AimingDirection", aimingDirection);
        // Usado en arbol de mezclas
        anim.SetFloat("AimingDirectionF", aimingDirection);

        // Si cambia el objetivo de disparo se deshabilita el disparo al frente
        if (aimingDirection != aimingDownFrontConst)
            anim.SetBool("ShootingFront", false);
    }

    void Update()
    {
        // Se captura la direccion y velocidad del movimiento horizontal
        //moveSpeedX = Input.GetAxis("Horizontal");
        moveSpeedX = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;
        moveSpeedY = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;

        HandleBodyState();

        HandleJump();

        // Se actualiza el estado del giro
        if (turning)
        {
            turnTime += Time.deltaTime;
            if (turnTime >= turnWaitTime)
            {
                turning = false;
                turnTime = 0;
            }
        }

        // Se controla la frecuencia de disparos
        if (lastShotTime >= shotWaitTime)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                lastShotTime = 0;
                Attack();
            }
        }
        else
        {
            lastShotTime += Time.deltaTime;
        }


    }

    void HandleBodyState()
    {
        if (Input.GetKeyDown(KeyCode.S) && moveSpeedX == 0) { bodyState = bodyState < 2 ? bodyState + 1 : 2; }
        else if (Input.GetKeyDown(KeyCode.W)) { bodyState = bodyState > 0 ? bodyState - 1 : 0; }

        if (moveSpeedX != 0 && bodyState == playerOnKneesConst) bodyState = playerStandingConst;

    }

    void HandleJump()
    {
        // Se captura la direccion del salto para reaccionar en caida libre y animaciones
        if (rigidbody.velocity.y >= 0.5f) goingUp = true;
        else goingUp = false;

        if (Input.GetButtonDown("Jump"))
        {
            // Si el personaje esta en el suelo o si kedan saltos permisibles aun y se presiona saltar
            if ((grounded || jumpCount < maxJumpCount))
            {
                // Se annade una fuerza vertical (Salto)
                rigidbody.AddForce(new Vector2(0.0f, jumpSpeed));
                // Se actualiza la cantidad de saltos
                if (jumpCount < maxJumpCount && !grounded) jumpCount++;
                // Al saltar se deshabilita el disparo hacia el frente
                anim.SetBool("ShootingFront", false);

                // Al saltar se reinicia el estado del jugador en el suelo
                bodyState = playerStandingConst;
            }
        }
    }

    void Flip()
    {
        // Gira el pesonaje 180 grados y actualiza el estado de las animaciones
        turning = true;
        transform.Rotate(Vector3.up, 180.0f, Space.World);
        // Se ignora el giro para el CharacterSprite porque la animacion es asimetrica
        anim.transform.Rotate(Vector3.up, 180.0f, Space.World);

        facingRight = !facingRight;
        anim.SetBool("FacingRight", facingRight);
        anim.SetBool("Turning", turning);
        anim.SetBool("ShootingFront", false);
    }
    void Attack()
    {
        // En el suelo no se puede disparar hacia abajo
        if (aimingDirection == aimingDownConst && grounded) return;

        // Sonido del disparo
        Camera.main.GetComponent<AudioSource>().PlayOneShot(baseShotSound, .5f);

        clone =
            Instantiate(shotPrefab, currentShotSpawn.transform.position, currentShotSpawn.transform.rotation) as Rigidbody;
        // Se otorga inicialmente la velocidad actual del jugador a la bala
        clone.velocity = rigidbody.velocity;
        // Luego se annade la velocidad del disparo
        clone.AddForce(currentShotSpawn.transform.right * shotSpeed);

        // Si al atacar se apunta al frente se habilita la bandera
        if (aimingDirection == aimingFrontConst)
            anim.SetBool("ShootingFront", true);

        if (!grounded) { shootingOnAir = true; }
    }

}
