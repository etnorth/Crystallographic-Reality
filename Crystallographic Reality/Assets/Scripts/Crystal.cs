using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System; // For StringSplitOptions to split e.g. 3 or 4 consecutive whitespaces (tab or one whitespace did not work) (also handles our Globalization)
using System.IO; // IO: InputOutput. Used to read our input file. OLD
using System.Linq; // Used for finding Min value in array, and for the array.Where() function
using UnityEngine.UI; // Used for images
//using System.Diagnostics; // Provides access to local and remote processes and enables you to start and stop local system processes

public class Crystal : MonoBehaviour
{
    public GameObject crystal; // An object to be used as the parent of the unit cell. In Unity I have selected an empty parent object "Crystal" for this. In hindsight I could have skipped this entirely and made this in the script, but this works fine.
    public GameObject atom; // Atom prefab. Selected manually in Unity. This could also have been made by using GameObject.CreatePrimitive() and then setting constraints via the script
    public List<Sprite> rotationSymbols; // List containing each rotation symbol
    public float eps = 0.0001f; // tolerance for comparing float numbers (abs(x)<eps => x=0). Meant to avoid rounding errors. (typically called eps or tol from MAT-IN1105)

    private Vector3 cellLength; // Length of the sides of the cell (a, b, c)
    private Vector3 cellAngle; // Angle of the lattice vectors  (alpha, beta, gamma)
    private string[] atomElement; // The element of each atom ( which can now be accessed through gameObject.name.Split(' ')[0] )
    private Vector3[] atomPos; // The position of each atom. (x,z,y), not (x,y,z)
    private float[][,] symmetryMatrices; // An array of Vector3-arrays (1st array to count operations, 2nd array is a 3x3 rotation + 3x1 translation matrix) Matrix given by normal (x,y,z,) and will be converted upon use
    private string[] symmetryMatricesType; // The type of operation for each symmetry matrix
    private float cellVolume; // Volume of the cell. Calculated in Start()
    private Vector3[] bravaisVectors; // Unit cell vectors NOTE: uses Unity (x,z,y)
    private float[,] bravaisMatrix; // We create a matrix for the bravais as well, so we can transform coordinates correctly NOTE: uses normal (x,y,z)
    private float[,] reciprocalMatrix; // Used to convert atom positions into coordinates suitable for finding out if it is in a(n) corner/edge/face

    private GameObject[] atomObjects; // Array of atom objects
    private GameObject[] symmetryElements; // An array of symmetry elements
    private Dictionary<string, Color> atomColors = new Dictionary<string, Color>()
    {
        // A Dictionary to apply colors depending on what atom it is
        // Using http://jmol.sourceforge.net/jscolors/
        {"H", Color.white},
        {"He", new Color(217/255f, 1, 1, 1) },
        {"Li", new Color(204/255f, 128/255f, 1, 1) },
        {"Be", new Color(194/255f, 1, 0, 1) },
        {"B", new Color(1, 181/255f, 181/255f, 1) },
        {"C", new Color(144/255f, 144/255f, 144/255f, 1) },
        {"N", new Color(48/255f, 80/255f, 248/255f, 1) },
        {"O", new Color(1, 13/255f, 13/255f, 1) },
        {"F", new Color(144/255f, 224/255f, 80/255f, 1) },
        {"Ne", new Color(179/255f, 227/255f, 245/255f, 1) },
        {"Na", new Color(171/255f, 92/255f, 242/255f, 1) },
        {"Mg", new Color(138/255f, 1, 0, 1) },
        {"Al", new Color(191/255f, 166/255f, 166/255f, 1) },
        {"Si", new Color(240/255f, 200/255f, 160/255f, 1) },
        {"P", new Color(1, 128/255f, 0, 1) },
        {"S", new Color(1, 1, 48/255f, 1) },
        {"Cl", new Color(31/255f, 240/255f, 31/255f, 1) },
        {"Ar", new Color(128/255f, 209/255f, 227/255f, 1) },
        {"K", new Color(143/255f, 64/255f, 212/255f, 1) },
        {"Ca", new Color(61/255f, 1, 0, 1) },
        {"Sc", new Color(230/255f, 230/255f, 230/255f, 1) },
        {"Ti", new Color(191/255f, 194/255f, 199/255f, 1) },
        {"V", new Color(166/255f, 166/255f, 171/255f, 1) },
        {"Cr", new Color(138/255f, 153/255f, 199/255f, 1) },
        {"Mn", new Color(156/255f, 122/255f, 199/255f, 1) },
        {"Fe", new Color(224/255f, 102/255f, 51/255f, 1) },
        {"Co", new Color(240/255f, 144/255f, 160/255f, 1) },
        {"Ni", new Color(80/255f, 208/255f, 80/255f, 1) },
        {"Cu", new Color(200/255f, 128/255f, 51/255f, 1) },
        {"Zn", new Color(125/255f, 128/255f, 176/255f, 1) },
        {"Ga", new Color(194/255f, 143/255f, 143/255f, 1) },
        {"Ge", new Color(102/255f, 143/255f, 143/255f, 1) },
        {"As", new Color(189/255f, 128/255f, 227/255f, 1) },
        {"Se", new Color(1, 161/255f, 0, 1) },
        {"Br", new Color(166/255f, 41/255f, 41/255f, 1) },
        {"Kr", new Color(92/255f, 184/255f, 209/255f, 1) },
        {"Rb", new Color(112/255f, 46/255f, 176/255f, 1) },
        {"Sr", new Color(0, 1, 0, 1) },
        {"Y", new Color(148/255f, 1, 1, 1) },
        {"Zr", new Color(148/255f, 224/255f, 224/255f, 1) },
        {"Nb", new Color(115/255f, 194/255f, 201/255f, 1) },
        {"Mo", new Color(84/255f, 181/255f, 181/255f, 1) },
        {"Tc", new Color(59/255f, 158/255f, 158/255f, 1) },
        {"Ru", new Color(36/255f, 143/255f, 143/255f, 1) },
        {"Rh", new Color(10/255f, 125/255f, 140/255f, 1) },
        {"Pd", new Color(0, 105, 133/255f, 1) },
        {"Ag", new Color(192/255f, 192/255f, 192/255f, 1) },
        {"Cd", new Color(1, 217/255f, 143/255f, 1) },
        {"In", new Color(166/255f, 117/255f, 115/255f, 1) },
        {"Sn", new Color(102/255f, 128/255f, 128/255f, 1) },
        {"Sb", new Color(158/255f, 99/255f, 181/255f, 1) },
        {"Te", new Color(212/255f, 122/255f, 0, 1) },
        {"I", new Color(148/255f, 0, 148/255f, 1) },
        {"Xe", new Color(66/255f, 158/255f, 176/255f, 1) },
        {"Cs", new Color(87/255f, 23/255f, 143/255f, 1) },
        {"Ba", new Color(0, 201/255f, 0, 1) },
        {"La", new Color(112/255f, 212/255f, 1, 1) },
        {"Ce", new Color(1, 1, 199/255f, 1) },
        {"Pr", new Color(217/255f, 1, 199/255f, 1) },
        {"Nd", new Color(199/255f, 1, 199/255f, 1) },
        {"Pm", new Color(163/255f, 255, 199/255f, 1) },
        {"Sm", new Color(143/255f, 1, 199/255f, 1) },
        {"Eu", new Color(97/255f, 1, 199/255f, 1) },
        {"Gd", new Color(69/255f, 1, 199/255f, 1) },
        {"Tb", new Color(48/255f, 1, 199/255f, 1) },
        {"Dy", new Color(31/255f, 1, 199/255f, 1) },
        {"Ho", new Color(0, 1, 156/255f, 1) },
        {"Er", new Color(0, 230/255f, 117/255f, 1) },
        {"Tm", new Color(0/255f, 212/255f, 82/255f, 1) },
        {"Yb", new Color(0/255f, 191/255f, 56/255f, 1) },
        {"Lu", new Color(0/255f, 171/255f, 36/255f, 1) },
        {"Hf", new Color(77/255f, 194/255f, 1, 1) },
        {"Ta", new Color(77/255f, 166/255f, 1, 1) },
        {"W", new Color(33/255f, 148/255f, 214/255f, 1) },
        {"Re", new Color(38/255f, 125/255f, 171/255f, 1) },
        {"Os", new Color(38/255f, 102/255f, 150/255f, 1) },
        {"Ir", new Color(23/255f, 84/255f, 135/255f, 1) },
        {"Pt", new Color(208/255f, 208/255f, 225/255f, 1) },
        {"Au", new Color(1, 209/255f, 35/255f, 1) },
        {"Hg", new Color(184/255f, 184/255f, 208/255f, 1) },
        {"Tl", new Color(166/255f, 84/255f, 77/255f, 1) },
        {"Pb", new Color(87/255f, 89/255f, 97/255f, 1) },
        {"Bi", new Color(158/255f, 79/255f, 181/255f, 1) },
        {"Po", new Color(171/255f, 92/255f, 0, 1) },
        {"At", new Color(117/255f, 79/255f, 69/255f, 1) },
        {"Rn", new Color(66/255f, 130/255f, 150/255f, 1) },
        {"Fr", new Color(66/255f, 0, 102/255f, 1) },
        {"Ra", new Color(0, 125/255f, 0, 1) },
        {"Ac", new Color(112/255f, 171/255f, 250/255f, 1) },
        {"Th", new Color(0, 186/255f, 1, 1) },
        {"Pa", new Color(0, 161/255f, 1, 1) },
        {"U", new Color(0, 154/255f, 1, 1) },
        {"Np", new Color(0, 128/255f, 1, 1) },
        {"Pu", new Color(0, 107/255f, 1, 1) },
        {"Am", new Color(84/255f, 92/255f, 242/255f, 1) },
        {"Cm", new Color(120/255f, 92/255f, 227/255f, 1) },
        {"Bk", new Color(138/255f, 79/255f, 227/255f, 1) },
        {"Cf", new Color(161/255f, 54/255f, 212/255f, 1) },
        {"Es", new Color(179/255f, 31/255f, 212/255f, 1) },
        {"Fm", new Color(179/255f, 31/255f, 186/255f, 1) },
        {"Md", new Color(179/255f, 13/255f, 166/255f, 1) },
        {"No", new Color(189/255f, 13/255f, 135/255f, 1) },
        {"Lr", new Color(199/255f, 0, 102/255f, 1) },
        {"Rf", new Color(204/255f, 0, 89/255f, 1) },
        {"Db", new Color(209/255f, 0, 79/255f, 1) },
        {"Sg", new Color(217/255f, 0, 69/255f, 1) },
        {"Bh", new Color(224/255f, 0, 56/255f, 1) },
        {"Hs", new Color(230/255f, 0, 46/255f, 1) },
        {"Mt", new Color(235/255f, 0, 38/255f, 1) },
        // Elements 110-118 not included, but they are all more or less red in CPK
        {"other", Color.red },
    }; // A Dictionary to apply colors depending on what atom it is
    private Dictionary<string, float> ionicRadii = new Dictionary<string, float>()
    {
        // A Dictionary containing the most common ionic radius for some atoms. radius given in picometers (10^-12)
        // Data taken from "Crystal" ionic radius here: https://en.wikipedia.org/wiki/Ionic_radius
        {"H",  10 }, // H+1 has radius -4 (two-coordinated) this is negative, so we set it to a small radius instead
        {"Li", 90 }, // Li+1
        {"Be", 59 }, // Be+2
        {"B", 41 }, // B+3
        {"C", 30 }, // C+4
        {"N", 132 }, // N-3 (four-coordinates)
        {"O", 126 }, // O- 2
        {"F", 119 }, // F-1
        {"Na", 116 }, // Na+1
        {"Mg", 86 }, // Mg+2
        {"Al", 67.5f }, // Al+3
        {"Si", 54 }, // Si+4
        {"P", 58 }, // P+3 (52 for P+5)
        {"S", 170 }, // S-2
        {"Cl", 167 }, // Cl-1
        {"K", 152 }, // K+1
        {"Ca", 114 }, // Ca+2
        {"Sc", 88.5f }, // Sc+3
        {"Ti", 74.5f }, // Ti+4
        {"V", 72 }, // V+4
        {"Cr", 58 }, // Cr+6 (low-spin)
        {"Mn", 67 }, // Mn+4
        {"Fe", 75 }, // Fe+2 (low-spin)
        {"Co", 79 }, // Co+2 (low-spin)
        {"Ni", 83 }, // Ni+2 (low-spin)
        {"Cu", 83 }, // Cu+2
        {"Zn", 88 }, // Zn+2
        {"Ga", 76 }, // Ga+3
        {"Ge", 67 }, // Ge+4
        {"As", 72 }, // As+3
        {"Se", 184 }, // Se-2
        {"Br", 182 }, // Br-1
        {"Rb", 166 }, // Rb+1
        {"Sr", 132 }, // Sr+2
        {"Y", 104 }, // Y+3
        {"Zr", 84 }, // Zr+4
        {"Nb", 78 }, // Nb+5
        {"Mo", 79 }, // Mo+4
        {"Tc", 78.5f }, // Tc+4
        {"Ru", 82 }, // Ru+3
        {"Rh", 80.5f }, // Rh+3
        {"Pd", 100 }, // Pd+2
        {"Ag", 73 }, // Ag+1 (two-coordinated)
        {"Cd", 109 }, // Cd+2
        {"In", 94 }, // In+3
        {"Sn", 83 }, // Sn+4
        {"Sb", 90 }, // Sb+3
        {"Te", 207 }, // Te-2
        {"I", 206 }, // I-1
        {"Xe", 62 }, // Xe+8
        {"Cs", 181 }, // Cs+1
        {"Ba", 149 }, // Ba+2
        {"La", 117.2f }, // La+3
        {"Ce", 115 }, // Ce+3
        {"Pr", 113 }, // Pr+3
        {"Nd", 112.3f }, // Nd+3
        {"Pm", 111 }, // Pm+3
        {"Sm", 109.8f }, // Sm+3
        {"Eu", 108.7f }, // Eu+3
        {"Gd", 107.8f }, // Gd+3
        {"Tb", 106.3f }, // Tb+3
        {"Dy", 105.2f }, // Dy+3
        {"Ho", 104.1f }, // Ho+3
        {"Er", 103 }, // Er+3
        {"Tm", 102 }, // Tm+3
        {"Yb", 100.8f }, // Yb+3
        {"Lu", 100.1f }, // Lu+3
        {"Hf", 85 }, // Hf+4
        {"Ta", 78 }, // Ta+5
        {"W", 74 }, // W+6
        {"Re", 77 }, // Re+4
        {"Os", 77 }, // Os+4
        {"Ir", 82 }, // Ir+3
        {"Pt", 94 }, // Pt+2
        {"Au", 151 }, // Au+1
        {"Hg", 133 }, // Hg+1
        {"Tl", 164 }, // Tl+1
        {"Pb", 91.5f }, // Pb+4
        {"Bi", 117 }, // Bi+5
        {"Po", 108 }, // Po+2
        {"At", 76 }, // At+7
        {"Fr", 194 }, // Fr+1
        {"Ra", 162 }, // Ra+2 (eight-coordinated)
        {"Ac", 126 }, // Ac+3
        {"Pa", 104 }, // Pa+4
        {"U", 87 }, // U+6
        {"Np", 124 }, // Np+3
        {"Pu", 100 }, // Pu+4
        {"Am", 111.5f }, // Am+3
        {"Cm", 111 }, // Cm+3
        {"Bk", 110 }, // Bk+3
        {"Cf", 109 }, // Cf+3
        {"Es", 92.8f }, // Ed+3
        {"other", 100 } // Other: Default to 100 pm

    }; // A Dictionary containing the most common ionic radius for some atoms. radius given in picometers (10^-12). Atoms way to large, so all are scaled a bit down in SetAtomSize()


