using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Fbx;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;
using Random = UnityEngine.Random;

public class ProceduralGen : MonoBehaviour
{
    #region Singleton

    public static ProceduralGen Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion Singleton

    #region Serializable Fields

    [SerializeField] private TMP_Text numSelectedUI;

    [Header("Camera Position")]
    [SerializeField] private Transform mainCamPosition;

    [SerializeField] private Transform[] objectViewCamPosition;

    [SerializeField] private Transform[] objectViewscalePosition;
    [SerializeField] private TMP_Text sizeUI;
    [SerializeField] private GameObject scaleprefab;

    [Header("UI Scene")]
    [SerializeField] private GameObject roomSpaceUI;

    [SerializeField] private GameObject promptUI;
    [SerializeField] private GameObject objectViewUI;
    [SerializeField] private GameObject preplacedUI;
    [SerializeField] private GameObject genUI;

    [Header("UI element")]
    [SerializeField] private GameObject setPrefab;
    [SerializeField] private TextMeshProUGUI objectViewRefreshText;
    [SerializeField] private TextMeshProUGUI objectViewSkipText;

    [Header("Materials")]
    [SerializeField] private List<Material> floorMaterial;

    [Header("model for testing")]
    [SerializeField] private GameObject wallPrefab;

    [SerializeField] private float wallHeight;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private int gridHeight;
    [SerializeField] private GameObject preplacedCubePrefab;
    [SerializeField] private Shader preplaceShader;
    [SerializeField] private GameObject indipoint;

    [Header("model object temporary")]
    [SerializeField] private GameObject chair;

    [SerializeField] private GameObject table;
    [SerializeField] private GameObject wardrobe;
    [SerializeField] private GameObject pictureframe;

    [Header("Setting")]
    [SerializeField] private int distanceThresholdWall = 1;

    [SerializeField] private int distanceThresholdCeiling = 1;

    #endregion Serializable Fields

    #region Private Fields

    private GridSystem gridSystem;
    private GPT gptSystem;
    private ShapE shapE;
    private ObjectPrep objprep;

    private GameObject wallParent;
    private GameObject roomGenerated;
    private GameObject scalegameobj;
    private GameObject currentobjectviewing;
    private Transform currentobjectviewingTransform;

    private int objectviewindextoggle = 0;
    private float currentHeight;
    private string roomDimension;

    private TMP_Dropdown preplaceDropDown;
    private TMP_Dropdown genmodeDropDown;
    private TMP_Dropdown objectSelectDropDown;
    private TMP_Dropdown objectVariationDropDown;
    private TMP_Dropdown objectTypeDropDown;
    private Slider roomHeightSlider;
    private Slider objectViewSizeSlider;
    private Slider objectViewRotationSlider;
    private Toggle objectViewAutoToggle;

    private string currentViewObj = null;
    private float currentViewObjSize = 0;
    private float currentViewObjRotation = 0;
    private string currentViewVariation = null;
    private bool objectviewing;
    private bool autoGenAll = false;

    private List<GameObject> setParent = new List<GameObject>();
    #endregion Private Fields

    #region Public fields

    public Dictionary<string, Tuple<float, int, List<string>, int, List<Vector3>>> gptResult { get; set; } = new ();

    public bool objectreqCompleted { get; set; }
    public bool gptreqCompleted { get; set; }

    #endregion Public fields

    #region List and Dicts

    // dictionary and list to store infomation

    // facing of wall, list of wall gameobject
    private Dictionary<string, List<GameObject>> wallStored = new Dictionary<string, List<GameObject>>();

    // cell vector, floor gameobject
    public Dictionary<Vector3Int, GameObject> floorStored = new Dictionary<Vector3Int, GameObject>();

    // 0 side of the room
    // 1 middle of the room (on the floor)
    // 2 on the wall
    // 3 ceiling
    // 4 center of the room (for storage)
    private List<List<Vector3Int>> gridCategories = new List<List<Vector3Int>>();

    //------------------------------------------------------------------

    // store preplaced object information
    // cell vector, object ID
    private Dictionary<Vector3Int, string> preplacedInfoStored = new Dictionary<Vector3Int, string>();

    // store the gameobject of the preplaced object
    private Dictionary<Vector3Int, GameObject> preplaceHighlightGameobject = new Dictionary<Vector3Int, GameObject>();

    // room object that are currently avilable
    // object ID, room object class
    private List<RoomObjects> roomObjectAvailable = new List<RoomObjects>();

    // number of each object to be placed in the room
    //private List<int> objectNumber = new List<int>();
    private Dictionary<RoomObjects, int> objectNumber = new Dictionary<RoomObjects, int>();

    // store tmp inputfield for each object
    private Dictionary<int, TMP_InputField> objectInputField = new Dictionary<int, TMP_InputField>();

    // info stored from object viewing and editing  tuple of size,varation name,rotation in y axis
    private Dictionary<string, Tuple<float, string, float, bool,int>> objectSelectionInfo = new ();

    // store current object in the room information
    private List<Tuple<RoomObjects, GameObject, List<Vector3Int>, Vector3Int>> allObjPlaced = new List<Tuple<RoomObjects, GameObject, List<Vector3Int>, Vector3Int>>();

    private Tuple<Vector2Int, Vector2Int> roomDimentionAndOffset;

    #endregion List and Dicts

    #region Unity Start and Update

    private void Start()
    {
        wallStored.Add("x+", new List<GameObject>());
        wallStored.Add("x-", new List<GameObject>());
        wallStored.Add("z+", new List<GameObject>());
        wallStored.Add("z-", new List<GameObject>());

        gridSystem = GetComponent<GridSystem>();
        objprep = GetComponent<ObjectPrep>();
        shapE = GetComponent<ShapE>();
        gptSystem = GameObject.Find("GPTSystem").GetComponent<GPT>();

        roomGenerated = new GameObject("Room Generated");
        genmodeDropDown = genUI.transform.Find("Dropdown").gameObject.GetComponent<TMP_Dropdown>();
        roomHeightSlider = roomSpaceUI.transform.Find("Height Slider").gameObject.GetComponent<Slider>();

        wallParent = new GameObject("Wall Parent");
        RoomGeneratedParent(wallParent);
        if (genmodeDropDown == null)
        {
            Debug.Log("can not find genmodeDropDown");
        }
        currentHeight = wallHeight;
        gridHeight = Mathf.CeilToInt(currentHeight / gridSystem.GetGridSize());
        objectviewing = false;

        var floorMaterialList = new List<string>();
        foreach (var item in floorMaterial)
        {
            floorMaterialList.Add(item.name);
        }
        //Debug.Log(String.Join(",", floorMaterialList.ToArray()));
        Instance.gptSystem.PROMT[0] += "3- From the following list, pick one that is the most suitable material for the floor of the environment: " + String.Join(",", floorMaterialList.ToArray()) + "\n3- ...";
        Instance.gptSystem.PROMT[0] +=
            "\n\n4- Do the environment have walls? If true, the wall height in meter\r\n4- true/false:m";

        ResetCamera();
        //view roomspace ui
        promptUI.SetActive(true);
        roomSpaceUI.SetActive(false);
        objectViewUI.SetActive(false);
        preplacedUI.SetActive(false);
        genUI.SetActive(false);
    }

