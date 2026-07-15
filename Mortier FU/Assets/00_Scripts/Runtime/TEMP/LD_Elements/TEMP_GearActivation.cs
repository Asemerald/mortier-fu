using System;
using System.Collections.Generic;
using MortierFu;
using UnityEngine;

public sealed class TEMP_GearActivation : LobbyInteractionZone
{
   [SerializeField] Animator _animator;
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
         _animator.SetBool("IsActive", true);
      }
      
   }

   protected override void Interact(PlayerManager player)
   {
      throw new NotImplementedException();
   }

   protected override void OnPlayerExited(PlayerManager player)
   {
      _activePlayers.Remove(player);
      if (_activePlayers.Count == 0)
      {
         _animator.SetBool("IsActive", false);
      }
   }
}
