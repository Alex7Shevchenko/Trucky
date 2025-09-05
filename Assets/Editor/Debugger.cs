using System.Linq;
using UnityEditor;
using UnityEngine;

public class Debugger : EditorWindow
{
    private const string KEY_GUID = "DataGUID";
    private const string KEY_MAIN_ATTACHMENT = "MainAttachmentIndex";
    private const string KEY_SECONDARY_ATTACHMENT = "SecondaryAttachmentIndex";
    private const string KEY_THIRD_ATTACHMENT = "ThirdAttachmentIndex";

    private const double NOTIFICATION_TIME = 1d;

    private DebuggerData _debuggerData;
    private PlayerManager[] _playerManagers;
    private string[] _playerNames;
    private int _selectedPlayer;

    private int _mainAttachmentSelection;
    private int _secondaryAttachmentSelection;
    private int _thirdAttachmentSelection;

    [MenuItem("Tools/Debugger")]
    public static void ShowWindow()
    {
        GetWindow<Debugger>("Debugger", true);
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        LoadData();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        SaveData();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _debuggerData = (DebuggerData)EditorGUILayout.ObjectField("Debugger Data", _debuggerData, typeof(DebuggerData), false);

            if (_playerNames == null || _playerManagers == null) return;
            
            _selectedPlayer = EditorGUILayout.Popup("Player", _selectedPlayer, _playerNames);

            GUILayout.Space(6);

            if (_debuggerData == null)
            {
                EditorGUILayout.HelpBox("Assign a DebuggerData asset", MessageType.Info);
                return;
            }

            DrawAttachmentRow("Main Attachment", _debuggerData.mainAttachments, ref _mainAttachmentSelection, () => TryAttach(GetAt(_debuggerData.mainAttachments, _mainAttachmentSelection), AttachmentType.Main));
            DrawAttachmentRow("Secondary Attachment", _debuggerData.secondaryAttachments, ref _secondaryAttachmentSelection, () => TryAttach(GetAt(_debuggerData.secondaryAttachments, _secondaryAttachmentSelection), AttachmentType.Secondary));
            DrawAttachmentRow("Third Attachment", _debuggerData.thirdAttachments, ref _thirdAttachmentSelection, () => TryAttach(GetAt(_debuggerData.thirdAttachments, _thirdAttachmentSelection), AttachmentType.Third));
        }
    }

    private void DrawAttachmentRow(string label, GameObject[] options, ref int index, System.Action onSet)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            var names = ToNames(options);
            using (new EditorGUI.DisabledScope(names.Length == 0))
            {
                index = EditorGUILayout.Popup(label, Mathf.Clamp(index, 0, Mathf.Max(0, names.Length - 1)), names);
                if (GUILayout.Button("Set Attachment", GUILayout.Width(100)))
                    onSet?.Invoke();
            }
        }
    }

    private string[] ToNames(GameObject[] array)
    {
        if (array == null || array.Length == 0)
            return new[] { "(None available)" };

        return array.Select(go => go ? go.name : "<Missing>").ToArray();
    }

    private GameObject GetAt(GameObject[] array, int index)
    {
        if (array == null || array.Length == 0) return null;
        index = Mathf.Clamp(index, 0, array.Length - 1);
        return array[index];
    }

    private void TryAttach(GameObject prefab, AttachmentType type)
    {
        if (_playerManagers == null)
        {
            ShowNotification(new GUIContent("Assign a Player first."), NOTIFICATION_TIME);
            return;
        }
        if (prefab == null)
        {
            ShowNotification(new GUIContent("Select a valid prefab in DebuggerData."), NOTIFICATION_TIME);
            return;
        }

        _playerManagers[_selectedPlayer].PlayerLoadout.AttachAttachment(prefab, type);
    }

    private void SaveData()
    {
        EditorPrefs.SetInt(KEY_MAIN_ATTACHMENT, _mainAttachmentSelection);
        EditorPrefs.SetInt(KEY_SECONDARY_ATTACHMENT, _secondaryAttachmentSelection);
        EditorPrefs.SetInt(KEY_THIRD_ATTACHMENT, _thirdAttachmentSelection);

        if (_debuggerData != null)
        {
            string path = AssetDatabase.GetAssetPath(_debuggerData);
            string guid = string.IsNullOrEmpty(path) ? "" : AssetDatabase.AssetPathToGUID(path);
            EditorPrefs.SetString(KEY_GUID, guid ?? "");
        }
        else
        {
            EditorPrefs.DeleteKey(KEY_GUID);
        }
    }

    private void LoadData()
    {
        _mainAttachmentSelection = EditorPrefs.GetInt(KEY_MAIN_ATTACHMENT, 0);
        _secondaryAttachmentSelection = EditorPrefs.GetInt(KEY_SECONDARY_ATTACHMENT, 0);
        _thirdAttachmentSelection = EditorPrefs.GetInt(KEY_THIRD_ATTACHMENT, 0);

        string guid = EditorPrefs.GetString(KEY_GUID, "");
        if (!string.IsNullOrEmpty(guid))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            _debuggerData = AssetDatabase.LoadAssetAtPath<DebuggerData>(path);
        }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            AutoFindPlayer();
        }
    }

    private void AutoFindPlayer()
    {
        PlayerManager[] playerManager = Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

        if (playerManager != null)
        {
            _playerManagers = playerManager;
            _playerNames = _playerManagers.Select(go => go ? go.name : "<Missing>").ToArray();
            ShowNotification(new GUIContent($"{playerManager.Length} players set"), NOTIFICATION_TIME);
            Repaint();
        }
        else
        {
            ShowNotification(new GUIContent("No PlayerManager found in scene."), NOTIFICATION_TIME);
        }
    }
}