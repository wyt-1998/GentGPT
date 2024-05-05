using System;
using System.Collections.Generic;
using UnityEngine;

public class GridSystem : MonoBehaviour
{
    //grid setting
    [SerializeField] private Grid grid;

    [SerializeField] private float gridSize = 1f;
    [SerializeField] private Camera cam;

    //gameobject required
    [SerializeField] private GameObject indicatorPrefab, highlightPrefab, selectPrefab, floorPrefab;

    private GameObject indicator, highlight, selector;
    private Vector3Int hoveredCellVector;

    [Header("Debug")]
    [SerializeField] private bool selectionMode = true;

    [SerializeField] private bool disabledHighlight = true;

    //mouse input detection
    private bool isRMouseDown = false;

    private bool isLMouseDown = false;
    private Vector3Int initCellL;
    private Vector3Int initCellR;
    private RaycastHit hit;
    private Vector3Int previousHover;
    private GameObject highlightParent;

    //selection
    public List<Vector3Int> highlightedVec { get; set; } = new List<Vector3Int>();

    //Dictionary to store each cell infomation
    public Dictionary<Vector3Int, Cell> cellInfo { get; set; } = new Dictionary<Vector3Int, Cell>();

    //Dictionary to for name to vec

    private void Start()
    {
        //grid setting
        grid.cellSize = new Vector3(gridSize, gridSize, gridSize);
        //setup indicator to the scale of grid
        indicator = Instantiate(indicatorPrefab);
        selector = Instantiate(selectPrefab);
        selector.SetActive(false);
        indicator.SetActive(false);
        indicator.transform.localScale = new Vector3(grid.cellSize.x, grid.cellSize.y, grid.cellSize.z);
        highlight = highlightPrefab;
        highlight.transform.localScale = new Vector3(grid.cellSize.x, grid.cellSize.y, grid.cellSize.z);
        highlightParent = new GameObject("Highlight parent");
    }

