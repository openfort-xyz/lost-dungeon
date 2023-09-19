using UnityEngine;
using UnityEngine.InputSystem;

public class Shooting : MonoBehaviour
{
    public Movement movement;

    private GameControls _gameControls;
    private InputAction _touchPosition;

    private void Awake()
    {
        _gameControls = new GameControls();
        _touchPosition = _gameControls.Player.TouchPosition;
        _touchPosition.Enable();
    }

    public void OnTouchPressed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            var touchPosition = _touchPosition.ReadValue<Vector2>();
            if (Camera.main != null)
            {
                var worldTouchPosition = Camera.main.ScreenToWorldPoint(touchPosition);
                
                //TODO shoot
            }
        }
    }
}
