using System.Collections.Generic;
using UnityEngine;

// One grid cell is a node
public class Node
{
    // Testing and Visualization Stuff
    public GameObject cube;
    
    public bool walkable;
    // World Position
    public Vector3 worldPos;
    // Grid Position
    public int x;
    public int y;

    // Distance from start
    public int gCost;
    // Estimated Distance to goal
    public int hCost;
    // gCost + hCost
    public int fCost;
    // Previous node
    public Node parent;
    
    // Lower fCost = better node
    
    // Creation of a node
    public Node(bool walkable, Vector3 worldPos, int x, int y)
    {
        this.walkable = walkable;
        this.worldPos = worldPos;
        this.x = x;
        this.y = y;
    }
    
    // Testing and Visualization stuff
    public void ChangeColor(Color color)
    {
        cube.GetComponent<Renderer>().material.color = color;
    }
}

public class GridScript : MonoBehaviour
{
    // Main grid where the robot moves
    public Node[,] grid;

    // Size of the grid where we can create cells
    public int gridSize = 10;

    // Cube used for visualization
    public GameObject gridCube;

    public Color walkableColor, notWalkableColor;
    
    private void Start()
    {
        // This creates the array of the grid
        grid = new Node[gridSize, gridSize];
        
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                grid[x, y] = new Node(false, new Vector3(x, 0, y), x, y);
            }
        }
        
        // int ranX = Random.Range(0, gridSize);
        // int ranY = Random.Range(0, gridSize);
        //
        // grid[ranX, ranY].ChangeColor(Color.white);
        //
        // int ranX2 = Random.Range(0, gridSize);
        // int ranY2 = Random.Range(0, gridSize);
        //
        // grid[ranX2, ranY2].ChangeColor(Color.white);
        
        // Till here all are visualization other than ranX, Y and those variables which are used to define random start and end positions
        
        // Uses pathfinder script to find a path
        // pathfinder.FindPath(grid[ranX, ranY], grid[ranX2, ranY2]);
    }

    public void CreateGrid(List<Vector2Int> walkableArea)
    {
        foreach (Vector2Int pos in walkableArea)
        {
            grid[pos.x, pos.y].walkable = true;
        }
        
        // Visualization stuff
        foreach (Node node in grid)
        {
            node.cube = Instantiate(gridCube, new Vector3(node.x, 0, node.y), Quaternion.identity, transform);
            node.ChangeColor(node.walkable ? walkableColor : notWalkableColor);
            node.cube.GetComponent<GridNodeScript>().walkable = node.walkable;
        }
    }

    // Returns all the neighbors of the given node as a list
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        int prevX = node.x - 1;
        int nextX = node.x + 1;
        int prevY = node.y - 1;
        int nextY = node.y + 1;

        foreach (Node n in grid)
        {
            // Left
            if (n.x == prevX && n.y == node.y)
            {
                neighbours.Add(n);
            }

            // Right
            if (n.x == nextX && n.y == node.y)
            {
                neighbours.Add(n);
            }
            
            // Top
            if (n.y == prevY && n.x == node.x)
            {
                neighbours.Add(n);
            }

            // Bottom
            if (n.y == nextY && n.x == node.x)
            {
                neighbours.Add(n);
            }
        }
        
        return neighbours;
    }

    public void UpdateCubeColor()
    {
        foreach (Node n in grid)
        {
            if (n.cube != null)
            {
                n.ChangeColor(n.walkable ? walkableColor : notWalkableColor);
            }
        }
    }
}
