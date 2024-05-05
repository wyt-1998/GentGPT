using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ObjectPrep : MonoBehaviour
{
    [SerializeField] private Material modelShader;
    [SerializeField] private Vector3 offsetRotation;

    public Tuple<GameObject, Vector3Int> PrepObjectAndBoundingBox(GameObject g, float gridsize, int type, float rotation)
    {
        if (g == null)
        {
            return null;
        }
        var model = g.transform.GetChild(0).gameObject;
        var mtransform = model.transform;
        model.transform.localRotation = Quaternion.Euler(mtransform.localRotation.eulerAngles.x, rotation, mtransform.localRotation.eulerAngles.z);
        var temp = mtransform.GetChild(0);
        temp.AddComponent<MeshCollider>();
        temp.GetComponent<MeshCollider>().sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
        temp.GetComponent<MeshCollider>().convex = true;

        var dia = FindDimention(model);
        Vector3Int boundingBoxSize = new Vector3Int(Mathf.CeilToInt(dia.x / gridsize), Mathf.CeilToInt(dia.y / gridsize), Mathf.CeilToInt(dia.z / gridsize));

        //translate to the side
        // move to center
        model.transform.localPosition = new Vector3(0, dia.y / 2, 0);
        if (type == 0 || type == 2)
        {
            //move it to the forward direction
            var offset = (((float)boundingBoxSize.z / 2) * gridsize - (dia.z / 2));
            model.transform.localPosition = new Vector3(0, (gridsize * boundingBoxSize.y) - (dia.y / 2), offset);
        }
        if (type == 3)
        {
            var offset = (((float)boundingBoxSize.y) * gridsize - (dia.y / 2));
            model.transform.localPosition = new Vector3(0, offset, 0);
        }
        return new Tuple<GameObject, Vector3Int>(g, boundingBoxSize);
    }

    public Vector3 FindDimention(GameObject model)
    {
        //set scale to 1 to check
        var save_scale = model.transform.localScale;
        model.transform.localScale = new Vector3(1, 1, 1);

        List<Vector3> vertices = new List<Vector3>();
        for (int i = 0; i < model.transform.childCount; i++)
        {
            GameObject child = model.transform.GetChild(i).gameObject;
            Mesh mesh = child.GetComponent<MeshFilter>().sharedMesh;
            foreach (var item in mesh.vertices)
            {
                vertices.Add(child.transform.TransformPoint(item));
            }
        }
        Vector3 minVector, maxVector;

        if (vertices == null || vertices.Count == 0)
        {
            Debug.LogError("The vector list is empty or null!");
            minVector = maxVector = Vector3.zero;
            return Vector3.zero;
        }
        // Initialize the min and max vectors with the first element of the list
        minVector = maxVector = vertices[0];

        // Loop through the list to find the minimum and maximum Vector3
        for (int i = 1; i < vertices.Count; i++)
        {
            Vector3 currentVector = vertices[i];

            // Find the minimum values for each component (x, y, z)
            minVector.x = Mathf.Min(minVector.x, currentVector.x);
            minVector.y = Mathf.Min(minVector.y, currentVector.y);
            minVector.z = Mathf.Min(minVector.z, currentVector.z);

            // Find the maximum values for each component (x, y, z)
            maxVector.x = Mathf.Max(maxVector.x, currentVector.x);
            maxVector.y = Mathf.Max(maxVector.y, currentVector.y);
            maxVector.z = Mathf.Max(maxVector.z, currentVector.z);
        }
        float ydiff = maxVector.y - minVector.y;
        float xdiff = maxVector.x - minVector.x;
        float zdiff = maxVector.z - minVector.z;

        model.transform.localScale = save_scale;
        return new Vector3(xdiff * save_scale.x, ydiff * save_scale.y, zdiff * save_scale.z);
    }

    //input a
    public GameObject PrepObjSize(GameObject g, float height)
    {
        if (g == null)
        {
            return null;
        }
        //TEMP CODE Replace with GPT generated answer
        if (height == 0)
        {
            height = 2f;
        }

        var model = g.transform.GetChild(0).gameObject;

        //reset the scale
        model.transform.localScale = new Vector3(1, 1, 1);
        Vector3 modelDiamention = FindDimention(model);

        var scale_factor = height / modelDiamention.y;
        model.transform.localScale = new Vector3(scale_factor, scale_factor, scale_factor);
        return g;
    }

    public GameObject PrepObjectStructure(GameObject g)
    {
        var model = new GameObject("model");
        var parent = new GameObject(g.name);
        g.transform.parent = model.transform;
        model.transform.parent = parent.transform;
        return parent;
    }

    public GameObject LoadModel(string objname, string varient)
    {
        //AssetDatabase.Refresh();
        Debug.Log("loading model of " + objname + " varient of " + varient);
        var objs = new List<UnityEngine.Object>(Resources.LoadAll<UnityEngine.Object>(objname));
        UnityEngine.Object loadedPrefab = null;
        for (int i = objs.Count - 1; i >= 0; i--)
        {
            if (i % 2 != 0) objs.RemoveAt(i);
        }
        foreach (var item in objs)
        {
            if (item.name == varient)
            {
                loadedPrefab = item;
            }
        }
        var g = (GameObject)Instantiate(loadedPrefab);
        g.transform.localRotation = Quaternion.Euler(offsetRotation);
        g.GetComponent<MeshFilter>().mesh = MakeReadableMeshCopy(g.GetComponent<MeshFilter>().mesh);
        g.GetComponent<Renderer>().material = modelShader;
        g = PrepObjectStructure(g);
        g.transform.position = new Vector3(999, 999, 999);
        return g;
    }

    public GameObject LoadModel(string objname)
    {
        //AssetDatabase.Refresh();
        Debug.Log("loading model of " + objname);
        var objs = new List<UnityEngine.Object>(Resources.LoadAll<UnityEngine.Object>(objname));
        UnityEngine.Object loadedPrefab = null;
        for (int i = objs.Count - 1; i >= 0; i--)
        {
            if (i % 2 != 0) objs.RemoveAt(i);
        }
        loadedPrefab = objs[0];
        var g = (GameObject)Instantiate(loadedPrefab);
        g.transform.localRotation = Quaternion.Euler(offsetRotation);
        g.GetComponent<MeshFilter>().mesh = MakeReadableMeshCopy(g.GetComponent<MeshFilter>().mesh);
        g.GetComponent<Renderer>().material = modelShader;
        g = PrepObjectStructure(g);
        g.transform.position = new Vector3(999, 999, 999);
        return g;
    }

    public Tuple<GameObject, Vector3Int> CompletePrep(string obj_name, string obj_variation, float size, float rotation, int obj_type, float gridsize)
    {
        var model = LoadModel(obj_name, obj_variation);
        PrepObjSize(model, size);
        var result = PrepObjectAndBoundingBox(model, gridsize, obj_type, rotation);
        return result;
    }

    public Tuple<GameObject, Vector3Int> CompletePrepAuto(string obj_name, float size, int obj_type, float gridsize)
    {
        //if autogen on
        //- get size from gpt
        //- get first varient
        //- rotation = 0

        var model = LoadModel(obj_name);
        PrepObjSize(model, size);
        var result = PrepObjectAndBoundingBox(model, gridsize, obj_type, 0);
        return result;
    }

    public Tuple<GameObject, Vector3Int> CompletePrepAuto(string obj_name, float size, float rotation, int obj_type, float gridsize)
    {
        //if autogen on
        //- get size from gpt
        //- get first varient
        //- rotation = 0

        var model = LoadModel(obj_name);
        PrepObjSize(model, size);
        var result = PrepObjectAndBoundingBox(model, gridsize, obj_type, rotation);
        return result;
    }

    //reference https://forum.unity.com/threads/reading-meshes-at-runtime-that-are-not-enabled-for-read-write.950170/
    public static Mesh MakeReadableMeshCopy(Mesh nonReadableMesh)
    {
        Mesh meshCopy = new Mesh();
        meshCopy.indexFormat = nonReadableMesh.indexFormat;

        // Handle vertices
        GraphicsBuffer verticesBuffer = nonReadableMesh.GetVertexBuffer(0);
        int totalSize = verticesBuffer.stride * verticesBuffer.count;
        byte[] data = new byte[totalSize];
        verticesBuffer.GetData(data);
        meshCopy.SetVertexBufferParams(nonReadableMesh.vertexCount, nonReadableMesh.GetVertexAttributes());
        meshCopy.SetVertexBufferData(data, 0, 0, totalSize);
        verticesBuffer.Release();

        // Handle triangles
        meshCopy.subMeshCount = nonReadableMesh.subMeshCount;
        GraphicsBuffer indexesBuffer = nonReadableMesh.GetIndexBuffer();
        int tot = indexesBuffer.stride * indexesBuffer.count;
        byte[] indexesData = new byte[tot];
        indexesBuffer.GetData(indexesData);
        meshCopy.SetIndexBufferParams(indexesBuffer.count, nonReadableMesh.indexFormat);
        meshCopy.SetIndexBufferData(indexesData, 0, 0, tot);
        indexesBuffer.Release();

        // Restore submesh structure
        uint currentIndexOffset = 0;
        for (int i = 0; i < meshCopy.subMeshCount; i++)
        {
            uint subMeshIndexCount = nonReadableMesh.GetIndexCount(i);
            meshCopy.SetSubMesh(i, new SubMeshDescriptor((int)currentIndexOffset, (int)subMeshIndexCount));
            currentIndexOffset += subMeshIndexCount;
        }

        // Recalculate normals and bounds
        meshCopy.RecalculateNormals();
        meshCopy.RecalculateBounds();

        return meshCopy;
    }
}