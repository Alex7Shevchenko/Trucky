using UnityEditor;
using UnityEngine;

public class Debugger : EditorWindow
{
    private PlayerManager _targetPlayer;
    private GameObject _mainAttachment;
    private GameObject _secondaryAttachment;

    [MenuItem("Tools/Debugger")]
    public static void ShowWindow()
    {
        var window = GetWindow<Debugger>("Debugger", true);
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _targetPlayer = (PlayerManager)EditorGUILayout.ObjectField("Player", _targetPlayer, typeof(PlayerManager), true);

            GUILayout.Space(6);
            
            using (new EditorGUILayout.HorizontalScope())
                {
                    _mainAttachment = (GameObject)EditorGUILayout.ObjectField("Main Weapon", _mainAttachment, typeof(GameObject), true);
                    if (GUILayout.Button("Set Weapon") && _targetPlayer != null)
                    {
                        _targetPlayer.PlayerLoadout.AttachMainAttachment(_mainAttachment);
                    }
                }

            GUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                _secondaryAttachment = (GameObject)EditorGUILayout.ObjectField("Secondary Weapon", _secondaryAttachment, typeof(GameObject), true);
                if (GUILayout.Button("Set Weapon") && _targetPlayer != null)
                {
                    _targetPlayer.PlayerLoadout.AttachSecondaryAttachment(_secondaryAttachment);
                }
            }
        }
    }
}
