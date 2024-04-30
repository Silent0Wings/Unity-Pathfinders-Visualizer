using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEditor.Progress;
[System.Serializable]
public struct UI_Elements
{
    public UnityEngine.UI.Button Trigger_button;
    public UnityEngine.UI.Slider Blocked_Slider;
    public UnityEngine.UI.Text Blocked_Text;
    public UnityEngine.UI.Slider Size_X_Slider;
    public UnityEngine.UI.Text Size_X_Text;
    public UnityEngine.UI.Slider Size_Z_Slider;
    public UnityEngine.UI.Text Size_Z_Text;
    public UnityEngine.UI.Text Steps_Text;


    public UnityEngine.UI.Toggle Blocked_Toggle;
    public UnityEngine.UI.Toggle Random_Toggle;
    public UnityEngine.UI.Toggle Diagonal_Toggle;
    public UnityEngine.UI.Toggle Shuffle_Toggle;

    public UnityEngine.UI.Dropdown Dropdown_Algo;

}
[System.Serializable]
public struct GizmosSettings
{
    [Space]
    [Header("Gizmos Settings:")]
    [Tooltip("The offset of the tiles")]
    public int Offset_Gizmos_Height;
    [Tooltip("The offset of the blocked tiles")]
    public int Offset_Gizmos_Blocked;
    [Tooltip("The size of the blocked spheres")]
    public float Blocked_Gizmos_Size;

    // Size with default values
    public Vector3 Size;

    [Tooltip("The color of the available tiles")]
    public Color Tile_Color;
    [Tooltip("The color of the final path")]
    public Color Path_Color;
    [Tooltip("The color of the start tile")]
    public Color Start_Color;
    [Tooltip("The color of the end tile")]
    public Color End_Color;
    [Tooltip("The color of the ground")]
    public Color Cube_Color;
    [Tooltip("The color of the obstacles")]
    public Color Obstacle_Color;
    [Tooltip("The color of the obstacles")]
    public Color Obstacle_Sphere_Color;
    [Tooltip("The color of the Explored Tile")]
    public Color Explored_Color;

    // Constructor with default values
    public GizmosSettings(int offsetHeight = 0, int offsetBlocked = 1, float blockedSize = 0.3f,
        Vector3? size = null, Color? tileColor = null, Color? pathColor = null,
        Color? startColor = null, Color? endColor = null, Color? cubeColor = null, Color? obstacleColor = null, Color? obstacle_sphere_Color = null, Color? exploredColor = null)
    {
        Offset_Gizmos_Height = offsetHeight;
        Offset_Gizmos_Blocked = offsetBlocked;
        Blocked_Gizmos_Size = blockedSize;

        // Assign default values if parameters are null
        Size = size ?? new Vector3(0.4f, 0.3f, 0.4f);
        Tile_Color = tileColor ?? Color.red;
        Path_Color = pathColor ?? Color.green;
        Start_Color = startColor ?? Color.yellow;
        End_Color = endColor ?? Color.cyan;
        Cube_Color = cubeColor ?? Color.blue;
        Obstacle_Color = obstacleColor ?? Color.black;
        Explored_Color = exploredColor ?? Color.cyan;
        Obstacle_Sphere_Color = obstacle_sphere_Color ?? Color.grey;
    }
}
public enum Path_Finding_Algorithm
{
    BFS,
    Bidirectional_BFS,
    DFS,
    Dijkstra,
    A_Star,
    Greedy_Best_First_Search,
    Swarm,
    Convergence_Swarm,
    Bidirectional_Swarm,
    Genetic_Algorithm
}
/// <summary>
/// this class a path finding algorithm using a grid of tiles 
/// </summary>
public class PathTraceVisual : MonoBehaviour
{
    #region General
    public Path_Finding_Algorithm Algorithm = Path_Finding_Algorithm.BFS;
    [Header("Generate a virtual Grid :")]
    [Tooltip("The size of the Grid X :")]
    private uint Size_X = 30;
    [Tooltip("The size of the Grid Z :")]
    private uint Size_Z = 30;
    public UI_Elements UI_Instance = new();
    public GizmosSettings Gizmos_Settings = new();
    #endregion
    #region Reference Lists
    [Space]
    [Header(" Reference Lists")]
    [Tooltip("The final path list")]
    public List<Node> Final_Path = new();
    [Tooltip("The grid of tiles")]
    public Dictionary<Vector3, Node> Main_Grid = new();
    public Dictionary<Vector3, Node> Virtual_Grid = new(); //a second virtual grid of the same layout to allow multiple exploration usually used for the swarm algorithm 

    #endregion
    #region Path Finding Start and End Postion
    [Space]
    [Header("Path Finding Start and End Position")]
    [Tooltip("The start position")]
    public Vector3 Path_Start = Vector3.zero;
    [Tooltip("The end position")]
    public Vector3 Path_End = new(30, 0, 30);
    #endregion
    #region Bool Control
    [Space]
    [Header("Bool Control")]
    [Tooltip("the trigger to start the path finding")]
    public bool Validate = false;
    [Tooltip("Randomise the start and end tile")]
    private bool Randomise_St_Ed = false;
    [Tooltip("Generate random obstacles")]
    private bool Generate_Blocked = false;
    [Tooltip("Allow diagonal movement")]
    private bool Diagonal = false;
    private bool Shuffle = false;
    #endregion
    #region Settings
    [Space]
    [Header("Simulation Settings")]
    [Tooltip("The minimum distance between the start and end tile")]
    public int Randomise_St_End_Minimum = 9;
    [Tooltip("The rate of the blocked tiles")]
    [Range(.1f, 10)]
    private float Blocked_Rate = 2f;
    [Tooltip("The start tile instance")]
    [SerializeField] public Node Start_Tile;
    [Tooltip("The end tile instance")]
    [SerializeField] public Node End_Tile;
    [Tooltip("The option of the direction group")]
    private int Number_of_nodes_explored = 0; //current depth of the algorithm it is cumulative meaning it represent the total number of tiles explored
    private int Search_Break_limit = 200; // how deep the algorithm can go before it stops
    // make a getter and setter for this
    public Node Bidirectional_Center = null; // the center of the swarm algorithm where the two swarms meet 
    #endregion
    #region Direction 
    //it represent the preference of directions
    [Tooltip("The direction allowed")]
    private Vector3[] The_Direction_group = new[]{ Vector3.right, Vector3.left, Vector3.forward, Vector3.back ,
       new (1, 0, 1),new (-1, 0, 1), new (1, 0, -1),  new (-1, 0, -1) };
    /*new Vector3[]{ Vector3.back, Vector3.left, Vector3.forward, Vector3.right }
    new Vector3[]{ Vector3.right, Vector3.left, Vector3.forward, Vector3.back },
    new Vector3[]{ Vector3.right, Vector3.forward, Vector3.back, Vector3.left },
    new Vector3[]{ Vector3.right, Vector3.back, Vector3.left, Vector3.forward },
    new Vector3[]{ Vector3.left, Vector3.forward, Vector3.right, Vector3.back },
    new Vector3[]{ Vector3.left, Vector3.back, Vector3.forward, Vector3.right },
    new Vector3[]{ Vector3.forward, Vector3.right, Vector3.back, Vector3.left },
    new Vector3[]{ Vector3.forward, Vector3.left, Vector3.right, Vector3.back },
    new Vector3[]{ Vector3.back, Vector3.right, Vector3.forward, Vector3.left },
    new Vector3[]{ Vector3.back, Vector3.left, Vector3.forward, Vector3.right }*/
    #endregion