    private void Update()
    {
        numSelectedUI.text = "Selected: " + gridSystem.highlightedVec.Count.ToString();
        gridHeight = Mathf.CeilToInt(currentHeight / gridSystem.GetGridSize());
        ChangeWallHeight();
        if (!objectviewing)
        {
            return;
        }
        if (objectreqCompleted)
        {
            Debug.Log("Object Request Complete");
            objectreqCompleted = false;
            StartCoroutine(RefreshedObjView());
        }

        if (gptreqCompleted)
        {
            UIManager.Instance.SetStatus("GPT Request Complete! Can Proceed!");
            gptreqCompleted = false;
        }
        if (!(objectSelectDropDown.options[objectSelectDropDown.value].text == currentViewObj && objectVariationDropDown.options[objectVariationDropDown.value].text == currentViewVariation))
        {
            Debug.Log("On change");
            // on changing object
            if (currentobjectviewing != null)
            {
                Debug.Log("save and delete previous");
                //update value
                // - size
                // - model
                // - rotation

                //update size
                var size = currentViewObjSize;
                //update roomobject
                var roomObject_inselection = currentViewObj;

                if (objectSelectionInfo.ContainsKey(roomObject_inselection))
                {
                    Debug.Log("update key!!! " + roomObject_inselection);
                    Debug.Log("size " + size);
                    objectSelectionInfo[roomObject_inselection] = new (size, currentViewVariation, currentViewObjRotation, objectViewAutoToggle.isOn, objectTypeDropDown.value);
                }
                else
                {
                    Debug.Log("create key!!! " + roomObject_inselection);
                    objectSelectionInfo.Add(roomObject_inselection, new (size, currentViewVariation, currentViewObjRotation, objectViewAutoToggle.isOn, objectTypeDropDown.value));
                }
                Debug.Log(currentViewVariation + " have been saved");
                //reset the rotation
                currentViewObjRotation = 0;
                currentViewObjSize = 0;
                Destroy(currentobjectviewing);
            }
            //update current obj
            currentViewObj = objectSelectDropDown.options[objectSelectDropDown.value].text;
            currentViewVariation = objectVariationDropDown.options[objectVariationDropDown.value].text;

            // generate the model
            currentobjectviewing = objprep.LoadModel(currentViewObj, currentViewVariation);
            currentobjectviewing.transform.position = new Vector3(1000, 0, 1000);
            // apply changes before
            // apply size transformation

            // if object not exist in dict, add it
            if (!objectSelectionInfo.ContainsKey(currentViewObj))
            {
                Debug.Log("create key " + currentViewObj);
                objectSelectionInfo.Add(currentViewObj, new (1, currentViewVariation, 0, true, 0));   //default true gpt size
            }
            Debug.Log("changing size to " + objectSelectionInfo[currentViewObj].Item1);
            var obj = objectSelectionInfo[currentViewObj];
            objprep.PrepObjSize(currentobjectviewing, obj.Item1);
            // apply rotation transformation
            currentobjectviewingTransform = currentobjectviewing.transform.Find("model").transform;
            currentobjectviewingTransform.localRotation = Quaternion.Euler(currentobjectviewingTransform.localRotation.eulerAngles.x, obj.Item3, currentobjectviewingTransform.localRotation.eulerAngles.z);



            objectTypeDropDown.value = obj.Item5;
            // update viewer slider ui
            objectViewSizeSlider.value = obj.Item1;
            currentViewObjSize = obj.Item1;
            OnChangeSliderSize();

            objectViewRotationSlider.value = obj.Item3;
            currentViewObjRotation = obj.Item3;
            OnChangeSliderRotation();
            objectViewAutoToggle.isOn = obj.Item4;
            var obj_dia = objprep.FindDimention(currentobjectviewing.transform.GetChild(0).gameObject);
            sizeUI.text = "Current Scale\nLengh = " + obj_dia.x.ToString("0.00") + "m\nHight = " + obj_dia.y.ToString("0.00") + "m\nWidth = " + obj_dia.z.ToString("0.00") + "m";
        }
    }

    #endregion Unity Start and Update

    #region Main Functions

    // First step: confirm prompt with button press
    public async void PromptEnteredButton()
    {
        // wait for gpt to finish, return object list
        var result = await gptSystem.GetResponse(0);

        if (result == null)
        {
            UIManager.Instance.SetStatus("Please enter the environment you want to create at the input field!", true);
            return;
        }

        GenerateObject(result.Item1);
        //gptResult = (await gptSystem.GetResponse(1)).Item2;
        // generate objects from gpt result using shape

        UIManager.Instance.SetStatus("Please create a single room by dragging on the grid! Meanwhile, 3D Model will be generated in the background (~3 mins)!");

        //ui transition Prepareing for Object View
        promptUI.SetActive(false);
        roomSpaceUI.SetActive(true);
    }

    // Second step: confirm room space with button press
    public void GenRoomSpaceButton()
    {
        if (isRoomValid(gridSystem.highlightedVec))
        {
            //required GPT: floor mat
            if (!objectreqCompleted)
            {
                UIManager.Instance.SetStatus("Generating 3D Model, please wait...", true);
                Debug.Log("Object Request NOT Completed");
                return;
            }

            Debug.Log("Object Request Complete");
            //UIManager.Instance.SetStatus("Shap-e request done!");

            objectreqCompleted = false;

            InitFloorAtHighlight();
            ChangeTextureFloor(Instance.gptSystem.floorMaterial);
            GetRoomDimension();
            gridSystem.ClearAllHighlight();
            if (GPT.Instance.GetWallInfo().Item1)
            {
                wallHeight = GPT.Instance.GetWallInfo().Item2;
                GenerateWall();
            }
            CategoriesArea();
            roomSpaceUI.SetActive(false);

            gptSystem.PROMT[1] = gptSystem.PROMT[1].Replace("[ROOM_SIZE]",
                "Knowledge: The room is " + roomDimension + " in meter and use all previous output as knowledge for following questions");
            gptSystem.GetResponse(1);
            PrepObjView();
        }
        else
        {
            Debug.LogWarning("Room is not valid");
            UIManager.Instance.SetStatus("Room not valid! Please make sure all cells is connected to each other!", true);
        }
    }

    // Third step: confirm object size and rotation with button press
    public void ObjectViewConfirm()
    {
        gridSystem.SetHighlightMode(true);
        //required gpt result to be finished
        if (gptResult.Count == 0)
        {
            Debug.Log("GPT result is not done yet");
            UIManager.Instance.SetStatus("Waiting for GPT result...", true);
            return;
        }
        if (currentobjectviewing != null)
        {
            var size = objprep.FindDimention(currentobjectviewing.transform.Find("model").gameObject).y;
            var roomObject_inselection = currentViewObj;
            Debug.Log(objectViewAutoToggle.isOn);
            if (objectSelectionInfo.ContainsKey(roomObject_inselection))
            {
                Debug.Log("saved " + roomObject_inselection);
                objectSelectionInfo[roomObject_inselection] = new (size, currentViewVariation, currentViewObjRotation, objectViewAutoToggle.isOn, objectTypeDropDown.value);
            }
            else
            {
                Debug.Log("create key!!! " + roomObject_inselection);
                objectSelectionInfo.Add(roomObject_inselection, new (size, currentViewVariation, currentViewObjRotation, objectViewAutoToggle.isOn, objectTypeDropDown.value));
            }
            //reset the rotation
            currentViewObjRotation = 0;
            Destroy(currentobjectviewing);
        }
        //process infomation from object view - preping the roomObjectAvailable
        LoadObjModelAndBB();

        //ui transition ** Preparing for Preplaced UI
        Destroy(scalegameobj);
        Camera.main.orthographic = false;
        ResetCamera();
        objectViewUI.SetActive(false);
        preplacedUI.SetActive(true);
        var option = new List<TMP_Dropdown.OptionData>();
        foreach (var item in roomObjectAvailable)
        {
            if (item.placementtype == 1 || item.placementtype == 2)
            {
                option.Add(new TMP_Dropdown.OptionData(item.name));
                var color_ = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                item.placeholderColor = color_;
            }
        }
        preplaceDropDown = preplacedUI.transform.Find("Dropdown").GetComponent<TMP_Dropdown>();
        preplaceDropDown.options = option;
        ChangeUIColorIndicator();
        Camera.main.GetComponent<CameraManager>().enabled = true;
        gridSystem.ClearAllHighlight();
    }

    // Forth step: confirm preplaced objects with button press
    public void PreplacedConfirmButton()
    {
        gridSystem.SetHighlightMode(false);
        gridSystem.ClearAllHighlight();

        preplacedUI.SetActive(false);
        genUI.SetActive(true);
        // prep the ui
        Transform pos_holder = genUI.transform.Find("position holder");
        for (int i = 0; i < roomObjectAvailable.Count; i++)
        {
            GameObject set;
            if (!(setParent.Count >= i+1))// check if index i already exist
            {
                set = Instantiate(setPrefab, pos_holder.GetChild(i));
            }
            else
            {
                set = setParent[i];
            }
            
            var num_ = 0;
            //counting preplaced object of that type
            foreach (var item in preplacedInfoStored.Values) if (item == roomObjectAvailable[i].name) num_++;
            //update preplace num
            set.transform.Find("preplace num").GetComponent<TMP_InputField>().text = num_.ToString();
            // storing num
            if (objectNumber.TryGetValue(roomObjectAvailable[i],out var num_preplace))
            {
                objectNumber[roomObjectAvailable[i]] = num_;
            }
            else
            {
                objectNumber.Add(roomObjectAvailable[i], num_);
                objectInputField.Add(i, set.transform.Find("inputField").GetComponent<TMP_InputField>());
                setParent.Add(set);
            }

            objectInputField[i].text = objectNumber[roomObjectAvailable[i]].ToString();
            set.transform.Find("object name").GetComponent<TMP_Text>().text = roomObjectAvailable[i].name;
            
        }
    }

    // Fifth step: start PCG with button press
    public void GenButton()
    {
        gridSystem.ClearAllHighlight();
        UpdateObjectNumber();
        CategoriesArea();

        // destroy object in the room previously
        DestroyAllPlaced();

        InstantatePreplaced();

        var mode = genmodeDropDown.value;
        if (mode == 0)
        {
            StartCoroutine(GPTgen());
            //disable button
        }
        else if (mode == 1)
        {
            StartCoroutine(SortUsingProceduralGen());
        }
    }

    private void DestroyAllPlaced()
    {
        foreach (var item in allObjPlaced) Destroy(item.Item2);
        allObjPlaced.Clear();
    }

    #endregion Main Functions

    #region Main Helper Functions

