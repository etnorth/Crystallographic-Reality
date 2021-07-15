using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System; // For StringSplitOptions to split e.g. 3 or 4 consecutive whitespaces (tab or one whitespace did not work) (also handles our Globalization)
using System.IO; // IO: InputOutput. Used to read our input file
//using System.Diagnostics; // Provides access to local and remote processes and enables you to start and stop local system processes

public class Crystal : MonoBehaviour
{
    private string infile; //.cif-file filename and location
    private int NumOfAtoms; // Number of UNIQUE atoms in the conventional cell (aka. number of atoms from input file, not the amount of atoms that are created by the end)
    private string fileinfo; // cif2cell's info about converted file
    private string[] atomElement; // The element of each atom
    private Vector3[] atomPos; // The position of each atom
    private Vector3 cellLength; // Length of the sides of the cell
    private Vector3 cellAngle; // Angle of the lattice vectors


    public GameObject parent; // An object to be used as the parent of the unit cell. In Unity I have selected an empty parent object "Crystal" for this.
    public GameObject atom; // Atom prefab. Selected manually in Unity
    private GameObject[] atomObjects; // Array of atom objects.
    public double scaleChange = 0.1f; // A scale for making unit cell smaller/larger.
    
    

    // Start is called before the first frame update
    void Start()
    {
        // Initialization (The thought is to have a separate game scene with buttons, sliders, etc. for setting up the crystal. This is parsed to this file and creates the appropriate crystal)
        infile = @"C:\Users\erlen\Documents\Github\Crystallographic-Reality\files\Si.cif"; // This will likely be input from user somehow. @ makes backslash parsable
        bool convertFile = true; // Specifies that the user wishes to convert their .cif to a .xyz automatically by the program

        if (convertFile)
        {
            ConvertCifToXyz(infile); // Converts .cif-file to .xyz-file using cif2cell (uses --no-reduce to get the conventional cell and not the primitive cell. This could maybe be changed by the user later)
        }
        ReadXYZ(Path.GetDirectoryName(infile) + @"\cif2cell_convert\" + Path.GetFileNameWithoutExtension(infile) + ".xyz"); // Reads converted .xyz-file (Could've had Convert_cif return file path to have this cleaner)
        CreateCell();


        /*
        NumOfAtoms = 12; // TEMPORARY
        AtomObjects = new GameObject[NumOfAtoms]; // Initializes list of atoms
        for (int i = 0; i < NumOfAtoms; i++) // Loops over each atom
        {
            atomObjects[i] = Instantiate(Atom, parent.transform, false); // Instantiate (Spawn) atom i
            atomObjects[i].transform.Translate(i/2f, 0, 0); // Move atom i by x-direction. This will correspond to atom positions after read_cif is done
            //Atoms[i].transform.localScale += scalechange;
        }
        */
    }

    // Update is called once per frame
    void Update()
    {
        
        //Scale unit cell down
        // USES OLD INPUT SYSTEM. DOES NOT WORK.
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

    // Called in Start
    void ConvertCifToXyz(string infile)
    {
        // Converts a .cif-file to .xyz using python and cif2cell in the command line

        System.Diagnostics.Process process = new System.Diagnostics.Process(); // Creates a process to run the Command Prompt
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(); // Defines a variable to insert our information in
        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden; // Hides the Command prompt window from user
        startInfo.FileName = "cmd.exe"; // Calls for the command prompt
        startInfo.Arguments = "/C cd " + Path.GetDirectoryName(infile) + 
            " & python cif2cell " + Path.GetFileName(infile) + 
            " --program=xyz --no-reduce --cartesian --outputfile=cif2cell_convert/" + 
            Path.GetFileNameWithoutExtension(infile) + ".xyz"; // Moves to appropriate directory and calls for convertion of chosen .cif-file
        process.StartInfo = startInfo; // Puts the information we have defined inside the process
        process.Start(); // Starts the process
        process.WaitForExit(); // Waits for the process to end before continuing

        if (File.Exists(Path.GetDirectoryName(infile) + @"\\cif2cell_convert\\" + Path.GetFileNameWithoutExtension(infile) + ".xyz")) { // If the converted file exists in the correct location
            Debug.Log(".cif-file converted to .xyz!"); // Prints to the Unity Console
        }
        else // Notifies that the program could not find the converted file
        {
            Debug.Log("Cannot confirm that the conversion worked."); // Prints to the Unity Console
        }

        // .xyz does not include the lattice vectors, so we fetch those manually from the .cif-file

        Debug.Log("Fetching cell parameters manually"); // Prints to the Unity Console
        string[] lines = File.ReadAllLines(infile); // Reads the .cif file as an array of lines
        int i = 0; // Counter for cellLength
        int j = 0; // Counter for cellAngle
        foreach (string line in lines)
        {
            if (line.Contains("_cell_length")) // Looks for the length of the sides of the cell
            {
                string[] words = line.Split(' '); // Splits the line into an array of words. Splits by whitespace
                if (words[1].Contains("(")) // If the file has included uncertainty, remove it.
                {
                    words[1] = words[1].Remove(words[1].Length - 3);
                }
                cellLength[i] = float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture); // Sets index i to the length (.cif uses x->y->z so 0->1->2 should be fine)
                
                i++;
            }
            else if (line.Contains("_cell_angle"))
            {
                string[] words = line.Split(' '); // Splits the line into an array of words. Splits by whitespace
                if (words[1].Contains("(")) // If the file has included uncertainty, remove it. Likely not the case for angles.
                {
                    words[1] = words[1].Remove(words[1].Length - 3);
                }
                cellAngle[j] = float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture); // Sets index i to the angle (.cif uses x->y->z so 0->1->2 should be fine)
                
                j++;
            }
            else if (i == 2 && j == 2) // When we have gotten all lengths and angles, stop the loop (saves time)
            {
                break;
            }
        }

    }