    #region Singleton 
    void Start()
    {
        //activate gizmos
        SceneView.lastActiveSceneView.drawGizmos = true;

        if (UI_Instance.Trigger_button != null) // if the trigger button is not null then we add the function UI_Button() to the button calls
        {
            UI_Instance.Trigger_button.onClick.RemoveAllListeners(); // remove all the listeners from the button calls
            UI_Instance.Trigger_button.onClick.AddListener(UI_Button); // add the function UI_Button() to the list listener to the button calls
        }

        Main_Grid = new();
        Virtual_Grid = new();
    }
    void Update()
    {
        Update_Ui();
        Handle_Triger();
    }
    void OnApplicationQuit()
    {
        Validate = false;

        Final_Path.Clear();
        Main_Grid.Clear();
        Start_Tile = null;
        End_Tile = null;
    }
    #endregion
    #region Path Finding Algorithm
    /// <summary>
    /// implementation of breadth first search algorithm
    /// </summary>
    /// <param name="Have_To_Do"></param>
    private List<Node> Breadth_First_Search(List<Node> Have_To_Do)
    {
        if (Number_of_nodes_explored > Search_Break_limit) // if the number of tiles explored is bigger than the limit then we stop this algorithm
        {
            Debug.LogError("Limit Reached");// log the error
            StopAllCoroutines();// stop the algorithm
            return null;
        }

        bool Local_Path_Found = false; // local path found
        List<Node> _Final_Path = new(); //the final path to return

        foreach (Node item in Have_To_Do) // check if the end tile is in the next to do list
        {
            if (item._Position == End_Tile._Position) // if the end tile is in the next to do list then we stop the algorithm
            {
                item.Explored = true; // set the tile to explored
                Local_Path_Found = true; // set the path found to true
                _Final_Path.Add(Main_Grid[item._Position]); // add the end Tile to the final path
                break; // break the loop
            }
        }

        List<Node> Next_To_Do = new(); // next to do list of neighboring tiles if applicable
        if (!Local_Path_Found) // if the path is not found then we continue
        {
            foreach (Node Current_Tile in Have_To_Do) // for each tile in the have to do list
            {
                Current_Tile.Explored = true; // set the tile to explored
                foreach (Vector3 Current_Direction in The_Direction_group) // for each direction in the direction vector group aka movement allowed
                {
                    Vector3 New_Pos = Current_Tile._Position + Current_Direction; // calculate the new position based on the current tile and the direction allowed
                    if (Main_Grid.ContainsKey(New_Pos)) // if the new position is in the grid then we continue
                    {
                        Number_of_nodes_explored++;
                        Node Next_Tile = Main_Grid[New_Pos]; // we fetch the tile in the newly calculated position from the grid
                        if (Next_Tile.Blocked == false && Next_Tile.Explored == false) // if this tile is not blocked and was not explored then we add it to the next to do list
                        {
                            Current_Tile.Children.Add(Next_Tile); // so the new tile tile must be a child of the current tile
                            Next_Tile.Parent = Current_Tile; // and we set the parent of the new tile to the current tile
                            Next_Tile.Explored = true; // set the tile to explored
                            Next_To_Do.Add(Next_Tile); // add the tile to the next to do list

                            if (Next_Tile.Is_End_Tile)
                            {
                                _Final_Path.Add(Main_Grid[Next_Tile._Position]);
                                _Final_Path.Add(Main_Grid[Current_Tile._Position]);
                                break;
                            }
                        }
                    }
                }
            }
        }
        else // if a path is found in the have to do we return 
        {
            return _Final_Path;
        }

        foreach (Node item in Next_To_Do) // check if the end tile is in the next to do list
        {
            if (item._Position == End_Tile._Position) // if the end tile is in the next to do list then we stop the algorithm
            {
                Local_Path_Found = true; // set the path found to true
                break; // break the loop
            }
        }
        if (Local_Path_Found == false) // if the path is not found then we continue
        {
            if (Next_To_Do.Count != 0) // if there is still tiles to explore then we continue
            {
                List<Node> Path_Builder = Breadth_First_Search(Next_To_Do); // recursively call the function to induce spreading
                foreach (Node item in Next_To_Do) // check if the end tile is in the next to do list
                {
                    if (Path_Builder != null)
                    {
                        if (Path_Builder[Path_Builder.Count - 1].Parent == Main_Grid[item._Position]) // if the end tile is in the next to do list then we stop the algorithm
                        {
                            Path_Builder.Add(Main_Grid[item._Position]);
                            break; // break the loop
                        }
                    }
                }

                if (Path_Builder != null && Path_Builder.Count != 0)
                {
                    _Final_Path.AddRange(Path_Builder); //  then we add the path builder to the path
                    return _Final_Path;
                }
            }
        }
        else // if the path is found then we abandon this branch
        {
            if (_Final_Path.Count != 0 && _Final_Path != null)
            {
                return _Final_Path;
            }
        }
        return null;
    }


    /// <summary>
    /// implementation of breadth first search algorithm TODO: stack overflow for values above 75x75
    /// </summary>
    /// <param name="Have_To_Do"></param>
    private List<Node> Depth_First_Search(Node Current_Tile)
    {
        // increase the number of tiles explored
        if (Number_of_nodes_explored > Search_Break_limit) // if the number of tiles explored is bigger than the limit then we stop this algorithm
        {
            Debug.LogError("Limit Reached");// log the error
            return null;
        }

        if (Current_Tile == null || Current_Tile.Blocked)
            return null;

        Current_Tile.Explored = true; //set this node as explored
        if (Current_Tile.Is_End_Tile) // if the node passed is the the end tile then we found a path
        {
            return new List<Node>() { Current_Tile };
        }

        int num = 0;
        for (int i = 0; i < The_Direction_group.Length; i++) // decide weather every direction is explored
        {
            Vector3 New_Pos = Current_Tile._Position + The_Direction_group[i]; // calculate the new position based on the current tile and the direction allowed
            if (Main_Grid.ContainsKey(New_Pos)) // if the new position is in the grid then we continue
            {
                if (Main_Grid[New_Pos].Is_End_Tile) // if this is the the end tile then we found a path
                {
                    Number_of_nodes_explored++;
                    Node Next_Tile = Main_Grid[New_Pos]; // we fetch the tile in the newly calculated position from the grid
                    Current_Tile.Children.Add(Next_Tile); // so the new tile tile must be a child of the current tile
                    Next_Tile.Parent = Current_Tile; // and we set the parent of the new tile to the current tile
                    Next_Tile.Explored = true; // set the tile to explored
                    return new List<Node> { Main_Grid[New_Pos], Current_Tile }; // if a path is found to the end tile then we return the end node and the current node
                }
                else
                {
                    if (Main_Grid[New_Pos].Explored)
                    {
                        num++;
                    }
                }
            }
        }
        if (num == The_Direction_group.Length) // set the bool in case it is fully explored 
        {
            return null;
        }
        List<Node> Path = new(); // the path to return

        List<Node> Path_Builder = new(); // build the path part by part until we find the end tile then we add the current tile to the path
        foreach (var Current_Direction in The_Direction_group) // for each direction in the direction vector group aka movement allowed
        {
            Vector3 New_Pos = Current_Tile._Position + Current_Direction; // calculate the new position based on the current tile and the direction allowed
            if (Main_Grid.ContainsKey(New_Pos)) // if the position is in the grid and not explored then we continue
            {
                Node Next_Tile = Main_Grid[New_Pos]; // we fetch the tile in the newly calculated position from the grid
                if (Next_Tile.Blocked == false && Next_Tile.Explored == false && Next_Tile != null) // if this tile is not blocked and was not explored then we add it to the next to do list
                {
                    Number_of_nodes_explored++;
                    Current_Tile.Children.Add(Next_Tile); // so the new tile tile must be a child of the current tile
                    Next_Tile.Parent = Current_Tile; // and we set the parent of the new tile to the current tile
                    Next_Tile.Explored = true; // set the tile to explored
                    List<Node> Temp_Spread_List = null;
                    Temp_Spread_List = Depth_First_Search(Main_Grid[New_Pos]);
                    if (Temp_Spread_List != null && Temp_Spread_List.Count != 0)
                    {
                        Temp_Spread_List.Add(Current_Tile);
                        Path_Builder.AddRange(Temp_Spread_List);
                        break;// if a path is found then we break the loop since we found a path
                    }
                }
            }
        }

        if (Path_Builder != null && Path_Builder.Count != 0) // if a path have been built then we add the current tile to the path
        {
            Path_Builder.Add(Current_Tile); // we add the current tile to the path
            Path.AddRange(Path_Builder); //  then we add the path builder to the path
            return Path;
        }
        return null;
    }


