using CodeMonkey.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Weapon : MonoBehaviour
{
    [SerializeField] GameObject flash;
    [SerializeField] float fireRate=.5f;
    [SerializeField] Transform projectile;
    [SerializeField] Transform startPoint;
    [SerializeField] float impactForce;
    [SerializeField] int MagazenSize;
    [SerializeField] float ReloadTime;

    float lastShot = .0f;
    Animator anim;
    bool canFire;
    Camera cam;
    int currentAmmo;
    bool reload;
    
    private BoxCollider2D _joystickCollider; // Reference to the BoxCollider2D component
    private HashSet<int> uiTouches = new HashSet<int>();

    private void Awake()
    {
        anim = GetComponentInParent<Animator>();
        cam = Camera.main;
    }

    private void Start()
    {
        currentAmmo = MagazenSize;
        WeaponHUD.instance.SetAmmo(currentAmmo, MagazenSize);
        
#if UNITY_ANDROID
        if (SceneManager.GetActiveScene().name != "Level") return; // Don't need in Lobby
        
        // Get the reference to the UI joystick's RectTransform
        var joystickGameObject = GameObject.FindWithTag("Joystick");
        
        if (joystickGameObject != null)
        {
            // Get the BoxCollider2D component of the joystick
            _joystickCollider = joystickGameObject.GetComponent<BoxCollider2D>();
        }
        else
        {
            Debug.LogError("Joystick GameObject not found with the specified tag.");
        }  
#endif
    }

    void Update()
    {
        //TODO Create WeaponClass needs to be a base class where weapons need to inherit from (like PistolGun).
        if (SceneManager.GetActiveScene().name != "Level") return; // Don't shoot in Lobby!!

#if UNITY_ANDROID
        // Iterate through all active touches
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            // Check if the touch is on the UI joystick
            if (UtilsClass.IsTouchOnUI(touch.fingerId, _joystickCollider, cam))
            {
                // Add this fingerId to our set of UI touches
                uiTouches.Add(touch.fingerId);
                continue;
            }

            // Check if this touch was initially on the UI
            if (uiTouches.Contains(touch.fingerId))
            {
                // Remove fingerId if touch has ended or been canceled
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    uiTouches.Remove(touch.fingerId);
                }
                continue;
            }

            // Check for touch to shoot
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved)
            {
                Vector3 touchPosition = UtilsClass.GetTouchWorldPosition(touch.position);
                if (!reload)
                {
                    Shoot(touchPosition, startPoint.position);
                }
                else if (Time.time > ReloadTime + lastShot) // Handle reload
                {
                    reload = false;
                    currentAmmo = MagazenSize; // Consider correcting the typo to "MagazineSize"
                }
            }

            // Remove this touch from the set of UI touches, as it has ended or been canceled
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                uiTouches.Remove(touch.fingerId);
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.R)) //TODO reload button for Android?
        {
            if (!reload)
            {
                currentAmmo = 0;
                Vector3 mousePosition = UtilsClass.GetMouseWorldPosition();
                Shoot(mousePosition, startPoint.position);
                WeaponHUD.instance.SetAmmo(currentAmmo, MagazenSize);
            }
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 mousePosition = UtilsClass.GetMouseWorldPosition();
            if (!reload)
            {
                Shoot(mousePosition, startPoint.position);
            }
            else
            {
                if (Time.time > ReloadTime + lastShot)
                {
                    reload = false;
                    currentAmmo = MagazenSize;
                }
            }
        }
#endif
    }

    public void Shoot(Vector3 gunEndPointPosition, Vector3 shootPosition)
    {
        if (currentAmmo > 0)
        {
            if (Time.time > fireRate + lastShot)
            {
                anim.SetTrigger("singleshot");
                flash.SetActive(true);
                ShootProjectile(shootPosition, gunEndPointPosition);
                CinemachineShake.Instance.ShakeCamera(2f, .1f);
                Invoke(nameof(disableFlash), fireRate / 2);
                lastShot = Time.time;

                currentAmmo--;
                WeaponHUD.instance.SetAmmo(currentAmmo, MagazenSize);

            }

        }
        else
        {
            reload = true;
            var reload_ = GameObject.FindObjectOfType<Reload>();
            if (reload_)
            {
                reload_.gameObject.SetActive(true);
                reload_.StartReloading(ReloadTime);

            }
        }
    }

    void disableFlash()
    {
        flash.SetActive(false);
    }

    private void ShootProjectile(Vector3 gunEndPointPosition, Vector3 shootPosition)
    {
        Transform bulletTransform = Instantiate(projectile, gunEndPointPosition, Quaternion.identity);

        Vector3 shootDir = (shootPosition - gunEndPointPosition).normalized;
        bulletTransform.GetComponent<Bullet>().Setup(shootDir,5f);
    }

}
