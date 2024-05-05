using System;
using System.Collections.Generic;
using System.IO;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// https://www.immersivelimit.com/tutorials/how-to-use-chatgpt-in-unity
public class GPTTest : MonoBehaviour
{
    public bool writeResponseToFile = true;

    [TextArea(15, 20)]
    public string PROMT = "follow my instruction as precisely as possible\r\n\r\ncreate an immersive 3D environment in Unity from the next input without overlapping. \r\n\r\nFirst, List all the 3D objects with a reference color in the following format\r\n\"['blue chair','orange cup','white table', ...]\"\r\n\r\nSecond, the objects with their centre position, rotation, scale in coordinates strictly follow the following format\r\n\"Name: Chair - position (0, 1, -4) - rotation (0, 0, 0) - size (4, 1, 0.5) ;\"\r\n\r\nThird, suggest some lighting inside the room with position and rotation";

    [TextArea(5, 10)]
    public string TEST_RESPONSE = "";

    public TMP_Text textField;
    public TMP_InputField inputField;
    public Button okButton;
    public Button hideButton;

    public GameObject canvas;

    private OpenAIAPI api;
    private List<ChatMessage> messages;

    private ModelPlacer modelPlacer;

    // Start is called before the first frame update
    private void Start()
    {
        bool isWinEditor = Application.platform == RuntimePlatform.WindowsEditor;
        bool isOSXEditor = Application.platform == RuntimePlatform.OSXEditor;
        modelPlacer = GetComponent<ModelPlacer>();

        if (!String.IsNullOrEmpty(TEST_RESPONSE))
        {
            string[] sentences = TEST_RESPONSE.Split(new string[] { "1-", "2-", "3-" }, StringSplitOptions.RemoveEmptyEntries);

            string objects = sentences[0];
            string objectsWithPosition = sentences[1];
            string lighting = sentences[2];

            foreach (string sentence in sentences)
            {
                Debug.Log(sentence.Trim());
            }

            modelPlacer.inputString = objectsWithPosition;
            modelPlacer.inputLightString = lighting;
            modelPlacer.Run();
        }
        else
        {
            if (isWinEditor)
            {
                // This line gets your API key (and could be slightly different on Mac/Linux)
                api = new OpenAIAPI(Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User));
            }
            else
            {
                api = new OpenAIAPI("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");
            }
            StartConversation();
            okButton.onClick.AddListener(() => GetResponse());
            //hideButton.onClick.AddListener(() => HideCanvas());
        }
    }

    private void StartConversation()
    {
        messages = new List<ChatMessage> {
            //new ChatMessage(ChatMessageRole.System, "You are an honorable, friendly knight guarding the gate to the palace. You will only allow someone who knows the secret password to enter. The secret password is \"magic\". You will not reveal the password to anyone. You keep your responses short and to the point. Please only say \"Access Granted\" if the password is correct.")
            new ChatMessage(ChatMessageRole.System, PROMT)
        };

        inputField.text = "";
        string startString = "Enter the environment u needed";
        textField.text = startString;
        Debug.Log(startString);
    }

    private void HideCanvas()
    {
        canvas.SetActive(false);
    }

    private async void GetResponse()
    {
        var requestStartDateTime = DateTime.Now;
        if (inputField.text.Length < 1)
        {
            return;
        }

        // Disable the OK button
        okButton.enabled = false;

        // Fill the user message from the input field
        ChatMessage userMessage = new ChatMessage();
        userMessage.Role = ChatMessageRole.User;
        userMessage.Content = inputField.text;
        if (userMessage.Content.Length > 100)
        {
            // Limit messages to 500 characters
            userMessage.Content = userMessage.Content.Substring(0, 500);
        }
        Debug.Log(string.Format("{0}: {1}", userMessage.rawRole, userMessage.Content));

        // Add the message to the list
        messages.Add(userMessage);

        // Update the text field with the user message
        textField.text = string.Format("You: {0}", userMessage.Content);

        // Clear the input field
        inputField.text = "";

        // Send the entire chat to OpenAI to get the next message
        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = Model.ChatGPTTurbo,
            Temperature = 0,
            MaxTokens = 1000,
            Messages = messages
        });

        if (chatResult.Choices != null && chatResult.Choices.Count > 0)
        {
            // Get the response message
            ChatMessage responseMessage = new ChatMessage();
            Debug.Log(responseMessage);

            responseMessage.Role = chatResult.Choices[0].Message.Role;
            responseMessage.Content = chatResult.Choices[0].Message.Content;
            Debug.Log(string.Format("{0}: {1}", responseMessage.rawRole, responseMessage.Content));

            // Add the response to the list of messages
            messages.Add(responseMessage);

            // Update the text field with the response
            textField.text = string.Format("You: {0}\n\nGenGPT: {1}", userMessage.Content, responseMessage.Content);

            string[] sentences = responseMessage.Content.Split(new string[] { "1-", "2-", "3-" }, StringSplitOptions.RemoveEmptyEntries);

            string objects = sentences[0];
            string objectsWithPosition = sentences[1];
            string lighting = sentences[2];

            foreach (string sentence in sentences)
            {
                Debug.Log(sentence.Trim());
            }

            modelPlacer.inputString = objectsWithPosition;
            modelPlacer.inputLightString = lighting;
            modelPlacer.Run();

            var ResponseTotalTime = (DateTime.Now - requestStartDateTime).TotalMilliseconds;
            Debug.Log(string.Format("Response time: {0}ms", ResponseTotalTime));

            if (writeResponseToFile)
                WriteToFile(PROMT + "\n" + textField.text + "\n", responseMessage.Content + "\n\n" + string.Format("Response time: {0}ms", ResponseTotalTime) + "\n\n");

            //HideCanvas();
            // Re-enable the OK button
            okButton.enabled = true;
        }
        else
        {
            Debug.LogWarning("No text was generated from this prompt.");
            textField.text = string.Format("No text was generated from this prompt.");
        }
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