    // Start is called before the first frame update
    void Start()
    {
        // Initialization (The thought is to have a separate game scene with buttons, sliders, etc. for setting up the crystal. This is parsed to this file and creates the appropriate crystal)

        ReadConvert(CrystalManager.outfile); // Reads the converted file and stores needed data
        //ReadConvert(@"C:\Users\erlen\AppData\LocalLow\UiO TeamVR\Crystallographic Reality\cif2cell_convert\LSMO.txt"); // Temporary, use comment above after testing, and when back in UI menu
        //ReadConvert(@"C:\Users\erlen\AppData\LocalLow\UiO TeamVR\Crystallographic Reality\cif2cell_convert\Si.txt"); // Temporary, use comment above after testing, and when back in UI menu

        // Sets up Lattice Vectors in relation to Unity's coordinate system
        // Got help from https://en.wikipedia.org/wiki/Fractional_coordinates (We use x,y,z)

        cellVolume = cellLength[0] * cellLength[1] * cellLength[2] // abc
            * Mathf.Sqrt(1 - (Mathf.Cos(cellAngle[0] * Mathf.Deg2Rad) * Mathf.Cos(cellAngle[0] * Mathf.Deg2Rad)) // * sqrt( 1-cos^2(alpha)
            - (Mathf.Cos(cellAngle[1] * Mathf.Deg2Rad) * Mathf.Cos(cellAngle[1] * Mathf.Deg2Rad)) // -cos^2(beta)
            - (Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad) * Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad)) // -cos^2(gamma)
            + (2 * Mathf.Cos(cellAngle[0] * Mathf.Deg2Rad) * Mathf.Cos(cellAngle[1] * Mathf.Deg2Rad) * Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad))); // + 2*cos(alpha)*cos(beta)*cos(gamma) )

        float reciprocalScale = 2 * Mathf.PI / Vector3.Dot(bravaisVectors[0], (Vector3.Cross(bravaisVectors[2], bravaisVectors[1]))); // 2pi / a*(b x c)

        Vector3[] reciprocalVectors = new Vector3[3]
        {
            reciprocalScale*Vector3.Cross(bravaisVectors[2],bravaisVectors[1]), // b x c = reci_1
            reciprocalScale*Vector3.Cross(bravaisVectors[0],bravaisVectors[2]), // a x b = reci_3
            reciprocalScale*Vector3.Cross(bravaisVectors[1],bravaisVectors[0]) // c x a = reci_2
        };
        reciprocalMatrix = new float[3, 3]
        {
            {reciprocalVectors[0][0], reciprocalVectors[0][2], reciprocalVectors[0][1] }, // a11, a12, a13
            {reciprocalVectors[2][0],reciprocalVectors[2][2],reciprocalVectors[2][1] }, // a21, a22, a23
            {reciprocalVectors[1][0],reciprocalVectors[1][2],reciprocalVectors[1][1] } // a31, a32, a33
        }; // Matrix for converting cartesian coordinates into bravais coordinates. Flipping vectors from xzy to xyz, so matrix looks jumbled


        EvalSymmetry(); // Evaluates each symmetry matrix and categorizes them
        CreateCrystal(); // Constructs the physical unit cell based on conventional atom positions, tags atoms if they match through symmetry, creates corner/edge/face atoms of cell, and adds unit cell "sticks"


        CreateUnitCellGrid();
        CreateSymmetry();

        crystal.transform.Find("Symmetries").gameObject.SetActive(false); // Hides the symmetries as they are misplaced and ugly
        crystal.transform.position = -bravaisVectors[0] / 2 - bravaisVectors[1] / 2 - bravaisVectors[2] / 2 + new Vector3(1, cellLength[2] / 2 + 1, 0); // Moves crystal so that the center of the crystal is in (0,0,0) (we can use .position as it has no parent object)

    }

    // Update is called once per frame
    void Update()
    {
        // Eventual runtime-updates to the crystal object can be performed here
    }

    // Called in Start
    void ReadConvert(string infile)
    {
        // Reads a cif2cell run's command-line output in as .txt-file and extracts needed data
        List<string> atomElementList = new List<string>();
        List<Vector3> atomPosList = new List<Vector3>();
        List<float[,]> symmetryMatricesList = new List<float[,]>(); // A list of multi-dimensional arrays (each Vector array is a 3x3 matrix + 3x1 translation)

        string[] lines = File.ReadAllLines(infile); // Reads the file and stores each line in the array "lines"
        string[] words; // We initialize words before using it, though we also could have initialized it within each if I believe
        for (int i = 0; i < lines.Length; i++) // Initially foreach, but took for to get easier enumeration and skippable lines
        {
            if (lines[i].Contains("Lattice parameters:"))
            {
                // cellLength (the length is two lines below "Lattice parameters:". Therefore i + 2)
                words = lines[i + 2].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // the "new[]" part is just to get the correct overload of the Split-function. RemoveEmptyEntries fixes consecutive spaces in the file
                cellLength.x = StringToFloat(words[0]);
                cellLength.y = StringToFloat(words[1]);
                cellLength.z = StringToFloat(words[2]); // We use x,y,z here

                // cellAngle (the angle is four lines below "Lattice parameters:". Therefore i + 4)
                words = lines[i + 4].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                cellAngle[0] = StringToFloat(words[0]); // alpha ([0] is equivalent to .x)
                cellAngle[1] = StringToFloat(words[1]); // beta
                cellAngle[2] = StringToFloat(words[2]); // gamma

                i += 4; // Skips next lines as they've already been read
            }
            else if (lines[i].Contains("Bravais lattice vectors :"))
            {
                // cellVectors and cellMatrix
                words = lines[i + 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // a_vec
                string[] moreWords = lines[i + 2].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // b_vec
                string[] evenMoreWords = lines[i + 3].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // c_vec

                bravaisMatrix = new float[,]
                {
                    {StringToFloat(words[0]), StringToFloat(words[1]), StringToFloat(words[2]) }, // We might need it for translational coordinates
                    {StringToFloat(moreWords[0]), StringToFloat(moreWords[1]), StringToFloat(moreWords[2]) },
                    {StringToFloat(evenMoreWords[0]), StringToFloat(evenMoreWords[1]), StringToFloat(evenMoreWords[2]) }
                };
                bravaisVectors = new Vector3[]
                {
                    new Vector3(bravaisMatrix[0, 0],bravaisMatrix[0, 2],bravaisMatrix[0, 1]), // a_vec (x,z,y)
                    new Vector3(bravaisMatrix[2, 0],bravaisMatrix[2, 2],bravaisMatrix[2, 1]), // c_vec (x,z,y)
                    new Vector3(bravaisMatrix[1, 0],bravaisMatrix[1, 2],bravaisMatrix[1, 1]), // b_vec (x,z,y)
                };

                i += 3; // Skips next lines as they've already been read
            }
            else if (lines[i].Contains("All sites")) // Looks for conventional cell atom sites
            {
                words = lines[i + 2].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // The 1st line with info is 2 lines below
                while (words.Length > 0) // After "All sites" is a blank line (len=0), so we keep going until then
                {
                    atomElementList.Add(words[0]);
                    atomPosList.Add(new Vector3(StringToFloat(words[1]),
                        StringToFloat(words[3]),
                        StringToFloat(words[2]))); // We use (x,z,y) as Unity has y be vertical, and z be horisontal like x

                    i++; // We increment i for each rep. site we read
                    words = lines[i + 2].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // Update words for next iteration
                }
            }
            else if (lines[i].Contains("Operation "))
            {
                words = lines[i + 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // The actual operation is one line below
                symmetryMatricesList.Add(new float[,]
                {
                    { StringToFloat(words[0]), StringToFloat(words[3]), StringToFloat(words[6]), StringToFloat(words[9]) }, // (a11, a12, a13, a14) row 1 (x,y,z)
                    { StringToFloat(words[1]), StringToFloat(words[4]), StringToFloat(words[7]), StringToFloat(words[10]) }, // (a21, a22, a23, a24) row 2 (x,y,z)
                    { StringToFloat(words[2]), StringToFloat(words[5]), StringToFloat(words[8]), StringToFloat(words[11]) }  // (a31, a32, a33, a34) row 3 (x,y,z)
                });
                i++; // We increment i to avoid reading the line of the numbers, and rather skip to next "Operation "
            }
        }
        // Converts lists to arrays, for faster access (When creating cells atomElement and atomPos will be converted back to lists and filled, then over to array again)
        atomElement = atomElementList.ToArray();
        atomPos = atomPosList.ToArray();
        symmetryMatrices = symmetryMatricesList.ToArray();

        Debug.Log("Read the cif2cell-converted file and stored data");
    }

    // Called in Start
    void EvalSymmetry()
    {
        // Evaluates each Vector3[] to determine type of symmetry operation. Uses the global symmetryMatrices
        List<string> symmetryMatricesTypeList = new List<string>();

        for (int i = 0; i < symmetryMatrices.Length; i++) // .Length counts from 1, so we subtract 1 to count from 0
        {
            float[,] matrix = symmetryMatrices[i];
            string axis;

            float det = Det(matrix);
            float trace = Trace(matrix);
            float absNonDiagSum = AbsNonDiagSum(matrix);

            // Step 1: Det(ermine) determinant
            if (det > 0) // If determinant is positive
            {
                // Identity or rotation axis (Not rotoinversion)

                // Step 2: Identity has Trace(M) = 3 and absNonDiagSum = 0
                if (Mathf.Abs(trace - 3) < eps & absNonDiagSum < eps) // uses < and a tolerance to avoid float number errors
                {
                    //Identity
                    symmetryMatricesTypeList.Add("Identity");
                }
                else
                {
                    // Rotation axis
                    int degOfRotation;

                    // Step 3: Determine axis of rotation
                    /*
                        1  0    0
                        0 cos -sin   x
                        0 sin  cos
                       
                        cos 0 -sin
                         0  1   0    y
                        sin 0  cos
                       
                        cos -sin 0
                        sin  cos 0   z
                         0    0  1
                        theta varies from 0.5 to -1 to 0.5 (6_1 -> 6_5) (which includes all 2, 3 and 4 within the interval) (Means only one axis will have diag = 1
                    */

                    // Find diagonal element with value = 1. This is the axis of rotation
                    if (Mathf.Abs(matrix[0, 0] - 1) < eps) // If element a11 = 1
                    {
                        // x-axis
                        axis = "(1'0'0)";
                        // Step 4: Determine degree of rotation (2=180, 3=120, 4=90, 6=60) // n=5,7 not included (molecules) (yet?)
                        degOfRotation = DegreeOfRotation(Mathf.Atan2(matrix[2, 1], matrix[1, 1]) * Mathf.Rad2Deg); // Uses atan(y/x) and converts to degrees

                    }
                    else if (Mathf.Abs(matrix[1, 1] - 1) < eps) // If element a22 = 1
                    {
                        // y-axis
                        axis = "(0'1'0)";
                        // Step 4: Determine degree of rotation (2=180, 3=120, 4=90, 6=60) // n=5,7 not included (molecules) (yet?)
                        degOfRotation = DegreeOfRotation(Mathf.Atan2(matrix[2, 0], matrix[0, 0]) * Mathf.Rad2Deg);
                    }
                    else if (Mathf.Abs(matrix[2, 2] - 1) < eps) // If element a33 = 1
                    {
                        // z-axis
                        axis = "(0'0'1)";
                        // Step 4: Determine degree of rotation (2=180, 3=120, 4=90, 6=60) // n=5,7 not included (molecules) (yet?)
                        degOfRotation = DegreeOfRotation(Mathf.Atan2(matrix[1, 0], matrix[0, 0]) * Mathf.Rad2Deg);
                    }
                    else
                    {
                        // Below only works if matrix is non-symmetric.
                        // Original taken from https://en.wikipedia.org/wiki/Rotation_matrix#Determining_the_axis
                        // See http://scipp.ucsc.edu/~haber/ph116A/rotation_11.pdf page 7 for full guide

                        float[] u = new float[] { // There was a prefactor, but it caused tricky thetas and ugly vectors, so I removed it.
                                (matrix[2, 1] - matrix[1, 2]),
                                (matrix[0, 2] - matrix[2, 0]),
                                (matrix[1, 0] - matrix[0, 1]) }; // u = (a32-a23, a13-a31, a21-a12)

                        if (Mathf.Abs(u[0]) < eps && Mathf.Abs(u[1]) < eps && Mathf.Abs(u[2]) < eps) // Should take care of symmetrycal matrices
                        {
                            Debug.LogError("NotImplemented: Find eigenvector with lambda=1 for rotation axis");
                            u = new float[] { 0, 0, 0 };
                        }


                        /* This caused issues for non-symmetrical matrices, so I changed it back to the old formula again
                        float[] u;
                        if ((Mathf.Abs(trace) + 1) < eps && (Mathf.Abs(trace) - 3) < eps) // trace != -1, 3
                        {
                            float prefactor = 1 / (Mathf.Sqrt((3 - trace) * (1 + trace)));
                            u = new float[] {
                                prefactor*(matrix[2, 1] - matrix[1, 2]),
                                prefactor*(matrix[0, 2] - matrix[2, 0]),
                                prefactor*(matrix[1, 0] - matrix[0, 1]) }; // u = prefactor * (a32-a23, a13-a31, a21-a12)
                        }
                        else
                        {
                            // We have ruled out theta=0 as that is identity. This is theta=pi rad = 180 deg
                            // However, we still do not know the axis of rotation
                            Debug.LogWarning("NotImplemented: Find eigenvector with lambda=1 for rotation axis");
                            u = new float[] { 0, 0, 0 };
                        }
                        */

                        axis = "(" + u[0] + "'" + u[1] + "'" + u[2] + ")";

                        // Now, to determine the angle theta for degOfRotation,
                        // we can either use that ||u||=2*sin(theta),
                        // or we can use Trace(matrix) = 1 + 2 cos(theta).
                        // Using trace seems simpler, computationally, as we have a function for that already. // Trace is might already be zero, but it wouldn't use trace instead of -1/2
                        // However, by using both methods, we can again use Atan2 for increased range of theta
                        float sin = Mathf.Sqrt((u[0] * u[0]) + (u[1] * u[1]) + (u[2] * u[2])) / 2; // ||u|| = sqrt(x^2+y^2+z^2). sin(theta) = ||u||/2
                        float cos = (trace - 1f) / 2; // Trace(M) = 1 + 2*cos -> cos = (Trace(M)-1)/2
                        degOfRotation = DegreeOfRotation(Mathf.Atan2(sin, cos) * Mathf.Rad2Deg); // Atan2 only works when sin>0, for sin<0 the angle is off by 180 deg. The function takes this into account
                    }

                    // Step 5: Determine rotation vs. screw
                    if (Mathf.Abs(matrix[0, 3]) + Mathf.Abs(matrix[1, 3]) + Mathf.Abs(matrix[2, 3]) < eps) // If the sum of the translation vector components = 0
                    {
                        // Rotation axis
                        symmetryMatricesTypeList.Add("Rotation " + axis + " " + degOfRotation);
                    }
                    else // Screw axis
                    {
                        // n_m = rotation (n) + translation (m/n). Try different values of m to fit with translation
                        // Originally did translation * n = m and compared for different m, but I could just assign it as-is
                        if (degOfRotation != 2) // 2-fold screw axis can only be 2_1, so we skip it entirely
                        {
                            if (Mathf.Abs(matrix[0, 3]) < eps) // Rotation can leave one coordinate zero and translate the other two, so we take this into account using if else
                            {
                                symmetryMatricesTypeList.Add("Screw " + axis + " " + degOfRotation + " " + Mathf.RoundToInt(Mathf.Abs(matrix[0, 3] * degOfRotation))); // Uses x-coordinate of translation vector. Also RoundToInt as sometimes it is 0.999
                            }
                            else // if x = 0, then y and z should be != 0
                            {
                                symmetryMatricesTypeList.Add("Screw " + axis + " " + degOfRotation + " " + Mathf.RoundToInt(Mathf.Abs(matrix[1, 3] * degOfRotation))); // Uses y-coordinate of translation vector
                            }
                        }
                        else
                        {
                            symmetryMatricesTypeList.Add("Screw " + axis + " " + degOfRotation + " 1");
                        }
                    }
                }

            }
            else if (det < 0) // If determinant is negative
            {
                // Inversion or reflection (also rotoinversion)

                // Step 2: Inversion has Trace(M) = - 3 and absNonDiagSum = 0
                if (Mathf.Abs(trace + 3) < eps & absNonDiagSum < eps)
                {
                    // Inversion
                    symmetryMatricesTypeList.Add("Inversion");
                }
                else
                {
                    // Reflection (or rotoinversion)
                    // Step 3: Determine plane of reflection (Reflections only have one diag with value = -1)
                    if (Mathf.Abs(matrix[0, 0] + 1) < eps) // x-axis is plane normal
                    {
                        axis = "(1'0'0)";
                        // Step 4: Determine mirror vs. glide
                    }
                    else if (Mathf.Abs(matrix[1, 1] + 1) < eps) // y-axis is plane normal
                    {
                        axis = "(0'1'0)";
                    }
                    else if (Mathf.Abs(matrix[2, 2] + 1) < eps) // z-axis is plane normal
                    {
                        axis = "(0'0'1)";
                    }
                    else
                    {
                        float[] v = { 1, 2, 3 }; // We define an arbitrary vector to be reflected
                        float[] u = LinTransform(matrix, v); // Mv = u. We mirror the vector
                        float[] displacement = { u[0] - v[0],
                            u[1] - v[1],
                            u[2] - v[2] }; // The displacement is the normal of the reflection plane. displacement = u-v
                        displacement = LinTransform(reciprocalMatrix, displacement); // We convert it to bravais coordinates so the numbers match a,b and c

                        if (Mathf.Abs(displacement[0]) < eps & Mathf.Abs(displacement[1]) < eps & Mathf.Abs(displacement[2]) < eps) // If the displacement = (0,0,0)
                        {
                            Debug.LogError("The plane normal is (0,0,0). Either the plane normal is (1,2,3) as that was our input, or this is a rotoinversion or something else? Symmetry operation: " + (i + 1));
                            axis = "unknown";
                        }
                        else
                        {
                            // Look for greatest common divisor, so the plane has correct miller index

                            float gcd = 1; // greatest common divisor. For scaling mirror axis correctly

                            //gcd = displacement.Where(x => Mathf.Abs(x) > eps).Min(); // Finds smallest non-zero coordinate in displacement. Not ture GCD, but works okay

                            /*
                            Taken from: https://stackoverflow.com/questions/18541832/c-sharp-find-the-greatest-common-divisor
                            List<float> nonZeroList = new List<float>(); // List to fill with nonZero elements. For non-cubic systems, the values are not always int

                            for (int j = 0; j < displacement.Length; j++) // Iterate to find nonZero elements
                            {
                                if (!(Mathf.Abs(displacement[j]) < eps)) // if nonZero
                                {
                                    nonZeroList.Add(Mathf.Abs(displacement[j])); // Add to list (adds absolute value for easier gcd testing after)
                                }
                            }
                            float[] nonZero = nonZeroList.ToArray();

                            switch (nonZero.Count)
                            {
                                case 1:
                                    gcd = nonZero[0];
                                    break;
                                case 2:
                                    while (nonZero[0]! < eps && nonZero[1]! < eps)
                                    {
                                        if (nonZero[0] > nonZero[1])
                                        {
                                            nonZero[0] %= nonZero[1];
                                        }
                                        else
                                        {
                                            nonZero[1] %= nonZero[0];
                                        }
                                    }
                                    gcd = nonZero[0] | nonZero[1];
                                    break;
                                case 3:

                                    break;
                            }
                            */

                            displacement = new float[] { displacement[0] / gcd,
                            displacement[1] / gcd,
                            displacement[2] / gcd }; // Makes displacement use smaller values ( (0,-5,-5) -> (0,1,1). Since (0,-1,1)==(0,1,-1) this should not cause issues with flipping planes incorrectly
                            axis = "(" + displacement[0] + "'" + displacement[1] + "'" + displacement[2] + ")"; // If this suddenly causes issues, use whitespace as separator
                        }
                    }

                    // Step 4: Determine mirror vs. glide
                    if (Mathf.Abs(matrix[0, 3]) + Mathf.Abs(matrix[1, 3]) + Mathf.Abs(matrix[2, 3]) < eps)
                    {
                        symmetryMatricesTypeList.Add("Mirror " + axis);
                    }
                    else
                    {
                        // Glide plane
                        // Step 5: Determine type of glide plane
                        // a-glide (0.5, 0, 0)
                        // b-glide (0, 0,5, 0)
                        // c-glide (0, 0, 0.5)
                        // n-glide (0.5, 0.5, 0.5)
                        // d-glide (0.25, 0.25, 0.25) (We ignore e-glide as that is two other glides combined)

                        if ((Mathf.Abs(matrix[0, 3]) - 0.5) < eps && (Mathf.Abs(matrix[1, 3]) + Mathf.Abs(matrix[2, 3])) < eps) // If x=0.5 and y and z = 0
                        {
                            // a-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " a");
                        }
                        else if ((Mathf.Abs(matrix[1, 3]) - 0.5) < eps && (Mathf.Abs(matrix[0, 3]) + Mathf.Abs(matrix[2, 3])) < eps) // If y=0.5 and x and z = 0
                        {
                            // b-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " b");
                        }
                        else if ((Mathf.Abs(matrix[2, 3]) - 0.5) < eps && (Mathf.Abs(matrix[0, 3]) + Mathf.Abs(matrix[2, 3])) < eps) // If z=0.5 and x and z = 0
                        {
                            // c-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " c");
                        }
                        else if (((Mathf.Abs(matrix[0, 3]) - 0.5) < eps && (Mathf.Abs(matrix[1, 3]) - 0.5) < eps)) // If x and y = 0.5
                        {
                            // n-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " n xy");
                        }
                        else if (((Mathf.Abs(matrix[0, 3]) - 0.5) < eps && (Mathf.Abs(matrix[2, 3]) - 0.5) < eps)) // If x and z = 0.5
                        {
                            // n-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " n xz");
                        }
                        else if (((Mathf.Abs(matrix[1, 3]) - 0.5) < eps && (Mathf.Abs(matrix[2, 3]) - 0.5) < eps)) // If y and z = 0.5
                        {
                            // n-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " n yz");
                        }
                        else if (((Mathf.Abs(matrix[0, 3]) - 0.25) < eps && (Mathf.Abs(matrix[1, 3]) - 0.25) < eps)) // If x and y = 0.5
                        {
                            // d-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " d xy");
                        }
                        else if (((Mathf.Abs(matrix[0, 3]) - 0.25) < eps && (Mathf.Abs(matrix[2, 3]) - 0.25) < eps)) // If x and z = 0.5
                        {
                            // d-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " d xz");
                        }
                        else if (((Mathf.Abs(matrix[1, 3]) - 0.25) < eps && (Mathf.Abs(matrix[2, 3]) - 0.25) < eps)) // If y and z = 0.5
                        {
                            // d-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " d yz");
                        }
                        else
                        {
                            // e-glide (I HOPE)
                            Debug.LogWarning("Could not determine type of glide plane. Maybe e-glide? Defaulting to e-glide. Symmetry operation: " + (i + 1));
                            symmetryMatricesTypeList.Add("Glide " + axis + " e");
                            //throw new NotImplementedException("Could not determine type of glide plane. Symmetry operation: " + (i + 1));
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Could not evaluate symmetry based on determinant: " + det + ". Fix not implemented. Symmetry operation: " + (i + 1));
                symmetryMatricesTypeList.Add("Unknown");
                //throw new NotImplementedException("Could not evaluate symmetry based on determinant: " + det + ". Fix not implemented. Symmetry operation: " + (i + 1));
            }
            Debug.Log("Identified Symmetry Operation " + (i + 1) + " as " + symmetryMatricesTypeList[i]);
        }
        symmetryMatricesType = symmetryMatricesTypeList.ToArray();
    }

    // Called in Start
    void CreateCrystal()
    {
        // Constructs the crystal unit cell by running the representative sites through each symmetry matrix
        // Whenever a duplicate atom is made, it should delete the duplicate and rather add a tag to the original atom, binding it to it's symmetrical equivalent

        GameObject atomParent = new GameObject("Atoms"); // Creates an empty GameObject to parent all atoms (mostly for tidier structure in Unity Editor)
        atomParent.transform.parent = crystal.transform; // Sets the atomParent as a child of the Crystal
        atomParent.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the atomParent is in the Crystal's (0,0,0) and not the worlds' (0,0,0)
        List<GameObject> atomObjectsList = new List<GameObject>(); // Creates a list to contain each atom's object for easier access. Will be converted to array at the end
        //List<string> atomElementsList = new List<string>(); // Creates a list to contain each atom's element for easier access. Will be converted to array at the end

        // Creates basic atom positions, and tags them with symmetries
        for (int i = 0; i < atomPos.Length; i++) // Loops over each conventional atom site LOOK INTO SYMMETRY TAGS
        {
            atomObjectsList.Add(Instantiate(atom, atomParent.transform, false)); // Creates a physical atom object, with atomParent as parent, and adds it to the list
            //atomElementsList.Add(atomElement[i]); // Adds the atom's element

            atomObjectsList[i].transform.localPosition = atomPos[i]; // Places the atom in its correct position
            atomObjectsList[i].name = atomElement[i] + " " + atomPos[i]; // Names the atom so they are easier to distinguish in the Unity Editor, AND to use for SetAtomColor which takes the name to find element
            SetAtomColor(atomObjectsList[i]); // Sets the atom's color based on its element
            SetAtomSize(atomObjectsList[i]); // Sets the atom's size based on its element

            Debug.Log("Added conventional atom: " + atomObjectsList[i].name);

            for (int j = 0; j < symmetryMatrices.Length; j++) // Loops over each symmetry matrix
            {
                Vector3 pos = PerformSymmetry(symmetryMatrices[j], atomPos[i]); // Performs symmetry operation on representative site (including translation)
                Vector3 bravaisPos = LinTransform(reciprocalMatrix, pos); // Transforms cartesian into bravais coordinates (a,0,0) -> (1,0,0)

                for (int k = 0; k < atomPos.Length; k++) // Loops over each conventional atom position
                {
                    // Checks if symmetry-made position is outside unit cell due to symmetry operation, and translates inside unit cell again
                    if (bravaisPos[0] > 1) // If the atom is outside the unit cell in the a direction
                    {
                        pos -= bravaisVectors[0]; // Subtract the a_vec to put it back inside
                    }
                    if (bravaisPos[1] > 1) // If the atom is outside the unit cell in the c direction
                    {
                        pos -= bravaisVectors[1]; // Subtract the c_vec to put it back inside
                    }
                    if (bravaisPos[2] > 1) // If the atom is outside the unit cell in the b direction
                    {
                        pos -= bravaisVectors[2];// Subtract the b_vec to put it back inside
                    }

                    if (atomElement[k] == atomElement[i] && // If new atom is of same element (if different element, the symmetry should be different, in my head)
                        (Mathf.Abs(pos[0] - atomPos[k][0]) < eps) &&
                        (Mathf.Abs(pos[1] - atomPos[k][1]) < eps) &&
                        (Mathf.Abs(pos[2] - atomPos[k][2]) < eps)) // Checks if position already exists from before for this atom
                    {
                        atomObjectsList[i].GetComponent<CustomTag>().AddTag((j + 1) + " " + k); // Adds symmetry tag to atom we just made. "symmetryOperationNumber equivalentAtomPosNumber"
                        Debug.Log("Added tag: \"" + (j + 1) + " " + k + "\" to atom: " + atomObjectsList[i].name);
                    }
                }
            }
        }

        // Adds corner/edge/face atoms
        // Solves for where one coordinate is zero (face)
        for (int i = 0; i < atomPos.Length; i++)
        {
            for (int j = 0; j < 3; j++) // Iterates over the bravais lattice vectors (a -> c -> b)
            {
                if (Mathf.Abs(LinTransform(reciprocalMatrix, atomPos[i])[j]) < eps) // If atom position is approx. 0 (relative to bravais lattice)
                {
                    Vector3 newPos = atomPos[i]; // Updates the equivalent position
                    newPos += bravaisVectors[j]; // We defined cellVectors as a_vec, b_vec, c_vec so it should be fine. Uses cartesian converted bravais coordinates

                    GameObject newAtom = Instantiate(atom, atomParent.transform, false); // Instantiates new atom

                    newAtom.transform.localPosition = newPos; // Sets the equivalent position
                    newAtom.name = atomElement[i] + " " + newPos; // Names the atom so they are easier to distinguish in the Unity Editor
                    SetAtomColor(newAtom); // Sets the atom's color based on its element
                    SetAtomSize(newAtom); // Sets the atom's size based on its element
                    newAtom.GetComponent<CustomTag>().tags = atomObjectsList[i].GetComponent<CustomTag>().tags; // Adds symmetry tags to atom

                    atomObjectsList.Add(newAtom); // Adds the atom to the list

                    Debug.Log("(1) Created atom: " + newPos.ToString("F2") + " from " + atomPos[i].ToString("F2"));

                    // Solves for where two coordinates are zero (edge). NOTE: This creates duplicates of atoms as (x,1,0) and (x,0,1) from previous loop are flipped to (x,1,1). Will destroy duplicates after
                    for (int k = 1; k < 3; k++) // Iterates over b and c (c -> b)
                    {
                        if (Mathf.Abs(LinTransform(reciprocalMatrix, newPos)[k]) < eps) // If atom position is approx. 0
                        {
                            Vector3 newerPos = newPos;
                            newerPos += bravaisVectors[k]; // Sets start of cell to end of cell

                            GameObject newerAtom = Instantiate(atom, atomParent.transform, false);

                            newerAtom.transform.localPosition = newerPos; // Sets the equivalent position
                            newerAtom.name = atomElement[i] + " " + newerPos; // Names the atom
                            SetAtomColor(newerAtom); // Sets the atom's color based on its element
                            SetAtomSize(newerAtom); // Sets the atom's size based on its element
                            newerAtom.GetComponent<CustomTag>().tags = atomObjectsList[i].GetComponent<CustomTag>().tags; // Adds symmetry tags to atom

                            atomObjectsList.Add(newerAtom); // Instantiates an equivalent atom to the original

                            Debug.Log("(2) Created atom: " + newerPos.ToString("F2") + " from " + newPos.ToString("F2"));

                            // Solves for where three coordinates are zero (corner)
                            // Iterates over b
                            if (Mathf.Abs(LinTransform(reciprocalMatrix, newerPos)[2]) < eps) // If atom position is approx. 0
                            {
                                Vector3 newestPos = newerPos;
                                newestPos += bravaisVectors[2]; // Sets start of cell to end of cell

                                GameObject newestAtom = Instantiate(atom, atomParent.transform, false);

                                newestAtom.transform.localPosition = newestPos; // Sets the equivalent position
                                newestAtom.name = atomElement[i] + " " + newestPos; // Names the atom
                                SetAtomColor(newestAtom); // Sets the atom's color based on its element
                                SetAtomSize(newestAtom); // Sets the atom's size based on its element
                                newestAtom.GetComponent<CustomTag>().tags = atomObjectsList[i].GetComponent<CustomTag>().tags; // Adds symmetry tags to atom

                                atomObjectsList.Add(newestAtom); // Instantiates an equivalent atom to the original

                                Debug.Log("(3) Created atom: " + newestPos.ToString("F2") + " from " + newerPos.ToString("F2"));
                            }
                        }
                    }
                }
            }
        }

        // Destroys duplicate atoms
        for (int i = 0; i < atomObjectsList.Count - 2; i++)
        {
            // (This should deal with all atoms, as the algorithm only makes dupes 2 indexes apart. If not, iterate again with j and j != 0)
            if (atomObjectsList[i].transform.position == atomObjectsList[i + 2].transform.position) // If position vectors are equal (Vector3 includes approximation)
            {
                Destroy(atomObjectsList[i + 2]); // Destroys atom
                atomObjectsList.RemoveAt(i + 2); // Removes the now destroyed atom from the list
                //atomElementsList.RemoveAt(i + 1); // Removes the element so we have track of it
            }
        }

        atomObjects = atomObjectsList.ToArray(); // Converts the atomObjectsList to an array (arrays are better, faster, harder, stronger)

        Debug.Log("Created Crystal");
    }

    // Called in Start
    void CreateUnitCellGrid()
    {
        // Creates the sticks for the sides of the unit cell, giving a clear picture of the unit cell's boundaries
        GameObject gridParent = new GameObject("Unit Cell Grid"); // Creates an empty GameObject to store gridLines in
        gridParent.transform.parent = crystal.transform; // Sets unitCellGrid as a child of the Crystal
        gridParent.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the unitCellGrid is in the Crystal's (0,0,0)

        GameObject gridLineX = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // Creates a standard Unity cylinder
        gridLineX.transform.parent = gridParent.transform; // Sets the gridLine to a child of the gridParent
        gridLineX.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the gridLine is in the gridParent's (0,0,0)

        GameObject gridLineY = Instantiate(gridLineX, gridParent.transform, false); // Creates copies
        GameObject gridLineZ = Instantiate(gridLineX, gridParent.transform, false);

        gridLineX.name = "X "; // Names the gridLines
        gridLineY.name = "Y ";
        gridLineZ.name = "Z ";

        // Scales them to be thinner and as long enough to strech the entire unit cell
        gridLineX.transform.localScale = new Vector3(0.05f, cellLength[0] / 2, 0.05f); // a
        gridLineY.transform.localScale = new Vector3(0.05f, cellLength[1] / 2, 0.05f); // b
        gridLineZ.transform.localScale = new Vector3(0.05f, cellLength[2] / 2, 0.05f); // c


        gridLineX.transform.LookAt(bravaisVectors[0] + crystal.transform.position); // Makes the gridLine's z-component look at the coordinate point the bravais vector points to
        gridLineY.transform.LookAt(bravaisVectors[2] + crystal.transform.position); // b_vec is [2]
        gridLineZ.transform.LookAt(bravaisVectors[1] + crystal.transform.position); // c_vec is [1]

        // We need to look at the y-component instead, so we rotate the gridLines
        gridLineX.transform.Rotate(90, 0, 0); // Rotates X so it faces correctly (parallel to bravais)
        gridLineY.transform.Rotate(90, 0, 0); // Rotates Y so it faces correctly (parallel to bravais)
        gridLineZ.transform.Rotate(90, 0, 0); // Rotates Z so it faces correctly (parallel to bravais)

        gridLineX.transform.localPosition = bravaisVectors[0] / 2f; // Moves the center of the cylinder to the center of its bravais vector (cylinder has its pivot in center not on its bottom)
        gridLineY.transform.localPosition = bravaisVectors[2] / 2f;
        gridLineZ.transform.localPosition = bravaisVectors[1] / 2f;

        // Adds and places gridLines for the other unit cell edges
        GameObject gridLineXA = Instantiate(gridLineX, gridParent.transform, false);
        gridLineXA.transform.localPosition += bravaisVectors[1]; // Translates along b
        GameObject gridLineXB = Instantiate(gridLineX, gridParent.transform, false);
        gridLineXB.transform.localPosition += bravaisVectors[2]; // Translates along c
        GameObject gridLineXC = Instantiate(gridLineX, gridParent.transform, false);
        gridLineXC.transform.localPosition += bravaisVectors[1] + bravaisVectors[2]; // Translates along b and c

        GameObject gridLineYA = Instantiate(gridLineY, gridParent.transform, false);
        gridLineYA.transform.localPosition += bravaisVectors[0]; // Translates along a
        GameObject gridLineYB = Instantiate(gridLineY, gridParent.transform, false);
        gridLineYB.transform.localPosition += bravaisVectors[1]; // Translates along c
        GameObject gridLineYC = Instantiate(gridLineY, gridParent.transform, false);
        gridLineYC.transform.localPosition += bravaisVectors[0] + bravaisVectors[1]; // Translates along a and c

        GameObject gridLineZA = Instantiate(gridLineZ, gridParent.transform, false);
        gridLineZA.transform.localPosition += bravaisVectors[0]; // Translates along a
        GameObject gridLineZB = Instantiate(gridLineZ, gridParent.transform, false);
        gridLineZB.transform.localPosition += bravaisVectors[2]; // Translates along b
        GameObject gridLineZC = Instantiate(gridLineZ, gridParent.transform, false);
        gridLineZC.transform.localPosition += bravaisVectors[0] + bravaisVectors[2]; // Translates along a and b

        Debug.Log("Created Unit Cell grid");
    }

    // Called in Start
    void CreateSymmetry()
    {
        // Investigates all Symmetries and visualizes them

        GameObject symmetryParent = new GameObject("Symmetries"); // Creates a parent to keep all symmetries
        symmetryParent.transform.parent = crystal.transform;
        symmetryParent.transform.localPosition = new Vector3(0, 0, 0);

        GameObject allRotationsParent = new GameObject("Rotations"); // Creates a parent to keep all rotations
        allRotationsParent.transform.parent = symmetryParent.transform;
        allRotationsParent.transform.localPosition = new Vector3(0, 0, 0);
        GameObject rotationParent = new GameObject("Normal rotations"); // Creates a parent to keep normal rotations
        rotationParent.transform.parent = allRotationsParent.transform;
        rotationParent.transform.localPosition = new Vector3(0, 0, 0);
        GameObject screwParent = new GameObject("Screw otations"); // Creates a parent to keep screw rotations
        screwParent.transform.parent = allRotationsParent.transform;
        screwParent.transform.localPosition = new Vector3(0, 0, 0);

        GameObject allReflectionsParent = new GameObject("Reflections"); // Creates a parent to keep all reflections
        allReflectionsParent.transform.parent = symmetryParent.transform;
        allReflectionsParent.transform.localPosition = new Vector3(0, 0, 0);
        GameObject mirrorParent = new GameObject("Mirror reflections"); // Creates a parent to keep mirror reflections
        mirrorParent.transform.parent = allReflectionsParent.transform;
        mirrorParent.transform.localPosition = new Vector3(0, 0, 0);
        GameObject glideParent = new GameObject("Glide reflections"); // Creates a parent to keep glide reflections
        glideParent.transform.parent = allReflectionsParent.transform;
        glideParent.transform.localPosition = new Vector3(0, 0, 0);

        List<GameObject> symmetryElementsList = new List<GameObject>(); // Creates a list to fill with symmetryElements so they can be accessed later

        for (int i = 0; i < symmetryMatricesType.Length; i++)
        {

            string symmetry = symmetryMatricesType[i];
            string[] symmetryInfo = symmetry.Split(' '); // Splits the symmetryInfo per whitespace (e.g. "Screw axis degree subscript")
            string symmetryType = symmetryInfo[0];

            if (symmetryInfo.Length > 1) // If the len>1 we have rotation/reflection
            {
                // Fetches the axis as that is used by all the symmetries. axis = "(x,y,z)"
                string axisString = symmetryInfo[1].Remove(0, 1); // Removes the 1st char (a parenthesis). axis = "x,y,z)"
                axisString = axisString.Remove(axisString.Length - 1); // Removes all the final character (closing parenthesis). axis = "x,y,z"
                float[] axis = new float[] // Initially I used comma as a coordinate separator, but that caused issues as decimals are comma in norway. Swapped it to '
                {
                        float.Parse(axisString.Split('\'')[0]), float.Parse(axisString.Split('\'')[1]), float.Parse(axisString.Split('\'')[2]) // Gets x, y, and z. These numbers have comma, so StringToFloat won't work. We therefore use the default float.Parse
                };

                // If this suddenly causes issues, use whitespace as separator here and in EvalSymmetry()

                switch (symmetryType)
                {
                    case "Rotation":
                        // Instantiate Rotation axis based on axis and degree of rotation
                        symmetryElementsList.Add(CreateRotation(symmetry, axis, int.Parse(symmetryInfo[2]), 0, rotationParent)); // [2] is the degOfRotation. We set subscript to 0
                        
                        break;

                    case "Screw":
                        // Instantiate Screw axis based on axis, degree of rotation and subscript
                        symmetryElementsList.Add(CreateRotation(symmetry, axis, int.Parse(symmetryInfo[2]), int.Parse(symmetryInfo[3]), screwParent)); // [2]=degOfRotation, [3]=subscript
                        
                        break;

                    case "Mirror":
                        // Instantiate Mirror plane based on plane normal and size of lattice (CreatePlane())
                        // The plane normal is actually the same as its miller index, so we could base our creation on that?
                        // The axis is made by displacement of an arbitrary vector (mirror vector and see where it went to find plane normal).
                        // This means that the plane position is in half of the displacement.
                        // We can create a plane facing the displacement, with its position in the displacement / 2f. However, this will likely make the plane difficult to scale perfectly for all angles
                        symmetryElementsList.Add(CreateReflection(symmetry, axis, null, null, mirrorParent)); // Creates reflection based on information

                        break;

                    case "Glide":
                        // Instantiate Glide plane based on plane normal and size of lattice (CreatePlane()), with different color to indicate type of glide
                        if (symmetryInfo.Length < 4) // If we don't have type of glide
                        {
                            List<string> symmetryInfoList = new List<string>(); // List to replace symmetryInfo
                            for (int j = 0; j< symmetryInfo.Length; j++) // Iterate over existing symmetryInfo
                            {
                                symmetryInfoList.Add(symmetryInfo[j]); // Fill new with old
                            }
                            symmetryInfoList.Add(null); // Add glideType
                            symmetryInfo = symmetryInfoList.ToArray(); // Convert to array so we can use CreateReflection()
                        }
                        symmetryElementsList.Add(CreateReflection(symmetry, axis, symmetryInfo[2], symmetryInfo[3], glideParent)); // Creates reflection based on information. [2] is glideType, [3] is glideDirection

                        break;

                    default: // Includes "Unknown"
                        symmetryElementsList.Add(new GameObject(symmetry)); // Adds empty GameObject with the name of the symmetry

                        break;
                }
            }
            else // We have Identity or Inversion
            {
                switch (symmetryType)
                {
                    case "Identity":
                        // This always exists, so ignore it. We make an empty object, but no render
                        GameObject identity = new GameObject("Identity");
                        identity.transform.parent = symmetryParent.transform;
                        identity.transform.localPosition = new Vector3(0, 0, 0);

                        symmetryElementsList.Add(identity);
                        break;

                    case "Inversion":
                        // Instantiate Inversion element. For now this is an empty object
                        GameObject inversion = new GameObject("Inversion");
                        inversion.transform.parent = symmetryParent.transform;
                        inversion.transform.localPosition = new Vector3(0, 0, 0);

                        symmetryElementsList.Add(inversion);

                        break;
                }
            }
        }
        symmetryElements = symmetryElementsList.ToArray(); // Converts the symmetries to arrays for faster access

        Debug.Log("Created visual symmetry elements. Number of symmetry elements: " + symmetryElements.Length);
    }

    // Called in SymmetryEval
    float Det(float[,] matrix)
    {
        //Finds the determinant of a 3x3 rotation matrix (here: in a 3x4 matrix (rotation + translation))
        if (matrix.Length < 3) // Throws an error if the matrix is too small
        {
            throw new ArgumentException("Matrix does not have enough elements to find 3x3 determinant");
        }

        float a = matrix[0, 0] * (matrix[1, 1] * matrix[2, 2] - matrix[1, 2] * matrix[2, 1]); // a11 * (a22*a33 - a23*a32)
        float b = matrix[0, 1] * (matrix[1, 0] * matrix[2, 2] - matrix[1, 2] * matrix[2, 0]); // a12 * (a21*a33 - a23*a31)
        float c = matrix[0, 2] * (matrix[1, 0] * matrix[2, 1] - matrix[1, 1] * matrix[2, 0]); // a13 * (a21*a32 - a22*a31)

        return a - b + c;
    }

    // Called in SymmetryEval
    float Trace(float[,] matrix)
    {
        // Adds diagonal elements of a 3x3 matrix (Here: 3x3 rotation + 3x1 translation)

        if (matrix.Length < 3) // Throws an error if the matrix is too small
        {
            throw new ArgumentException("Matrix does not have enough elements to trace 3x3 matrix");
        }

        return matrix[0, 0] + matrix[1, 1] + matrix[2, 2];
    }

    // Called in SymmetryEval
    float AbsNonDiagSum(float[,] matrix)
    {
        // Adds together the sum of the absolute value of all non-diagonal elements in a 3x3 matrix (Here: 3x3 rotation + 3x1 translation)
        // Uses absolute value of induvidual values to avoid elements cancelling each other out

        if (matrix.Length < 3) // Throws an error if the matrix is too small
        {
            throw new ArgumentException("Matrix does not have enough elements to add non-diag elements in 3x3 matrix");
        }

        return Mathf.Abs(matrix[0, 1]) + Mathf.Abs(matrix[0, 2])
            + Mathf.Abs(matrix[1, 0]) + Mathf.Abs(matrix[1, 2])
            + Mathf.Abs(matrix[2, 0]) + Mathf.Abs(matrix[2, 1]); // Could have looped but is more complicated and not needed (plus this is likely faster)
    }

    // Called in SymmetryEval/Rotation axis
    int DegreeOfRotation(float theta, float eps = 0.0001f)
    {
        // This function determines the degree of rotation of a rotation axis based on its angle
        if (Mathf.Abs(theta - 180) < eps | Mathf.Abs(theta + 180) < eps)
        {
            return 2;
        }
        else if (Mathf.Abs(theta - 120) < eps | Mathf.Abs(theta + 120) < eps) // Sometimes angle was -120, so we added a check for this on all degrees
        {
            return 3;
        }
        else if (Mathf.Abs(theta - 90) < eps | Mathf.Abs(theta + 90) < eps) // arctan(1/0) = error, but atan2() gives -90. However, it should give +90. We take this into account here.
        {
            return 4;
        }
        else if (Mathf.Abs(theta - 60) < eps | Mathf.Abs(theta + 60) < eps)
        {
            return 6;
        }
        else
        {
            Debug.LogError("Degree of rotation was not 2, 3, 4 or 6. theta=" + theta);
            return 0;
            //throw new NotImplementedException("Degrees not matching 2, 3, 4 and 6 not implemented");
        }
    }

    // Called in SymmetryEval
    float[] LinTransform(float[,] M, float[] v)
    {
        // This function performs a linear transformation "M" on a vector "v"

        float[] u = new float[v.Length]; // Define the output vector to fill iteratively
        for (int i = 0; i < v.Length; i++) // Loop over each element in the vector (each row in v)
        {
            for (int j = 0; j < M.GetLength(0); j++) // Loop over each row j, in column i, of M (array.GetLength(0) gives number of rows, array.GetLength(1) gives number of columns)
            {
                u[j] += v[i] * M[j, i];
            }
        }
        return u;
    }

    // Overload of LinTransform, called in CreateCrystal for corner/edge/face atoms
    Vector3 LinTransform(float[,] M, Vector3 v)
    {
        // Takes in a normal (x,y,z) matrix M, and a Unity (x,z,y) Vector v
        float[] a = new float[] { v[0], v[2], v[1] }; // Swaps y and z so that the third coordinate is the vertical axis ( (x,z,y)->(x,y,z) )
        float[] b = LinTransform(M, a); // Transforms the array-vector, a, with the matrix, M. This uses the overload of LinTransform that uses arrays, where the actual transformation is perfomed

        return new Vector3(b[0], b[2], b[1]); // Swaps y and z back again ( (x,y,z) -> (x,z,y) )
    }

    // Called in CreateCrystal. Overload to work for Vector3 using "(x,z,y)"
    Vector3 PerformSymmetry(float[,] M, Vector3 v)
    {
        // This LinTransform takes a multidimensional array as a matrix and a Unity.Vector3, and transforms the vector using the matrix
        // Unity uses y is vertical and z is horisontal, but we use z as vertical and y as horisontal.
        // We therefore need to switch the vector around before and after the transform (or switch the matrix around).

        float[] a = new float[] { v[0], v[2], v[1] }; // Swaps y and z so that the third coordinate is the vertical axis ( (x,z,y)->(x,y,z) )
        float[] b = LinTransform(M, a); // Transforms the array-vector, a, with the matrix, M. This uses the overload of LinTransform that uses arrays, where the actual transformation is perfomed
        float[] translation = new float[] // Adds translation component
        {
            M[0,3], // x
            M[1,3], // y
            M[2,3], // z
        };
        translation = LinTransform(bravaisMatrix, translation); // Converts translation from bravais (a,b,c) to cartesian (x,y,z) system
        b = new float[] { b[0] + translation[0],
            b[1] + translation[1],
            b[2] + translation[2] }; // Translates b according to the translational component of M

        return new Vector3(b[0], b[2], b[1]); // Swaps y and z back again ( (x,y,z) -> (x,z,y) )
    }

    // Called in CreateCrystal
    void SetAtomColor(GameObject atom)
    {
        // Sets the color of an atom through the renderer's material by accessing a global dictionary "atomColors"
        // Uses the name of the GameObject to set the color
        string element = atom.name.Split(' ')[0]; // Gets the "First name" of the gameobject (e.g. "Si" from "Si (0,0,0)") and sets that as the element
        if (element.Contains("/"))
        {
            // We have multiple atoms with occurences
            element = element.Split('/')[0]; // For now, default to first atom, later maybe include Random.range(occ1, occ2)(or other if more than two atoms)
        }
        try
        {
            atom.GetComponent<Renderer>().material.color = atomColors[element];
        }
        catch (KeyNotFoundException) // If atom is not in the dictonary, default to "other"
        {
            atom.GetComponent<Renderer>().material.color = atomColors["other"];
        }

    }

    // Called in CreateCrystal
    void SetAtomSize(GameObject atom)
    {
        // Sets the size of an atom by accessing a global dictionary "ionicRadii"
        string element = atom.name.Split(' ')[0]; // Gets the "First name" of the gameobject (e.g. "Si" from "Si (0,0,0)") and sets that as the element
        if (element.Contains("/"))
        {
            // We have multiple atoms with occurences
            element = element.Split('/')[0]; // For now, default to first atom, later maybe include Random.range(occ1, occ2)(or other if more than two atoms)
        }
        try
        {
            atom.transform.localScale *=  (ionicRadii[element]*0.02f); // Scales the atom by its ionic radius. 
        }
        catch // If atom is not in the dictonary, default to "other"
        {
            atom.transform.localScale *=  (ionicRadii["other"]*0.02f);
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

    float StringToFloat(string str)
    {
        return float.Parse(str, System.Globalization.CultureInfo.InvariantCulture); // InvariantCulture makes sure commas and periods don't cause problems
    }

    // Called in CreateSymmetry
    GameObject CreateRotation(string parentName, float[] axis, int degOfRotation, int subscript = 0, GameObject parent = null)
    {
        // Creates a rotation / screw axis based on its axis, and the fold + subscript (3_1, 3_2, ...)
        // Takes in the whole symmetryInfo


        GameObject rotationParent = new GameObject(parentName); // Creates a parent for the symmetry, as it will be composed of the axis and symbols at the end
        rotationParent.transform.parent = parent.transform;
        rotationParent.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the gridLine is in the gridParent's (0,0,0)

        Vector3 axisVec = new Vector3(axis[0], axis[2], axis[1]); // We swap so the vector is x,z,y
        axisVec = LinTransform(bravaisMatrix, axisVec); // The axis is given in bravais coordinates, but we implement it using cartesian, so we convert it

        GameObject rotationAxis = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // Creates an axis
        rotationAxis.transform.parent = rotationParent.transform; // Sets the axis as a child
        rotationAxis.name = "Axis";

        rotationAxis.transform.localScale = new Vector3(0.3f, axisVec.magnitude / 2f, 0.3f); // Sets the length of the axis to be half the length of the vector in cartesian
        rotationAxis.transform.LookAt(axisVec); // We point the rotation axis the correct way NOTE: For some reason adding crystal.transform.position made it weird, so I removed it
        rotationAxis.transform.Rotate(90, 0, 0); // And we rotate it as cylinders have y up, but lookAt points z in the direction
        rotationAxis.transform.localPosition = axisVec / 2f; // Moves the axis so it is in the correct position

        switch(parentName.Split(' ')[0])
        {
            case "Rotation":
                rotationAxis.GetComponent<Renderer>().material.color = new Color(255 / 255f, 106 / 255f, 0f, 0.75f); // Colors the axis transparent orange

                break;
            case "Screw":

                rotationAxis.GetComponent<Renderer>().material.color = new Color(255f, 0f, 220 / 255f, 0.75f); // Colors the axis transparent magenta
                break;
        }
        ToTransparentMode(rotationAxis.GetComponent<Renderer>().material); // Sets rotation axis to Transparent mode

        // Creates image to contain symbol. Taken from: https://gamedev.stackexchange.com/questions/102431/how-to-create-gui-image-with-script with modifications
        GameObject symbol = new GameObject("Symbol"); // Creates an object for our symbol
        symbol.transform.parent = rotationParent.transform; // Sets it as a child
        symbol.transform.localPosition = new Vector3(0, 0, 0); // Fixes position to be locally zero
        symbol.transform.LookAt(axisVec + crystal.transform.position); // Makes the symbol look along the cylinder. Here we needed the crystal again. (LookAt uses worldPosition)
        symbol.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f); // Scales the Sprite down, as it is very large
        if (Mathf.Abs(subscript) < eps)
        {
            symbol.name = degOfRotation + "-fold"; // Sets the name to match the sprite
            symbol.AddComponent<SpriteRenderer>().sprite = rotationSymbols.Find(item => item.name == symbol.name); // Finds the sprite for 2-fold, adds a sprite component and sets the sprite
            symbol.SetActive(true); // Activates the GameObject
        }
        else
        {
            symbol.name = degOfRotation + "_" + subscript + "-fold"; // Sets the name to match the sprite
            symbol.AddComponent<SpriteRenderer>().sprite = rotationSymbols.Find(item => item.name == symbol.name); // Finds the sprite for 2-fold, adds a sprite component and sets the sprite
            symbol.SetActive(true); // Activates the GameObject
        }

        symbol.GetComponent<SpriteRenderer>().material.color = rotationAxis.GetComponent<Renderer>().material.color; // Sets the symbol color to match the axis color (don't need material. on left, but for consistency I added it)

        GameObject symbolEnd = Instantiate(symbol, rotationParent.transform, false); // Instantiates a symbol for the other end of the rotation axis
        symbolEnd.name = symbol.name + " end";
        symbolEnd.transform.localPosition = axisVec; // Sets position to the end of the axis

        return rotationParent;
    }

    // Called in CreateSymmetry
    GameObject CreateReflection(string parentName, float[] axis, string glideType = null, string glideDirection = null, GameObject parent = null)
    {
        // Creates a Reflection plane based on it's plane normal (axis) and type of plane. Name has format ("Glide axis glideType glideDirection")

        GameObject reflectionParent = new GameObject(parentName); // Creates a parent object to contain the mirror's front and backside
        reflectionParent.transform.parent = parent.transform;
        reflectionParent.transform.localPosition = new Vector3(0, 0, 0);

        Vector3 axisVec = new Vector3(axis[0], axis[2], axis[1]); // We swap so the vector is x,z,y
        axisVec = LinTransform(bravaisMatrix, axisVec); // The axis is given in bravais coordinates, but we implement it using cartesian, so we convert it

        GameObject reflection = GameObject.CreatePrimitive(PrimitiveType.Quad); // Creates a plane (quad~=plane)
        reflection.transform.parent = reflectionParent.transform; // Sets the axis as a child
        reflection.name = "Plane";

        reflection.transform.localScale = new Vector3(1, 1, 1) * Mathf.Pow(cellVolume, 1f / 3f); // Scales the plane to the cube of the cell volume, for now
        reflection.transform.LookAt(axisVec); // Appearently does NOT need crystal.transform
        reflection.transform.localPosition = axisVec / 2f; // Moves the axis so it is in the correct position.

        // Color plane based on mirror/glide and type
        if (glideType == null) // Mirror
        {
            reflection.GetComponent<Renderer>().material.color = new Color(1, 1, 0, 0.75f); // Transparent Yellow
        }
        else // Glide
        {
            switch (glideType)
            {
                case "n":
                    reflection.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 0.75f); // Transparent green
                    break;
                case "d":
                    reflection.GetComponent<Renderer>().material.color = new Color(112 / 255f, 209 / 255f, 244 / 255f, 0.75f); // Transparent "Ford Diamond Blue"
                    break;

                case "e":
                    reflection.GetComponent<Renderer>().material.color = new Color(1, 0, 1, 0.75f); // Transparent magenta
                    break;
                default: //a,b or c-glide
                    reflection.GetComponent<Renderer>().material.color = new Color(0, 0, 1, 0.75f); // Transparent blue
                    break;

            }
        }
        ToTransparentMode(reflection.GetComponent<Renderer>().material); // Make transparent

        GameObject reflectionBack = Instantiate(reflection, reflectionParent.transform, false); // Instantiates the backside of the plane
        reflectionBack.transform.LookAt(-axisVec + crystal.transform.position); // Appearently DOES need crystal.transform

        reflectionParent.transform.localPosition += (bravaisVectors[0] + bravaisVectors[1] + bravaisVectors[2]) / 2f; // We move the reflection plane to half the cell, so it is placed correctly

        return reflectionParent;
    }
}