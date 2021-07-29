# Crystallographic Reality
 A VR program designed to visualize and manipulate crystral structures.

## The Dream
An environment that lets you "live" in the unit cell, seeing symmetries as you look around.
The follwing list includes wanted features, some might be doable during summer, some might be done later:
- Show mirrors / inversion points etc.
  - Turn off and on induvidual symmetry elements
  - Point at connected atoms to visualize their symmetry connection
- Symmetry "sticks" on the atom
- Surfaces, polyhedrals, etc.

### Note about cif2cell's license
The GNU General Public License does not permit incorporating your program into proprietary programs. If your program is a subroutine library, you may consider it more useful to permit linking proprietary applications with the library. If this is what you want to do, use the GNU Lesser General Public License instead of this License. But first, please read <https://www.gnu.org/licenses/why-not-lgpl.html>.


## Some specific things to do
**Always document the process**
- [ ] Read and convert .cif-files to a physical unit(y) cell
  - [x] Cell_length
    - Read manually
  - [x] Cell_angle
    - Read manually
  - [x] atom_positions
    - Handled by converting .cif to .xyz
  - [ ] Ball-and-Stick model
    - [x] Ball
      - Use Dictionary to change color of the atoms
        - Might need to re-structure so atomElement and atomPos functions together
        No, just instantiate with color change immediately and apply same color for equivalentAtom as the other atom
          - Use more/evenMore then remove the same elements
          - Add a tag to the GameObject when Instantiating (if possible?)
    - [ ] Stick
      - Algorithm to create sticks (heavy work)
      - pdb has sticks. Use pdb instead of xyz?? (easier, but requires making a ConvertCiftoPDB and ReadPDB function)
  - [x] Unit Cell grid
    - Can scale down cylinders to rods (0.05,1,0.05) and place them according to Cell_length (and angle eventually)
    Create a prefab with scaling (and shader?), and Instantiate in CreateCell)() function
- [ ] Create a functioning player, with working hands
  - Figure out XR Interaction Toolkit
  - [ ] Hand assets
    - [ ] Search the web
  - [ ] Scale crystal up/down?
    - Scale XR Rig. Gives same effect, but likely easier (and can keep crystal in Å)
  - [ ] Teleport movement
  - [ ] Turn with joystick
- [ ] Add symmetries
  - [ ] Symmetry GameObjects
    - [x] Glide/mirror planes
    - [ ] Rotation/Screw axis (Rotoinversion here?)
    - [ ] Identity/Inversion Center
  - Create Algorithm
    - [ ] Hermann-Mauguin Space Group
      - [x] Lattice Type (P, I, F) etc.
      - [ ] Screw axes
      - [x] Glide planes
      - [ ] Inversion Centers
    - [ ] Adjust for different crystal systems (Triclinic -> Cubic)
  - Read directly from cif2cell output and convert symmetry matrices into mirrors and axes
    - [ ] Create matrix file to read from, and read from it
      - --print-symmetry-operations // --print-seitz-matrices > matrix.txt
    - [ ] Convert matrixes into actual symmetry (go from matrix no. X to plane/axis/center no. Y)
      - https://www.cryst.ehu.es/html/resources/bogota2018/Bogota2018_3_SymmOper_Students.pdf
        - Identity
          - Always no. 1
          - det = +1 & matrix has no -1 & ... OR ((1,0,0),(0,1,0),(0,0,1))
        - Inversion
          - det = -1 & matrix only has -1 & ... OR ((-1,0,0),(0,-1,0),(0,0-1))
        - Axis
          - rotation axis: det = +1
          - screw axis: det = +1 & translation != (0,0,0)
          - rotoinversion: det = -1 & rotation axis = -1 (not +1)
          - (-cos,  sin,  0)
          - (-sin, -cos,  0)
          - (   0,    0, -1) (for rotation around z-axis) (normal rotation has +1, rotoinversion has -1)
        - Plane
          - Mirror plane: det = -1 & matrix only has one -1
          - Glide plane: det = -1 & matrix only has one -1 & translation != (0,0,0)
