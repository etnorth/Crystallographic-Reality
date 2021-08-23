using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{

    public InputActionReference scaleUp;
    public InputActionReference scaleDown;

    private void Update()
    {
        bool up = scaleUp.action.ReadValue<bool>();
        bool down = scaleUp.action.ReadValue<bool>();

        if (up)
        {
            gameObject.transform.localScale *= 1.1f;
        }
        else if (down)
        {
            gameObject.transform.localScale *= 0.9f;
        }
    }
}
