using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestVertex : MonoBehaviour
{
    private Vector3 minVector, maxVector;

    // Start is called before the first frame update
    private void Start()
    {
        Mesh mesh = this.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        FindMinMax(vertices.ToList(), 0.5f);

        Debug.Log("Minimum Vector3: " + minVector);
        Debug.Log("Maximum Vector3: " + maxVector);
    }

    // This function will return the minimum and maximum Vector3 from the list
    public void FindMinMax(List<Vector3> vectorList, float gridsize)
    {
        if (vectorList == null || vectorList.Count == 0)
        {
            Debug.LogError("The vector list is empty or null!");
            minVector = maxVector = Vector3.zero;
            return;
        }
        // Initialize the min and max vectors with the first element of the list
        minVector = maxVector = vectorList[0];

        // Loop through the list to find the minimum and maximum Vector3
        for (int i = 1; i < vectorList.Count; i++)
        {
            Vector3 currentVector = vectorList[i];

            // Find the minimum values for each component (x, y, z)
            minVector.x = Mathf.Min(minVector.x, currentVector.x);
            minVector.y = Mathf.Min(minVector.y, currentVector.y);
            minVector.z = Mathf.Min(minVector.z, currentVector.z);

            // Find the maximum values for each component (x, y, z)
            maxVector.x = Mathf.Max(maxVector.x, currentVector.x);
            maxVector.y = Mathf.Max(maxVector.y, currentVector.y);
            maxVector.z = Mathf.Max(maxVector.z, currentVector.z);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a semitransparent red cube at the transforms position
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawLine(minVector, maxVector);
    }
}