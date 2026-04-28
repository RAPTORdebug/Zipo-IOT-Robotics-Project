using UnityEngine;

// Sets colors for the grid cubes
public class GridCubeScript : MonoBehaviour
{
    public bool walkable;

    private Material mat;

    public Color s, ns;
    
    private void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        if (walkable)
        {
            mat.color = s;
        }
        else
        {
            mat.color = ns;
        }
    }
    
    private void OnMouseOver()
    {
        if (Input.GetMouseButton(0))
        {
            walkable = true;
        }

        if (Input.GetMouseButton(1))
        {
            walkable = false;
        }
    }
}