    private void PrepObjView()
    {
        gridSystem.ClearAllHighlight();
        objectViewUI.SetActive(true);
        Camera.main.transform.position = objectViewCamPosition[0].position;
        Camera.main.transform.rotation = objectViewCamPosition[0].rotation;
        scalegameobj = Instantiate(scaleprefab, objectViewscalePosition[0].position, Quaternion.identity);
        objectviewing = true;
        // assigning ui element
        objectSelectDropDown = objectViewUI.transform.Find("Object Selection Group").Find("Object Selection").gameObject.GetComponent<TMP_Dropdown>();
        objectVariationDropDown = objectViewUI.transform.Find("Object Variation Group").Find("Object Variation Selection").gameObject.GetComponent<TMP_Dropdown>();
        objectTypeDropDown = objectViewUI.transform.Find("Type Group").Find("Dropdown").gameObject.GetComponent<TMP_Dropdown>();
        objectViewSizeSlider = objectViewUI.transform.Find("Size Group").Find("Size slider").gameObject.GetComponent<Slider>();
        objectViewRotationSlider = objectViewUI.transform.Find("Rotation Group").Find("Rotation slider").gameObject.GetComponent<Slider>();
        objectViewAutoToggle = objectViewUI.transform.Find("Auto Gen for Object").gameObject.GetComponent<Toggle>();
        //load existing data from files
        ObjectViewUpdateObjectSelection();
    }

    private void InstantatePreplaced()
    {
        // delete prepaced object item
        foreach (var item in preplaceHighlightGameobject.Values) Destroy(item);
        //Process preplaced object
        foreach (var vec in preplacedInfoStored.Keys)
        {
            // initialise at prefab at that point
            var room_obj = NameToRoomObj(preplacedInfoStored[vec]);
            //check if the obj still avaliable
            if (room_obj.objectPrefab == null)
            {
                Debug.Log("unable to find object prefab " + room_obj.name);
            }
            else
            {
                InitiateAndArrangeBigObject(room_obj, vec, Vector3.zero);
            }
        }
    }

    private RoomObjects NameToRoomObj(string name)
    {
        foreach (var obj in roomObjectAvailable)
        {
            if (obj.name == name)
            {
                return obj;
            }
        }
        return null;
    }

    private List<Vector3Int> ListConverntFindClosestRoomCell(List<Vector3> list_input, List<Vector3Int> roomspace)
    {
        List<Vector3> list = new List<Vector3>(list_input);
        List<Vector3Int> result = new List<Vector3Int>();
        foreach (var item in list)
        {
            var item_ = new Vector3Int(Mathf.RoundToInt(item.x), Mathf.RoundToInt(item.y), Mathf.RoundToInt(item.z));
            if (roomspace.Contains(item_))
            {
                result.Add(item_);
                continue;
            }
            //find cloest rooms cell to the item
            float mindist = 99f;
            Vector3Int cloest_cell = roomspace[0];
            foreach (var roomcell in roomspace)
            {
                if (mindist > Vector3Int.Distance(roomcell, item_))
                {
                    mindist = Vector3Int.Distance(roomcell, item_);
                    cloest_cell = roomcell;
                }
            }
            result.Add(cloest_cell);
        }
        return result;
    }

    //arranging big objects into a correct position return bool if the object fit or not if not the object will be deleted
    private bool InitiateAndArrangeBigObject(RoomObjects objtype, Vector3Int vec, Vector3 sidetolook)
    {
        Debug.Log("initialising " + objtype.name + " at vec " + vec + " look of " + sidetolook);
        var flexibility_threshold = 4; // Defind Threshold

        // ***INITIALISE OBJECT***
        var obj = Instantiate(objtype.objectPrefab);
        RoomGeneratedParent(obj);
        var boundingbox = objtype.size;

        // [temporary code]
        //obj.transform.position = gridSystem.CellToWorldCenterBottom(vec);
        //objectPlaced.Add(obj);
        var cellfilter4proximity = new List<Vector3Int>();
        if (objtype.placementtype == 0) // lean against the wall
        {
            if (90 == Mathf.Abs(Vector3.Angle(-FindClosesWall(vec), obj.transform.forward)))
            {
                //switch the size
                boundingbox = new Vector3Int(boundingbox.z, boundingbox.y, boundingbox.x);
            }
            obj.transform.localRotation = Quaternion.LookRotation(-FindClosesWall(vec), Vector3.up);
            cellfilter4proximity.AddRange(gridCategories[0]);
            cellfilter4proximity.AddRange(gridCategories[1]);
        }
        if (objtype.placementtype == 1) //middle of the room
        {
            cellfilter4proximity.AddRange(gridCategories[1]);
            if (sidetolook != Vector3.zero)
            {
                if (90 == Mathf.Abs(Vector3.Angle(sidetolook, obj.transform.forward)))
                {
                    boundingbox = new Vector3Int(boundingbox.z, boundingbox.y, boundingbox.x);
                }
                obj.transform.rotation = Quaternion.LookRotation(sidetolook, Vector3.up);
            }
        }
        if (objtype.placementtype == 2) //on the wall
        {
            //face away from the wall
            if (90 == Mathf.Abs(Vector3.Angle(-FindClosesWall(vec), obj.transform.forward)))
            {
                boundingbox = new Vector3Int(boundingbox.z, boundingbox.y, boundingbox.x);
            }
            obj.transform.rotation = Quaternion.LookRotation(-FindClosesWall(vec), Vector3.up);
            cellfilter4proximity.AddRange(gridCategories[2]);
        }
        //list of cell in the bounding box
        List<Vector3Int> bbcheck;
        List<Vector3Int> bbcheck_inv;
        if (objtype.placementtype == 3) //ceiling
        {
            cellfilter4proximity.AddRange(gridCategories[3]);
            bbcheck = GetListBoundingBox(boundingbox)[2];
            bbcheck_inv = GetListBoundingBox(boundingbox)[3];
        }
        else
        {
            //list of cell in the bounding box
            bbcheck = GetListBoundingBox(boundingbox)[0];
            bbcheck_inv = GetListBoundingBox(boundingbox)[1];
        }

        //list of cell in the bounding box
        var cell2check = gridSystem.GetCellInProximity(vec, flexibility_threshold, cellfilter4proximity);
        List<List<Vector3Int>> list_avaliblespace = new List<List<Vector3Int>>();
        //iterate though each cell within the proximity to check the avalibility then add to @list_avaliblespace
        foreach (var cell in cell2check)
        {
            // check the bounding box
            List<Vector3Int> avalible_cell = new List<Vector3Int>();
            //check avalibility of the bounding box
            if (CheckBB(bbcheck, cell))
            {
                foreach (var offset in bbcheck)
                {
                    avalible_cell.Add(offset + cell);
                }
                list_avaliblespace.Add(avalible_cell);
            }
            else if (CheckBB(bbcheck_inv, cell))
            {
                foreach (var offset in bbcheck_inv)
                {
                    avalible_cell.Add(offset + cell);
                }
                list_avaliblespace.Add(avalible_cell);
            }
        }
        // avalaible space?
        if (list_avaliblespace.Count != 0)
        {
            //find the best position (closest to the initial cell)
            List<float> dis_bbpos = new List<float>();
            foreach (var item in list_avaliblespace)
            {
                var averagepos = GetMeanVector(item);
                dis_bbpos.Add(Vector3.Distance(averagepos, vec));
            }
            var bestposition = list_avaliblespace[dis_bbpos.IndexOf(dis_bbpos.Min())];
            //make the cell unavaliable
            MakeCellsUnavaliable(bestposition);

            //move the object to the middle bottom grid
            obj.transform.position = FindMidBottomAveragePos(bestposition);
            Tuple<RoomObjects, GameObject, List<Vector3Int>, Vector3Int> temp2store = new (objtype, obj, bestposition, vec);
            allObjPlaced.Add(temp2store);
        }
        else
        {
            Destroy(obj);
            return false;
        }
        return true;
    }

    public void GenerateObject(String obj = "n/a")
    {
        var trimmed = obj.Replace(", ", ",").Trim();
        Debug.Log(trimmed);
        StartCoroutine(shapE.PostData(trimmed, false, false, 1));

        string[] objlist = trimmed.Split(",");
        // add all the object
        foreach (var item in objlist)
        {
            roomObjectAvailable.Add(new RoomObjects(item.Trim(), null, Vector3Int.zero, 0));
        }
    }

    private void ChangeColorWall()
    {
    }

    private void ChangeTextureFloor(string textureName)
    {
        Debug.Log("Calling " + textureName);
        foreach (var item in floorMaterial)
        {
            if (item.name == textureName.Trim())
            {
                foreach (var floor in floorStored.Values)
                {
                    floor.GetComponent<MeshRenderer>().material = item;
                }
            }
        }
    }
    
