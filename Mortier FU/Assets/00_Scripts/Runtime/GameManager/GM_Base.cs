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
        // TO DO : spawn des joueurs, remise à 0 de leur état, etc...
        // TO DO : remise à zéro des scores, des variables etc...
        // TO DO : lancement de la musique de jeu
        // TO DO : définir le nombre max de round et StartRound()
    }

    private void StartRound()
    {
        // TO DO : remise à zéro des variables de round, timer, etc...
        // TO DO : lancement de la musique de round
        // TO DO : replacer les joueurs au point de spawn
        // TO DO : activer les controllers
    }

    private void EndRound()
    {
        // TO DO : doit être appeler à chaque fois qu'un joueur est mort
        // TO DO : désactiver les controllers
        // TO DO : gestion des scores, affichage des résultats, etc...
        // TO DO : lancement de la musique de fin de round
        // TO DO : vérifier si la partie est finie et si oui, EndGame() sinon StartBonusSelection()
    }

    private void StartBonusSelection()
    {
        // TO DO : afficher l'écran de sélection de bonus
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
