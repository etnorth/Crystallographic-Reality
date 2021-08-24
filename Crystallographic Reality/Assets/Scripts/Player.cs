using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{

    public InputActionReference scaleUpReference;
    public InputActionReference scaleDownReference;

    //GameObject symmetryObject = Crystal.crystal.transform.Find("Symmetries"); // Needed an object reference (script needs to be sure that the object exists)
    private GameObject crystal; // Could also use public and put the crystal object on the script, or use Serialize to keep it private, but accessable in the editor. This avoids .Find()
    private GameObject symmetries;
    public InputActionReference symmetryReference;

    private void Start()
    {
        crystal = GameObject.Find("Crystal"); // Finds a GameObject named "Crystal"
    }

    private void Update()
    {
        // Player scaling
        if (scaleUpReference.action.triggered)
        {
            gameObject.transform.localScale *= 1.1f;
        }
        if (scaleDownReference.action.triggered)
        {
            gameObject.transform.localScale *= 0.9f;
        }

        // Show/hide symmetry
        if (symmetryReference.action.triggered) // If button is pressed, ON->OFF & OFF->ON
        {
            symmetries = crystal.transform.Find("Symmetries").gameObject; // Finds a child of the crystal called "Symmetries" and gets the object
            if (symmetries.activeSelf)
            {
                symmetries.SetActive(false);
            }
            else
            {
                symmetries.SetActive(true);
            }
            
        }


    }
}