    private void LoadObjModelAndBB() // need to redo
    {
        foreach (var roomobj in roomObjectAvailable)
        {
            var obj_info = gptResult[roomobj.name];
            Debug.Log(roomobj.name + " = " + obj_info.Item3.Count);
            //dealing type.......
            if (obj_info.Item3.Count != 0)// small object
            {
                Debug.Log(roomobj.name+ " = small object");
                roomobj.placeableObj = obj_info.Item3;
                roomobj.placementtype = 5;
            }
            else
            {
                Debug.Log("wall override");
                // if there is no wall, override the placement type
                if (!GPT.Instance.GetWallInfo().Item1)
                {
                    roomobj.placementtype = 1;
                }
                else
                {
                    roomobj.placementtype = obj_info.Item2;
                }
            }

            int new_placetype = 99;
            if (objectSelectionInfo.TryGetValue(roomobj.name, out var objinfo))
            {
                switch (objinfo.Item5)
                {
                    case 0: // let gpt decide
                        break;
                    case 1: // near wall
                        new_placetype = 0;
                        break;
                    case 2: // middle of the room
                        new_placetype = 1;
                        break;
                    case 3: // on the wall
                        new_placetype = 2;
                        break;
                    case 4: // on the ceiling
                        new_placetype = 3;
                        break;
                    default:
                        new_placetype = 1;
                        break;
                }
            }
            Debug.Log(roomobj.name + " " +  new_placetype.ToString());
            if (objectSelectionInfo.TryGetValue(roomobj.name, out var objselectinfo))
            {
                //data to override
                // - size,rotation,variation
                if (objselectinfo.Item4 || autoGenAll) //if autogen
                {
                    var typechose = roomobj.placementtype;
                    Debug.Log(typechose + " = 1");
                    if (new_placetype != 99) // user want the old type
                    {
                        typechose = new_placetype;
                    }
                    roomobj.placementtype = typechose;
                    var rotation = objselectinfo.Item3;
                    Debug.Log(typechose + " ====== 1");
                    var prepobjresult = objprep.CompletePrepAuto(roomobj.name, gptResult[roomobj.name].Item1, rotation, roomobj.placementtype, gridSystem.GetGridSize());
                    roomobj.objectPrefab = prepobjresult.Item1;
                    roomobj.size = prepobjresult.Item2;
                }
                else
                {
                    var typechose = roomobj.placementtype;
                    Debug.Log(typechose + " = 2");
                    if (new_placetype != 99) // user want the old type
                    {
                        typechose = new_placetype;
                    }
                    roomobj.placementtype = typechose;
                    Debug.Log(typechose + " ====== 2");
                    Debug.Log("new " + roomobj + new_placetype.ToString());
                    var size = objselectinfo.Item1;
                    var variation_name = objselectinfo.Item2;
                    var rotation = objselectinfo.Item3;
                    var prepobjresult = objprep.CompletePrep(roomobj.name, variation_name, size, rotation, roomobj.placementtype, gridSystem.GetGridSize());
                    // set obj prefab
                    roomobj.objectPrefab = prepobjresult.Item1;
                    roomobj.size = prepobjresult.Item2;
                }
                if (roomobj.placementtype == 1)
                {
                    roomobj.size += new Vector3Int(1,0,1);
                }
            }
            else
            {
                Debug.Log(roomobj.placementtype + " = 3" + roomobj.name);
                var prepobjresult = objprep.CompletePrepAuto(roomobj.name, gptResult[roomobj.name].Item1, roomobj.placementtype, gridSystem.GetGridSize());
                roomobj.objectPrefab = prepobjresult.Item1;
                roomobj.size = prepobjresult.Item2;
            }

            var trimmed_name = roomobj.name.Trim().ToLower();
            if (trimmed_name.Contains("chair")
                || trimmed_name.Contains("stool")
                || trimmed_name.Contains("sofa")
                || trimmed_name.Contains("seat")
                || trimmed_name.Contains("beanbag")
                || trimmed_name.Contains("bench"))
            {
                roomobj.tag = "chair";
            }
            else if (trimmed_name.Contains("table")
                || trimmed_name.Contains("desk"))
            {
                roomobj.tag = "table";
            }
        }
    }

    //initialise floor placeholder
    public void InitFloorAtHighlight()// display
    {
        GameObject floorParent = new GameObject("floorParent");
        RoomGeneratedParent(floorParent);
        foreach (var item in gridSystem.highlightedVec)
        {
            var temp = Instantiate(floorPrefab, gridSystem.CellToWorldCenterBottom(item), Quaternion.identity);
            temp.transform.parent = floorParent.transform;
            if (floorStored.ContainsKey(item))
            {
                floorStored[item] = temp;
            }
            else
            {
                floorStored.Add(item, temp);
            }
            var gridSize = gridSystem.GetGridSize();
            floorStored[item].transform.localScale = new Vector3(gridSize, 0.1f, gridSize);
            gridSystem.GetCellfromGridCord(item).roomArea = true;
        }
    }

