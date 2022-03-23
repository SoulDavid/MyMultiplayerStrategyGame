using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class Health : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;

    [SyncVar(hook = nameof(HandleHealthUpdated))]
    private int currentHealth;

    public event Action ServerOnDie;

    public event Action<int, int> ClientOnHealthUpdated;

    #region Server
    public override void OnStartServer()
    {
        currentHealth = maxHealth;

        UnitBase.ServerOnPlayerDie += ServerHandlePlayerDie;
    }

    public override void OnStopServer()
    {
        UnitBase.ServerOnPlayerDie -= ServerHandlePlayerDie;
    }

    [Server]
    private void ServerHandlePlayerDie(int connectionId)
    {
        if(connectionToClient.connectionId != connectionId) { return; }
        DealDamage(currentHealth);
    }

    [Server]
    public void DealDamage(int damageAmount)
    {
        if(currentHealth == 0) { return; }

        //Forma abreviada, que significa, que siempre pilla el máximo. Si currentHealth - damageaAmount es menor que 0, cogera 0, si no, coge el valor que ha salido
        currentHealth = Mathf.Max(currentHealth - damageAmount, 0);
        //currentHealth -= damageAmount;
        //if(currentHealth < 0) { currentHealth = 0; }

        if(currentHealth != 0) { return; }

        ServerOnDie?.Invoke();
    }
    #endregion

    #region Client
    private void HandleHealthUpdated(int oldHealth, int newHealth)
    {
        ClientOnHealthUpdated?.Invoke(newHealth, maxHealth);
    }
    #endregion


}
