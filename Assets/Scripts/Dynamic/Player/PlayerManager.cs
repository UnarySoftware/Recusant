using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour, ISystem
{
    public static PlayerManager Instance = null;

    private PlayerNetwork _ourPlayer = null;

    [SerializeField]
    private List<PlayerNetwork> _players = new();

    [InitDependency(typeof(Networking))]
    public void Initialize()
    {
        
    }

    public void Deinitialize()
    {
        
    }

    public PlayerNetwork GetOurPlayer()
    {
        return _ourPlayer;
    }

    public void AddPlayer(PlayerNetwork player)
    {
        if(player.IsInputSource)
        {
            _ourPlayer = player;
        }

        _players.Add(player);
    }

    public void RemovePlayer(PlayerNetwork player)
    {
        if (player.IsInputSource)
        {
            _ourPlayer = null;
        }

        _players.Remove(player);
    }
}
