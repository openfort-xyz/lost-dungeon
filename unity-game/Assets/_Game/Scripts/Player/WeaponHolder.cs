using System.Collections.Generic;
using CodeMonkey.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WeaponHolder : MonoBehaviour
{
    [SerializeField] float sortYLimit;
    [SerializeField] Transform hands;
    [SerializeField] WeaponContainer weaponContainer;
    [SerializeField] private BoxCollider2D joystickCollider; // Reference to the BoxCollider2D component
    
    private WeaponSlot _currentWeapon;
    
    Camera cam;
    SpriteRenderer _spriteRenderer;

    private bool _equipped;
    private HashSet<int> uiTouches = new HashSet<int>();


    private void OnEnable()
    {
        Shop.onItemEquipped += OnShopWeaponEquipped;
        Shop.onItemUnequipped += OnShopWeaponUnequipped;
    }

    private void OnDisable()
    {
        Shop.onItemEquipped -= OnShopWeaponEquipped;
        Shop.onItemUnequipped -= OnShopWeaponUnequipped;
    }

    private void Start()
    {
        cam = Camera.main;
        
        if (SceneManager.GetActiveScene().name != "Level") return;
        
        EquipWeapon(StaticPlayerData.EquipedWeapon);
        SetAmmoVisual();
        _equipped = true;
    }
    
    private void Update()
    {
        FacePointer();
    }

    public bool IsEquipped()
    {
        return _equipped;
    }
    
    private void OnShopWeaponEquipped(ShopItem item, int id)
    {
        // If there's a previous weapon equipped, we unequip it first.
        if (hands.childCount > 0)
        {
            OnShopWeaponUnequipped();
        }
        
        EquipWeapon(id.ToString());
        SetAmmoVisual();
        _equipped = true;
    }

    private void OnShopWeaponUnequipped()
    {
        for (int i = 0; i < hands.childCount; i++)
        {
            Destroy(hands.GetChild(i).gameObject);
        }

        HideAmmoVisual();
        StaticPlayerData.EquipedWeapon = "";
        _equipped = false;
    }

    private void FacePointer()
    {
        if (!_equipped) return;
        
#if UNITY_ANDROID
        // Iterate through all active touches
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            // Check if the touch is on the UI joystick
            if (UtilsClass.IsTouchOnUI(touch.fingerId, joystickCollider, cam))
            {
                // Add this fingerId to our set of UI touches
                uiTouches.Add(touch.fingerId);
                continue;  // Skip the rest of the loop for this iteration
            }

            // Check if this touch was initially on the UI
            if (uiTouches.Contains(touch.fingerId))
            {
                // Remove fingerId if touch has ended or been canceled
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    uiTouches.Remove(touch.fingerId);
                }
                continue; // Skip the rest of the loop for this iteration
            }

            // Update the object's rotation to face the touch direction
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved)
            {
                Vector3 touchPosition = UtilsClass.GetTouchWorldPosition(touch.position);

                if (_spriteRenderer != null)
                {
                    if (touchPosition.x > transform.position.x) _spriteRenderer.flipY = false;
                    else if (touchPosition.x < transform.position.x) _spriteRenderer.flipY = true;

                    if (touchPosition.y > transform.position.y + sortYLimit) _spriteRenderer.sortingOrder = 0;
                    else if (touchPosition.y < transform.position.y + sortYLimit) _spriteRenderer.sortingOrder = 2;
                }

                Vector2 direction = new Vector2(
                    touchPosition.x - transform.position.x,
                    touchPosition.y - transform.position.y
                );

                transform.right = direction;
            }

            // Remove this touch from the set of UI touches, as it has ended or been canceled
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                uiTouches.Remove(touch.fingerId);
            }
        }
#else
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = cam.ScreenToWorldPoint(mousePosition);

        if (_spriteRenderer != null)
        {
            // snap weapon
            if (mousePosition.x > transform.position.x) _spriteRenderer.flipY = false;
            else if (mousePosition.x < transform.position.x) _spriteRenderer.flipY = true;

            if (mousePosition.y > transform.position.y + sortYLimit) _spriteRenderer.sortingOrder = 0;
            else if (mousePosition.y < transform.position.y + sortYLimit) _spriteRenderer.sortingOrder = 2;
        }

        Vector2 direction = new Vector2(
            mousePosition.x - transform.position.x,
            mousePosition.y - transform.position.y
        );

        transform.right = direction;
#endif
    }

    private void EquipWeapon(string id)
    {
        foreach (var weapon in weaponContainer.weapons)
        {
            if (weapon.weaponID.ToString() == id)
            {
                _currentWeapon = weapon;
                
                var weaponGO = Instantiate(_currentWeapon.prefab, hands);
                _spriteRenderer = weaponGO.GetComponent<SpriteRenderer>();
                
                StaticPlayerData.EquipedWeapon = id;
                break;
            }
        }
    }
    
    private void SetAmmoVisual()
    {
        WeaponHUD.instance.SetImage(_spriteRenderer.sprite);
    }

    private void HideAmmoVisual()
    {
        WeaponHUD.instance.Hide();
    }
}