    /// <summary>
    /// implementation of bidirectional breadth first search algorithm
    /// </summary>
    private void Bidirectional_Breadth_First_Search()
    {
        Dictionary<Vector3, Node> first_Start = new();
        first_Start.Add(Start_Tile._Position, Main_Grid[Start_Tile._Position]);
        Dictionary<Vector3, Node> Start_Spread = Directional_BFS_Start_Spread(first_Start);

        if (Virtual_Grid != null)
            Virtual_Grid.Clear();
        foreach (var item in Main_Grid)
        {
            Virtual_Grid.Add(item.Key, new Node(item.Value));
        }
        Dictionary<Vector3, Node> first_End = new();
        first_End.Add(End_Tile._Position, Virtual_Grid[End_Tile._Position]);
        Dictionary<Vector3, Node> End_Spread = Directional_BFS_End_Spread(first_End);


        bool Terminate = false;
        if (Start_Spread != null && End_Spread != null)
        {
            // check if they have a common tile
            foreach (var item in Start_Spread)
            {
                foreach (var item2 in End_Spread)
                {
                    if (item.Value._Position == item2.Value._Position)
                    {
                        Debug.Log("Path Found");
                        return;
                    }
                }
            }

            while (Number_of_nodes_explored < Search_Break_limit && Terminate == false)
            {

                Start_Spread = Directional_BFS_Start_Spread(Start_Spread);
                End_Spread = Directional_BFS_End_Spread(End_Spread);

                if (End_Spread == null || Start_Spread == null || End_Spread.Count == 0 || Start_Spread.Count == 0)
                    break;

                // check if they have a common tile
                foreach (var item in Start_Spread)
                {
                    foreach (var item2 in End_Spread)
                    {
                        if (item.Value._Position == item2.Value._Position)
                        {
                            Bidirectional_Center = Main_Grid[item.Value._Position];
                            Terminate = true;
                            break;
                        }
                    }
                    if (Terminate)
                        break;
                }
                if (Terminate)
                    break;
            }

        }

        if (Bidirectional_Center != null)
        {
            Debug.Log(Bidirectional_Center._Position);

            List<Node> Path_Builder_Start = new(); // build the path part by part until we find the start tile then we add the current tile to the path
            if (Main_Grid[Bidirectional_Center._Position].Parent != null)//was the end tile linked to a parent tile ?
            {
                float Start_Time = Time.time;
                Node Current_Tile = Main_Grid[Bidirectional_Center._Position];
                while (Current_Tile._Position != Start_Tile._Position) // if yes then we add the tile to the final path
                {
                    if (Current_Tile == null) // if the current tile is null then we break the loop
                    {
                        break;
                    }
                    if (Current_Tile.Parent != null || Current_Tile.Is_Start_Tile) // if the current tile has a parent then we add it to the final path
                    {
                        Current_Tile = Current_Tile.Parent; // set the current tile to the parent tile
                        Path_Builder_Start.Add(Current_Tile); // add the current tile to the final path
                    }
                    else
                    {
                        break;
                    }
                    //time break to avoid infinite loop
                    if (Time.time > Start_Time + 20) // if the time is bigger than the start time + 20 then we break the loop
                    {
                        Debug.Log("Time Limit !");
                        break;
                    }
                }
            }
            else //if not then no path was not found
            {
                Debug.Log("No Path Found  ;");
            }


            List<Node> Path_Builder_End = new(); // build the path part by part until we find the end tile then we add the current tile to the path
            Path_Builder_End.Add(Main_Grid[Bidirectional_Center._Position]); // since both path builder start and end are reversed we add the center tile to the path builder start
            if (Virtual_Grid[Bidirectional_Center._Position].Parent != null)//was the end tile linked to a parent tile ?
            {
                float Start_Time = Time.time;
                Node Current_Tile = Virtual_Grid[Bidirectional_Center._Position];
                while (Current_Tile._Position != End_Tile._Position) // if yes then we add the tile to the final path
                {
                    if (Current_Tile == null) // if the current tile is null then we break the loop
                    {
                        break;
                    }
                    if (Current_Tile.Parent != null || Current_Tile.Is_End_Tile) // if the current tile has a parent then we add it to the final path
                    {
                        Current_Tile = Current_Tile.Parent; // set the current tile to the parent tile
                        Path_Builder_End.Add(Current_Tile); // add the current tile to the final path
                    }
                    else
                    {
                        break;
                    }
                    //time break to avoid infinite loop
                    if (Time.time > Start_Time + 20) // if the time is bigger than the start time + 20 then we break the loop
                    {
                        Debug.Log("Time Limit !");
                        break;
                    }
                }
            }
            else //if not then no path was not found
            {
                Debug.Log("No Path Found  ;");
            }
            Path_Builder_Start.Reverse();

            Path_Builder_Start.AddRange(Path_Builder_End);
            Final_Path = Path_Builder_Start;
            Final_Path.Reverse();
        }

        foreach (var item in Virtual_Grid) // assign the exploration value of the virtual grid into the main one
        {
            if (item.Value.Explored)
                Main_Grid[item.Value._Position].Explored = true;
        }
    }
    /// <summary>
    /// the direction spread of the BFS algorithm from the start tile
    /// </summary>
    /// <param name="Have_To_Do"></param>
    /// <returns></returns>
    private Dictionary<Vector3, Node> Directional_BFS_Start_Spread(Dictionary<Vector3, Node> Have_To_Do)
    {
        Number_of_nodes_explored++; // increase the number of tiles explored
        /*  if (Number_of_nodes_explored > Search_Break_limit) // if the number of tiles explored is bigger than the limit then we stop this algorithm
          {
              Debug.LogError("Limit Reached");// log the error
              StopAllCoroutines();// stop the algorithm
              return null;
          }*/

        Dictionary<Vector3, Node> Next_To_Do = new();
        foreach (var Current_Tile in Have_To_Do) // for each tile in the have to do list
        {
            Current_Tile.Value.Explored = true; // set the tile to explored
            foreach (Vector3 Current_Direction in The_Direction_group) // for each direction in the direction vector group aka movement allowed
            {
                Vector3 New_Pos = Current_Tile.Value._Position + Current_Direction; // calculate the new position based on the current tile and the direction allowed
                if (Main_Grid.ContainsKey(New_Pos)) // if the new position is in the grid then we continue
                {
                    Node Next_Tile = Main_Grid[New_Pos]; // we fetch the tile in the newly calculated position from the grid
                    if (Next_Tile.Blocked == false && Next_Tile.Explored == false) // if this tile is not blocked and was not explored then we add it to the next to do list
                    {
                        Number_of_nodes_explored++;
                        Current_Tile.Value.Children.Add(Next_Tile); // so the new tile tile must be a child of the current tile
                        Next_Tile.Parent = Current_Tile.Value; // and we set the parent of the new tile to the current tile
                        Next_Tile.Explored = true; // set the tile to explored
                        Next_To_Do.Add(Next_Tile._Position, Next_Tile); // add the tile to the next to do list
                    }
                }
            }

        }
        return Next_To_Do;

    }
    /// <summary>
    /// the direction spread of the BFS algorithm from the end tile
    /// </summary>
    /// <param name="Have_To_Do"></param>
    /// <returns></returns>
    private Dictionary<Vector3, Node> Directional_BFS_End_Spread(Dictionary<Vector3, Node> Have_To_Do)
    {
        Number_of_nodes_explored++; // increase the number of tiles explored
        /*  if (Number_of_nodes_explored > Search_Break_limit) // if the number of tiles explored is bigger than the limit then we stop this algorithm
          {
              Debug.LogError("Limit Reached");// log the error
              StopAllCoroutines();// stop the algorithm
              return null;
          }*/

        Dictionary<Vector3, Node> Next_To_Do = new();
        foreach (var Current_Tile in Have_To_Do) // for each tile in the have to do list
        {
            Current_Tile.Value.Explored = true; // set the tile to explored
            foreach (Vector3 Current_Direction in The_Direction_group) // for each direction in the direction vector group aka movement allowed
            {
                Vector3 New_Pos = Current_Tile.Value._Position + Current_Direction; // calculate the new position based on the current tile and the direction allowed
                if (Virtual_Grid.ContainsKey(New_Pos)) // if the new position is in the grid then we continue
                {
                    Node Next_Tile = Virtual_Grid[New_Pos]; // we fetch the tile in the newly calculated position from the grid
                    if (Next_Tile.Blocked == false && Next_Tile.Explored == false) // if this tile is not blocked and was not explored then we add it to the next to do list
                    {
                        Number_of_nodes_explored++;
                        Current_Tile.Value.Children.Add(Next_Tile); // so the new tile tile must be a child of the current tile
                        Next_Tile.Parent = Current_Tile.Value; // and we set the parent of the new tile to the current tile
                        Next_Tile.Explored = true; // set the tile to explored
                        Next_To_Do.Add(Next_Tile._Position, Next_Tile); // add the tile to the next to do list
                    }
                }
            }

        }
        return Next_To_Do;

    }


