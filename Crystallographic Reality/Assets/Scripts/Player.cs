using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{

    public InputActionReference scaleUpReference;
    public InputActionReference scaleDownReference;

    private void Update()
    {
        //bool up = scaleUpReference.action.ReadValue<bool>();
        //bool down = scaleDownReference.action.ReadValue<bool>();

        if (scaleUpReference.action.triggered)
        {
            gameObject.transform.localScale *= 1.1f;
        }
        if (scaleDownReference.action.triggered)
        {
            gameObject.transform.localScale *= 0.9f;
        }
    }

    private void Scale(InputAction.CallbackContext context)
    {

    }
}
