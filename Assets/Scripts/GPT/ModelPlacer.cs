using UnityEngine;

public class ModelPlacer : MonoBehaviour
{
    [SerializeField] public bool isRB = false;
    public bool debugRun = false;

    [TextArea(5, 10)]
    [SerializeField] public string inputString;

    [TextArea(1, 5)]
    [SerializeField] public string inputLightString;

    private string[] objInfo;
    private GameObject plane;

    private static GameObject environment;
    private GameObject lights;

    // Start is called before the first frame update
    private string ExtractValueBetween(string input, string startMarker, string endMarker)
    {
        int startIndex = input.IndexOf(startMarker) + startMarker.Length;
        int endIndex = input.IndexOf(endMarker, startIndex);
        return input.Substring(startIndex, endIndex - startIndex);
    }

    private Vector3 FloatExtractToVec3(string input)
    {
        string[] components = input.Split(',', 'x');
        float x = float.Parse(components[0].Trim().Replace("m", ""));
        float y = float.Parse(components[1].Trim().Replace("m", ""));
        float z = float.Parse(components[2].Trim().Replace("m", ""));
        return new Vector3(x, y, z);
    }

    private void Start()
    {
        if (debugRun) Run();
    }

    public void Run()
    {
        Vector3 position; // absolute position
        Vector3 rotation;
        Vector3 size;

        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.position = new Vector3(0, -1, 0);
        plane.transform.localScale = new Vector3(10, 1, 10);

        inputString = inputString.ToLower();
        inputLightString = inputLightString.ToLower();
        var itemList = inputString.Split(';');
        var lightList = inputLightString.Split(';');

        lights = new GameObject("Lights");
        environment = new GameObject("Environment");

        for (int i = 0; i < itemList.Length - 1; i++)
        {
            string positionString = ExtractValueBetween(itemList[i], "position: (", ")");
            string rotationString = ExtractValueBetween(itemList[i], "rotation: (", ")");
            string sizeString = ExtractValueBetween(itemList[i], "size: (", ")");

            position = FloatExtractToVec3(positionString);
            rotation = FloatExtractToVec3(rotationString);
            size = FloatExtractToVec3(sizeString);

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            if (isRB)
            {
                var rb = cube.AddComponent<Rigidbody>();
                rb.freezeRotation = true;
            }

            cube.name = ExtractValueBetween(itemList[i], "name: ", "-");
            cube.transform.position = position;
            cube.transform.localRotation = Quaternion.Euler(rotation);
            cube.transform.localScale = size;
            cube.transform.parent = environment.transform;
        }

        for (int i = 0; i < lightList.Length - 1; i++)
        {
            string positionString = ExtractValueBetween(lightList[i], "position: (", ")");
            string rotationString = ExtractValueBetween(lightList[i], "rotation: (", ")");

            position = FloatExtractToVec3(positionString);
            rotation = FloatExtractToVec3(rotationString);

            var light = new GameObject();
            Light lightComp = light.AddComponent<Light>();
            light.name = ExtractValueBetween(lightList[i], "name: ", "-");

            light.transform.position = position;
            light.transform.localRotation = Quaternion.Euler(rotation);
            light.transform.parent = lights.transform;
        }
    }

    //[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
    //static void DrawGameObjectName(Transform transform, GizmoType gizmoType)
    //{
    //    GUIStyle style = new GUIStyle();
    //    style.normal.textColor = Color.red;
    //    if (Application.isPlaying) {
    //        if (transform.IsChildOf(environment.transform) && !transform.name.Equals(environment.transform.name)) {
    //            Handles.Label(transform.position, transform.gameObject.name, style);
    //        }
    //    }
    //}
}