    /// <summary>
    /// implementation of Swarm search algorithm TODO: fix the bug where the algorithm does not consider nodes in the direction >90
    /// </summary>
    /// <param name="Have_To_Do"></param>
    private List<Node> Swarm(List<Node> Have_To_Do, bool Skip)
    {
        Skip = !Skip;

        Number_of_nodes_explored++; // increase the number of tiles explored
        if (Number_of_nodes_explored > Search_Break_limit) // if the number of tiles explored is bigger than the limit then we stop this algorithm
        {
            Debug.LogError("Limit Reached");// log the error
            StopAllCoroutines();// stop the algorithm
            return null;
        }

        bool Local_Path_Found = false; // local path found
        List<Node> _Final_Path = new(); //the final path to return

        foreach (Node item in Have_To_Do) // check if the end tile is in the next to do list
        {
            if (item._Position == End_Tile._Position) // if the end tile is in the next to do list then we stop the algorithm
            {
                item.Explored = true; // set the tile to explored
                Local_Path_Found = true; // set the path found to true
                _Final_Path.Add(Main_Grid[item._Position]); // add the end Tile to the final path
                break; // break the loop
            }
        }

        List<Node> Next_To_Do = new(); // next to do list of neighboring tiles if applicable
        if (!Local_Path_Found) // if the path is not found then we continue
        {
            foreach (Node Current_Tile in Have_To_Do) // for each tile in the have to do list
            {
                Current_Tile.Explored = true; // set the tile to explored
                foreach (Vector3 Current_Direction in The_Direction_group) // for each direction in the direction vector group aka movement allowed
                {
                    Vector3 New_Pos = Current_Tile._Position + Current_Direction; // calculate the new position based on the current tile and the direction allowed
                    if (Main_Grid.ContainsKey(New_Pos)) // if the new position is in the grid then we continue
                    {
                        Node Next_Tile = Main_Grid[New_Pos]; // we fetch the tile in the newly calculated position from the grid
                        if (Next_Tile.Blocked == false && Next_Tile.Explored == false) // if this tile is not blocked and was not explored then we add it to the next to do list
                        {
                            float angle = Vector3.Angle((New_Pos - Start_Tile._Position), (End_Tile._Position - Start_Tile._Position));// find the angle between the start and end tile and the current tile and the start tile and return the angle between them

                            if (angle <= 90 || !Skip)
                            {
                                Number_of_nodes_explored++;
                                Current_Tile.Children.Add(Next_Tile); // so the new tile tile must be a child of the current tile
                                Next_Tile.Parent = Current_Tile; // and we set the parent of the new tile to the current tile
                                Next_Tile.Explored = true; // set the tile to explored
                                Next_To_Do.Add(Next_Tile); // add the tile to the next to do list

                                if (Next_Tile.Is_End_Tile)
                                {
                                    _Final_Path.Add(Main_Grid[Next_Tile._Position]);
                                    _Final_Path.Add(Main_Grid[Current_Tile._Position]);
                                    break;
                                }
                            }
                            else
                            {
                                if (Skip) // if the skip above wase not executed then we add the current tile to the next to do list since we skipped it
                                {
                                    Next_To_Do.Add(Current_Tile);
                                }
                            }
                        }

                    }
                }
            }
        }
        else // if a path is found in the have to do we return 
        {
            return _Final_Path;
        }

        foreach (Node item in Next_To_Do) // check if the end tile is in the next to do list
        {
            if (item._Position == End_Tile._Position) // if the end tile is in the next to do list then we stop the algorithm
            {
                Local_Path_Found = true; // set the path found to true
                break; // break the loop
            }
        }
        if (Local_Path_Found == false) // if the path is not found then we continue
        {
            if (Next_To_Do.Count != 0) // if there is still tiles to explore then we continue
            {
                List<Node> Path_Builder = Swarm(Next_To_Do, Skip); // recursively call the function to induce spreading

                foreach (Node item in Next_To_Do) // check if the end tile is in the next to do list
                {
                    if (Path_Builder != null)
                        if (Path_Builder[Path_Builder.Count - 1].Parent == Main_Grid[item._Position]) // if the end tile is in the next to do list then we stop the algorithm
                        {
                            Path_Builder.Add(Main_Grid[item._Position]);
                            break; // break the loop
                        }
                }

                if (Path_Builder != null && Path_Builder.Count != 0)
                {
                    _Final_Path.AddRange(Path_Builder); //  then we add the path builder to the path
                    return _Final_Path;
                }
            }
        }
        else // if the path is found then we abandon this branch
        {
            if (_Final_Path.Count != 0 && _Final_Path != null)
            {
                return _Final_Path;
            }
        }
        return null;
    }


