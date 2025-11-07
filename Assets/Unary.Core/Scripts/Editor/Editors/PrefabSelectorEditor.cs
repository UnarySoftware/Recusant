using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unary.Core.Editor
{
    public class PrefabSearchWindow : EditorWindow
    {
        private static GUIStyle leftAlignedButton;

        public static Action<bool> OnSelected;

        private List<Texture2D> _tempTextures = new();
        private List<Texture2D> _textures = new();
        private List<string> _paths = new();
        private List<GameObject> _objects = new();

        private int _scaleList = 28;
        private int _scaleMin = 48;
        private int _scaleMax = 128;
        private int _scale = 85;

        private string _search = null;
        private List<int> _searchIndexes = new();

        private int _selectIndex = -1;
        private int _pingIndex = -1;

        private Vector2 _scrollPos;

        public static Type TargetType { get; set; }
        public static GameObject SelectedObject { get; private set; }
        public static bool IsOpened { get; private set; }

        private bool _wantToClose = false;

        private void OnEnable()
        {
            _wantToClose = false;

            // Probably just domain reloaded or incorrectly used in property drawers, we need to close then
            if (TargetType == null)
            {
                _wantToClose = true;
                return;
            }

            IsOpened = true;

            _tempTextures.Clear();
            _textures.Clear();
            _paths.Clear();
            _objects.Clear();
            _search = null;
            _searchIndexes.Clear();
            SelectedObject = null;
            _selectIndex = -1;
            _pingIndex = -1;

            _paths = PrefabManager.GetPathsWithComponents(TargetType);

            for (int i = 0; i < _paths.Count; i++)
            {
                _objects.Add(AssetDatabase.LoadAssetAtPath<GameObject>(_paths[i]));
                _tempTextures.Add(AssetPreview.GetMiniThumbnail(_objects[i]));
                _textures.Add(null);
            }
        }

        private void OnDisable()
        {
            IsOpened = false;
        }

        private void DuplicateTexture(Texture2D texture, int index)
        {
            if (texture == null)
            {
                return;
            }

            Texture2D target = _textures[index];
            target = new(texture.width, texture.height, texture.format, false);
            target.SetPixels32(texture.GetPixels32());
            target.Apply();
        }

        private void Ping()
        {
            if (_pingIndex == -1)
            {
                return;
            }

            EditorGUIUtility.PingObject(_objects[_pingIndex]);
        }

        private void Selected()
        {
            if (_selectIndex != -1)
            {
                SelectedObject = _objects[_selectIndex];
            }
            OnSelected(true);
            Close();
        }

        private void UpdateSearch()
        {
            _searchIndexes.Clear();

            string searchString = _search.ToLower();

            for (int i = 0; i < _paths.Count; i++)
            {
                string path = _paths[i].ToLower();

                if (path.Contains(searchString))
                {
                    _searchIndexes.Add(i);
                }
            }
        }

        private void DisplayEntry(int index, bool listView)
        {
            Texture2D texture = _textures[index];

            if (texture == null)
            {
                texture = _tempTextures[index];
            }

            if (listView)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(texture, GUILayout.Width(_scaleList), GUILayout.Height(_scaleList)))
                {
                    _pingIndex = index;
                    Ping();
                }

                if (GUILayout.Button(_paths[index], leftAlignedButton, GUILayout.Height(_scaleList)))
                {
                    _selectIndex = index;
                    Selected();
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginVertical();

                if (GUILayout.Button(texture, GUILayout.Width(_scale), GUILayout.Height(_scale)))
                {
                    _pingIndex = index;
                    Ping();
                }

                if (GUILayout.Button(_objects[index].name, GUILayout.Width(_scale)))
                {
                    _selectIndex = index;
                    Selected();
                }

                EditorGUILayout.EndVertical();
            }
        }

        void OnGUI()
        {
            if (leftAlignedButton == null)
            {
                leftAlignedButton = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (_wantToClose)
            {
                Close();
                return;
            }

            for (int i = 0; i < _textures.Count; i++)
            {
                Texture2D texture = _textures[i];

                if (texture != null)
                {
                    continue;
                }

                DuplicateTexture(AssetPreview.GetAssetPreview(_objects[i]), i);
            }

            string resultSearch = EditorGUILayout.TextField("Search:", _search);

            if (_search != resultSearch)
            {
                _search = resultSearch;
                UpdateSearch();
            }

            Event currentEvent = Event.current;
            if (currentEvent.control && currentEvent.type == EventType.ScrollWheel)
            {
                float scrollDelta = currentEvent.delta.y;

                _scale -= (int)(scrollDelta * 2.0f);
                _scale = Mathf.Clamp(_scale, _scaleMin, _scaleMax);

                currentEvent.Use();
            }

            _scale = EditorGUILayout.IntSlider("Scale:", _scale, _scaleMin, _scaleMax);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            bool list = false;

            if (_scale == _scaleMin)
            {
                list = true;
            }

            int maxRows = (int)(position.width / (_scale + 4));

            int counter = 0;

            if (!list)
            {
                EditorGUILayout.BeginHorizontal();
            }

            if (_searchIndexes.Count > 0)
            {
                foreach (var index in _searchIndexes)
                {
                    DisplayEntry(index, list);
                    counter++;

                    if (!list && counter == maxRows)
                    {
                        counter = 0;
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                }
            }
            else
            {
                for (int i = 0; i < _textures.Count; i++)
                {
                    DisplayEntry(i, list);
                    counter++;

                    if (!list && counter == maxRows)
                    {
                        counter = 0;
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                }
            }

            if (!list)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (_pingIndex == -1)
            {
                return;
            }

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField($"Full Path: {_paths[_pingIndex]}");
        }
    }
}
