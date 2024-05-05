using UnityEngine;

public class Intersect : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        CheckIntersect(GameObject.Find("Table"));
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void CheckIntersect(GameObject otherObj)
    {
        //var bounds1 = otherObj.gameObject.GetComponent<Renderer>().bounds;
        //var bounds2 = gameObject.GetComponent<Renderer>().bounds;

        foreach (Transform child in transform)
        {
            var bounds1 = child.gameObject.GetComponent<Renderer>().bounds;
            foreach (Transform child2 in otherObj.transform)
            {
                var bounds2 = child2.gameObject.GetComponent<Renderer>().bounds;
                if (bounds1.Intersects(bounds2))
                {
                    Debug.Log("Intersect");
                    Debug.Log(gameObject.name);
                    Debug.Log(child.gameObject.name);
                }
            }
        }
    }
}