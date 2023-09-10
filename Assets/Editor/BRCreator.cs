using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Linq;

public class BRCreator : EditorWindow
{
    private Label main;
    private Button loadButton;
    private Button saveButton;
    private Button spawnCheckpointButton;
    private Button removeCheckpointButton;

    private Button addStartPositionButton;
    private TextField textField;

    private RaceConfig raceConfig;
    private Label raceMeta;


    [MenuItem("Window/UI Toolkit/BRCreator")]
    public static void ShowExample()
    {
        BRCreator wnd = GetWindow<BRCreator>();
        wnd.titleContent = new GUIContent("BRCreator");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        VisualElement header = new Label("Bomb Rush Cyberfunk Race Creator plugin for Unity.");
        header.style.fontSize = 20;
        header.style.marginBottom = 20;
        root.Add(header);

        main = new Label("Please select a race file!");
        main.style.fontSize = 16;
        main.style.marginBottom = 20;
        root.Add(main);

        raceMeta = new Label();
        raceMeta.style.fontSize = 18;
        raceMeta.style.marginBottom = 30;
        root.Add(raceMeta);

        textField = new TextField("File Path");
        textField.name = "FilePath";
        root.Add(textField);

        loadButton = new Button();
        loadButton.name = "ActionLoad";
        loadButton.text = "Load a race";
        loadButton.style.marginBottom = 20;
        root.Add(loadButton);

        addStartPositionButton = new Button();
        addStartPositionButton.name = "ActionAddStartPosition";
        addStartPositionButton.text = "Add a start position at the current position";
        addStartPositionButton.style.marginBottom = 20;

        spawnCheckpointButton = new Button();
        spawnCheckpointButton.name = "ActionSpawnCheckpoint";
        spawnCheckpointButton.text = "Spawn a checkpoint at the current position";
        spawnCheckpointButton.style.marginBottom = 20;

        removeCheckpointButton = new Button();
        removeCheckpointButton.name = "ActionRemoveCheckpoint";
        removeCheckpointButton.text = "Remove the last checkpoint";
        removeCheckpointButton.style.marginBottom = 20;

        saveButton = new Button();
        saveButton.name = "ActionSave";

        saveButton.text = "Save the current race";

        loadButton.RegisterCallback<ClickEvent>(LoadRace);
        saveButton.RegisterCallback<ClickEvent>(SaveRace);
        spawnCheckpointButton.RegisterCallback<ClickEvent>(SpawnCheckpoint);
        removeCheckpointButton.RegisterCallback<ClickEvent>(RemoveCheckpoint);
        addStartPositionButton.RegisterCallback<ClickEvent>(AddStartPosition);
    }

    private void AddStartPosition(ClickEvent evt)
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        Vector3 viewerPosition = sceneView.camera.transform.position;

        raceConfig.StartPosition = new SerDesVector3(viewerPosition.x, viewerPosition.y, viewerPosition.z);

