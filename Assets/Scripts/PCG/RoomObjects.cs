using System.Collections.Generic;
using UnityEngine;

public class RoomObjects
{
    public string name { get; set; }
    public Color placeholderColor { get; set; }
    public GameObject objectPrefab { get; set; }
    public Vector3Int size { get; set; }
    public string tag { get; set; }
    public int placementtype { get; set; }      // 0 side of the room

    // 1 middle of the room (on the floor)
    // 2 on the wall
    // 3 ceiling
    // 4 center of the room (for storage)
    // 5 small object
    public List<string> placeableObj { get; set; } // list of object that this object can be place on

    public RoomObjects(string name, GameObject objectprefab, Vector3Int size, int placementtype)
    {
        this.name = name;
        this.objectPrefab = objectprefab;
        this.size = size;
        this.placementtype = placementtype;
    }

    public RoomObjects(string name, GameObject objectprefab, Vector3Int size, int placementtype, List<string> placeable)
    {
        this.name = name;
        this.objectPrefab = objectprefab;
        this.size = size;
        this.placementtype = placementtype;
        this.placeableObj = placeable;
    }
}