    /// <summary>
    /// implementation of bidirectional swarm search algorithm
    /// </summary>
    private void Bidirectional_Swarm()
    {
        Dictionary<Vector3, Node> first_Start = new();
        first_Start.Add(Start_Tile._Position, Main_Grid[Start_Tile._Position]);
        Dictionary<Vector3, Node> Start_Spread = Directional_Swarm_Start_Spread(first_Start, false);

        if (Virtual_Grid != null)
            Virtual_Grid.Clear();
        foreach (var item in Main_Grid)
        {
            Virtual_Grid.Add(item.Key, new Node(item.Value));
        }
        Dictionary<Vector3, Node> first_End = new();
        first_End.Add(End_Tile._Position, Virtual_Grid[End_Tile._Position]);
        Dictionary<Vector3, Node> End_Spread = Directional_Swarm_End_Spread(first_End, false);


        bool Terminate = false;
        if (Start_Spread != null && End_Spread != null)
        {
            // check if they have a common tile
            foreach (var item in Start_Spread)
            {
                foreach (var item2 in End_Spread)
                {
                    if (item.Value._Position == item2.Value._Position)
                    {
                        Debug.Log("Path Found");
                        return;
                    }
                }
            }
            bool temp_skip = false;
            while (Number_of_nodes_explored < Search_Break_limit && Terminate == false)
            {
                Start_Spread = Directional_Swarm_Start_Spread(Start_Spread, temp_skip);
                End_Spread = Directional_Swarm_End_Spread(End_Spread, temp_skip);
                temp_skip = !temp_skip;

                if (End_Spread == null || Start_Spread == null || End_Spread.Count == 0 || Start_Spread.Count == 0)
                    break;

                // check if they have a common tile
                foreach (var item in Start_Spread)
                {
                    foreach (var item2 in End_Spread)
                    {
                        if (item.Value._Position == item2.Value._Position)
                        {
                            Bidirectional_Center = Main_Grid[item.Value._Position];
                            Debug.Log("Path Found");
                            Terminate = true;
                            break;
                        }
                    }
                    if (Terminate)
                        break;
                }
                if (Terminate)
                    break;
            }

        }

        if (Bidirectional_Center != null)
        {
            Debug.Log(Bidirectional_Center._Position);

            List<Node> Path_Builder_Start = new(); // build the path part by part until we find the start tile then we add the current tile to the path
            if (Main_Grid[Bidirectional_Center._Position].Parent != null)//was the end tile linked to a parent tile ?
            {
                float Start_Time = Time.time;
                Node Current_Tile = Main_Grid[Bidirectional_Center._Position];
                while (Current_Tile._Position != Start_Tile._Position) // if yes then we add the tile to the final path
                {
                    if (Current_Tile == null) // if the current tile is null then we break the loop
                    {
                        break;
                    }
                    if (Current_Tile.Parent != null || Current_Tile.Is_Start_Tile) // if the current tile has a parent then we add it to the final path
                    {
                        Current_Tile = Current_Tile.Parent; // set the current tile to the parent tile
                        Path_Builder_Start.Add(Current_Tile); // add the current tile to the final path
                    }
                    else
                    {
                        break;
                    }
                    //time break to avoid infinite loop
                    if (Time.time > Start_Time + 20) // if the time is bigger than the start time + 20 then we break the loop
                    {
                        Debug.Log("Time Limit !");
                        break;
                    }
                }
            }
            else //if not then no path was not found
            {
                Debug.Log("No Path Found  ;");
            }


            List<Node> Path_Builder_End = new(); // build the path part by part until we find the end tile then we add the current tile to the path
            Path_Builder_End.Add(Main_Grid[Bidirectional_Center._Position]); // since both path builder start and end are reversed we add the center tile to the path builder start
            if (Virtual_Grid[Bidirectional_Center._Position].Parent != null)//was the end tile linked to a parent tile ?
            {
                float Start_Time = Time.time;
                Node Current_Tile = Virtual_Grid[Bidirectional_Center._Position];
                while (Current_Tile._Position != End_Tile._Position) // if yes then we add the tile to the final path
                {
                    if (Current_Tile == null) // if the current tile is null then we break the loop
                    {
                        break;
                    }
                    if (Current_Tile.Parent != null || Current_Tile.Is_End_Tile) // if the current tile has a parent then we add it to the final path
                    {
                        Current_Tile = Current_Tile.Parent; // set the current tile to the parent tile
                        Path_Builder_End.Add(Current_Tile); // add the current tile to the final path
                    }
                    else
                    {
                        break;
                    }
                    //time break to avoid infinite loop
                    if (Time.time > Start_Time + 20) // if the time is bigger than the start time + 20 then we break the loop
                    {
                        Debug.Log("Time Limit !");
                        break;
                    }
                }
            }
            else //if not then no path was not found
            {
                Debug.Log("No Path Found  ;");
            }
            Path_Builder_Start.Reverse();

            Path_Builder_Start.AddRange(Path_Builder_End);
            Final_Path = Path_Builder_Start;
            Final_Path.Reverse();
        }

        foreach (var item in Virtual_Grid) // assign the exploration value of the virtual grid into the main one
        {
            if (item.Value.Explored)
                Main_Grid[item.Value._Position].Explored = true;
        }
    }
    /// <summary>
    /// the direction spread of the swarm algorithm from the start tile
    /// </summary>
    /// <param name="Have_To_Do"></param>
    /// <returns></returns>
    private Dictionary<Vector3, Node> Directional_Swarm_Start_Spread(Dictionary<Vector3, Node> Have_To_Do, bool Skip)
    {
        Number_of_nodes_explored++; // increase the number of tiles explored
        /*  if (Number_of_nodes_explored > Search_Break_limit) // if the number of tiles explored is bigger than the limit then we stop this algorithm
          {
              Debug.LogError("Limit Reached");// log the error
              StopAllCoroutines();// stop the algorithm
              return null;
          }*/

        Dictionary<Vector3, Node> Next_To_Do = new();
        foreach (var Current_Tile in Have_To_Do) // for each tile in the have to do list
        {
            Current_Tile.Value.Explored = true; // set the tile to explored
            foreach (Vector3 Current_Direction in The_Direction_group) // for each direction in the direction vector group aka movement allowed
            {
                Vector3 New_Pos = Current_Tile.Value._Position + Current_Direction; // calculate the new position based on the current tile and the direction allowed
                if (Main_Grid.ContainsKey(New_Pos)) // if the new position is in the grid then we continue
                {
                    Node Next_Tile = Main_Grid[New_Pos]; // we fetch the tile in the newly calculated position from the grid
                    if (Next_Tile.Blocked == false && Next_Tile.Explored == false) // if this tile is not blocked and was not explored then we add it to the next to do list
                    {
                        float angle = Vector3.Angle((New_Pos - Start_Tile._Position), (End_Tile._Position - Start_Tile._Position));// find the angle between the start and end tile and the current tile and the start tile and return the angle between them
                        if (angle <= 90 || !Skip)
                        {
                            Number_of_nodes_explored++;
                            Current_Tile.Value.Children.Add(Next_Tile); // so the new tile tile must be a child of the current tile
                            Next_Tile.Parent = Current_Tile.Value; // and we set the parent of the new tile to the current tile
                            Next_Tile.Explored = true; // set the tile to explored
                            Next_To_Do.Add(Next_Tile._Position, Next_Tile); // add the tile to the next to do list
                        }
                        else
                        {
                            if (Skip) // if the skip above wase not executed then we add the current tile to the next to do list since we skipped it
                            {
                            }
                        }

                    }
                }


            }

        }
        return Next_To_Do;

    }
    /// <summary>
    /// the direction spread of the swarm algorithm from the end tile
    /// </summary>
    /// <param name="Have_To_Do"></param>
    /// <returns></returns>
    private Dictionary<Vector3, Node> Directional_Swarm_End_Spread(Dictionary<Vector3, Node> Have_To_Do, bool Skip)
    {
        Number_of_nodes_explored++; // increase the number of tiles explored
        /*  if (Number_of_nodes_explored > Search_Break_limit) // if the number of tiles explored is bigger than the limit then we stop this algorithm
          {
              Debug.LogError("Limit Reached");// log the error
              StopAllCoroutines();// stop the algorithm
              return null;
          }*/

        Dictionary<Vector3, Node> Next_To_Do = new();
        foreach (var Current_Tile in Have_To_Do) // for each tile in the have to do list
        {
            Current_Tile.Value.Explored = true; // set the tile to explored
            foreach (Vector3 Current_Direction in The_Direction_group) // for each direction in the direction vector group aka movement allowed
            {
                Vector3 New_Pos = Current_Tile.Value._Position + Current_Direction; // calculate the new position based on the current tile and the direction allowed
                if (Virtual_Grid.ContainsKey(New_Pos)) // if the new position is in the grid then we continue
                {


                    Node Next_Tile = Virtual_Grid[New_Pos]; // we fetch the tile in the newly calculated position from the grid
                    if (Next_Tile.Blocked == false && Next_Tile.Explored == false) // if this tile is not blocked and was not explored then we add it to the next to do list
                    {
                        float angle = Vector3.Angle((New_Pos - Start_Tile._Position), (End_Tile._Position - Start_Tile._Position));// find the angle between the start and end tile and the current tile and the start tile and return the angle between them
                        if (angle <= 90 || !Skip)
                        {
                            Number_of_nodes_explored++;
                            Current_Tile.Value.Children.Add(Next_Tile); // so the new tile tile must be a child of the current tile
                            Next_Tile.Parent = Current_Tile.Value; // and we set the parent of the new tile to the current tile
                            Next_Tile.Explored = true; // set the tile to explored
                            Next_To_Do.Add(Next_Tile._Position, Next_Tile); // add the tile to the next to do list
                        }
                        else
                        {
                            if (Skip) // if the skip above wase not executed then we add the current tile to the next to do list since we skipped it
                            {
                            }
                        }

                    }
                }
            }

        }
        return Next_To_Do;

    }

