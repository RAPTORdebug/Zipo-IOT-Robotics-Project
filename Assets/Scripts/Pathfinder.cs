using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public GridScript gridScript;
    
    // Takes two nodes as inputs and find a path
    public List<Node> FindPath(Node startNode, Node endNode)
    {
        bool pathFound = false;
        
        // Nodes to check
        List<Node> openSet = new List<Node>();
        // Nodes that are already checked
        List<Node> closedSet = new List<Node>();
        
        // Algorithm begins from the start
        openSet.Add(startNode);
        // 0 gCost because it's the start
        startNode.gCost = 0;
        // hCost is the estimation so we get the simple estimation using this function
        startNode.hCost = GetDistance(startNode, endNode);
        
        // Run while openSet is not empty
        while (openSet.Count > 0)
        {
            // Gets the node with the best fCost
            Node currentNode = GetBestFCost(openSet);
            
            // Check if we are currently at the end node
            if (currentNode.Equals(endNode))
            {
                pathFound = true;
                break;
            }
            
            // If not then remove current one from openSet and add to closedSet
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            
            // We get all the neighbors of the current Node using the function in gridScript
            foreach (Node n in gridScript.GetNeighbours(currentNode))
            {
                // If the node is not walkable or is already in the closed set we don't check it
                if (!n.walkable || closedSet.Contains(n))
                    continue;
                
                // Adds 1 to the gCost which means it is one more cell further from the start
                int newCost = currentNode.gCost + 1;
                
                // Checks if openSet contains the node or newCost is lower than neighbors gCost
                if (!openSet.Contains(n) || newCost < n.gCost)
                {
                    // If the above statement is true we add the current node to openSet
                    n.gCost = newCost;
                    n.hCost = GetDistance(n, endNode);
                    n.parent = currentNode;
                    openSet.Add(n);
                }
            }
        }

        if (pathFound)
        {
            Debug.Log("Path found");
        }
        else
        {
            Debug.Log("No path found");
            return new List<Node>();
        }

        // Creates a list of nodes which is the path
        List<Node> path =  new List<Node>();
        
        path.Add(endNode);
        
        Node cNode = endNode;

        while (cNode != startNode)
        {
            cNode = cNode.parent;
            path.Add(cNode);
        }
        
        
        // Reverse the path so it is going from start to end
        path.Reverse();
        
        path.RemoveAt(0);

        return path;
    }

    // Takes a list of nodes as input and checks fCost in all of the nodes to see what has the best fCost
    private Node GetBestFCost(List<Node> ns)
    {
        int index = 0;
        int fCost = ns[index].gCost + ns[index].hCost;
        
        for (int i = 0; i < ns.Count; i++)
        {
            Node n =  ns[i];
            if (n.gCost + n.hCost < fCost)
            {
                fCost = n.gCost + n.hCost;
                index = i;
            }
        }
        
        return ns[index];
    }

    // Takes a distance estimation by subtracting grid positions
    private int GetDistance(Node nodeA, Node nodeB)
    {
        int dx = Mathf.Abs(nodeA.x - nodeB.x);
        int dy = Mathf.Abs(nodeA.y - nodeB.y);

        return dx + dy;
    }
}
