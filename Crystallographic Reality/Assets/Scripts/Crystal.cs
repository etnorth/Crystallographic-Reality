using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO; // IO: InputOutput. Used to read our input file

public class Crystal : MonoBehaviour
{
    public GameObject parent;
    public GameObject Atom;

    // Start is called before the first frame update
    void Start()
    {
        TestCreateCell();
        //Read();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // TestCreateCell is called in Start
    void TestCreateCell()
    {
        // This function is a test and will create a unit cell
        var atom1 = Instantiate(Atom, parent.transform, false); // Creates an atom at the parent's position
        var atom2 = Instantiate(Atom, parent.transform, false); atom2.transform.Translate(1f, 0f, 0f); // Creates an atom and moves it from the parent's position
        var atom3 = Instantiate(Atom, parent.transform, false); atom3.transform.Translate(0f, 1f, 0f); // Creates an atom and moves it from the parent's position
        var atom4 = Instantiate(Atom, parent.transform, false); atom4.transform.Translate(0f, 0f, 1f); // Creates an atom and moves it from the parent's position
        var atom5 = Instantiate(Atom, parent.transform, false); atom5.transform.Translate(0f, 1f, 1f); // Creates an atom and moves it from the parent's position
        var atom6 = Instantiate(Atom, parent.transform, false); atom6.transform.Translate(1f, 0f, 1f); // Creates an atom and moves it from the parent's position
        var atom7 = Instantiate(Atom, parent.transform, false); atom7.transform.Translate(1f, 1f, 0f); // Creates an atom and moves it from the parent's position
        var atom8 = Instantiate(Atom, parent.transform, false); atom8.transform.Translate(1f, 1f, 1f); // Creates an atom and moves it from the parent's position
        var atom9 = Instantiate(Atom, parent.transform, false); atom9.transform.Translate(0.25f, 0.25f, 0.25f); // Creates an atom and moves it from the parent's position
        var atom10 = Instantiate(Atom, parent.transform, false); atom10.transform.Translate(0.25f, 0.75f, 0.75f); // Creates an atom and moves it from the parent's position
        var atom11 = Instantiate(Atom, parent.transform, false); atom11.transform.Translate(0.75f, 0.25f, 0.75f); // Creates an atom and moves it from the parent's position
        var atom12 = Instantiate(Atom, parent.transform, false); atom12.transform.Translate(0.75f, 0.75f, 0.25f); // Creates an atom and moves it from the parent's position

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