    #endregion
    #region Operation
    /// <summary>
    /// start the path finding based on unity validation
    /// </summary>
    private void Handle_Triger()
    {
        try
        {
            if (Validate)
            {
                Full_Reset();
                if (Diagonal)
                {
                    The_Direction_group = new[]{
       new (1, 0, 1),new (-1, 0, 1), new (1, 0, -1),  new (-1, 0, -1), Vector3.right, Vector3.left, Vector3.forward, Vector3.back  };
                }// assign the diagonal direction group
                else
                {
                    The_Direction_group = new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
                }

                if (Shuffle)
                {
                    Shuffle_Direction_Vector();
                }// shuffle the direction vector group

                Algorithm_UI_Dropdown();
                if (Main_Grid.Count != Size_X * Size_Z)
                {
                    Center_Camera();
                }
                Search_Break_limit = Mathf.Clamp((int)((Size_X >= Size_Z) ? (int)Size_X : (int)Size_Z) * 10, 0, (int)Size_X * (int)Size_Z) * 50000; //limit calculation
                Establish_Grid();

                if (Main_Grid != null && Virtual_Grid != null && Start_Tile != null && End_Tile != null) // if the main dependency are not null then we continue
                {
                    Validate = !Validate;
                    if (Main_Grid.Count() != 0 && Virtual_Grid.Count() != 0) // if the grid is not empty then we continue
                    {
                        if (Start_Tile._Position != End_Tile._Position) // if the start tile is not the end tile then we continue
                        {
                            Find_Path();
                        }
                    }
                }
                else
                {
                    Validate = !Validate;
                    return;
                }
            }
        }
        catch (System.IndexOutOfRangeException)
        {
            Debug.LogError("Out Of Range Exception");
            throw;
        }
        catch (System.Exception)
        {
            Debug.LogError("Error detected");
            throw;
        }
    }
    /// <summary>
    /// Attempt to find a path from start to end
    /// </summary>
    private void Find_Path()
    {
        #region Attempt to find a path
        // write me a switch case for the algorithm
        Number_of_nodes_explored = 0;
        this.Bidirectional_Center = null;
        if (Start_Tile != null && End_Tile != null)
            switch (Algorithm)
            {
                case Path_Finding_Algorithm.BFS:
                    Final_Path = Breadth_First_Search(new List<Node> { Start_Tile });
                    break;
                case Path_Finding_Algorithm.Bidirectional_BFS:
                    Bidirectional_Breadth_First_Search();
                    break;
                case Path_Finding_Algorithm.DFS:
                    Final_Path = Depth_First_Search(Start_Tile);
                    break;
                case Path_Finding_Algorithm.Dijkstra:
                    break;
                case Path_Finding_Algorithm.A_Star:
                    break;
                case Path_Finding_Algorithm.Greedy_Best_First_Search:
                    break;
                case Path_Finding_Algorithm.Swarm:
                    Final_Path = Swarm(new List<Node> { Start_Tile }, true);
                    break;
                case Path_Finding_Algorithm.Convergence_Swarm:
                    break;
                case Path_Finding_Algorithm.Bidirectional_Swarm:
                    Bidirectional_Swarm();
                    break;
                case Path_Finding_Algorithm.Genetic_Algorithm:
                    break;
                default:
                    break;
            }

        #endregion
    }
    /// <summary>
    /// establish the start and end tile from the grid based on the nearest tile to the start and end position entered by the user btw the values are clamped to the grid size
    /// </summary>
    private void Establish_start_End()
    {
        #region Detect the start tile and the end tile from the grid
        if (Randomise_St_Ed) // if the randomise start and end is true then we randomise the start and end tile
        {
            Randomise_Start_End(); // randomise the start and end tile
        }
        else // if the randomise start and end is false then we calculate the start and end tile
        {
            float Closest_to_Start = 99999999999999;
            float Closest_to_End = 99999999999999;
            Start_Tile = null;
            End_Tile = null;

            foreach (var item in Main_Grid) // for each tile in the grid we calculate the distance between the tile and the start and end position
            {
                float Temp_Start = Vector3.Distance(item.Key, Path_Start); // calculate the distance between the tile and the start position
                float Temp_End = Vector3.Distance(item.Key, Path_End); // calculate the distance between the tile and the end position

                if (Temp_Start <= Closest_to_Start) // if the distance between the tile and the start position is smaller than the closest to start then we set the start tile to the current tile
                {
                    Closest_to_Start = Temp_Start;
                    Start_Tile = item.Value;
                }
                if (Temp_End <= Closest_to_End) // if the distance between the tile and the end position is smaller than the closest to end then we set the end tile to the current tile
                {
                    Closest_to_End = Temp_End;
                    End_Tile = item.Value;
                }
            }
            if (Start_Tile != null || End_Tile != null) // if the start tile or the end tile is not null then we set the start tile to start tile and the end tile to end tile
            {
                Start_Tile.Is_Start_Tile = true;
                End_Tile.Is_End_Tile = true;
                Main_Grid[Start_Tile._Position].Is_Start_Tile = true;
                Main_Grid[End_Tile._Position].Is_End_Tile = true;
            }
            else
            {
                Debug.LogError("Start Node Or End Node Not Found");
            }
        }

        #endregion
    }
    /// <summary>
    /// Randomise the start and end tile
    /// </summary>
    private void Randomise_Start_End()
    {
        uint break_limit = 10;
        uint limit_counter = 0;
        // Define minimum distance
        Randomise_St_End_Minimum = (int)((Size_X >= Size_Z) ? Mathf.Clamp(Randomise_St_End_Minimum, 2, Size_X) : Mathf.Clamp(Randomise_St_End_Minimum, 2, Size_Z)); // if the x is bigger than the z then the minimum is the x else it is the z

        int minDistance = Randomise_St_End_Minimum;

        // Generate a random X and Z for the start tile
        uint Rand_X = (uint)Random.Range(0, Size_X);
        uint Rand_Z = (uint)Random.Range(0, Size_Z);
        Start_Tile = Main_Grid[new Vector3(Rand_X, 0, Rand_Z)];

        // Variables for the end tile
        uint End_X = 0, End_Z = 0;
        float distance;

        do
        {
            limit_counter++;
            // Generate a random X and Z for the end tile
            End_X = (uint)Random.Range(0, Size_X);
            End_Z = (uint)Random.Range(0, Size_Z);

            // Calculate the distance between start and end tile
            distance = Vector3.Distance(new Vector3(Rand_X, 0, Rand_Z), new Vector3(End_X, 0, End_Z));
        } while (distance < minDistance && limit_counter <= break_limit);

        // Set the end tile
        End_Tile = Main_Grid[new Vector3(End_X, 0, End_Z)];

        // Mark start and end tiles
        Start_Tile.Is_Start_Tile = true;
        End_Tile.Is_End_Tile = true;


    }
    /// <summary>
    /// create tiles to fill the grid
    /// </summary>
    private void Generate_Virtual_Grid()
    {
        Main_Grid.Clear(); // clear the grid
        for (int i = 0; i < Size_X; i++) // generate a grid of tiles based on the size of the grid
        {
            for (int j = 0; j < Size_Z; j++)
            {
                Node New_Tile = new(new(i, 0, j), ("-x:" + i + "-y:" + 0 + "-z:" + j)); // create a new tile
                Main_Grid.Add(New_Tile._Position, New_Tile); // add the new tile to the grid
            }
        }
    }
    /// <summary>
    /// fill the Block_Pos with random obstacles based on the blocked rate and the size of the grid limit to 3000 iterations
    /// </summary>
    private void Generate_Random_Obstacles()
    {
        if (Start_Tile != null && End_Tile != null)
            for (int i = 0; i < Mathf.Clamp((Size_X + Size_Z) * Blocked_Rate, .1f, 2000); i++) // generate a number of blocked tiles based on the blocked rate and the size of the grid
            {
                int Random_X = Random.Range(0, (int)Size_X); // generate a random X
                int Random_Z = Random.Range(0, (int)Size_Z); // generate a random Z
                Vector3 New_Blocked = new(Random_X, 0, Random_Z); // generate a random blocked tile
                if (Start_Tile._Position == New_Blocked || End_Tile._Position == New_Blocked) // if the blocked tile is the start or the end tile then we skip it
                {
                    continue;
                }
                if (Main_Grid.ContainsKey(New_Blocked)) // if the blocked tile is in the grid then we add it to the block_pos
                {
                    if (Main_Grid[New_Blocked].Blocked) // if the blocked tile is already in the block_pos then we skip it
                    {
                        i--;
                    }
                    else // if the blocked tile is not in the block_pos then we add it to the block_pos
                    {
                        Main_Grid[New_Blocked].Blocked = true; // set the blocked tile to blocked
                    }
                }
                else
                {
                    Debug.Log("Generated A Out Of Range Blocked Position - Generate_Random_Obstacles() Line :704");
                }
            }
    }
    /// <summary>
    /// Establish the link and stores it in Final_Path
    /// </summary>
    private void Establish_Final_Path()
    {
        Final_Path = new List<Node>();
        if (End_Tile.Parent != null)//was the end tile linked to a parent tile ?
        {
            float Start_Time = Time.time;
            Node Current_Tile = End_Tile;
            while (Current_Tile != Start_Tile) // if yes then we add the tile to the final path
            {
                if (Current_Tile == null) // if the current tile is null then we break the loop
                {
                    break;
                }
                if (Current_Tile.Parent != null) // if the current tile has a parent then we add it to the final path
                {
                    Current_Tile = Current_Tile.Parent; // set the current tile to the parent tile
                    Final_Path.Add(Current_Tile); // add the current tile to the final path
                }
                else
                {
                    break;
                }
                //time break to avoid infinite loop
                if (Time.time > Start_Time + 20) // if the time is bigger than the start time + 20 then we break the loop
                {
                    Debug.Log("Time Limit !");
                    break;
                }
            }
        }
        else //if not then no path was not found
        {
            Debug.Log("No Path Found  ;");
        }
    }
    /// <summary>
    /// Depending on the type of visualisation either create a grid of tiles based on existing objects created by the user or Generate them virtually (deprecated)
    /// </summary>
    private void Establish_Grid()
    {
        Main_Grid.Clear(); // clear the grid
        Virtual_Grid.Clear();
        Generate_Virtual_Grid(); // generate a virtual grid
        Establish_start_End(); // establish the start and end tile

        if (Generate_Blocked) // if the generate blocked is true then we generate random obstacles
        {
            Generate_Random_Obstacles();
        }

        // generate a virtual grid from the main grid
        foreach (var item in Main_Grid) // assign the exploration value of the virtual grid into the main one
        {
            if (!Virtual_Grid.ContainsKey(item.Key))
                Virtual_Grid.Add(item.Key, new Node(item.Value));
        }
    }

