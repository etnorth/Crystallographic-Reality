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
    private float cellVolume;
    private Vector3[] cellVectors; // Unit cell vectors
    private string spaceGroup;


    public GameObject crystal; // An object to be used as the parent of the unit cell. In Unity I have selected an empty parent object "Crystal" for this. In hindsight I could have skipped this entirely and made this in the script, but this works fine.
    public GameObject atom; // Atom prefab. Selected manually in Unity. This could also have been made by using GameObject.CreatePrimitive() and then setting constraints via the script
    private GameObject[] atomObjects; // Array of atom objects.
    private Dictionary<string, Color> atomColors = new Dictionary<string, Color>() // A Dictionary to apply colors depending on what atom it is
    {
        // Using https://en.wikipedia.org/wiki/CPK_coloring#Typical_assignments
        {"H", Color.white},
        {"C", Color.black},
        {"N", Color.blue},
        {"O", Color.red},
        {"F", Color.green}, {"Cl", Color.green},
        {"Br", new Color(153/255f, 34/255f, 0, 1)}, // "Dark red"
        {"I", new Color(102/255f, 0, 187/255f, 1)}, //"Dark violet"
        // Skipped noble gases (He, Ne, Ar, Xe, Kr use "Cyan")
        {"P", new Color(255/255f, 153/255f, 0, 1)}, // "Orange"
        {"S", Color.yellow},
        {"B", new Color(255/255f, 170/255f, 119/255f, 1)}, // "Beige
        {"Li", Color.magenta}, {"Na", Color.magenta}, {"K", Color.magenta}, {"Rb", Color.magenta}, {"Cs", Color.magenta},{"Fr", Color.magenta}, // Alkali metals (Should be "Violet")
        {"Be", new Color(0, 119/255f, 0, 1)}, {"Mg", new Color(0, 119/255f, 0, 1)}, {"Ca", new Color(0, 119/255f, 0, 1)}, {"Sr", new Color(0, 119/255f, 0, 1)}, {"Ba", new Color(0, 119/255f, 0, 1)}, {"Ra", new Color(0, 119/255f, 0, 1)}, // Alkaline earth metals ("Dark green")
        {"Ti", Color.gray},
        {"Fe", new Color(221/255f, 119/255f, 0, 1)}, // "Dark orange"
        {"other", new Color(221/255f, 119/255f, 1, 1)}, // "Pink"

        // Not from "Typical Assignments
        {"Si", Color.gray},
        {"Cu", Color.yellow}, // I'd prefer "Orange"
    };
    public float scaleChange; // A scale for making unit cell smaller/larger.
    
    

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

        // Sets up Lattice Vectors in relation to Unity's coordinate system
        /*cellVectors = new Vector3[3] // Be aware these might be wrong ;) (especially c_vec, because WOOF)
        {
            new Vector3(Mathf.Cos(cellAngle[2]), 0, Mathf.Sin(cellAngle[2]))*cellLength[0], // a_vec = (cos(gamma/2), sin(gamma/2), 0) * a (Remember Unity uses (x,z,y), but we use (x,y,z)
            new Vector3(Mathf.Sin(cellAngle[2]), 0, Mathf.Cos(cellAngle[2]))*cellLength[1], // b_vec = (sin(gamma/2), cos(gamma/2), 0) * b
            new Vector3(Mathf.Sin(cellAngle[0]), 1, Mathf.Sin(cellAngle[1]))*cellLength[2] // c_vec = (sin(alpha), sin(beta), z) * c
        };*/
        cellVectors = new Vector3[3] // Got help from https://en.wikipedia.org/wiki/Fractional_coordinates (Remember Unity uses (x,z,y), but we use (x,y,z)
        {
            new Vector3(cellLength[0], cellLength[2] * Mathf.Cos(cellAngle[1] * Mathf.Deg2Rad), cellLength[1] * Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad)), // a_vec
            new Vector3(0, cellLength[2] * ( ( Mathf.Cos(cellAngle[0] * Mathf.Deg2Rad) - Mathf.Cos(cellAngle[1] * Mathf.Deg2Rad)*Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad) ) / Mathf.Sin(cellAngle[2] * Mathf.Deg2Rad)), cellLength[1] * Mathf.Sin(cellAngle[2] * Mathf.Deg2Rad)), // b_vec
            new Vector3(0, ( cellVolume / ( cellLength[0] * cellLength[1] * Mathf.Sin(cellAngle[2] * Mathf.Deg2Rad) ) ), 0) // c_vec
        };


        CreateCell();
        AddSymmetry();

        GameObject plane = CreatePlane(new Vector3(0, 0, 0), new Vector3(5.43053f, 0, 0), new Vector3(0, 5.43053f, 0), new Vector3(5.43053f, 5.43053f, 0), new Color(1, 1, 0, 0.5f), crystal);
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

        // .xyz does not include the lattice vectors, so we fetch those manually from the .cif-file. We also fetch the spacegroup

        Debug.Log("Fetching cell parameters manually"); // Prints to the Unity Console
        string[] lines = File.ReadAllLines(infile); // Reads the .cif file as an array of lines
        int i = 0; // Counter for cellLength
        int j = 0; // Counter for cellAngle
        int k = 0; // Counter for spaceGroup and cellVolume (could have combined these to a counter for all needed parameters)
        foreach (string line in lines)
        {
            if (line.Contains("_cell_length")) // Looks for the length of the sides of the cell (a, b, c)
            {
                string[] words = line.Split(' '); // Splits the line into an array of words. Splits by whitespace
                if (words[1].Contains("(")) // If the file has included uncertainty, remove it.
                {
                    words[1] = words[1].Remove(words[1].Length - 3);
                }
                cellLength[i] = float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture); // Sets index i to the length (.cif uses x->y->z so 0->1->2 should be fine)

                i++;
            }
            else if (line.Contains("_cell_angle")) // Looks for the angles (alpha, beta, gamma)
            {
                string[] words = line.Split(' '); // Splits the line into an array of words. Splits by whitespace
                if (words[1].Contains("(")) // If the file has included uncertainty, remove it. Likely not the case for angles.
                {
                    words[1] = words[1].Remove(words[1].Length - 3);
                }
                cellAngle[j] = float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture); // Sets index i to the angle (.cif uses x->y->z so 0->1->2 should be fine)

                j++;
            }
            else if (line.Contains("_symmetry_space_group_name_H-M")) // Looks for the Space Group by Hermann-Mauguin notation
            {
                string[] words = line.Split('\''); // Splits the line into an array of words. Splits by '
                spaceGroup = words[1]; // [0] = _symmetry_space_group_name_H-M , [1] = F d -3 m S, [2] = \n

                k++;
            }
            else if (line.Contains("_cell_volume"))
            {
                string[] words = line.Split(' ');
                cellVolume = float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture);

                k++;
            }
            else if (i == 2 && j == 2 && k == 2) // When we have gotten all lengths and angles, stop the loop (saves time)
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

        GameObject atomParent = new GameObject("Atoms"); // Creates an empty GameObject to contain all atoms
        atomParent.transform.parent = crystal.transform; // Sets the atomParent as a child of the Crystal
        atomParent.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the atomParent is in the Crystal's (0,0,0) and not the worlds' (0,0,0)
        List<GameObject> atomObjectsList = new List<GameObject>();
        List<string> atomElementsList = new List<string>();

        Vector3 equivalentPosition; // Initializes position for equivalent atoms (atoms on edges or corners will be duplicated in other edges and corners)
        List<GameObject> equivalentAtomObjects = new List<GameObject>(); // Creates a list for the new atoms
        List<string> equivalentAtomElements = new List<string>(); // Creates a list for the new atomElements
        int equivalentCount = 0;

        
        for (int i = 0; i < NumOfAtoms; i++)
        {
            atomObjectsList.Add(Instantiate(atom, atomParent.transform, false)); // Creates an atom with its position relative to the parent
            atomElementsList.Add(atomElement[i]); // Adds the atoms element to the list we will be using
            atomObjectsList[i].transform.localPosition = atomPos[i]; // Sets the atom position to match that of the .xyz-file (can do scaling in Update() )
            SetAtomColor(atomObjectsList[i], atomElement[i]); // Sets the atom's color based on its element
            atomObjectsList[i].name = atomElement[i] + " " +  atomPos[i]; // Names the atom so they are easier to distinguish in the Unity Editor

            // Creates atoms not in the .xyz-file, but still useful or necessary
            for (int j = 0; j < 3; j++) // Iterates over x, y and z
            {
                if (Mathf.Abs(atomPos[i][j]) < 0.0001f) // If atom position is approx. 0
                {
                    equivalentPosition = atomPos[i]; // Updates the equivalent position
                    equivalentPosition[j] = cellLength[j]; // Sets start of cell to end of cell to duplicate other end of cell for equivalent atom

                    equivalentAtomObjects.Add(Instantiate(atom, atomParent.transform, false)); // Instantiates an equivalent atom to the original
                    equivalentAtomElements.Add(atomElement[i]); // Adds the atomElement for the equivalent atom
                    equivalentAtomObjects[equivalentCount].transform.localPosition = equivalentPosition; // Sets the equivalent position
                    SetAtomColor(equivalentAtomObjects[equivalentCount], equivalentAtomElements[equivalentCount]); // Sets the atom's color based on its element
                    equivalentAtomObjects[equivalentCount].name = equivalentAtomElements[equivalentCount] + " " + equivalentPosition; // Names the atom so they are easier to distinguish in the Unity Editor

                    Debug.Log("1st created " + equivalentPosition + " from " + atomPos[i]);
                    equivalentCount++;
                }
            }
        }

        
        // Solves the cases where two coordinates are zero. NOTE: This creates duplicates of atoms as (x,1,0) and (x,0,1) from previous loop are flipped to (x,1,1). Will destroy duplicates.
        List<GameObject> moreEquivalentAtomObjects = new List<GameObject>();
        List<string> moreEquivalentAtomElements = new List<string>();
        equivalentCount = 0;

        for (int i = 0; i < equivalentAtomObjects.Count; i++)
        {

            for (int j = 1; j < 3; j++) // Iterates over y and z
            {
                if (Mathf.Abs(equivalentAtomObjects[i].transform.localPosition[j]) < 0.0001f) // If atom position is approx. 0
                {
                    equivalentPosition = equivalentAtomObjects[i].transform.localPosition;
                    equivalentPosition[j] = cellLength[j]; // Sets start of cell to end of cell to duplicate other end of cell for equivalent atom

                    moreEquivalentAtomObjects.Add(Instantiate(atom, atomParent.transform, false)); // Instantiates an equivalent atom to the original
                    moreEquivalentAtomElements.Add(equivalentAtomElements[i]); // Adds the atomElement for the equivalent atom
                    moreEquivalentAtomObjects[equivalentCount].transform.localPosition = equivalentPosition; // Sets the equivalent position
                    SetAtomColor(moreEquivalentAtomObjects[equivalentCount], moreEquivalentAtomElements[equivalentCount]); // Sets the atom's color based on its element
                    moreEquivalentAtomObjects[equivalentCount].name = moreEquivalentAtomElements[equivalentCount] + " " + equivalentPosition; // Names the atom so they are easier to distinguish in the Unity Editor

                    Debug.Log("2nd created " + equivalentPosition + " from " + equivalentAtomObjects[i].transform.localPosition);
                    equivalentCount++;
                }
            }
        }

        // Solves the case where three coordinates are zero (origin)
        List<GameObject> evenMoreEquivalentAtomObjects = new List<GameObject>();
        List<string> evenMoreEquivalentAtomElements = new List<string>();
        equivalentCount = 0;

        for (int i = 0; i < moreEquivalentAtomObjects.Count; i++)
        {

            // Iterates over z
            if (Mathf.Abs(moreEquivalentAtomObjects[i].transform.localPosition[2]) < 0.0001f) // If atom position is approx. 0
            {
                equivalentPosition = moreEquivalentAtomObjects[i].transform.localPosition;
                equivalentPosition[2] = cellLength[2]; // Sets start of cell to end of cell to duplicate other end of cell for equivalent atom

                evenMoreEquivalentAtomObjects.Add(Instantiate(atom, atomParent.transform, false)); // Instantiates an equivalent atom to the original
                evenMoreEquivalentAtomElements.Add(moreEquivalentAtomElements[i]); // Adds the atomElement for the equivalent atom
                evenMoreEquivalentAtomObjects[equivalentCount].transform.localPosition = equivalentPosition; // Sets the equivalent position
                SetAtomColor(evenMoreEquivalentAtomObjects[equivalentCount], evenMoreEquivalentAtomElements[equivalentCount]); // Sets the atom's color based on its element
                evenMoreEquivalentAtomObjects[equivalentCount].name = evenMoreEquivalentAtomElements[equivalentCount] + " " + equivalentPosition; // Names the atom so they are easier to distinguish in the Unity Editor

                Debug.Log("3rd created " + equivalentPosition + " from " + moreEquivalentAtomObjects[i].transform.localPosition);
                equivalentCount++;
            }

        }

        // Adds lists together
        atomObjectsList.AddRange(equivalentAtomObjects);
        atomObjectsList.AddRange(moreEquivalentAtomObjects);
        atomObjectsList.AddRange(evenMoreEquivalentAtomObjects);

        atomElementsList.AddRange(equivalentAtomElements);
        atomElementsList.AddRange(moreEquivalentAtomElements);
        atomElementsList.AddRange(evenMoreEquivalentAtomElements);


        // Destroys duplicate atoms
        for (int i = 0; i < atomObjectsList.Count-1; i++)
        {
            if (atomObjectsList[i].transform.position == atomObjectsList[i+1].transform.position) // If position vectors are equal (Vector3 includes approximation)
            {
                Destroy(atomObjectsList[i + 1]); // Destroys atom
                atomObjectsList.RemoveAt(i + 1); // Removes the now destroyed atom from the list
                atomElementsList.RemoveAt(i + 1); // Removes the element so we have track of it
            }
        }

        atomObjects = atomObjectsList.ToArray(); // Converts the atomObjectsList to an array (arrays are better, faster, harder, stronger)
        atomElement = atomElementsList.ToArray(); // Updates the atomElement array to match all our atoms


        // Adds grid lines for the unit cell (Done really dirty, but quicker than thinking out an algorithm)
        {
            GameObject unitCellGrid = new GameObject("Unit Cell Grid"); // Creates an empty GameObject to store gridLines in
            unitCellGrid.transform.parent = crystal.transform; // Sets unitCellGrid as a child of the Crystal
            unitCellGrid.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the unitCellGrid is in the Crystal's (0,0,0)

            GameObject gridLineX = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gridLineX.transform.parent = unitCellGrid.transform; // Sets the gridLine to a child of the Unit Cell Grid
            gridLineX.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the gridLine is in the unitCellGrid's (0,0,0)
            gridLineX.transform.localScale = new Vector3(0.05f, cellLength[0] / 2, 0.05f);

            GameObject gridLineY = Instantiate(gridLineX, unitCellGrid.transform, false); // Uses gridLineX as a template for Y and Z
            GameObject gridLineZ = Instantiate(gridLineX, unitCellGrid.transform, false);

            gridLineX.name = "X ";
            gridLineY.name = "Y ";
            gridLineZ.name = "Z ";
            gridLineX.transform.Translate(cellLength[0] / 2f, 0, 0);
            gridLineY.transform.Translate(0, 0, cellLength[1] / 2f); // y in crystallography is z in Unity
            gridLineZ.transform.Translate(0, cellLength[2] / 2f, 0); // z in crystallography is y in Unity
            gridLineX.transform.Rotate(0, 0, 90); // NOTE: Rotate() rotates along (z, x, y) not (x, y, z)
            gridLineY.transform.Rotate(90, 0, 0);
            gridLineZ.transform.Rotate(0, 0, 0); // Standard is along vertical axis

            // Rotates gridLines to match cell_angle
            gridLineX.transform.RotateAround(unitCellGrid.transform.position, new Vector3(0, cellLength[2], 0), (cellAngle[2] - 90) / 2); // gamma half
            gridLineY.transform.RotateAround(unitCellGrid.transform.position, new Vector3(0, cellLength[2], 0), (90 - cellAngle[2]) / 2); // gamma half
            gridLineZ.transform.RotateAround(unitCellGrid.transform.position, gridLineX.transform.localPosition, 90 - cellAngle[0]); // alpha. Rotate the z-axis from origin along the new x-axis by "90-alpha" degrees
            gridLineZ.transform.RotateAround(unitCellGrid.transform.position, gridLineY.transform.localPosition, cellAngle[1] - 90); // beta. Rotate the z-axis from origin along the new y-axis by "beta-90" degrees


            // Adds and places gridLines for the other unit cell edges
            GameObject gridLineXA = Instantiate(gridLineX, unitCellGrid.transform, false);
            gridLineXA.transform.localPosition = new Vector3(cellLength[0] / 2f, cellLength[2], 0);
            GameObject gridLineXB = Instantiate(gridLineX, unitCellGrid.transform, false);
            gridLineXB.transform.localPosition = new Vector3(cellLength[0] / 2f, 0, cellLength[1]);
            GameObject gridLineXC = Instantiate(gridLineX, unitCellGrid.transform, false);
            gridLineXC.transform.localPosition = new Vector3(cellLength[0] / 2f, cellLength[2], cellLength[1]);

            GameObject gridLineYA = Instantiate(gridLineY, unitCellGrid.transform, false);
            gridLineYA.transform.localPosition = new Vector3(cellLength[0], 0, cellLength[1] / 2f);
            GameObject gridLineYB = Instantiate(gridLineY, unitCellGrid.transform, false);
            gridLineYB.transform.localPosition = new Vector3(0, cellLength[2], cellLength[1] / 2f);
            GameObject gridLineYC = Instantiate(gridLineY, unitCellGrid.transform, false);
            gridLineYC.transform.localPosition = new Vector3(cellLength[0], cellLength[2], cellLength[1] / 2f);

            GameObject gridLineZA = Instantiate(gridLineZ, unitCellGrid.transform, false);
            gridLineZA.transform.localPosition = new Vector3(cellLength[0], cellLength[2] / 2f, 0);
            GameObject gridLineZB = Instantiate(gridLineZ, unitCellGrid.transform, false);
            gridLineZB.transform.localPosition = new Vector3(0, cellLength[2] / 2f, cellLength[1]);
            GameObject gridLineZC = Instantiate(gridLineZ, unitCellGrid.transform, false);
            gridLineZC.transform.localPosition = new Vector3(cellLength[0], cellLength[2] / 2f, cellLength[1]);
        }
    }

    // Called in Start
    void AddSymmetry()
    {
        // Adds symmetry to the cell.  F d -3 m S

        string[] spaceGroupSymbols = spaceGroup.Split(' '); // Splits SpaceGroup into each symbol
        GameObject symmetryParent = new GameObject("Symmetry"); // Creates an emprty GameObject to contain symmetries
        symmetryParent.transform.parent = crystal.transform; // Sets the symmetryParent as a child of the Crystal
        symmetryParent.transform.localPosition = new Vector3(0, 0, 0); // Sets the symmetryParent to (0,0,0)

        // Lattice Type (Could also compare this with a dictionary)
        switch (spaceGroupSymbols[0])
        {
            case "P":
                // Primitive

                // Atoms in corners

                break;

            case "I":
                // Body centered

                // Atoms in all corners
                // atom in center. (a/2, b/2, c/2)

                break;

            case "F":
                // Face centered

                // Atoms in all corners
                // Atoms on all faces. (a/2, b/2, 0) and all equivalent

                break;

            case "A":
                // Base centered on A faces only

                // Atoms in all corners
                // Atoms on all A faces

                break;

            case "B":
                // Base centered on B faces only

                // Atoms in all corners
                // Atoms on all B faces

                break;

            case "C":
                // Base centered on C faces only

                // Atoms in all corners
                // Atoms on all C faces

                break;

            case "R":
                // Rhombohedral

                // Place atoms in all corners
                // If there is any Rhombohedral stuff, do it (don't think there is)

                break;
            default:
                Debug.Log("Could not determine Lattice type");
                break;
        }

        // Screw axes and Glide planes

        List<GameObject> symmetryElement = new List<GameObject>();

        for (int i = 1; i < spaceGroupSymbols.Length; i++) // Iterates over the symbols, but skips the Lattice symbol
        {
            try
            {
                int.Parse(spaceGroupSymbols[i]); // If this works, the element is a screw axis. If it fails, the element is a glide plane
                Debug.Log("This is a screw axis!: " + spaceGroupSymbols[i]);


            }
            catch (FormatException)
            {
                Debug.Log("This is a glide plane!: " + spaceGroupSymbols[i]);
                switch (spaceGroupSymbols[i])
                {
                    case "a":
                        // Glide translation along half a
                        {

                        }
                        break;
                    case "b":
                        // Glide translation along half b
                        {

                        }
                        break;
                    case "c":
                        // Glide translation along half c
                        {

                        }
                        break;
                    case "n":
                        // Glide translation along half of a face diagonal
                        {

                        }
                        break;
                    case "d":
                        // Glide translation along quarter of a face diagonal
                        {

                        }
                        break;
                    case "e":
                        // Two glides with the same glide plane and translation along two (different) half lattice-vectors (e.g. a and b)
                        {

                        }
                        break;
                    case "m":
                        {
                            // Normal Mirror plane
                            // along axis corresponding to i (i=1 -> x, i=2 -> y, i=3 -> z) I THINK. Could also be others maybe, depending on higher-order axes and stuff..?

                            if (i==3) // Z
                            {
                                GameObject mirror = CreatePlane(
                                    new Vector3(0, 0, 0), // BottomLeft (always in origin)
                                    cellVectors[0], // BottomRight ( Should be vec(a) )
                                    cellVectors[1], // TopLeft ( Should be vec(b) )
                                    cellVectors[0] + cellVectors[1], // TopRight ( Should be vec(a+b) )
                                    new Color(0, 1, 0, 0.5f),
                                    symmetryParent);
                                mirror.transform.parent = symmetryParent.transform;
                                mirror.transform.localPosition = new Vector3(0, 0, 0);
                            }


                            // This is for z (testing)
                            /*{
                                symmetryElement.Add(new GameObject("Mirror")); // Creates an empty GameObject to keep the Mirror in as the mirror is two parts
                                symmetryElement[symmetryElement.Count - 1].transform.SetParent(symmetryParent.transform, false); // Count-1 gives the index of the final element aka. the element we just made
                                symmetryElement[symmetryElement.Count - 1].transform.localPosition = new Vector3(cellLength[0] / 2, 0, cellLength[1] / 2); // z-coordinate = 0

                                GameObject mirrorBottom = GameObject.CreatePrimitive(PrimitiveType.Quad); // Create the underside of the mirror (Quad is more or less a Plane)
                                mirrorBottom.transform.parent = symmetryElement[symmetryElement.Count - 1].transform; // Make "Mirror" its parent
                                mirrorBottom.transform.localPosition = new Vector3(0, 0, 0); // Set the position (it kept being in the wrong place)
                                mirrorBottom.transform.localScale = new Vector3(cellLength[0], cellLength[1], 1);
                                mirrorBottom.GetComponent<Renderer>().material.color = new Color(0, 0, 1, 0.5f); // Sets the color to a semi-transparent blue (RGBA)
                                ToTransparentMode(mirrorBottom.GetComponent<Renderer>().material); // Makes the material use the Transparent rendering mode

                                GameObject mirrorTop = Instantiate(mirrorBottom, symmetryElement[symmetryElement.Count - 1].transform, false); // Clones the underside

                                mirrorBottom.transform.Rotate(-90, 0, 0); // Rotates the mirror to lie down (underside)
                                mirrorTop.transform.Rotate(90, 0, 0); // Rotates the mirror to lie down (overside)

                                symmetryElement.Add(Instantiate(symmetryElement[symmetryElement.Count - 1], symmetryParent.transform, false)); // Clones the mirror
                                symmetryElement[symmetryElement.Count - 1].transform.localPosition = new Vector3(cellLength[0] / 2, cellLength[2], cellLength[1] / 2); // z-coordinate = c


                                //symmetryElement.Add(Instantiate(symmetryElement[symmetryElement.Count], parent.transform, false));
                                //symmetryElement[symmetryElement.Count].transform.localPosition = new Vector3(cellLength[0] / 2, cellLength[2], cellLength[1] / 2);
                                //symmetryElement[symmetryElement.Count].transform.Rotate(180, 0, 0);
                            }*/
                        }
                        break;
                    default:
                        Debug.Log("Could not recognize " + spaceGroupSymbols[i] + " as a symmetry element");
                        break;
                }
            }
        }

        // After iterating over the symmetries

    }

    // Called in CreateCell
    void SetAtomColor(GameObject atom, string element)
    {
        // Sets the color of an atom through the renderer's material by accessing a global dictionary "atomColors"
        try
        {
            atom.GetComponent<Renderer>().material.SetColor("_Color", atomColors[element]); // Changes the material color of the gameobject's renderer component
        }
        catch (KeyNotFoundException) // If atom is not in the dictonary, default to "other"
        {
            atom.GetComponent<Renderer>().material.SetColor("_Color", atomColors["other"]);
        }
    }

    // Called in AddSymmetry
    void ToTransparentMode(Material material)
    {
        // Makes a material use the Transparent Rendering Mode
        // Taken from https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/StandardShaderGUI.cs (this is how Unity changes rendering in Client)
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    // Called in AddSymmetry
    GameObject CreatePlane(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 topRight, Color color, GameObject parent = null)
    {
        // Creates a Quad GameObject for symmetry planes using four input coordinates (each corner). This should make planes work in crystals where not all angles are 90
        // Made using https://docs.unity3d.com/Manual/Example-CreatingaBillboardPlane.html

        GameObject planeBoth = new GameObject("Plane");
        GameObject planeFront = new GameObject("Front");
        planeFront.transform.parent = planeBoth.transform;

        // Creates a mesh
        {
            MeshRenderer meshRenderer = planeFront.AddComponent<MeshRenderer>(); // Adds a meshRenderer to the planeFront, and stores it for ease of access
            meshRenderer.material = new Material(Shader.Find("Standard")); // Not sure why this is needed, but without it looks purple (guessing it is the "lack-of-material"-material) (was sharedMaterial, but changed it to material)

            MeshFilter meshFilter = planeFront.AddComponent<MeshFilter>(); // Adds a meshFilter

            Mesh mesh = new Mesh(); // Creates a mesh

            Vector3[] vertices = new Vector3[4] // Creates an array of vertices that the shape uses
            {
            bottomLeft, bottomRight, topLeft, topRight
            };
            mesh.vertices = vertices; // Gives the mesh our made vertices

            int[] tris = new int[6] // triangeles(?) for the mesh
            {
            // lower left triangle
            0,2,1,
            // upper right triangle
            2,3,1
            };
            mesh.triangles = tris;

            Vector3[] normals = new Vector3[4] // Normals(?) for the mesh
            {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[4]
            {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
            };
            mesh.uv = uv;

            meshFilter.mesh = mesh; // Applies our newly made mesh to the meshFilter
        }

        planeFront.GetComponent<Renderer>().material.color = color; // Sets the color of the material
        ToTransparentMode(planeFront.GetComponent<Renderer>().material); // Makes the material use the Transparent rendering mode

        GameObject planeBack = Instantiate(planeFront, planeBoth.transform, false); // Adds the backside of the plane (Unity only renders one side of the mesh we made)
        planeBack.name = "Back";

        // Pivot of this GameObject is in bottomLeft and not the center of the item, so we need to adjust for offsets
        //planeFront.transform.localPosition = -topRight / 2; // Adjusts for offset
        //planeBack.transform.localPosition = -topRight / 2; // Adjusts for offset
        planeBack.transform.RotateAround(planeBack.GetComponent<Renderer>().bounds.center, bottomRight, 180); // Rotates around the center of the plane (renderer.bounds.center gives "center of bounding box")

        planeBoth.transform.parent = parent.transform;
        planeBoth.transform.localPosition = new Vector3(0, 0, 0);


        return planeBoth;
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
