using Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Recusant
{
    [SystemPrefabInject("prefabslocal/ui/ui.prefab")]
    public sealed class Ui : System<Ui>
    {
        [SerializeField]
        private List<PrefabRef<UiState>> _states;

        private readonly List<UiState> _uiStatesList = new();
        private readonly Dictionary<Type, UiState> _uiStatesDict = new();
        private UiState _currentState = null;

        private UiState _mainMenu = null;

        public override void Initialize()
        {
            //UiState[] targetStates = GetComponentsInChildren<UiState>();

            foreach (var state in _states)
            {
                RegisterState(Instantiate(state.Value, transform).GetComponent<UiState>());
            }

            if (_mainMenu != null)
            {
                _currentState = _mainMenu;
                _mainMenu.GetComponent<UIDocument>().rootVisualElement.style.visibility = Visibility.Visible;
                _mainMenu.Initialize();
            }
        }

        public void RegisterState(UiState state)
        {
            Type type = state.GetType();
            _uiStatesDict[type] = state;
            _uiStatesList.Add(state);
            if (state is MainMenuState)
            {
                _mainMenu = state;
            }
            else
            {
                state.Initialize();
            }

            if (state is CoreState)
            {
                state.Open();
            }
            else
            {
                state.GetComponent<UIDocument>().rootVisualElement.style.visibility = Visibility.Hidden;
            }
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
                Core.Logger.Instance.Error("Tried opening non-existing UI state " + state.Name);
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
