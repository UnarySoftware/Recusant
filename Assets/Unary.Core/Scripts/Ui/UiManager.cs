using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unary.Core
{
    public class UiManager : CoreSystem<UiManager>
    {
        private readonly List<UiState> _uiStatesList = new();
        private readonly Dictionary<Type, UiState> _uiStatesDict = new();
        private UiState _currentState = null;

        public UiState State
        {
            get
            {
                return _currentState;
            }
        }

        private void RegisterState(GameObject stateGameObject)
        {
            UiState state = stateGameObject.GetComponent<UiState>();

            Type type = state.GetType();
            _uiStatesDict[type] = state;
            _uiStatesList.Add(state);
            state.Initialize();

            if (state is CoreState)
            {
                state.Open();
            }
            else
            {
                state.GetComponent<UIDocument>().rootVisualElement.style.visibility = Visibility.Hidden;
            }
        }

        public struct Entry : IComparable<Entry>
        {
            public float Order;
            public GameObject GameObject;

            public Entry(float order, GameObject gameObject)
            {
                Order = order;
                GameObject = gameObject;
            }

            public readonly int CompareTo(Entry other)
            {
                return Order.CompareTo(other.Order);
            }
        }

        public override bool Initialize()
        {
            List<string> paths = ContentLoader.Instance.GetAssetPaths(typeof(UiStateAsset));

            List<Entry> sorting = new();

            foreach (var path in paths)
            {
                UiStateAsset asset = ContentLoader.Instance.LoadAsset<UiStateAsset>(path);

                float sortingOrder = asset.State.Value.GetComponent<UIDocument>().sortingOrder;

                sorting.Add(new(sortingOrder, asset.State.Value));
            }

            sorting.Sort();

            foreach (var entry in sorting)
            {
                RegisterState(Instantiate(entry.GameObject, transform));
            }

            return true;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            for (int i = _uiStatesList.Count - 1; i >= 0; i--)
            {
                UiState state = _uiStatesList[i];
                state.Close();
                state.Deinitialize();
            }
        }

        public void GoForward(Type state)
        {
            _uiStatesDict.TryGetValue(state, out UiState targetState);

            if (targetState == null)
            {
                Logger.Instance.Error("Tried opening non-existing UI state " + state.Name);
                return;
            }

            if (_currentState == targetState)
            {
                return;
            }

            if (_currentState != null)
            {
                _currentState.Close();
                _currentState.GetComponent<UIDocument>().rootVisualElement.style.visibility = Visibility.Hidden;
            }

            _currentState = targetState;
            _currentState.GetComponent<UIDocument>().rootVisualElement.style.visibility = Visibility.Visible;
            _currentState.Open();
        }

        public void GoBack()
        {
            if (_currentState != null && _currentState.GetBackState() != null)
            {
                GoForward(_currentState.GetBackState());
            }
        }
    }
}
