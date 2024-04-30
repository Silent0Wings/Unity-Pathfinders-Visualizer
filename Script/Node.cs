using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Node class for storing the tile information
/// </summary>  
public class Node
{
    #region Properties
    [Header("Node Properties")]
    [Tooltip("The name of the tile.")]
    public string Name;
    [Tooltip("The parent of the tile.")]
    public Node Parent;
    [Tooltip("The children of the tile.")]
    public List<Node> Children = new List<Node>();
    [Tooltip("Whether the tile is blocked or not.")]
    public bool Blocked;
    [Tooltip("Whether the tile is explored or not.")]
    public bool Explored;
    [Tooltip("Whether the tile is in a start or not.")]
    public bool Is_Start_Tile;
    [Tooltip("Whether the tile is in a an end tile or not.")]
    public bool Is_End_Tile;
    [Tooltip("The position of the tile.")]
    public Vector3 _Position;
    #endregion

    #region Constructors
    /// <summary>
    /// default constructor
    /// </summary>
    public Node()
    {
        Name = "";
        Parent = null;
        Children.Clear();
        Blocked = false;
        Explored = false;
        Is_Start_Tile = false;
        Is_End_Tile = false;
        _Position = Vector3.zero;
    }
    /// <summary>
    /// a copy constructor
    /// </summary>
    /// <param name="temp_Node"></param>
    public Node(Node temp_Node)
    {
        Name = temp_Node.Name;
        Parent = temp_Node.Parent;
        Children = temp_Node.Children;
        Blocked = temp_Node.Blocked;
        Explored = temp_Node.Explored;
        Is_Start_Tile = temp_Node.Is_Start_Tile;
        Is_End_Tile = temp_Node.Is_End_Tile;
        _Position = temp_Node._Position;
    }

    /// <summary>
    /// tile constructor takes in a position and a name
    /// </summary>
    /// <param name="temp_Pos"></param>
    /// <param name="temp_Name"></param>
    public Node(Vector3 temp_Pos, string temp_Name)
    {
        Parent = null;
        Children.Clear();
        Blocked = false;
        Explored = false;
        Is_Start_Tile = false;
        Is_End_Tile = false;
        _Position = temp_Pos;
        Name = temp_Name;
    }
    #endregion

    #region functions
    /// <summary>
    /// limited clear
    /// </summary>
    public void LimitedClear()
    {
        Parent = null;
        Children.Clear();
        Blocked = false;
        Explored = false;
        Is_Start_Tile = false;
        Is_End_Tile = false;
    }
    /// <summary>
    /// Fully clears the tile.
    /// </summary>
    public void Full_clear()
    {
        Parent = null;
        Name = "";
        Children.Clear();
        Blocked = false;
        Explored = false;
        Is_Start_Tile = false;
        Is_End_Tile = false;
        _Position = Vector3.zero;
    }
    #endregion
}