    /// <summary>
    /// reset the final path by shallow clearing the Lists
    /// </summary>
    private void Reset_Final_Path()
    {
        //  this is called the compound null-coalescing operator it is used to check if the list is null if it is then we create a new list
        Final_Path ??= new();
        Final_Path.Clear(); // clear the final path
    }
    /// <summary>
    /// Reset all the tiles in the grid to base value except the reference sensitives values
    /// </summary>
    private void Reset_Grid_Tiles()
    {
        if (Main_Grid == null)
        {
            Main_Grid = new();
        }
        else
        {
            foreach (KeyValuePair<Vector3, Node> entry in Main_Grid)
            {
                entry.Value.LimitedClear();
            }
        }

        if (Virtual_Grid == null)
        {
            Virtual_Grid = new();
        }
        else
        {
            foreach (KeyValuePair<Vector3, Node> entry in Virtual_Grid)
            {
                entry.Value.LimitedClear();
            }
        }
    }

    /// <summary>
    /// clear all the grids available
    /// </summary>
    private void Full_Reset()
    {
        Bidirectional_Center = null;
        Start_Tile = null;
        End_Tile = null;

        Reset_Grid_Tiles();
        Reset_Final_Path();
    }
    /// <summary>
    /// Bidirectional_Center The Camera Based On The Grid Size and adjust the perspective
    /// </summary>
    private void Center_Camera()
    {
        Camera.main.transform.position = (new Vector3(Size_X / 2, 10, Size_Z / 2)); // center the camera
        Camera.main.orthographic = true; // set the camera to orthographic
        Camera.main.orthographicSize = (Size_X >= Size_Z) ? (Size_X / 2 + 5) : (Size_Z / 2 + 5); // if the x is bigger than the z then the orthographic size is the x / 2 else it is the z / 2
    }
    /// <summary>
    /// this shuffle the direction vector group
    /// </summary>
    private void Shuffle_Direction_Vector()
    {
        for (int i = The_Direction_group.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (The_Direction_group[i], The_Direction_group[j]) = (The_Direction_group[j], The_Direction_group[i]);
        }
    }
    #endregion
    #region UI
    /// <summary>
    /// Updating the ui elements of this script
    /// </summary>
    private void Update_Ui()
    {
        //updating the text of the ui elements
        UI_Instance.Blocked_Text.text = (UI_Instance.Blocked_Slider != null) ? UI_Instance.Blocked_Slider.value.ToString() : "";
        UI_Instance.Size_X_Text.text = (UI_Instance.Size_X_Slider != null) ? UI_Instance.Size_X_Slider.value.ToString() : "";
        UI_Instance.Size_Z_Text.text = (UI_Instance.Size_Z_Slider != null) ? UI_Instance.Size_Z_Slider.value.ToString() : "";

        //updating the values of the ui elements
        Size_X = (UI_Instance.Size_X_Slider != null) ? (uint)UI_Instance.Size_X_Slider.value : 10;
        Size_Z = (UI_Instance.Size_Z_Slider != null) ? (uint)UI_Instance.Size_Z_Slider.value : 10;
        Blocked_Rate = (UI_Instance.Blocked_Slider != null) ? UI_Instance.Blocked_Slider.value : 1;
        Generate_Blocked = (UI_Instance.Blocked_Toggle != null) ? UI_Instance.Blocked_Toggle.isOn : false;
        Randomise_St_Ed = (UI_Instance.Random_Toggle != null) ? UI_Instance.Random_Toggle.isOn : false;
        Diagonal = (UI_Instance.Diagonal_Toggle != null) ? UI_Instance.Diagonal_Toggle.isOn : false;
        Shuffle = (UI_Instance.Shuffle_Toggle != null) ? UI_Instance.Shuffle_Toggle.isOn : false;

        if (UI_Instance.Steps_Text != null)
        {
            UI_Instance.Steps_Text.text = Number_of_nodes_explored.ToString();
        }

    }
    /// <summary>
    /// UI button interaction that is used to start the path finding
    /// </summary>
    public void UI_Button()
    {
        Validate = true;
    }
    /// <summary>
    /// Algorithm UI Dropdown logic
    /// </summary>
    public void Algorithm_UI_Dropdown()
    {
        if (UI_Instance.Dropdown_Algo != null)
        {
            if (UI_Instance.Dropdown_Algo.options.Count != 0)
            {
                switch (UI_Instance.Dropdown_Algo.options[UI_Instance.Dropdown_Algo.value].text)
                {
                    case "BFS":
                        {
                            Algorithm = Path_Finding_Algorithm.BFS; break;
                        }
                    case "DFS":
                        {
                            Algorithm = Path_Finding_Algorithm.DFS; break;
                        }
                    case "Bidirectional_BFS":
                        {
                            Algorithm = Path_Finding_Algorithm.Bidirectional_BFS; break;
                        }
                    case "Dijkstra":
                        {
                            Algorithm = Path_Finding_Algorithm.Dijkstra; break;
                        }
                    case "A_Star":
                        {
                            Algorithm = Path_Finding_Algorithm.A_Star; break;
                        }
                    case "Greedy_Best_First_Search":
                        {
                            Algorithm = Path_Finding_Algorithm.Greedy_Best_First_Search; break;
                        }
                    case "Swarm":
                        {
                            Algorithm = Path_Finding_Algorithm.Swarm; break;
                        }
                    case "Convergence_Swarm":
                        {
                            Algorithm = Path_Finding_Algorithm.Convergence_Swarm; break;
                        }
                    case "Bidirectional_Swarm":
                        {
                            Algorithm = Path_Finding_Algorithm.Bidirectional_Swarm; break;
                        }
                    default:
                        {
                            Algorithm = Path_Finding_Algorithm.BFS; break;
                        }
                }
            }
        }
        else
        {
            Algorithm = Path_Finding_Algorithm.BFS;
        }
    }
    #endregion
    #region Visual Debug
    /// <summary>
    /// adjust the alpha of the gizmos to be more visible
    /// </summary>
    public void AdjustAlpha()
    {
        if (Gizmos_Settings.Tile_Color.a < 40f)
            Gizmos_Settings.Tile_Color = new Color(Gizmos_Settings.Tile_Color.r, Gizmos_Settings.Tile_Color.g, Gizmos_Settings.Tile_Color.b, 100f);

        if (Gizmos_Settings.Path_Color.a < 40f)
            Gizmos_Settings.Path_Color = new Color(Gizmos_Settings.Path_Color.r, Gizmos_Settings.Path_Color.g, Gizmos_Settings.Path_Color.b, 100f);

        if (Gizmos_Settings.Start_Color.a < 40f)
            Gizmos_Settings.Start_Color = new Color(Gizmos_Settings.Start_Color.r, Gizmos_Settings.Start_Color.g, Gizmos_Settings.Start_Color.b, 100f);

        if (Gizmos_Settings.End_Color.a < 40f)
            Gizmos_Settings.End_Color = new Color(Gizmos_Settings.End_Color.r, Gizmos_Settings.End_Color.g, Gizmos_Settings.End_Color.b, 100f);

        if (Gizmos_Settings.Cube_Color.a < 40f)
            Gizmos_Settings.Cube_Color = new Color(Gizmos_Settings.Cube_Color.r, Gizmos_Settings.Cube_Color.g, Gizmos_Settings.Cube_Color.b, 100f);

        if (Gizmos_Settings.Obstacle_Color.a < 40f)
            Gizmos_Settings.Obstacle_Color = new Color(Gizmos_Settings.Obstacle_Color.r, Gizmos_Settings.Obstacle_Color.g, Gizmos_Settings.Obstacle_Color.b, 100f);

        if (Gizmos_Settings.Explored_Color.a < 40f)
            Gizmos_Settings.Explored_Color = new Color(Gizmos_Settings.Explored_Color.r, Gizmos_Settings.Explored_Color.g, Gizmos_Settings.Explored_Color.b, 100f);

        if (Gizmos_Settings.Obstacle_Sphere_Color.a < 40f)
            Gizmos_Settings.Obstacle_Sphere_Color = new Color(Gizmos_Settings.Obstacle_Sphere_Color.r, Gizmos_Settings.Obstacle_Sphere_Color.g, Gizmos_Settings.Obstacle_Sphere_Color.b, 100f);
    }
    //draw the gizmos
    void OnDrawGizmos()
    {
        AdjustAlpha();
        //Draw The Simulated Cubes
        if (Main_Grid.Count != 0)
        {
            foreach (var item in Main_Grid)
            {
                if (item.Value.Blocked)
                {
                    Gizmos.color = Gizmos_Settings.Obstacle_Color;
                    Gizmos.DrawCube(item.Key, new Vector3(1, 1, 1));
                }
                else if (Bidirectional_Center != null && item.Value._Position == Bidirectional_Center._Position)
                {
                    Gizmos.color = Gizmos_Settings.End_Color;
                    Gizmos.DrawCube(item.Key, new Vector3(1, 1, 1));
                }
                else if (item.Value.Explored)
                {
                    Gizmos.color = Gizmos_Settings.Explored_Color;
                    Gizmos.DrawCube(item.Key, new Vector3(1, 1, 1));
                }
                else
                {
                    Gizmos.color = Gizmos_Settings.Cube_Color;
                    Gizmos.DrawCube(item.Key, new Vector3(1, 1, 1));
                }
            }
        }
        //Draw The Real Cubes
        if (Main_Grid.Count != 0)
        {
            foreach (var item in Main_Grid)
            {
                Gizmos.color = Gizmos_Settings.Tile_Color;
                Gizmos.DrawCube(new Vector3(item.Value._Position.x, item.Value._Position.y + Gizmos_Settings.Offset_Gizmos_Height, item.Value._Position.z), Gizmos_Settings.Size);
            }
        }
        //Draw The Start And End Tiles
        if (Start_Tile != null && End_Tile != null)
        {
            Gizmos.color = Gizmos_Settings.Start_Color;
            Gizmos.DrawSphere(new Vector3(Start_Tile._Position.x, Start_Tile._Position.y + Gizmos_Settings.Offset_Gizmos_Blocked + .1f, Start_Tile._Position.z), Gizmos_Settings.Blocked_Gizmos_Size);
            Gizmos.color = Gizmos_Settings.End_Color;
            Gizmos.DrawSphere(new Vector3(End_Tile._Position.x, End_Tile._Position.y + Gizmos_Settings.Offset_Gizmos_Blocked + .1f, End_Tile._Position.z), Gizmos_Settings.Blocked_Gizmos_Size);
        }

        //Draw The Calculated Start And End Tiles
        if (Randomise_St_Ed == false)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(new Vector3(Path_End.x, Path_End.y + Gizmos_Settings.Offset_Gizmos_Height, Path_End.z), Gizmos_Settings.Blocked_Gizmos_Size);
            Gizmos.DrawSphere(new Vector3(Path_Start.x, Path_Start.y + Gizmos_Settings.Offset_Gizmos_Height, Path_Start.z), Gizmos_Settings.Blocked_Gizmos_Size);
        }

