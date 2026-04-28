using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainControlScript : MonoBehaviour
{
    public GridScript gs;
    public Pathfinder pf;

    public Transform car;
    public Vector3 carPosition;
    
    public Node carNode;
    private Node targetNode;

    public int carDirection = 0;
    private int[] carRots = { 0, 90, 180, -90 };
    
    public List<int> outputPath = new List<int>();

    private bool onHold;

    public FirebaseManager fm;

    public Vector3Int realtimeCarPosition;
    public int realtimeCarDirection;

    private bool pathGenerated;

    public float rotationSpeed;
    public float movementSpeed;
    
    private NotificationSystem notificationSystem;

    private bool driving;
private bool obstacleDetected;

    public Button resetButton;
    public GameObject mainWindowPanel;

    public Color hoverColor, targetNodeColor;

    private void Start()
    {
        notificationSystem = GetComponent<NotificationSystem>();

        notificationSystem.SendNotification("Click on a cell to send the robot to that cell");

        Debug.Log("Now Set Destination");
        carPosition = car.position;
        carPosition.y = 0;

        fm.SetInt("car/carDirection", carDirection, () => { });
        fm.SetInt("car/xPos", carNode.x, () => { });
        fm.SetInt("car/yPos", carNode.y, () => { });

        fm.SetBool("car/update", true);

        Debug.Log(carDirection);

        mainWindowPanel.SetActive(true);
        resetButton.interactable = true;
    }

    private void Update()
    {
        if (driving)
        {
            fm.ReadBool("car/drive", (d) =>
            {
                if (!d)
                {
                    if (!obstacleDetected)
                    {
                        Debug.Log("Went to path");
                        notificationSystem.SendNotification("Destination Reached");
                        gs.UpdateCubeColor();
                        pathGenerated = true;
                        onHold = false;
                        driving = false;
                        resetButton.interactable = true;
                    }
                }
            });
        }
        
        if (!onHold)
        {
            SelectDestination(); 
        }
        
        GetRealtimePositionData();
        
        if (targetNode != null && onHold && !pathGenerated)
        {
            GeneratePath();
        }
    }

    private void GeneratePath()
    {
        outputPath.Clear();
        Debug.Log(carNode.x);
        Debug.Log(carNode.y);
        List<Node> ns = pf.FindPath(carNode, targetNode);
        targetNode.ChangeColor(targetNodeColor);
        int cn = 0;
        int iterations = 0;
        Debug.Log("Reached Here");
        while (cn < ns.Count && iterations < 10000)
        {
            Vector3 nextCellPosition = new(ns[cn].x, 0, ns[cn].y);
            Debug.Log($"Next Cell X: {nextCellPosition.x}, Next Cell Z: {nextCellPosition.z}");

            if (GetCarNextCell("front") == nextCellPosition)
            {
                outputPath.Add(1);
                carPosition = GetCarNextCell("front");
                carNode = gs.grid[(int)carPosition.x, (int)carPosition.z];
                if (carPosition == new Vector3(targetNode.x, 0, targetNode.y))
                {
                    // This is where we add to firebase and shit and then it's done ok?
                    Debug.Log("Path Output Done");
                    fm.SetBool("car/obstacleInFront", false);
                    obstacleDetected = false;
                    fm.SetPathAndDrive(outputPath.ToArray(), () =>
                    {
                        driving = true;
                    });
                    break;
                }

                cn++;
            }
            else
            {
                if (GetCarNextCell("right") == nextCellPosition)
                {
                    outputPath.Add(2);
                    carDirection = (carDirection + 1) % 4;
                }
                else if (GetCarNextCell("left") == nextCellPosition)
                {
                    outputPath.Add(3);
                    carDirection = (carDirection + 3) % 4;
                }
                else if (GetCarNextCell("back") == nextCellPosition)
                {
                    outputPath.Add(2);
                    outputPath.Add(2);
                    carDirection = (carDirection + 1) % 4;
                    carDirection = (carDirection + 1) % 4;
                }
                else
                {
                    Debug.LogError("Something is wrong");
                }
            }

            iterations++;
        }
    }

    private void GetRealtimePositionData()
    {
        fm.ReadInt("car/carDirection", (value) =>
        {
            realtimeCarDirection = value;
            car.rotation = Quaternion.RotateTowards(car.rotation,
                Quaternion.Euler(car.rotation.x, carRots[realtimeCarDirection], car.rotation.z), rotationSpeed);
        });
        fm.ReadInt("car/xPos", (x) =>
        {
            fm.ReadInt("car/yPos", (y) =>
            {
                realtimeCarPosition = new Vector3Int(x, 0, y);
                car.position = Vector3.MoveTowards(car.position, realtimeCarPosition, movementSpeed);
            });
        });

        fm.ReadBool("car/obstacleInFront", (b) =>
        {
            if (b)
            {
                obstacleDetected = true;
                
                resetButton.interactable = false;
                
                notificationSystem.SendNotification("Obstacle Detected");
                
                Vector3 nextCell = new();

                switch (realtimeCarDirection)
                {
                    case 0:
                        nextCell = realtimeCarPosition + new Vector3(0, 0, 1); // north
                        break;
                    case 1:
                        nextCell = realtimeCarPosition + new Vector3(1, 0, 0); // east
                        break;
                    case 2:
                        nextCell = realtimeCarPosition + new Vector3(0, 0, -1); // south
                        break;
                    case 3:
                        nextCell = realtimeCarPosition + new Vector3(-1, 0, 0); // west
                        break;
                }

                gs.grid[(int)nextCell.x, (int)nextCell.z].cube.GetComponent<GridNodeScript>().walkable = false;
                gs.grid[(int)nextCell.x, (int)nextCell.z].walkable = false;

                carNode = gs.grid[realtimeCarPosition.x, realtimeCarPosition.z];

                carDirection = realtimeCarDirection;
                carPosition = realtimeCarPosition;
                
                gs.UpdateCubeColor();

                GeneratePath();
            }
        });
    }

    private Vector3 GetCarNextCell(string side)
    {
        int dir = carDirection;

        if (side == "front") dir = carDirection;
        if (side == "right") dir = (carDirection + 1) % 4;
        if (side == "left") dir = (carDirection + 3) % 4;
        if (side == "back") dir = (carDirection + 2) % 4;

        switch (dir)
        {
            case 0: return carPosition + new Vector3(0, 0, 1);  // north
            case 1: return carPosition + new Vector3(1, 0, 0);  // east
            case 2: return carPosition + new Vector3(0, 0, -1); // south
            case 3: return carPosition + new Vector3(-1, 0, 0); // west
        }

        return carPosition;
    }

    private void SelectDestination()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("GridCube")))
        {
            GridNodeScript node = hit.collider.GetComponent<GridNodeScript>();
            
            gs.UpdateCubeColor();

            if (node != null)
            {
                gs.grid[(int) hit.transform.position.x,  (int) hit.transform.position.z].ChangeColor(hoverColor);
                if (Input.GetMouseButtonDown(0))
                {
                    if (node.walkable)
                    {
                        resetButton.interactable = false;
                        Debug.Log("Destination Selected");
                        targetNode = gs.grid[(int) hit.transform.position.x,  (int) hit.transform.position.z];
                        Debug.Log($"Target X: {targetNode.x}, Target Y: {targetNode.y}");
                        pathGenerated = false;
                        onHold = true;
                    }
                    else
                    {
                        Debug.Log("Not walkable");
                    }
                }
            }
        }
        else
        {
            gs.UpdateCubeColor();
        }
    }

    public void Redraw()
    {
        SceneManager.LoadScene("MainScene");
    }
}
