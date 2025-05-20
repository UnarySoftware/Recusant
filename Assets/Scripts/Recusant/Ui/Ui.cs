using Core;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Recusant
{
    [PrefabInject("Assets/Recusant/PrefabsLocal/Ui.prefab")]
    public sealed class Ui : System<Ui>
    {
        private readonly List<UiState> _uiStatesList = new();
        private readonly Dictionary<Type, UiState> _uiStatesDict = new();
        private UiState _currentState = null;

        public override void Initialize()
        {
            UiState[] targetStates = GetComponentsInChildren<UiState>();

            UiState mainMenu = null;

            foreach (UiState state in targetStates)
            {
                Type type = state.GetType();
                _uiStatesDict[type] = state;
                _uiStatesList.Add(state);
                if (state is MainMenuState)
                {
                    mainMenu = state;
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

            if (mainMenu != null)
            {
                _currentState = mainMenu;
                mainMenu.GetComponent<UIDocument>().rootVisualElement.style.visibility = Visibility.Visible;
                mainMenu.Initialize();
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
                Logger.Instance.Error("Tried opening non-existing UI state " + state.Name);
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
