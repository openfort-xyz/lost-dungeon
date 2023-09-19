using UnityEngine;
using UnityEngine.Events;

public class ButtonSimulator : MonoBehaviour
{
    public UnityEvent onPressed;

    private bool isPressed;

    // For mobile interaction
    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                isPressed = true;
                // Apply visual feedback for button press.
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                if (isPressed)
                {
                    // Trigger button action.
                    onPressed?.Invoke();
                }
                isPressed = false;
                // Apply visual feedback for button release.
            }
        }
    }
}