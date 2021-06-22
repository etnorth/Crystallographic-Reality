using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO; // IO: InputOutput. Used to read our input file

public class Crystal : MonoBehaviour
{
    public GameObject parent; // An object to be used as the parent of the unit cell
    public GameObject Atom; // Atom prefab
    public int NumOfAtoms; // Total number of atoms
    public GameObject[] Atoms; // Array of atom objects. Initialized in Start
    public float scaleChange = 0.1f; // A scale for making unit cell smaller/larger.

    // Start is called before the first frame update
    void Start()
    {
        // Initialization
        Read_cif();

        Atoms = new GameObject[NumOfAtoms]; // Initializes list of atoms
        for (int i = 0; i < NumOfAtoms; i++) // Loops over each atom
        {
            Atoms[i] = Instantiate(Atom, parent.transform, false); // Instantiate (Spawn) atom i
            Atoms[i].transform.Translate(i, 0, 0); // Move atom i by x-direction. This will correspond to atom positions after read_cif is done
            //Atoms[i].transform.localScale += scalechange;
        }


        TestCreateCell();
        //Read();
    }

    // Update is called once per frame
    void Update()
    {
        
        //Scale unit cell down
        /*
        if (trigger)
        {
            Debug.Log("Left Controller Trigger is held down");
            parent.transform.localScale -= new Vector3(scaleChange, scaleChange, scaleChange);
        }

        //Scale unit cell up
        if (Input.GetKey("Axis1D.SecondaryHandTrigger"))
        {
            Debug.Log("Right Controller Trigger is held down");
            parent.transform.localScale += new Vector3(scaleChange, scaleChange, scaleChange);
        }
        */
    }

    // Read_cif is called in Start
    void Read_cif()
    {
        //Reads a cif-file given by the user

    }

    // TestCreateCell is called in Start
    void TestCreateCell()
    {
        // This function is a test and will create a unit cell manually

        /*Atoms = [atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9, atom10, atom11, atom12];
        var  atom1 = Instantiate(Atom, parent.transform, false); // Creates an atom at the parent's position
        var  atom2 = Instantiate(Atom, parent.transform, false);  atom2.transform.Translate(1f    * scale, 0f    * scale, 0f    * scale); // Creates an atom and moves it from the parent's position
        var  atom3 = Instantiate(Atom, parent.transform, false);  atom3.transform.Translate(0f    * scale, 1f    * scale, 0f    * scale); // Creates an atom and moves it from the parent's position
        var  atom4 = Instantiate(Atom, parent.transform, false);  atom4.transform.Translate(0f    * scale, 0f    * scale, 1f    * scale); // Creates an atom and moves it from the parent's position
        var  atom5 = Instantiate(Atom, parent.transform, false);  atom5.transform.Translate(0f    * scale, 1f    * scale, 1f    * scale); // Creates an atom and moves it from the parent's position
        var  atom6 = Instantiate(Atom, parent.transform, false);  atom6.transform.Translate(1f    * scale, 0f    * scale, 1f    * scale); // Creates an atom and moves it from the parent's position
        var  atom7 = Instantiate(Atom, parent.transform, false);  atom7.transform.Translate(1f    * scale, 1f    * scale, 0f    * scale); // Creates an atom and moves it from the parent's position
        var  atom8 = Instantiate(Atom, parent.transform, false);  atom8.transform.Translate(1f    * scale, 1f    * scale, 1f    * scale); // Creates an atom and moves it from the parent's position
        var  atom9 = Instantiate(Atom, parent.transform, false);  atom9.transform.Translate(0.25f * scale, 0.25f * scale, 0.25f * scale); // Creates an atom and moves it from the parent's position
        var atom10 = Instantiate(Atom, parent.transform, false); atom10.transform.Translate(0.25f * scale, 0.75f * scale, 0.75f * scale); // Creates an atom and moves it from the parent's position
        var atom11 = Instantiate(Atom, parent.transform, false); atom11.transform.Translate(0.75f * scale, 0.25f * scale, 0.75f * scale); // Creates an atom and moves it from the parent's position
        var atom12 = Instantiate(Atom, parent.transform, false); atom12.transform.Translate(0.75f * scale, 0.75f * scale, 0.25f * scale); // Creates an atom and moves it from the parent's position
        */
    }

    // Read is called in Start
    void Read()
    {
        // This function will read an input-file and store any useful data
        string path = @"C:\Users\erlen\Documents\Github\Crystallographic-Reality\cif files for converting\Si.cif"; // Declaration of out input file
        if (!File.Exists(path)) // If the file does not exist
        {
            // Notify that file does not exist
            Debug.Log("File does not exist."); // Prints to the Unity Console
        }

        // Open the file to read from
        using (StreamReader sr = File.OpenText(path)) // Declares sr as our "StreamRead" which reads the input file
        {
            string s;
            while ((s = sr.ReadLine()) != null) // While sr is not empty
            {
                Debug.Log(s); // Prints to the Unity Console
            }
        }
    }
}