        //Draw The Calculated Path
        if (Final_Path != null && Final_Path.Count > 0)
        {
            Gizmos.color = Gizmos_Settings.Path_Color;
            foreach (var item in Final_Path)
            {
                if (item == null)
                    continue;
                Gizmos.DrawCube(new Vector3(item._Position.x, item._Position.y + Gizmos_Settings.Offset_Gizmos_Height + .5f, item._Position.z), Gizmos_Settings.Size); ;
            }
            Node Previous = null;

            //Draws the connection from the last tile to the end tile
            if (Final_Path[0] != null)
            {
                Gizmos.DrawLine(new Vector3(Final_Path[0]._Position.x, Final_Path[0]._Position.y + Gizmos_Settings.Offset_Gizmos_Height, Final_Path[0]._Position.z), new Vector3(End_Tile._Position.x, End_Tile._Position.y + Gizmos_Settings.Offset_Gizmos_Height, End_Tile._Position.z));
            }

            int i = 0;
            foreach (var item in Final_Path)
            {
                if (item == null)
                    continue;
                if (i == 0)
                {
                    Previous = item;
                }
                else
                {
                    Gizmos.color = Gizmos_Settings.Path_Color;
                    Gizmos.DrawLine(new Vector3(Previous._Position.x, Previous._Position.y + Gizmos_Settings.Offset_Gizmos_Height, Previous._Position.z), new Vector3(item._Position.x, item._Position.y + Gizmos_Settings.Offset_Gizmos_Height, item._Position.z));
                    Previous = item;
                }
                i++;
            }
            //draws the connection from the first tile to the start tile
            if (Previous != null)
            {
                Gizmos.DrawLine(new Vector3(Previous._Position.x, Previous._Position.y + Gizmos_Settings.Offset_Gizmos_Height, Previous._Position.z), new Vector3(Start_Tile._Position.x, Start_Tile._Position.y + Gizmos_Settings.Offset_Gizmos_Height, Start_Tile._Position.z));
            }
        }
        //Draw The Blocked Areas
        if (Main_Grid.Count != 0)
        {
            Gizmos.color = Gizmos_Settings.Obstacle_Sphere_Color;
            foreach (var item in Main_Grid)
            {
                if (item.Value.Blocked)
                    Gizmos.DrawSphere(new Vector3(item.Key.x, item.Key.y + Gizmos_Settings.Offset_Gizmos_Blocked, item.Key.z), Gizmos_Settings.Blocked_Gizmos_Size);
            }
        }
        //Draw The Center Of The Bidirectional Search
        if (Bidirectional_Center != null) // this is the center of the swarm
        {
            if (Algorithm == Path_Finding_Algorithm.Bidirectional_BFS || Algorithm == Path_Finding_Algorithm.Bidirectional_Swarm)
            {
                Gizmos.color = Gizmos_Settings.Start_Color;
                Gizmos.DrawSphere(new Vector3(Bidirectional_Center._Position.x, Bidirectional_Center._Position.y + Gizmos_Settings.Offset_Gizmos_Height, Bidirectional_Center._Position.z), .4f);
            }
        }
    }
    #endregion
}