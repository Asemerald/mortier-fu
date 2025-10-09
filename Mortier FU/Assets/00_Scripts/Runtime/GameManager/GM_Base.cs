using System.Collections.Generic;
using UnityEngine;
using MortierFu;
using UnityEngine.InputSystem;

public class GM_Base : MonoBehaviour
{
    public static GM_Base Instance { get; private set; }
    
    private List<PlayerInput> _joinedPlayers;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void StartGame()
    {
        // TODO : spawn des joueurs, remise à 0 de leur état, etc...
        // TODO : remise à zéro des scores, des variables etc...
        // TODO : lancement de la musique de jeu
        // TODO : définir le nombre max de round et StartRound()
    }

    private void StartRound()
    {
        // TODO : remise à zéro des variables de round, timer, etc...
        // TODO : lancement de la musique de round
        // TODO : replacer les joueurs au point de spawn
        // TODO : activer les controllers
    }

    private void EndRound()
    {
        // TODO : doit être appeler à chaque fois qu'un joueur est mort
        // TODO : désactiver les controllers
        // TODO : gestion des scores, affichage des résultats, etc...
        // TODO : lancement de la musique de fin de round
        // TODO : vérifier si la partie est finie et si oui, EndGame() sinon StartBonusSelection()
    }

    private void StartBonusSelection()
    {
        // TODO : afficher l'écran de sélection de bonus
    }
    
    private void EndBonusSelection()
    {
        
    }

    private void EndGame()
    {
        
    }

    public void RegisterPlayer(PlayerInput playerInput)
    {
        if(_joinedPlayers.Contains(playerInput)) return;
        
        _joinedPlayers.Add(playerInput);
    }
}
