﻿using System;
using System.Collections.Generic;

public sealed class Ui : CoreSystem<Ui>
{
    private readonly List<UiState> _uiStatesList = new();
    private readonly Dictionary<Type, UiState> _uiStatesDict = new();
    private UiState _currentState = null;

    [InitDependency()]
    public override void Initialize()
    {
        UiState[] targetStates = GetComponentsInChildren<UiState>();

        UiState mainMenu = null;

        foreach(UiState state in targetStates) 
        {
            Type type = state.GetType();
            _uiStatesDict[type] = state;
            _uiStatesList.Add(state);
            if(state is MainMenuState)
            {
                mainMenu = state;
            }
            else
            {
                state.Initialize();
            }

            if(state is CoreState)
            {
                state.Open();
            }
            else
            {
                state.gameObject.SetActive(false);
            }
        }

        if (mainMenu != null)
        {
            _currentState = mainMenu;
            mainMenu.gameObject.SetActive(true);
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
            _currentState.gameObject.SetActive(false);
        }

        _currentState = targetState;
        _currentState.gameObject.SetActive(true);
        _currentState.Open();
    }

    public void GoBack()
    {
        if(_currentState != null && _currentState.GetBackState() != null) 
        {
            GoForward(_currentState.GetBackState());
        }
    }
}
