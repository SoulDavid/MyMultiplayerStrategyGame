using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;
using TMPro;
using UnityEngine.UI;

public class UnitSpawner : NetworkBehaviour, IPointerClickHandler
{
    [SerializeField] private Health health = null;
    [SerializeField] private Unit unitPrefab = null;
    [SerializeField] private Transform spawnTransform;
    [SerializeField] private TMP_Text remainingUnitsText = null;
    [SerializeField] private Image unitProgressImage = null;
    [SerializeField] private int maxUnitQueue = 5;
    [SerializeField] private float spawnMoveRange = 7f;
    [SerializeField] private float unitSpawnDuration = 5f;

    private float progressVelocity;

    [SyncVar(hook = nameof(ClientHandleQueuedUnitsUpdated))]
    private int queuedUnits;
    [SyncVar]
    private float unitTimer;

    private void Update()
    {
        //Si estas en el server, produce unidades
        if(isServer)
        {
            ProduceUnits();
        }

        if(isClient)
        {
            UpdateTimerDisplay();
        }
    }

    #region Server
    public override void OnStartServer()
    {
        health.ServerOnDie += HandleServerOnDie;
    }

    public override void OnStopServer()
    {
        health.ServerOnDie -= HandleServerOnDie;
    }

    [Server]
    private void HandleServerOnDie()
    {
        NetworkServer.Destroy(gameObject);
    }

    [Command]
    private void CmdSpawnUnit()
    {
        if(queuedUnits == maxUnitQueue) { return; }

        RTSPlayer playerClient = connectionToClient.identity.GetComponent<RTSPlayer>();

        if(playerClient.GetResources() < unitPrefab.GetResourceCost()) { return; }

        queuedUnits++;

        playerClient.SetResources(playerClient.GetResources() - unitPrefab.GetResourceCost());
    }

    [Server]
    private void ProduceUnits()
    {
        if(queuedUnits == 0) { return; }

        unitTimer += Time.deltaTime;

        //Cuando este ready, spawnea una unidad
        if(unitTimer < unitSpawnDuration) { return; }

        GameObject unitInstance = Instantiate(unitPrefab.gameObject, spawnTransform.position, spawnTransform.rotation);

        NetworkServer.Spawn(unitInstance, connectionToClient);

        Vector3 spawnOffset = Random.insideUnitSphere * spawnMoveRange;
        spawnOffset.y = spawnTransform.position.y;

        PlayerMovement unitMovement = unitInstance.GetComponent<PlayerMovement>();
        unitMovement.ServerMove(spawnTransform.position + spawnOffset);

        //Reset timer
        queuedUnits--;
        unitTimer = 0f;
    }
    #endregion

    #region Client
    //Se llama a esta funcion siempre que se clique en este objeto
    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button != PointerEventData.InputButton.Left) { return; }
        if(!hasAuthority) { return; }
        CmdSpawnUnit();
    }

    private void ClientHandleQueuedUnitsUpdated(int oldUnits, int newUnits)
    {
        remainingUnitsText.text = newUnits.ToString();
    }

    private void UpdateTimerDisplay()
    {
        float newProgress = unitTimer / unitSpawnDuration;

        if(newProgress < unitProgressImage.fillAmount)
        {
            unitProgressImage.fillAmount = newProgress;
        }
        else
        {
            unitProgressImage.fillAmount = Mathf.SmoothDamp(unitProgressImage.fillAmount, newProgress, ref progressVelocity, 0.1f);
        }
    }
    #endregion
}