    private IEnumerator GPTgen()
    {
        //foreach (var item in gptResult.Keys)
        //{
        //    //gptResult[item]
        //    Debug.Log(gptResult[item].Item1 + " " + gptResult[item].Item2 + " " + gptResult[item].Item4);
        //    var result = "";
        //    foreach (var x in gptResult[item].Item3)
        //    {
        //        result = result + " " + x;
        //    }
        //    foreach (var x in gptResult[item].Item5)
        //    {
        //        result = result + " " + x + " ||";
        //    }
        //    Debug.Log(result);
        //    }

        //gen all the big item first
        foreach (var item in gptResult.Keys)
        {
            if (gptResult[item].Item2 != 5) // big object
            {
                var amount2gen = gptResult[item].Item4;
                List<Vector3> location2gen = new List<Vector3>(gptResult[item].Item5);
                for (int j = 0; j < location2gen.Count; j++)
                {
                    location2gen[j] += new Vector3Int(roomDimentionAndOffset.Item2.x, 0, roomDimentionAndOffset.Item2.y);
                }
                Debug.Log(item + " | amout to gen = " + amount2gen + "location = " + location2gen.Count);
                var new_loc_2_gen = ListConverntFindClosestRoomCell(location2gen, gridCategories[gptResult[item].Item2]);
                var test = "";
                foreach (var x in location2gen)
                {
                    test = test + " " + x;
                }
                Debug.Log(test);
                test = "";
                foreach (var x in new_loc_2_gen)
                {
                    test = test + " " + x;
                }
                Debug.Log(test);
                for (int i = 0; i < location2gen.Count; i++)
                {
                    //??? TODO: GEN
                    if (InitiateAndArrangeBigObject(NameToRoomObj(item), new_loc_2_gen[i], Vector3.zero))
                    {
                        Debug.Log(item + " has successfully gen at " + new_loc_2_gen[i]);
                    }
                    else
                    {
                        Debug.LogWarning(item + " can not be gen at " + new_loc_2_gen[i]);
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
        foreach (var item in gptResult.Keys)
        {
            if (gptResult[item].Item2 == 5) // small object
            {
                var roomobj = NameToRoomObj(item);
                var amount2gen = gptResult[item].Item4;
                var list_placeable_obj = gptResult[item].Item3;
                if (list_placeable_obj.Count == 0)
                {
                    Debug.LogWarning(item + " can not be place due to them having no object to be placed on");
                    continue;
                }

                var list_placeable_obj_placed = new List<GameObject>();

                foreach (var obj_ in allObjPlaced)
                {
                    if (list_placeable_obj.Contains(obj_.Item1.name))
                    {
                        list_placeable_obj_placed.Add(obj_.Item2);
                    }
                }
                if (list_placeable_obj_placed.Count == 0)
                {
                    Debug.LogWarning(item + " can not be place due to them having no object to be placed on in the room");
                    continue;
                }
                // if there no obj in the room that this object can be place on

                for (int i = 0; i < amount2gen; i++) // amount of obj to be gen
                {
                    for (int j = 0; j < 20; j++)
                    {
                        //get randomplace to place obj on
                        var rand_obj2placeon = list_placeable_obj_placed[Random.Range(0, list_placeable_obj_placed.Count)];
                        //???
                        if (InitiateSmallObjOnTop(roomobj, rand_obj2placeon))
                        {
                            break;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
        // base on list of object and room layout information
        yield return null;
    }

    // The percedure gen function
    private IEnumerator SortUsingProceduralGen()
    {
        // randomly place excess object in to the roomm 1 by 1
        var numberOfObjectToBeGen = FindExcessObj().Item1;
        var rotate_object_index = 0;
        var count = 0;// fail safe
        while (numberOfObjectToBeGen.Sum() != 0 || count == 99)
        {
            // place object
            if (numberOfObjectToBeGen[rotate_object_index] != 0) //if there are room left to gen
            {
                //check the object rule
                var objclass = roomObjectAvailable[rotate_object_index];
                Debug.Log("genning" + objclass.name);
                var objtype = objclass.placementtype;
                for (int i = 0; i < 20; i++)
                {
                    var rand_num = Random.Range(0, gridCategories[objtype].Count);
                    var chosen_vec = gridCategories[objtype][rand_num];
                    //gen object
                    if (InitiateAndArrangeBigObject(roomObjectAvailable[rotate_object_index], chosen_vec, Vector3.zero))
                    {
                        yield return new WaitForSeconds(0.1f);
                        break;
                    }
                    if (i == 19)
                    {
                        Debug.Log("unsuccessful placesment");
                    }
                }
                //find avaliable space to place object at

                numberOfObjectToBeGen[rotate_object_index]--;
            }
            rotate_object_index++;
            if (rotate_object_index >= roomObjectAvailable.Count)
            {
                rotate_object_index = 0;
            }
            count++;
        }
        var chairobjToBeGen = FindExcessObj().Item2;
        while (chairobjToBeGen.Sum() != 0 || count == 99)
        {
            if (chairobjToBeGen[rotate_object_index] != 0)
            {
                var roomObj_ = roomObjectAvailable[rotate_object_index];
                List<Tuple<RoomObjects, GameObject, List<Vector3Int>, Vector3Int>> allavaliabletable = new List<Tuple<RoomObjects, GameObject, List<Vector3Int>, Vector3Int>>();
                //get list of space close to table
                foreach (var item in allObjPlaced)
                {
                    if (item.Item1.tag == "table" && item.Item1.placementtype == 1) allavaliabletable.Add(item);
                }
                for (int i = 0; i < 20; i++)
                {
                    Debug.Log(allavaliabletable.Count);
                    Vector3Int place_to_init = Vector3Int.zero;
                    Vector3 sidetolook;
                    if (allavaliabletable.Count == 0)// if there is no table
                    {
                        sidetolook = Vector3.zero;
                        var rand_num = Random.Range(0, gridCategories[roomObj_.placementtype].Count);
                        place_to_init = gridCategories[roomObj_.placementtype][rand_num];
                    }
                    else
                    {
                        var randomindex = Random.Range(0, allavaliabletable.Count);
                        var tableobj = allavaliabletable[randomindex];
                        var rand_side = Random.Range(0, 4);
                        place_to_init = NeighbourOf(tableobj.Item4)[rand_side];
                        sidetolook = Vector3.zero;
                        switch (rand_side)
                        {
                            case 0:
                                sidetolook = new Vector3(-1, 0, 0);
                                break;

                            case 1:
                                sidetolook = new Vector3(1, 0, 0);
                                break;

                            case 2:
                                sidetolook = new Vector3(0, 0, -1);
                                break;

                            case 3:
                                sidetolook = new Vector3(0, 0, 1);
                                break;
                        }
                    }

                    //gen object
                    if (InitiateAndArrangeBigObject(roomObjectAvailable[rotate_object_index], place_to_init, sidetolook))
                    {
                        yield return new WaitForSeconds(0.1f);
                        break;
                    }
                    if (i == 19)
                    {
                        Debug.Log("unsuccessful placesment");
                    }
                }

                chairobjToBeGen[rotate_object_index]--;
            }
            rotate_object_index++;
            if (rotate_object_index >= roomObjectAvailable.Count) rotate_object_index = 0; //reset the rotation
            count++;
        }
        // generate small object once all bit object have been generated
        var smallobjToBeGen = FindExcessObj().Item3;
        rotate_object_index = 0;
        count = 0;// fail safe

        while (smallobjToBeGen.Sum() != 0 || count == 99)
        {
            if (smallobjToBeGen[rotate_object_index] != 0)
            {
                // generate small object
                var roomObj_ = roomObjectAvailable[rotate_object_index];
                Debug.Log("Generating " + roomObj_.name);
                // find all avaliable object to be place on
                var listofplaceableobj = roomObj_.placeableObj;
                if (listofplaceableobj == null)
                {
                    continue;
                }
                Debug.Log("number of object that can be placed on " + listofplaceableobj.Count);
                List<GameObject> gameobj_placeable = new List<GameObject>();
                foreach (var item in allObjPlaced)
                {
                    var name_of_obj = item.Item1.name;
                    if (listofplaceableobj.Contains(name_of_obj))
                    {
                        gameobj_placeable.Add(item.Item2);
                    }
                }
                Debug.Log("gameobj placeable on scene " + gameobj_placeable.Count);
                // random object on the list{gameobj_placeable} and place roomObj_ on
                if (gameobj_placeable.Count != 0) // check if there exist smt that can be placed on
                {
                    for (int i = 0; i < 99; i++)
                    {
                        var obj2placeOn = gameobj_placeable[Random.Range(0, gameobj_placeable.Count)];
                        Debug.Log("trying to place obj on " + obj2placeOn.transform.position);
                        if (InitiateSmallObjOnTop(roomObj_, obj2placeOn))
                        {
                            Debug.Log("successfully placed object");
                            yield return new WaitForSeconds(0.1f);
                            break;
                        }
                        if (i == 98)
                        {
                            Debug.Log("cant find place to put " + roomObj_.name);
                        }
                    }
                }
                else
                {
                    Debug.Log("No object can be placed on");
                }
                smallobjToBeGen[rotate_object_index]--;
            }
            rotate_object_index++;
            if (rotate_object_index >= roomObjectAvailable.Count) rotate_object_index = 0; //reset the rotation
            count++;
        }
        yield return null;
    }

    private void GetRoomDimension()
    {
        var list_room_cell = floorStored.Keys.ToList();
        var min_x = list_room_cell[0].x;
        var max_x = list_room_cell[0].x;
        var min_z = list_room_cell[0].z;
        var max_z = list_room_cell[0].z;
        foreach (var item in list_room_cell)
        {
            if (item.x > max_x) max_x = item.x;
            if (item.z > max_z) max_z = item.z;

            if (item.x < min_x) min_x = item.x;
            if (item.z < min_z) min_z = item.z;
        }
        Vector2Int dimension = new Vector2Int(max_x - min_x, max_z - min_z);
        Vector2Int offset = new Vector2Int(min_x, min_z);
        Debug.Log("dimension = " + dimension + " offset = " + offset);
        roomDimension = dimension.ToString().Replace(',', 'x');
        roomDimentionAndOffset = new Tuple<Vector2Int, Vector2Int>(dimension, offset);
    }

    private bool InitiateSmallObjOnTop(RoomObjects obj2place, GameObject obj2beplacedon)
    {
        //TODO
        //check bounding box of the bottom object
        // initialise a raycast on to the gameobject from the top to find the point to place obj at
        // at that point try check in around bounding box if there are obj close by if yes try other point if can not find place to put at all return false
        // otherwise initialise the object at the point
        // add the object to the obj list{objectPlaced}
        var obj_pos = obj2beplacedon.transform.position;
        var model = obj2beplacedon.transform.GetChild(0).gameObject;
        var bb = objprep.FindDimention(model);
        Debug.Log("PLACING SMALL OBJECT: " + obj2place.name);
        //get area to raycast down
        float height_to_raycast_y = obj_pos.y + bb.y + 0.1f;
        for (int i = 0; i < 5; i++) // randomise 5 position
        {
            var percentage = 0.85f;
            Vector3 area2raycast_1 = new Vector3(obj_pos.x - (bb.x * percentage / 2), height_to_raycast_y, obj_pos.z - (bb.z * percentage / 2));
            Vector3 area2raycast_2 = new Vector3(obj_pos.x + (bb.x * percentage / 2), height_to_raycast_y, obj_pos.z + (bb.z * percentage / 2));
            Vector3 rand_pos_to_raycast = new Vector3(Random.Range(area2raycast_1.x, area2raycast_2.x), height_to_raycast_y, Random.Range(area2raycast_1.z, area2raycast_2.z));

            var obj_created = Instantiate(obj2place.objectPrefab, rand_pos_to_raycast, Quaternion.identity);
            RoomGeneratedParent(obj_created);
            //if not intersect with object then move it down
            var prev_pos = rand_pos_to_raycast;
            bool isplaced = true;
            for (int j = 0; j < 50; j++)
            {
                if (CheckIntersectAllObj(obj_created))
                {
                    break;
                }

                prev_pos = obj_created.transform.position;
                obj_created.transform.position += new Vector3(0, -0.02f, 0);
                Debug.Log("moving obj down to pos " + obj_created.transform.position);
                
                if (j == 19)
                {
                    Debug.Log("still going?");
                    isplaced = false;
                }
            }
            if (prev_pos != rand_pos_to_raycast && isplaced) // if obj is not intersect at the initial position
            {
                obj_created.transform.position = prev_pos + new Vector3(0, 0, 0);
                allObjPlaced.Add(new Tuple<RoomObjects, GameObject, List<Vector3Int>, Vector3Int>(obj2place, obj_created, null, Vector3Int.zero));
                return true;
            }
            else
            {
                Destroy(obj_created);
            }
        }
        Debug.Log("can not find place to place object:" + obj2place.name);
        return false;
    }

    private bool CheckIntersectAllObj(GameObject a)
    {
        foreach (var item in allObjPlaced)
        {
            if (CheckIntersect(a, item.Item2))
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckIntersect(GameObject a, GameObject b)
    {
        var colliderA = a.transform.GetChild(0).GetChild(0).GetComponent<MeshCollider>();
        var colliderB = b.transform.GetChild(0).GetChild(0).GetComponent<MeshCollider>();
        var transformA = a.transform.GetChild(0).GetChild(0);
        var transformB = b.transform.GetChild(0).GetChild(0);
        return Physics.ComputePenetration(colliderA, transformA.position, transformA.rotation, colliderB, transformB.position, transformB.rotation, out var dir, out float dis);
    }

    //split grid in the room into different categories
    private void CategoriesArea()
    {
        List<List<Vector3Int>> new_category = new List<List<Vector3Int>>();

        var list = new List<Vector3Int>(floorStored.Keys);
        var threshold = distanceThresholdWall;
        var innerArea = new List<Vector3Int>(list);
        for (int i = 0; i < threshold; i++)
        {
            var edge = FindEdgeCell(innerArea);
            innerArea = ListSubtract(innerArea, edge);
        }
        var outterArea = ListSubtract(list, innerArea);

        var edgeSpace = new List<Vector3Int>(outterArea);
        var centerSpace = new List<Vector3Int>(innerArea);
        var wallSpace = new List<Vector3Int>();
        var ceilingSpace = new List<Vector3Int>();
        var centerupperSpace = new List<Vector3Int>();

        for (int i = 1; i < gridHeight - distanceThresholdCeiling; i++)
        {
            foreach (var item in outterArea)
            {
                wallSpace.Add(new Vector3Int(item.x, i, item.z));
            }
            foreach (var item in innerArea)
            {
                centerupperSpace.Add(new Vector3Int(item.x, i, item.z));
            }
        }

        for (int i = gridHeight - distanceThresholdCeiling; i < gridHeight; i++)
        {
            foreach (var item in list)
            {
                ceilingSpace.Add(new Vector3Int(item.x, i, item.z));
            }
        }
        new_category.Add(edgeSpace);
        new_category.Add(centerSpace);
        new_category.Add(wallSpace);
        new_category.Add(ceilingSpace);
        new_category.Add(centerupperSpace);
        gridCategories = new_category;
    }

    public void ObjectViewUpdateObjectSelection()
    {
        //update value of the variation if object got change
        var optionObj = new List<TMP_Dropdown.OptionData>();

        foreach (var obj in roomObjectAvailable) optionObj.Add(new TMP_Dropdown.OptionData(obj.name)); // get list of roomobjectavaliable option

        objectSelectDropDown.options = optionObj;

        var optionObj_variation = new List<TMP_Dropdown.OptionData>();

        //update view variation to new
        var variation_name_new = new List<string>();
        AssetDatabase.Refresh();
        var variation_obj = new List<UnityEngine.Object>(Resources.LoadAll<UnityEngine.Object>(roomObjectAvailable[objectSelectDropDown.value].name));
        for (int i = variation_obj.Count - 1; i >= 0; i--) if (i % 2 != 0) variation_obj.RemoveAt(i);
        //Debug.Log("object " + objectSelectDropDown.options[objectSelectDropDown.value].text + " " + variation_obj.Count);

        foreach (var item in variation_obj)
        {
            optionObj_variation.Add(new TMP_Dropdown.OptionData(item.name));
            variation_name_new.Add(item.name);
        }
        
        objectVariationDropDown.options = optionObj_variation;
        if (currentViewObj == null)
        {
            Debug.Log("Null");
            objectVariationDropDown.value = 0;
        }else if (objectSelectionInfo.TryGetValue(objectSelectDropDown.options[objectSelectDropDown.value].text, out var value))
        {
            Debug.Log("Showing variation of "+ value.Item2);
            var variation_chosen_for_obj = value.Item2;
            objectVariationDropDown.value = variation_name_new.IndexOf(variation_chosen_for_obj); ;
        }
        else
        {
            objectVariationDropDown.value = 0;
        }
        
    }

    #endregion Main Helper Functions

    #region UI Functions

    public void BackPrePlaced()
    {
        genUI.SetActive(false);
        preplacedUI.SetActive(true);
        gridSystem.SetHighlightMode(true);
        DestroyAllPlaced();
        // clear all previous preplacce highlight block
        foreach (var item in preplaceHighlightGameobject.Keys)
        {
            Destroy(preplaceHighlightGameobject[item]);
        }
        preplacedInfoStored.Clear();
        preplaceHighlightGameobject.Clear();
        //try initiate preplacce highlight block again base on the info stored
        //foreach (var item in preplacedInfoStored.Keys)
        //{ 
        //    var obj_name = preplacedInfoStored[item];

        //    var obj_ = Instantiate(preplacedCubePrefab, gridSystem.CellToWorldCenter(item), Quaternion.identity);
        //    obj_.GetComponent<Renderer>().material = new Material(preplaceShader);
        //    obj_.GetComponent<Renderer>().material.SetColor("_Color", NameToRoomObj(obj_name).placeholderColor);

        //    preplaceHighlightGameobject.Add(item, obj_);
        //}
    }
    public void ResetCamera()
    {
        Camera.main.transform.position = mainCamPosition.position;
        Camera.main.transform.rotation = mainCamPosition.rotation;

        if (GameObject.Find("UI").activeSelf == false)
        {
            GameObject.Find("UI").SetActive(true);
        }
    }
    private IEnumerator RefreshedObjView()
    {
        yield return new WaitForSeconds(10);
        Debug.Log("refreshed");
        RefreshObjView();
    }

    public void RefreshObjView()
    {
        StartCoroutine(RefreshedObjViewBtn());
        ObjectViewUpdateObjectSelection();
    }

    private IEnumerator RefreshedObjViewBtn()
    {
        objectViewRefreshText.gameObject.GetComponentInParent<Button>().interactable = false;
        objectViewRefreshText.SetText("Refreshing");
        yield return new WaitForSeconds(3);
        objectViewRefreshText.gameObject.GetComponentInParent<Button>().interactable = true;
        //UIManager.Instance.SetStatus("Object Refreshed!");
        objectViewRefreshText.SetText("Refresh");
    }

    public void ToggleViewChange()
    {
        objectviewindextoggle += 1;
        if (objectviewindextoggle >= objectViewCamPosition.Length)
        {
            objectviewindextoggle = 0;
        }
        if (objectviewindextoggle != 0)
        {
            Camera.main.orthographic = true;
        }
        else
        {
            Camera.main.orthographic = false;
        }
        scalegameobj.transform.position = objectViewscalePosition[objectviewindextoggle].position;
        Camera.main.transform.position = objectViewCamPosition[objectviewindextoggle].position;
        Camera.main.transform.rotation = objectViewCamPosition[objectviewindextoggle].rotation;
    }

    public void ObjectViewAddMoveVarieation()
    {
        // sent request and refreshed the page
        StartCoroutine(shapE.PostData(currentViewObj, true, true, 1));
    }

    public void ObjectViewReset()
    {
        //set slider to default
        objectViewSizeSlider.value = 1f;
        objectViewRotationSlider.value = 0;
    }

    public void ObjectViewAutoGenAll()
    {
        autoGenAll = true;
        ObjectViewConfirm();
    }

    public void PrePlacedRemoveButton()
    {
        foreach (var item in gridSystem.highlightedVec)
        {
            if (gridSystem.GetCellfromGridCord(item).roomArea)
            {
                if (preplacedInfoStored.ContainsKey(item))
                {
                    Destroy(preplaceHighlightGameobject[item]);
                    preplaceHighlightGameobject.Remove(item);
                    preplacedInfoStored.Remove(item);
                }
            }
        }
        gridSystem.ClearAllHighlight();
    }

    public void PrePlacedApplyButton()
    {
        foreach (var item in gridSystem.highlightedVec)
        {
            if (gridSystem.GetCellfromGridCord(item).roomArea)
            {
                if (!preplacedInfoStored.ContainsKey(item))
                {
                    preplacedInfoStored.Add(item, preplaceDropDown.options[preplaceDropDown.value].text);
                }
                else
                {
                    //OVER RIDING
                    Destroy(preplaceHighlightGameobject[item]);
                    //update info
                    preplacedInfoStored[item] = preplaceDropDown.options[preplaceDropDown.value].text;
                }

                // add some indicator
                var obj_ = Instantiate(preplacedCubePrefab, gridSystem.CellToWorldCenter(item), Quaternion.identity);
                obj_.GetComponent<Renderer>().material = new Material(preplaceShader);
                obj_.GetComponent<Renderer>().material.SetColor("_Color", roomObjectAvailable[preplaceDropDown.value].placeholderColor);

                if (preplaceHighlightGameobject.ContainsKey(item))
                {
                    preplaceHighlightGameobject[item] = obj_;
                }
                else
                {
                    preplaceHighlightGameobject.Add(item, obj_);
                }
            }
        }
        gridSystem.ClearAllHighlight();
    }

    public void ClearAllPrePlacedButton()
    {
        preplacedInfoStored.Clear();
        foreach (var item in preplaceHighlightGameobject.Values)
        {
            Destroy(item);
        }
        preplaceHighlightGameobject.Clear();
    }

    public void ChangeUIColorIndicator()
    {
        var choice = preplacedUI.transform.Find("Dropdown").GetComponent<TMP_Dropdown>().value;
        preplacedUI.transform.Find("Color Indicator").GetComponent<Image>().color = roomObjectAvailable[choice].placeholderColor;
    }

    public void OnChangeSliderRoomHeight()
    {
        wallHeight = roomHeightSlider.value;
    }

    public void OnChangeSliderSize()
    {
        float size = objectViewSizeSlider.value;
        currentobjectviewingTransform.localScale = new Vector3(size, size, size);
        currentViewObjSize = size;
        var obj_dia = objprep.FindDimention(currentobjectviewing.transform.GetChild(0).gameObject);
        sizeUI.text = "Current Scale\nLengh = " + obj_dia.x.ToString("0.00") + "m\nHight = " + obj_dia.y.ToString("0.00") + "m\nWidth = " + obj_dia.z.ToString("0.00") + "m";
    }

    public void OnChangeSliderRotation()
    {
        float yrotation = objectViewRotationSlider.value;
        currentobjectviewingTransform.localRotation = Quaternion.Euler(currentobjectviewingTransform.eulerAngles.x, yrotation, currentobjectviewingTransform.eulerAngles.z);
        currentViewObjRotation = yrotation;
    }

    public void OnPressDeselectedHighlight()
    {
        gridSystem.ClearAllHighlight();
    }

    public void OnChangeGPTGen(bool isOn)
    {
        //if (isOn)
        //{
        //    objectViewSizeSlider.interactable = false;
        //    objectViewRotationSlider.interactable = false;
        //}
        //else
        //{
        //    objectViewSizeSlider.interactable = true;
        //    objectViewRotationSlider.interactable = true;
        //}
    }
    public void SaveAsPrefab()
    {
        //string path = "Assets/Prefabs/Rooms" + roomGenerated.name + ".prefab";
        //path = AssetDatabase.GenerateUniqueAssetPath(path);
        //PrefabUtility.SaveAsPrefabAsset(roomGenerated, path);

        var fileName = "RoomGenerated";
        using (FbxManager fbxManager = FbxManager.Create())
        {
            // configure IO settings.
            fbxManager.SetIOSettings(FbxIOSettings.Create(fbxManager, Globals.IOSROOT));

            // Export the scene
            using (FbxExporter exporter = FbxExporter.Create(fbxManager, "myExporter"))
            {
                string filePath = Path.Combine(Application.dataPath, "RoomGenerated.fbx");
                ModelExporter.ExportObject(filePath, roomGenerated);
                //// Initialize the exporter.
                //bool status = exporter.Initialize(fileName, -1, fbxManager.GetIOSettings());

                //// Create a new scene to export
                //FbxScene scene = FbxScene.Create(fbxManager, "myScene");

                //// Export the scene to the file.
                //exporter.Export(scene);
            }
        }
    }

    public void VR()
    {
        GameObject.Find("XRRig").transform.position = new Vector3(0, 0, 0);
        GameObject.Find("XRRig").transform.rotation = Quaternion.Euler(0, 0, 0);
        GameObject.Find("UI").SetActive(false);
    }

    #endregion UI Functions

    #region Helper Function
    private void RoomGeneratedParent(GameObject g)
    {
        g.transform.parent = roomGenerated.transform;
    }

    //helper fuction to find the change to the number of object needed to gen
    private Tuple<List<int>, List<int>, List<int>> FindExcessObj()
    {
        List<int> CountobjToBePlace = new List<int>();
        List<int> CountchairobjToBePlace = new List<int>();
        List<int> CountSmallobjToBePlace = new List<int>();
        for (int i = 0; i < roomObjectAvailable.Count; i++) // iterate thought each object type
        {
            int count = 0;
            //check for number of existing obj in preplaced info
            foreach (var preplacedobj in preplacedInfoStored.Values)
            {
                if (preplacedobj == roomObjectAvailable[i].name) count++;
            }
            //if preplace number of object bigger than object number
            if (count > objectNumber[roomObjectAvailable[i]])
            {
                objectInputField[i].text = count.ToString();
                CountobjToBePlace.Add(0);
            }
            else
            {
                //exception of small object
                if (roomObjectAvailable[i].placementtype == 5)
                {
                    CountobjToBePlace.Add(0);
                    CountchairobjToBePlace.Add(0);
                    CountSmallobjToBePlace.Add(objectNumber[roomObjectAvailable[i]] - count);
                }
                else if (roomObjectAvailable[i].tag == "chair" && roomObjectAvailable[i].placementtype == 1)
                {
                    CountchairobjToBePlace.Add(objectNumber[roomObjectAvailable[i]] - count);
                    CountobjToBePlace.Add(0);
                    CountSmallobjToBePlace.Add(0);
                }
                else
                {
                    CountobjToBePlace.Add(objectNumber[roomObjectAvailable[i]] - count);
                    CountSmallobjToBePlace.Add(0);
                    CountchairobjToBePlace.Add(0);
                }
            }
        }
        return new Tuple<List<int>, List<int>, List<int>>(CountobjToBePlace, CountchairobjToBePlace, CountSmallobjToBePlace);
    }

    private List<List<Vector3Int>> GetListBoundingBox(Vector3Int size)
    {
        List<Vector3Int> output1 = new List<Vector3Int>();
        List<Vector3Int> output2 = new List<Vector3Int>();
        List<Vector3Int> output3 = new List<Vector3Int>();
        List<Vector3Int> output4 = new List<Vector3Int>();
        List<List<Vector3Int>> final_output = new List<List<Vector3Int>>();
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    output1.Add(new Vector3Int(x, y, z));
                    output2.Add(new Vector3Int(-x, y, -z));
                    output3.Add(new Vector3Int(x, -y, z));
                    output4.Add(new Vector3Int(-x, -y, -z));
                }
            }
        }
        final_output.Add(output1);
        final_output.Add(output2);
        //for reverse
        final_output.Add(output3);
        final_output.Add(output4);
        return final_output;
    }

    private Vector3 FindClosesWall(Vector3Int vec)
    {
        Vector3 worldpositionVec = gridSystem.CellToWorldCenterBottom(vec);
        List<Vector3> allwall = new List<Vector3>();
        foreach (var listOfWallGameobject in wallStored.Values)
        {
            foreach (var wall in listOfWallGameobject)
            {
                allwall.Add(wall.transform.position);
            }
        }
        var closestwall = allwall[0];
        foreach (var wall in allwall)
        {
            if (Vector3.Distance(worldpositionVec, wall) < Vector3.Distance(worldpositionVec, closestwall))
            {
                closestwall = wall;
            }
        }
        var vector2wall = worldpositionVec - closestwall;
        if (Mathf.Abs(vector2wall.x) == Mathf.Abs(vector2wall.z))
        {
            return new Vector3(Mathf.Sign(vector2wall.x), 0, Mathf.Sign(vector2wall.z));
        }
        else if (Mathf.Abs(vector2wall.x) > Mathf.Abs(vector2wall.z))
        {
            return new Vector3(Mathf.Sign(vector2wall.x), 0, 0);
        }
        else
        {
            return new Vector3(0, 0, Mathf.Sign(vector2wall.z));
        }
    }

    private void UpdateObjectNumber()
    {
        for (int i = 0; i < objectInputField.Count; i++)
        {
            objectNumber[roomObjectAvailable[i]] = Int16.Parse(objectInputField[i].text);
        }
    }

    private void MakeCellsUnavaliable(List<Vector3Int> list)
    {
        //for (var categorie = 0; categorie < gridCategories.Count; categorie++)
        //{
        //    for (var cell = 0; cell < gridCategories[categorie].Count; cell++)
        //    {
        //        if (list.Contains(gridCategories[categorie][cell]))
        //        {
        //            gridCategories[categorie].RemoveAt(cell);
        //        }
        //    }
        //}
        foreach (var cellToRemove in list)
        {
            foreach (var category in gridCategories)
            {
                category.Remove(cellToRemove);
            }
        }
    }

    private Vector3 FindMidBottomAveragePos(List<Vector3Int> listv)
    {
        var absolutemidposition = GetMeanVector(listv);
        List<Vector3Int> midcellposition = new List<Vector3Int>();
        List<Vector3> midcellworldposition = new List<Vector3>();
        //dealing with x
        var midx = new List<int>();
        var midz = new List<int>();
        if (absolutemidposition.x % 1 != 0)
        {
            midx.Add(Mathf.CeilToInt(absolutemidposition.x));
            midx.Add(Mathf.FloorToInt(absolutemidposition.x));
        }
        else
        {
            midx.Add((int)absolutemidposition.x);
        }
        if (absolutemidposition.z % 1 != 0)
        {
            midz.Add(Mathf.CeilToInt(absolutemidposition.z));
            midz.Add(Mathf.FloorToInt(absolutemidposition.z));
        }
        else
        {
            midz.Add((int)absolutemidposition.z);
        }
        //find the lowest y
        var miny = 99;
        foreach (var cell in listv)
        {
            if (cell.y < miny) miny = cell.y;
        }
        foreach (var x in midx)
        {
            foreach (var z in midz)
            {
                midcellposition.Add(new Vector3Int(x, miny, z));
            }
        }
        foreach (var item in midcellposition)
        {
            midcellworldposition.Add(gridSystem.CellToWorldCenterBottom(item));
        }

        return GetMeanVector(midcellworldposition);
    }

    private Vector3 GetMeanVector(List<Vector3> positions)
    {
        if (positions.Count == 0)
            return Vector3.zero;

        float x = 0f;
        float y = 0f;
        float z = 0f;

        foreach (Vector3 pos in positions)
        {
            x += pos.x;
            y += pos.y;
            z += pos.z;
        }
        return new Vector3(x / positions.Count, y / positions.Count, z / positions.Count);
    }

    private Vector3 GetMeanVector(List<Vector3Int> positions)
    {
        if (positions.Count == 0)
            return Vector3.zero;

        float x = 0f;
        float y = 0f;
        float z = 0f;

        foreach (Vector3 pos in positions)
        {
            x += pos.x;
            y += pos.y;
            z += pos.z;
        }
        return new Vector3(x / positions.Count, y / positions.Count, z / positions.Count);
    }

    private bool CheckBB(List<Vector3Int> bb_offset, Vector3Int v)
    {
        foreach (var t in bb_offset)
        {
            if (!IsCellAvaliable(v + t))
            {
                return false;
            }
        }
        return true;
    }

    private bool IsCellAvaliable(Vector3Int cell)
    {
        foreach (var list in gridCategories)
        {
            if (list.Contains(cell))
            {
                return true;
            }
        }
        return false;
    }

    //Helping function to substract list from list
    private List<Vector3Int> ListSubtract(List<Vector3Int> a, List<Vector3Int> b)
    {
        List<Vector3Int> result = new List<Vector3Int>(a.Except(b).ToList());
        return result;
    }

    //generate wall around floor tiles
    private void GenerateWall()
    {
        var list = new List<Vector3Int>(floorStored.Keys);
        foreach (var grid in list)//for every wall cell in the list
        {
            if (!gridSystem.GetCellfromGridCord(grid + new Vector3Int(1, 0, 0)).roomArea)
            {
                Vector3 position = gridSystem.GetMiddleOfTheCellCornerPostion(grid)[0] + new Vector3(0, wallHeight / 2, 0);
                Quaternion rotation = Quaternion.Euler(0, 0, 90);

                var wallPlaceHolder = Instantiate(wallPrefab, position, rotation);
                wallPlaceHolder.transform.localScale = new Vector3(wallHeight, 1, gridSystem.GetGridSize()) * 0.1f;
                wallPlaceHolder.transform.parent = wallParent.transform;
                wallStored["x+"].Add(wallPlaceHolder);
            }
            if (!gridSystem.GetCellfromGridCord(grid + new Vector3Int(-1, 0, 0)).roomArea)
            {
                Vector3 position = gridSystem.GetMiddleOfTheCellCornerPostion(grid)[1] + new Vector3(0, wallHeight / 2, 0);
                Quaternion rotation = Quaternion.Euler(0, 0, -90);

                var wallPlaceHolder = Instantiate(wallPrefab, position, rotation);
                wallPlaceHolder.transform.localScale = new Vector3(wallHeight, 1, gridSystem.GetGridSize()) * 0.1f;
                wallPlaceHolder.transform.parent = wallParent.transform;
                wallStored["x-"].Add(wallPlaceHolder);
            }
            if (!gridSystem.GetCellfromGridCord(grid + new Vector3Int(0, 0, 1)).roomArea)
            {
                Vector3 position = gridSystem.GetMiddleOfTheCellCornerPostion(grid)[2] + new Vector3(0, wallHeight / 2, 0);
                Quaternion rotation = Quaternion.Euler(-90, 0, 0);

                var wallPlaceHolder = Instantiate(wallPrefab, position, rotation);
                wallPlaceHolder.transform.localScale = new Vector3(gridSystem.GetGridSize(), 1, wallHeight) * 0.1f;
                wallPlaceHolder.transform.parent = wallParent.transform;
                wallStored["z+"].Add(wallPlaceHolder);
            }
            if (!gridSystem.GetCellfromGridCord(grid + new Vector3Int(0, 0, -1)).roomArea)
            {
                Vector3 position = gridSystem.GetMiddleOfTheCellCornerPostion(grid)[3] + new Vector3(0, wallHeight / 2, 0);
                Quaternion rotation = Quaternion.Euler(90, 0, 0);

                var wallPlaceHolder = Instantiate(wallPrefab, position, rotation);
                wallPlaceHolder.transform.localScale = new Vector3(gridSystem.GetGridSize(), 1, wallHeight) * 0.1f;
                wallPlaceHolder.transform.parent = wallParent.transform;
                wallStored["z-"].Add(wallPlaceHolder);
            }
        }
    }

    //able for a Dynamic change of wall height
    private void ChangeWallHeight()
    {
        if (currentHeight != wallHeight)
        {
            foreach (var item in wallStored["x+"])
            {
                //change all wall to match the wallheight
                item.transform.position = new Vector3(item.transform.position.x, wallHeight / 2, item.transform.position.z);
                item.transform.localScale = new Vector3(wallHeight, 1, gridSystem.GetGridSize()) * 0.1f;
            }
            foreach (var item in wallStored["x-"])
            {
                //change all wall to match the wallheight
                item.transform.position = new Vector3(item.transform.position.x, wallHeight / 2, item.transform.position.z);
                item.transform.localScale = new Vector3(wallHeight, 1, gridSystem.GetGridSize()) * 0.1f;
            }
            foreach (var item in wallStored["z+"])
            {
                //change all wall to match the wallheight
                item.transform.position = new Vector3(item.transform.position.x, wallHeight / 2, item.transform.position.z);
                item.transform.localScale = new Vector3(gridSystem.GetGridSize(), 1, wallHeight) * 0.1f;
            }
            foreach (var item in wallStored["z-"])
            {
                //change all wall to match the wallheight
                item.transform.position = new Vector3(item.transform.position.x, wallHeight / 2, item.transform.position.z);
                item.transform.localScale = new Vector3(gridSystem.GetGridSize(), 1, wallHeight) * 0.1f;
            }
            currentHeight = wallHeight;
        }
    }

    //find the edge cell from a list of vertex
    private List<Vector3Int> FindEdgeCell(List<Vector3Int> list)
    {
        List<Vector3Int> output = new List<Vector3Int>();
        foreach (var cell in list) // iterate each cell in the list
        {
            // check the neighbour of each cell
            foreach (var edge in NeighbourIncludingDiagonalOf(cell))
            {
                if (!list.Contains(edge))
                {
                    if (!output.Contains(cell))
                    {
                        output.Add(cell);
                    }
                }
            }
        }
        return output;
    }

    //identifying if the list of vertex are in a single cluster or not
    private bool isRoomValid(List<Vector3Int> list)
    {
        if (list.Count == 0)
        {
            return false;
        }
        List<Vector3Int> listOfHighlightedVector = new List<Vector3Int>(list);
        //generate a output list
        List<Vector3Int> outputList = new List<Vector3Int>();
        List<Vector3Int> temp = new List<Vector3Int>();
        //take the first element of the list and put it into the output list
        outputList.Add(listOfHighlightedVector[0]);
        listOfHighlightedVector.RemoveAt(0);

        //looking for neighbour tile in the list
        while (true)
        {
            foreach (var cell in outputList)
            {
                //get find all the neighour cell
                var nearbyCells = NeighbourOf(cell);
                //check if each of the neighourcell is in list or not if yes add to the output list
                foreach (var nearbyCell in nearbyCells)
                {
                    if (listOfHighlightedVector.Contains(nearbyCell))
                    {
                        temp.Add(nearbyCell);
                        listOfHighlightedVector.Remove(nearbyCell);
                    }
                }
            }
            if (temp.Count > 0)
            {
                outputList.AddRange(temp);
                temp.Clear();
            }
            else break;
        }
        if (listOfHighlightedVector.Count > 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    //helper function
    private List<Vector3Int> FindNeighbourInList(List<Vector3Int> group, List<Vector3Int> list)
    {
        List<Vector3Int> outputList = new List<Vector3Int>();
        foreach (var cell in group)
        {
            var nearbyCells = NeighbourOf(cell);
            foreach (var nearbyCell in nearbyCells)
            {
                if (list.Contains(nearbyCell))
                {
                    outputList.Add(nearbyCell);
                    list.Remove(nearbyCell);
                }
            }
        }
        return outputList;
    }

    //find neibour cell
    private List<Vector3Int> NeighbourOf(Vector3Int v)
    {
        List<Vector3Int> outputList = new List<Vector3Int>();
        //above
        outputList.Add(v + new Vector3Int(1, 0, 0));
        outputList.Add(v + new Vector3Int(-1, 0, 0));
        outputList.Add(v + new Vector3Int(0, 0, 1));
        outputList.Add(v + new Vector3Int(0, 0, -1));
        return outputList;
    }

    //find neibour cell including the diagonal
    private List<Vector3Int> NeighbourIncludingDiagonalOf(Vector3Int v)
    {
        List<Vector3Int> outputList = new List<Vector3Int>();
        //above
        outputList.Add(v + new Vector3Int(1, 0, 0));
        outputList.Add(v + new Vector3Int(1, 0, 1));
        outputList.Add(v + new Vector3Int(1, 0, -1));
        outputList.Add(v + new Vector3Int(-1, 0, 0));
        outputList.Add(v + new Vector3Int(-1, 0, 1));
        outputList.Add(v + new Vector3Int(-1, 0, -1));
        outputList.Add(v + new Vector3Int(0, 0, 1));
        outputList.Add(v + new Vector3Int(0, 0, -1));
        return outputList;
    }

    #endregion Helper Function
}