    private void Update()
    {
        if (disabledHighlight)
        {
            return;
        }
        //detecting mouse ray
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 100))
        {
            if (hit.transform.name != "Plane")
            {
                if (Input.GetMouseButtonUp(0))
                {
                    isLMouseDown = false;
                    HideSelector();
                }
                if (Input.GetMouseButtonUp(1))
                {
                    isRMouseDown = false;
                    HideSelector();
                }
                indicator.gameObject.SetActive(false);
                return;
            }
            //if hit show indicator on the tiles
            indicator.gameObject.SetActive(true);
            hoveredCellVector = grid.WorldToCell(hit.point);
            indicator.transform.position = CellToWorldCenter(hoveredCellVector);
            if (selectionMode)
            {
                // On Click Left MB
                if (Input.GetMouseButtonDown(0))
                {
                    isLMouseDown = true;
                    initCellL = hoveredCellVector;
                }
                // On Click Right MB
                if (Input.GetMouseButtonDown(1))
                {
                    isRMouseDown = true;
                    initCellR = hoveredCellVector;
                }
                // On Released Left MB
                if (Input.GetMouseButtonUp(0))
                {
                    if (initCellL != Vector3Int.zero)
                    {
                        // iterate thought all the cell between the origin point and the last hover cell
                        foreach (var item in FindCellsInRectangle(hoveredCellVector, initCellL))
                        {
                            // if cell not previously is highlighted
                            if (!highlightedVec.Contains(item))
                            {
                                InitHighlightAtCell(item);
                            }
                        }
                    }
                    initCellL = Vector3Int.zero;
                    isLMouseDown = false;
                    HideSelector();
                }
                // On Released Right MB
                if (Input.GetMouseButtonUp(1))
                {
                    // iterate thought all the cell between the origin point and the last hover cell
                    if (initCellR != Vector3Int.zero)
                    {
                        foreach (var item in FindCellsInRectangle(hoveredCellVector, initCellR))
                        {
                            if (highlightedVec.Contains(item))
                            {
                                ClearCellHighlight(item);
                            }
                        }
                    }
                    initCellR = Vector3Int.zero;
                    isRMouseDown = false;
                    HideSelector();
                }
                // On Left mouse down
                if (isLMouseDown && (previousHover != hoveredCellVector))
                {
                    //show selected
                    ShowSelected(hoveredCellVector, initCellL);
                }
                // On Right mouse down
                else if (isRMouseDown && (previousHover != hoveredCellVector))
                {
                    ShowSelected(hoveredCellVector, initCellR);
                }
            }
            previousHover = hoveredCellVector;
        }
        else // if raycast not hit
        {
            indicator.gameObject.SetActive(false);
            hoveredCellVector = new Vector3Int(999, 999, 999);
        }
    }

    public Vector3Int GetGridHovering()
    {
        return hoveredCellVector;
    }

    //[Support Function] selection feature (draging mouse show the selected tiles)
    //render the selected tile in rectangle in between a and b
    private void ShowSelected(Vector3Int a, Vector3Int b)
    {
        Vector3 center = (CellToWorldCenter(a) + CellToWorldCenter(b)) / 2f;
        Vector3 scale = new Vector3(
            Mathf.Abs(CellToWorldCenter(a).x - CellToWorldCenter(b).x) + gridSize,
            selector.transform.localScale.y,
            Mathf.Abs(CellToWorldCenter(a).z - CellToWorldCenter(b).z) + gridSize
            );
        selector.SetActive(true);
        selector.transform.localScale = scale;
        selector.transform.position = center;
    }

    //hide selector obj
    private void HideSelector()
    {
        selector.SetActive(false);
    }

    // find all cell in rectangle between a and b
    private List<Vector3Int> FindCellsInRectangle(Vector3Int a, Vector3Int b)
    {
        List<Vector3Int> output = new List<Vector3Int>();
        int min_x = Math.Min(a.x, b.x);
        int max_x = Math.Max(a.x, b.x);
        int min_y = Math.Min(a.y, b.y);
        int max_y = Math.Max(a.y, b.y);
        int min_z = Math.Min(a.z, b.z);
        int max_z = Math.Max(a.z, b.z);
        for (int x = min_x; x <= max_x; x++)
        {
            for (int y = min_y; y <= max_y; y++)
            {
                for (int z = min_z; z <= max_z; z++)
                {
                    output.Add(new Vector3Int(x, y, z));
                }
            }
        }
        return output;
    }

    // get the world position of the middle of the cell from the grid cord
    public Vector3 CellToWorldCenter(Vector3Int v)
    {
        return grid.CellToWorld(v) + new Vector3(grid.cellSize.x / 2, grid.cellSize.y / 2, grid.cellSize.z / 2);
    }

    public Vector3 CellToWorldCenterBottom(Vector3Int v)
    {
        return grid.CellToWorld(v) + new Vector3(grid.cellSize.x / 2, 0, grid.cellSize.z / 2);
    }

    private void ClearCellHighlight(Vector3Int v)
    {
        highlightedVec.Remove(v);
        Destroy(GetCellfromGridCord(v).cellHighlight);
    }

    public void ClearAllHighlight()
    {
        if (highlightedVec.Count > 0)
        {
            foreach (var item in highlightedVec)
            {
                Destroy(GetCellfromGridCord(item).cellHighlight);
            }
            highlightedVec.Clear();
        }
    }

    public void InitHighlightAtCell(Vector3Int v)
    {
        var temp = Instantiate(highlight, CellToWorldCenter(v), Quaternion.identity);
        temp.transform.parent = highlightParent.transform;
        GetCellfromGridCord(v).cellHighlight = temp;
        highlightedVec.Add(v);
    }

    public void InitHighlightAtCells(List<Vector3Int> listv)
    {
        foreach (var item in listv)
        {
            InitHighlightAtCell(item);
        }
    }

    //--------------------------------------Public Function-----------------------------------------------
    //get cell info from the grid cordinate
    public Cell GetCellfromGridCord(Vector3Int v)
    {
        if (!cellInfo.ContainsKey(v)) // create a default object
        {
            var tempCell = new Cell(v, "None");
            cellInfo.Add(v, tempCell);
        }
        return cellInfo[v];
    }

    //toggle selection mode
    public void SetSelectionMode(bool mode)
    {
        if (mode) // if selection mode On
        {
            selectionMode = true;
        }
        else //if selection mode Off
        {
            selectionMode = false;
            // clear infomation on highlight and selection
            HideSelector();
            ClearAllHighlight();
        }
    }

    //toggle highlight
    public void SetHighlightMode(bool mode)
    {
        if (mode) // highlight on
        {
            disabledHighlight = false;
        }
        else //highlight off
        {
            disabledHighlight = true;
            // clear infomation on highlight and selection
            HideSelector();
            ClearAllHighlight();
        }
    }

    //get grid size
    public float GetGridSize()
    {
        return grid.cellSize.x;
    }

    public List<Vector3> GetMiddleOfTheCellCornerPostion(Vector3Int v)
    {
        List<Vector3> result = new List<Vector3>();
        var mid_cell = CellToWorldCenterBottom(v);
        result.Add(mid_cell + new Vector3(0.5f, 0, 0) * gridSize);
        result.Add(mid_cell + new Vector3(-0.5f, 0, 0) * gridSize);
        result.Add(mid_cell + new Vector3(0, 0, 0.5f) * gridSize);
        result.Add(mid_cell + new Vector3(0, 0, -0.5f) * gridSize);
        return result;
    }

    public List<Vector3Int> GetCellInProximity(Vector3Int v, int threshold, List<Vector3Int> list)
    {
        List<Vector3Int> result = new List<Vector3Int>();
        for (int x = -threshold; x <= threshold; x++)
        {
            for (int y = -threshold; y <= threshold; y++)
            {
                for (int z = -threshold; z <= threshold; z++)
                {
                    var temp = v + new Vector3Int(x, y, z);
                    if (list.Contains(temp))
                    {
                        result.Add(temp);
                    }
                }
            }
        }
        return result;
    }
}