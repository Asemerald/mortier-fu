using System;
using System.Collections.Generic;
using MortierFu;
using UnityEngine;

public abstract class TEMP_GearActivation : LobbyInteractionZone
{
   [SerializeField] private Animator _animator;
   private List<PlayerManager> _activePlayers;

   private void Awake()
   {
      _activePlayers = new List<PlayerManager>();
   }

   protected override void OnPlayerEntered(PlayerManager player)
   {
      _activePlayers.Add(player);
      if (_activePlayers.Count == 1)
      {
         Debug.Log("in");
      }
      
      
   }

   protected override void OnPlayerExited(PlayerManager player)
   {
      _activePlayers.Remove(player);
      if (_activePlayers.Count == 0)
      {
         Debug.Log("exit");
      }
   }
}
