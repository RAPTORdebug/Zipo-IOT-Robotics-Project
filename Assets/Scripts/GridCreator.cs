using System.Collections.Generic;
using UnityEngine;

// Unity stuff
// Basically it just lets you draw the correct cell diagram on top of the grid then let you place the car
public class GridCreator : MonoBehaviour
{
    private int gridSize;
    public GameObject gridCube;
    public GridScript gs;
    
    private List<GameObject> cubes = new();
    private List<Vector2Int> walkableArea = new();

    public GameObject car;

    private bool gridDone, placedCar;

    public CalibaratingScript cs;
    public MainControlScript mcs;

    private int carDirection;
    
    private int[] carRots = { 0, 90, 180, -90 };

    public GameObject starterPanel;

    private NotificationSystem notificationSystem;

    public Color selectedColor, notSelectedColor;
    
    private void Start()
    {
        notificationSystem = GetComponent<NotificationSystem>();
        starterPanel.SetActive(true);
        gridSize = gs.gridSize;
        GenerateGrid();
        notificationSystem.SendNotification("Draw the cell diagram in the grid below");
        gs.walkableColor = selectedColor;
        gs.notWalkableColor = notSelectedColor;
    }

    private void Update()
    {
        if (gridDone && !placedCar)
        {
            PlaceCar();
        }
    }

    public void PlaceCarButton()
    {
        foreach (GameObject cube in cubes)
        {
            if (cube.GetComponent<GridCubeScript>().walkable)
            {
                walkableArea.Add(new((int) cube.transform.position.x, (int) cube.transform.position.z));
            }
        }
            
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
            
        gs.CreateGrid(walkableArea);

        gridDone = true;
    }

    private void GenerateGrid()
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                GameObject c = Instantiate(gridCube, new(i, 0, j), Quaternion.identity, transform);
                c.GetComponent<GridCubeScript>().s = selectedColor;
                c.GetComponent<GridCubeScript>().ns = notSelectedColor;
                cubes.Add(c);
            }
        }
    }

    private void PlaceCar()
    {
        car.SetActive(true);
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("GridCube")))
        {
            car.transform.position = new Vector3(
                hit.transform.position.x,
                car.transform.position.y,
                hit.transform.position.z
            );

            GridNodeScript node = hit.collider.GetComponent<GridNodeScript>();

            if (node != null)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (node.walkable)
                    {
                        placedCar = true;
                        cs.enabled = true;
                        Node n = gs.grid[(int)hit.transform.position.x, (int)hit.transform.position.z];
                        mcs.carNode = n;
                        mcs.carDirection = carDirection;
                        starterPanel.SetActive(false);
                        this.enabled = false;
                    }
                    else
                    {
                        Debug.Log("Not walkable");
                    }
                }
            }
        }
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll < 0f)
        {
            carDirection = (carDirection + 3) % 4;
        }

        if (scroll > 0f)
        {
            carDirection = (carDirection + 1) % 4;
        }

        car.transform.rotation =
            Quaternion.Euler(car.transform.rotation.x, carRots[carDirection], car.transform.rotation.z);
    }
}