        main.text = "Start position added!";
        Debug.Log($"Start position added at {viewerPosition.x} {viewerPosition.y} {viewerPosition.z}");
    }

    private void RemoveCheckpoint(ClickEvent evt)
    {
        var current_cp = UnityEngine.Object.FindObjectsOfType<BoxCollider>().Where(x => x.name.StartsWith("brc-checkpoint-")).ToList();
        if (current_cp.Count() == 0)
        {
            main.text = "No checkpoints to remove!";
            return;
        }

        var last = current_cp.First();
        UnityEngine.Object.DestroyImmediate(last.gameObject);

        raceConfig.MapPins.Remove(raceConfig.MapPins.First());

        main.text = "Checkpoint removed!";
    }

    private void SpawnCheckpoint(ClickEvent evt)
    {
        var next_cp = $"brc-checkpoint-{raceConfig.MapPins.Count() + 1}";
        SceneView sceneView = SceneView.lastActiveSceneView;
        Vector3 viewerPosition = sceneView.camera.transform.position;

        var cp = new GameObject(next_cp).AddComponent<BoxCollider>();
        cp.transform.position = viewerPosition;
        cp.isTrigger = true;
        cp.size = new Vector3(5.5f, 10.5f, 5.5f); //This is hardcoded in SlopCrew !

        raceConfig.MapPins.Add(new SerDesVector3(cp.transform.position.x, cp.transform.position.y, cp.transform.position.z));

        main.text = "Checkpoint added!";
    }

    private void SaveRace(ClickEvent evt)
    {
        var current_cp = UnityEngine.Object.FindObjectsOfType<BoxCollider>().Where(x => x.name.StartsWith("brc-checkpoint-")).ToList();
        current_cp.Reverse();

        var pins = new List<SerDesVector3>();
        foreach (var cp in current_cp)
        {
            pins.Add(new SerDesVector3(cp.transform.position.x, cp.transform.position.y, cp.transform.position.z));
        }

        raceConfig.MapPins = pins;

        raceConfig.ToFile(textField.value);

        main.text = "Race saved!";
    }

    private void LoadRace(ClickEvent evt)
    {
        if (string.IsNullOrEmpty(textField.value))
        {
            main.text = "Please enter a file path!";
            return;
        }

        if (!textField.value.EndsWith(".json"))
        {
            main.text = "Please enter a valid file path (.json)!";
            return;
        }

        try
        {
            raceConfig = RaceConfig.ToRaceConfig(textField.value);
        }
        catch (Exception e)
        {
            main.text = "Error loading file, check console!";
            Debug.LogException(e);
            return;
        }

        var old_cp = UnityEngine.Object.FindObjectsOfType<BoxCollider>().Where(x => x.name.StartsWith("brc-checkpoint-")).ToArray();
        foreach (var old in old_cp)
        {
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        int i = 1;
        foreach (var pin in raceConfig.MapPins)
        {
            var cp = new GameObject($"brc-checkpoint-{i}").AddComponent<BoxCollider>();
            cp.transform.position = new Vector3(pin.X, pin.Y, pin.Z);
            cp.isTrigger = true;
            cp.size = new Vector3(5.5f, 10.5f, 5.5f); //This is hardcoded in SlopCrew !

            i++;
        }

        main.text = "Race loaded!";

        var old_start = UnityEngine.Object.FindObjectsOfType<BoxCollider>().FirstOrDefault(x => x.name.Equals("brc-start-position"));
        if (old_start != null)
        {
            UnityEngine.Object.DestroyImmediate(old_start.gameObject);
        }

        if (raceConfig.StartPosition != null)
        {
            var cp = new GameObject($"brc-start-position").AddComponent<BoxCollider>();
            cp.transform.position = new Vector3(raceConfig.StartPosition.X, raceConfig.StartPosition.Y, raceConfig.StartPosition.Z);
            cp.isTrigger = false;
            cp.size = new Vector3(1.0f, 1.0f, 1.0f);
        }

        raceMeta.text = $"Stage: {raceConfig.GetInGameName()} \n" +
        $"Start Position: ({raceConfig.StartPosition.X}  {raceConfig.StartPosition.Y}  {raceConfig.StartPosition.Z}) \n" +
        $"Checkpoints: {raceConfig.MapPins.Count()} \n";

        rootVisualElement.Add(addStartPositionButton);
        rootVisualElement.Add(spawnCheckpointButton);
        rootVisualElement.Add(removeCheckpointButton);
        rootVisualElement.Add(saveButton);
    }

}


public class RaceConfig
{
    public int Stage { get; set; } = -1;
    public ICollection<SerDesVector3> MapPins { get; set; } = new List<SerDesVector3>();

    public SerDesVector3 StartPosition { get; set; } = new SerDesVector3(0, 0, 0);

    public string GetInGameName()
    {
        switch (Stage)
        {
            case 5:
                return "Hideout";
            case 11:
                return "MILLENNIUM SQUARE";
            case 4:
                return "Versum hill";
            case 6:
                return "Millenium mall";
            case 12:
                return "Brink terminal";
            case 9:
                return "Pyramid Island";
            case 7:
                return "Mataan";
            default:
                return "Unknown (update GetInGameName)";

        }
    }

    public static RaceConfig ToRaceConfig(string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open);
        using var sr = new StreamReader(fileStream, Encoding.UTF8);
        using var reader = new JsonTextReader(sr);

        var serializer = JsonSerializer.CreateDefault();

        return serializer.Deserialize<RaceConfig>(reader);
    }

    public void ToFile(string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Create);
        using var sr = new StreamWriter(fileStream, Encoding.UTF8);
        using var writer = new JsonTextWriter(sr);

        var serializer = JsonSerializer.CreateDefault();
        serializer.Serialize(writer, this);
    }
}

public class SerDesVector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }


    public SerDesVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}