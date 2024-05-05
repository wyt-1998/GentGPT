using UnityEngine;

public class Cell
{
    public Vector3Int cellPosition { get; set; }
    public string cellLabel { get; set; }
    public bool roomArea { get; set; }
    public GameObject cellObject { get; set; }
    public GameObject cellFloor { get; set; }
    public GameObject cellHighlight { get; set; }

    public Cell(Vector3Int cell_p, string cell_l)
    {
        this.cellPosition = cell_p;
        this.cellLabel = cell_l;
        this.cellObject = null;
        this.cellHighlight = null;
        this.roomArea = false;
    }
}