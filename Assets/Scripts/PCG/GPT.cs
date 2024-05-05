using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GPT : MonoBehaviour
{
    public bool writeResponseToFile = true;
    public bool useTestResponse = false;

    [TextArea(10, 50)]
    public List<string> PROMT = new List<String> ( new[]{
        "Let's work this out in a step by step way to be sure we have the right answer.\r\n\r\nvery important to follow the following instructions as precisely as possible, very important to generate output in my specific format without additional context and symbol, do not generate title and description for each questions\r\n\r\nAs a professional room designer, create an immersive 3D environment from the next input, generate at least 10 distinct object\r\n\r\n1- Generate a list of 3D objects, with a reference color, add the name of the environment between the color and the object, assign each object a unique English alphabet and replace all future output from the name to the letter assigned when asked, strictly follow the following format \r\n1- blue classroom chair: A, orange classroom plant: B, red classroom desk: C, ...:\r\n\r\nOnly use the assigned letters from now on\r\n\r\n2- From the objects from the first output, filter out the objects only placed on the floor as furniture alone, for example table and chair\r\n2- ...,...\r\n\r\n", 
        "3- place the rest of the objects on top of the second output if applicable\r\n3- ...+...,...+...\r\n\r\n4- catergorize the item into 4 category by replacing the number inside the bracket, each item can only in one category, hanging on the wall:2, hanging from the ceiling:3, lean against the wall:0, middle of the room:1\r\n4- ...:0\r\n\r\n5- generate reference size as heights for all object in meter \r\n5- ...:m \r\n\r\nKnowledge: The room is 20x20x5 in meter\r\n\r\n6- Use all the previous output as knowledge, suggest the amount of all object from the user input\r\n6- ...:1\r\n\r\n7- Generate position for object only in output 2 according to the amount suggested from output 6, spread out the object, where 0,0,0 is the center of the room\r\n7- ...:x;y;z" });

    [TextArea(5, 10)]
    public string testReponse;

    [TextArea(10, 50)] 
    public string gptResultRaw;

    public string banObjects;

    public TMP_Text textField;
    public TMP_InputField inputField;
    public Button okButton;
    public GameObject canvas;

    private OpenAIAPI api;
    private List<ChatMessage> messages;

    private ShapE shapE;
    public static GPT Instance { get; private set; }

    public string floorMaterial = "";
    private string userMsg = "";
    private string wallInfoRaw = "";

    private Tuple<bool, float> wallInfo = new Tuple<bool, float>(false, 0);
    private Dictionary<string, string> objLetter = new Dictionary<string, string>();

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

    // Start is called before the first frame update
    private void Start()
    {
        shapE = GameObject.Find("GridSystem").GetComponent<ShapE>();
        api = new OpenAIAPI("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");

        //BAN OBJECTS
        PROMT[0] = PROMT[0].Replace("[BAN_LIST]", banObjects);

        StartConversation();
    }

    private void StartConversation()
    {
        messages = new List<ChatMessage>();

        inputField.text = "";
        string startString = "GenGPT: Enter the environment you want to create";
        textField.text = startString;
        //Debug.Log(startString);
    }

    public async Task<Tuple<string, Dictionary<string, Tuple<float, int, List<string>, int, List<Vector3>>>>> GetResponse(int promptIndex)
    {
        
        Debug.Log("GetResponse is gettin called");
        if (useTestResponse)
        {
            UIManager.Instance.SetStatus("GPT: Using Test Prompt", true);
            if (promptIndex == 0) {return HandleResponse(testReponse, 0); }
            else
            { HandleResponse(testReponse, 1); }
            return null;
        }
        var requestStartDateTime = DateTime.Now;
        if (inputField.text.Length < 1 && promptIndex == 0)
        {
            textField.text += "\n\nPlease enter a message";
            return null;
        }
        if (promptIndex == 0)
        {
            userMsg = inputField.text;
        }
        // Disable the OK button
        okButton.enabled = false;
        
        if(promptIndex == 0) {        
            messages.Add(new ChatMessage(ChatMessageRole.System, PROMT[0]));
        }
        else {
            messages.Add(new ChatMessage(ChatMessageRole.System, PROMT[0] + "\n" + PROMT[1]));
        }

        // Fill the user message from the input field
        ChatMessage userMessage = new ChatMessage
        {
            Role = ChatMessageRole.User,
            Content = userMsg
        };
        //if (userMessage.Content.Length > 100)
        //{
        //    // Limit messages to 500 characters
        //    userMessage.Content = userMessage.Content.Substring(0, 100);
        //}
        Debug.Log(string.Format("{0}: {1}", userMessage.rawRole, userMessage.Content));

        // Add the message to the list
        messages.Add(userMessage);

        // Update the text field with the user message
        textField.text = string.Format("You: {0}", userMessage.Content);

        // Clear the input field
        inputField.text = "";

        textField.text += "\nGenerating response...\n\n";
        UIManager.Instance.ProgressStatus("GPT");
        // Send the entire chat to OpenAI to get the next message
        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = Model.GPT4,
            Temperature = 0,
            MaxTokens = 1000,
            Messages = messages
        });

        if (chatResult.Choices != null && chatResult.Choices.Count > 0)
        {
            UIManager.Instance.StopProgressStatus();
            // Get the response message
            ChatMessage responseMessage = new ChatMessage();

            responseMessage.Role = chatResult.Choices[0].Message.Role;
            responseMessage.Content = chatResult.Choices[0].Message.Content;
            Debug.Log(string.Format("{0}: {1}", responseMessage.rawRole, responseMessage.Content));

            // Add the response to the list of messages
            messages.Add(responseMessage);

            // Update the text field with the original message and response
            textField.text = string.Format("You: {0}\n\nGenGPT: {1}", userMessage.Content, responseMessage.Content);

            var response = HandleResponse(responseMessage.Content, promptIndex);

            var responseTotalTime = (DateTime.Now - requestStartDateTime).TotalMilliseconds;
            Debug.Log(string.Format("Response time: {0}ms", responseTotalTime));

            if (writeResponseToFile && promptIndex == 1)
                WriteToFile(PROMT + "\n" + textField.text + "\n", responseMessage.Content + "\n\n" + string.Format("Response time: {0}ms", responseTotalTime) + "\n\n");
            
            gptResultRaw = responseMessage.Content;
            return new Tuple<string, Dictionary<string, Tuple<float, int, List<string>, int, List<Vector3>>>>(response.Item1, response.Item2);
        }

        UIManager.Instance.StopProgressStatus();
        Debug.LogWarning("No text was generated from this prompt.");
        textField.text = "No text was generated from this prompt.";
        return null;
    }

    private string objects = String.Empty;
    private List<string> objList = new List<string>();
    private Tuple<string, Dictionary<string, Tuple<float, int, List<string>, int, List<Vector3>>>> HandleResponse(string responseMessage, int promptIndex)
    {
        string objectWithLetter = String.Empty;    //1 - all obj: A
        string objectOnGround = String.Empty;      //2 - obj (ground only)

        var objInfo = new Dictionary<string, Tuple<float, int, List<string>, int, List<Vector3>>>();

        // Split the response into sentences
        string[] sentences = responseMessage.Split(new[] { "1-", "2-", "3-", "4-", "5-", "6-", "7-", "8-", "9-" }, StringSplitOptions.RemoveEmptyEntries);
        if (promptIndex == 0)
        {
            objectWithLetter = sentences[0].Trim('\r', '\n').TrimEnd('.');

            // object letter
            foreach (var item in objectWithLetter.Split(","))   // cup: A
            {
                var temp = item.Split(":");
                //Debug.Log(temp[1].Trim() + ":" + temp[0].Trim());
                objLetter.Add(temp[1].Trim(), temp[0].Trim());    // A: cup
            }

            foreach (var item in objLetter.Keys)
            {
                Debug.Log(objLetter[item]);
                objList.Add(objLetter[item]);
            }

            objects = String.Join(",", objList.ToArray()); //obj list for obj gen format: ...,...

            //foreach(var item in objects)
            //{
            //    Debug.Log(item);
            //}

            floorMaterial = sentences[2].Trim('\r', '\n').TrimEnd('.').ToLower();
            wallInfoRaw = sentences[3].Trim('\r', '\n').TrimEnd('.').ToLower();

            return new (objects, null);
        }
        else
        {
            StartCoroutine(HandleInfo(sentences));
        }

        return null;
    }
     
    private IEnumerator HandleInfo(string[] sentences)
    {
        string objectOnGround = String.Empty;      //2 - obj (ground only)
        string objectOnObject = String.Empty;      //5 - obj + obj
        string objectType = String.Empty;          //6 - obj: (0,1,2,3,4,5)
        string objectSize = String.Empty;          //7 - obj: size
        string objectAmount = String.Empty;        //8 - obj: amount
        string objectCord = String.Empty;          //9 - obj: x;y;z (ground only)

        objectOnGround = sentences[1].Trim('\r', '\n').TrimEnd('.');
        objectOnObject = sentences[4].Trim('\r', '\n').TrimEnd('.');
        objectType = sentences[5].Trim('\r', '\n').TrimEnd('.');
        objectSize = sentences[6].Trim('\r', '\n').TrimEnd('.');
        objectAmount = sentences[7].Trim('\r', '\n').TrimEnd('.'); // amount of all object
        objectCord = sentences[8].Trim('\r', '\n').TrimEnd('.'); // coord for all ground object
                                                                 //Debug.Log(objectType);
        // object on ground
        List<string> objOnGroundList = new ();
        foreach (var item in objectOnGround.Split(','))
        {
            objOnGroundList.Add(GetObjName(item));
        }

        // object allow on
        Dictionary<string, List<string>> objAllowOnDict = new Dictionary<string, List<string>>();
        foreach (var item in objectOnObject.Split(","))
        {
            
            //cup + table
            var objName = item.Split("+");
            if (!objAllowOnDict.Keys.Contains(GetObjName(objName[0])))
            {
                objAllowOnDict.Add(GetObjName(objName[0]), new List<string>());
            }
            Debug.Log(GetObjName(objName[0]) + " can be put on top of " + GetObjName(objName[1]));
            objAllowOnDict[GetObjName(objName[0])].Add(GetObjName(objName[1]));
        }

        // object type
        Dictionary<string, int> objTypeDict = new Dictionary<string, int>();
        foreach (var item in objectType.Split(","))
        {
            var temp = item.Split(":");
            //Debug.Log(temp[0].Trim() + " ; "+ temp[1].Trim());
            objTypeDict.Add(GetObjName(temp[0]), int.Parse(temp[1]));
        }

        // object size
        Dictionary<string, float> objSizeDict = new Dictionary<string, float>();
        foreach (var item in objectSize.Split(","))
        {
            var temp = item.Split(":");
            var size = temp[1].Trim();
            objSizeDict.Add(GetObjName(temp[0]), float.Parse(size.Remove(size.Length - 1, 1)));
        }

        // object amount
        Dictionary<string, int> objAmountDict = new Dictionary<string, int>();
        foreach (var item in objectAmount.Split(","))
        {
            var temp = item.Split(":");
            //Debug.Log(temp[0].Trim() + " ; "+ temp[1].Trim());
            objAmountDict.Add(GetObjName(temp[0]), int.Parse(temp[1]));
        }

        // object coord
        Dictionary<string, List<Vector3>> objCoordDict = new Dictionary<string, List<Vector3>>();
        foreach (var item in objectCord.Split(","))
        {
            var temp = item.Split(":");
            var name = GetObjName(temp[0]);
            if (!objCoordDict.Keys.Contains(name))
            {
                objCoordDict.Add(name, new List<Vector3>());
            }
            objCoordDict[name].Add(StringToVector3(temp[1]));
        }

        //Debug.Log(objList);

        foreach (var obj in objList)
        {
            var objName = obj.Trim();
            int objType;
            var objAllowOnList = new List<string>();
            var objCoordList = new List<Vector3>();

            // obj type override if not found from gpt object type dict
            if (!objTypeDict.Keys.Contains(objName))
            {
                // override to middle of the room
                objType = 1;
                Debug.Log(objName + " object type not found, override to " + objType);                
            }
            else
            {
                objType = objTypeDict[objName];
            }

            //if (objType != 0 && objType != 1)
            //{
            //    // force override object type according to object on ground from gpt
            //    if (objOnGroundList.Any(stringToCheck => stringToCheck.Contains(objName)))
            //    {
            //        objType = 1;
            //        Debug.Log(objName + " in ON GROUND, override to " + objType);
            //    }
            //}

            // size override if not found
            float size;
            if (!objSizeDict.Keys.Contains(objName))
            {
                if (objType == 5)   // small objects
                {
                    size = 0.3f;
                }
                else
                {
                    size = 1f;
                }

                Debug.Log(objName + " size not found, override to " + size);
            }
            else
            {
                size = objSizeDict[objName];
            }

            // amount override if not found
            int amount;
            if (!objAmountDict.Keys.Contains(objName))
            {
                amount = 1;
                Debug.Log(objName + " amount not found, override to " + amount);
            }
            else
            {
                amount = objAmountDict[objName];
            }

            if (objAllowOnDict.Keys.Contains(objName))
            {
                objAllowOnList = objAllowOnDict[objName];
            }

            if (objCoordDict.Keys.Contains(objName))
            {
                objCoordList = objCoordDict[objName];
            }

            GameObject.Find("GridSystem").GetComponent<ProceduralGen>().gptResult.Add(objName,
                new (size, objType, objAllowOnList, amount,
                    objCoordList));
        }
        ProceduralGen.Instance.gptreqCompleted = true;
        yield return null;
    }

    public Tuple<bool,float> GetWallInfo()
    {
        var temp = wallInfoRaw.Split(":");
        bool haveWall = bool.Parse(temp[0]);
        if (haveWall)
        {
            return new (haveWall, float.Parse(temp[1].Substring(0, temp[1].Length - 1)) * 2);
        }
        return new (haveWall, 0);
    }

    private List<string> GetPromptSperator(int promptIndex)
    {
        List<string> seperator = new List<string>();
        int maxPromtIndex = PROMT[promptIndex].Where(char.IsDigit).Select(x => int.Parse(x.ToString())).Max();

        for (int i = 0; i == maxPromtIndex; i++)
        {
            seperator.Add(i.ToString() + "-");
        }
        seperator.ForEach(p => Debug.Log(p));
        return seperator;
    }
    private Vector3 StringToVector3(string sVector)
    {
        // split the items
        string[] sArray = sVector.Split(';');

        // store as a Vector3, keeping digits only
        Vector3 result = new Vector3(
            MathF.Ceiling(float.Parse(String.Concat(sArray[0].Where(char.IsDigit)))),
            0,
            MathF.Ceiling(float.Parse(String.Concat(sArray[1].Where(char.IsDigit)))));

        return result;
    }

    private string GetObjName(string itemLetter)
    {
        return (objLetter[(itemLetter).Trim()]).Trim();
    }

    public void ClearTextButton()
    {
        inputField.text = "";
    }

    public void WriteToFile(string str1, string str2)
    {
        string path = Application.dataPath + "/Promt and Response/" + string.Format("{0:yyyy-MM-dd_HH-mm-ss-fff}", DateTime.Now) + ".txt";
        //Create folder if not exist
        Directory.CreateDirectory(Application.dataPath + "/Promt and Response/");
        //Write text to the file
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(str1);
        writer.WriteLine(str2);
        writer.Close();
    }
}