    // Called in Start
    void ReadXYZ(string cellfile)
    {
        // This function will read an .xyz-file and store the data to create the cell
        if (!File.Exists(cellfile)) // If the file does not exist
        {
            // Notify that file does not exist
            Debug.Log("Could not find the converted .xyz-file. This could get funky."); // Prints to the Unity Console
        }

        // Open the file to read from
        using (StreamReader filestream = File.OpenText(cellfile)) // Declares filestream as our "StreamRead" which reads the input file. "using" handles closing and resources, like with open in python (I believe)
        {
            NumOfAtoms = int.Parse(filestream.ReadLine()); // Reads 1st line and converts to int (1st line is number of atoms)
            fileinfo = filestream.ReadLine(); // Reads 2nd line (which is fileinfo from cif2cell)

            atomElement = new string[NumOfAtoms]; // Creates an empty string array of length equaling the number of atoms
            atomPos = new Vector3[NumOfAtoms];
            string line;

            int i = 0;
            while ((line = filestream.ReadLine()) != null) // While filestream is not empty. Think I could have done this with for-loop and without FileStream
            {
                string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // Splits the line on each tab into words
                atomElement[i] = words[0];
                atomPos[i] = new Vector3(float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(words[2], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(words[3], System.Globalization.CultureInfo.InvariantCulture)); // NullReferenceException: Object reference not set to an instance of an object

                i++;
            }
        }

        /*
        string[] atomElement = new string[NumOfAtoms]; // Creates an empty string array of length equaling the number of atoms
        Transform[] atomPos = new Transform[NumOfAtoms];


        string[] lines = File.ReadAllLines(cellfile); // Reads all lines and saves it to an array of lines
        
        int i = 0;

        foreach (string line in lines)
        {
            string[] words = line.Split('\t'); // Splits the line on each tab into words
            
            foreach (string word in words)
            {
                Debug.Log(word);
            }

            atomElement[i] = words[0];
            atomPos[i].position = new Vector3(float.Parse(words[1]), float.Parse(words[2]), float.Parse(words[3]));

            i++;
        }
        */
    }

    // Called in Start
    void CreateCell()
    {
        // Constructs the cell that is being investigated.

        /* Inverts celllength and normalizes positions. Works, but unless we shrink the atoms, they overlap and when scaling crystal, they become way too large.
        for (int i = 0; i < 3; i++)
        {
            cellLength[i] = 1.0f / cellLength[i]; // Inverts cellLength so we can normalize each atomPos with Vector3.Scale
        }

        for (int i = 0; i < NumOfAtoms; i++)
        {
            atomPos[i] = Vector3.Scale(atomPos[i], cellLength); // Normalizes atomPos so everything is fractional in the crystal
        }
        */

        //atomObjects = new GameObject[NumOfAtoms]; // Initializes the array of AtomObjects with a set length so we can fill the array iteratively
        List<GameObject> atomObjectsList = new List<GameObject>();
        Vector3 equivalentPosition; // Initializes position for equivalent atoms (atoms on edges or corners will be duplicated in other edges and corners)
        List<GameObject> equivalentAtomObjects = new List<GameObject>(); // Creates a list for the new atoms
        int equivalentCount = 0;

        
        for (int i = 0; i < NumOfAtoms; i++)
        {
            atomObjectsList.Add(Instantiate(atom, parent.transform, false)); // Creates an atom with its position relative to the parent
            atomObjectsList[i].transform.Translate(atomPos[i]); // Sets the atom position to match that of the .xyz-file (can do scaling in Update() )

            // Creates atoms not in the .xyz-file, but still useful or necessary
            for (int j = 0; j < 3; j++) // Iterates over x, y and z
            {
                if (Mathf.Abs(atomPos[i][j]) < 0.0001f) // If atom position is approx. 0
                {
                    equivalentPosition = atomPos[i];

                    equivalentPosition[j] = cellLength[j]; // Sets start of cell to end of cell to duplicate other end of cell for equivalent atom
                    equivalentAtomObjects.Add(Instantiate(atom, parent.transform, false)); // Instantiates an equivalent atom to the original
                    equivalentAtomObjects[equivalentCount].transform.Translate(equivalentPosition); // Sets the equivalent position
                    Debug.Log("1st created " + equivalentPosition + " from " + atomPos[i]);
                    equivalentCount++;
                }
            }
        }

        
        // Solves the cases where two coordinates are zero. NOTE: This creates duplicates of atoms as (x,1,0) and (x,0,1) from previous loop are flipped to (x,1,1). Will destroy duplicates.
        List<GameObject> moreEquivalentAtomObjects = new List<GameObject>();
        equivalentCount = 0;

        for (int i = 0; i < equivalentAtomObjects.Count; i++)
        {

            for (int j = 1; j < 3; j++) // Iterates over y and z
            {
                if (Mathf.Abs(equivalentAtomObjects[i].transform.localPosition[j]) < 0.0001f) // If atom position is approx. 0
                {
                    equivalentPosition = equivalentAtomObjects[i].transform.localPosition;

                    equivalentPosition[j] = cellLength[j]; // Sets start of cell to end of cell to duplicate other end of cell for equivalent atom
                    moreEquivalentAtomObjects.Add(Instantiate(atom, parent.transform, false)); // Instantiates an equivalent atom to the original
                    moreEquivalentAtomObjects[equivalentCount].transform.Translate(equivalentPosition); // Sets the equivalent position
                    Debug.Log("2nd created " + equivalentPosition + " from " + equivalentAtomObjects[i].transform.localPosition);
                    equivalentCount++;
                }
            }
        }

        // Solves the case where three coordinates are zero (origin)
        List<GameObject> evenMoreEquivalentAtomObjects = new List<GameObject>();
        equivalentCount = 0;

        for (int i = 0; i < moreEquivalentAtomObjects.Count; i++)
        {

            // Iterates over z
            if (Mathf.Abs(moreEquivalentAtomObjects[i].transform.localPosition[2]) < 0.0001f) // If atom position is approx. 0
            {
                equivalentPosition = moreEquivalentAtomObjects[i].transform.localPosition;

                equivalentPosition[2] = cellLength[2]; // Sets start of cell to end of cell to duplicate other end of cell for equivalent atom
                evenMoreEquivalentAtomObjects.Add(Instantiate(atom, parent.transform, false)); // Instantiates an equivalent atom to the original
                evenMoreEquivalentAtomObjects[equivalentCount].transform.Translate(equivalentPosition); // Sets the equivalent position
                Debug.Log("3rd created " + equivalentPosition + " from " + moreEquivalentAtomObjects[i].transform.localPosition);
                equivalentCount++;
            }

        }

        // Adds lists together
        atomObjectsList.AddRange(equivalentAtomObjects);
        atomObjectsList.AddRange(moreEquivalentAtomObjects);
        atomObjectsList.AddRange(evenMoreEquivalentAtomObjects);


        // Destroys duplicate atoms
        for (int i = 0; i < atomObjectsList.Count-1; i++)
        {
            if (atomObjectsList[i].transform.position == atomObjectsList[i+1].transform.position) // If position vectors are equal (Vector3 includes approximation)
            {
                Destroy(atomObjectsList[i + 1]);
            }
        }


        atomObjects = atomObjectsList.ToArray(); // Converts the atomObjectsList to an array (arrays are better, faster, harder, stronger)
    }

    /* Deprecated
    void Read_cif()
    {
        //Reads a cif-file given by the user. Deprecated as we will read a different file type, but leaving it in should it be useful later in development.
        double a_length;
        double b_length;
        double c_length;
        double a_angle;
        double b_angle;
        double c_angle;

        infile = @"C:\Users\erlen\Documents\Github\Crystallographic-Reality\cif files for converting\Si.cif";

        if (!File.Exists(infile))
        {
            Debug.Log("File does not exist!");
            return;
        }

        string[] lines = File.ReadAllLines(infile);

        foreach (string line in lines)
        {
            if (line.Contains("_cell_length"))
            {
                string[] words = line.Split(' ');
                Debug.Log("Word: " + words[1] + "\n");
                if (words[1].Contains("("))
                {
                    words[1] = words[1].Remove(words[1].Length-3);
                    Debug.Log("() removed: " + words[1] + "\n");
                }
                //a_length = double.Parse(words[1]);
                a_length = double.Parse("5.43053");
                Debug.Log("a: " + a_length);
            }
        }
    }
    */

    /* TestCreateCell is called in Start
    void TestCreateCell()
    {
        // This function is a test and will create a unit cell manually

        Atoms = [atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9, atom10, atom11, atom12];
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
        
    }
    */
}
