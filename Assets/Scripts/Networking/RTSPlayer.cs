using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class RTSPlayer : NetworkBehaviour
{
    private List<Unit> myUnits = new List<Unit>();
    private List<Building> myBuildings = new List<Building>();
    private Color teamColor = new Color();

    [SerializeField] private Building[] buildings = new Building[0];
    [SerializeField] private LayerMask buildingBlockLayer = new LayerMask();
    [SerializeField] private float buildingRangeLimits = 5f;
    [SerializeField] private Transform cameraTransform = null;

    public event Action<int> ClientOnResourcesUpdated;
    public static event Action<bool> AuthorityOnPartyOwnerStateUpdated;
    public static event Action ClientOnInfoUpdated;

    [SyncVar(hook = nameof(ClientHandleResourcesUpdated))]
    private int resources = 500;
    [SyncVar(hook = nameof(AuthorityHandlePartyOwnerStateUpdated))]
    private bool isPartyOwner = false;
    [SyncVar(hook = nameof(ClientHandleDisplayNameUpdated))]
    private string displayName;

    #region Gets Functions
    public List<Unit> GetMyUnits()
    {
        return myUnits;
    }

    public List<Building> GetMyBuildings()
    {
        return myBuildings;
    }

    public int GetResources()
    {
        return resources;
    }

    public Color getTeamColor()
    {
        return teamColor;
    }

    public Transform GetCameraTransform()
    {
        return cameraTransform;
    }

    public bool GetIsPartyOwner()
    {
        return isPartyOwner;
    }

    public string GetDisplayName()
    {
        return displayName;
    }
    #endregion

    #region Server
    public override void OnStartServer()
    {
        Unit.ServerOnUnitSpawned += ServerHandleUnitSpawned;
        Unit.ServerOnUnitDespawned += ServerHandleUnitDespawned;

        Building.ServerOnBuildingSpawned += ServerHandleBuildingSpawned;
        Building.ServerOnBuildingDespawned += ServerHandleBuildingDespawned;

        DontDestroyOnLoad(gameObject);
    }

    public override void OnStopServer()
    {
        Unit.ServerOnUnitSpawned -= ServerHandleUnitSpawned;
        Unit.ServerOnUnitDespawned -= ServerHandleUnitDespawned;

        Building.ServerOnBuildingSpawned -= ServerHandleBuildingSpawned;
        Building.ServerOnBuildingDespawned -= ServerHandleBuildingDespawned;
    }

    public bool CanPlaceBuilding(BoxCollider buildingCollider, Vector3 position)
    {
        if (Physics.CheckBox(position + buildingCollider.center, buildingCollider.size / 2, Quaternion.identity, buildingBlockLayer)) { return false; }

        foreach (Building building in myBuildings)
        {
            if ((position - building.transform.position).sqrMagnitude <= buildingRangeLimits * buildingRangeLimits)
            {
                return true;
            }
        }

        return false;
    }

    [Command]
    public void CmdTryPlaceBuilding(int buildingId, Vector3 position)
    {
        Building buildingToPlace = null;

        foreach(Building building in buildings)
        {
            if(building.GetId() == buildingId)
            {
                buildingToPlace = building;
                break;
            }
        }

        if(buildingToPlace == null) { return; }

        if(resources < buildingToPlace.GetPrice()) { return; }

        BoxCollider buildingCollider = buildingToPlace.GetComponent<BoxCollider>();

        if(!CanPlaceBuilding(buildingCollider, position)) { return; }

        GameObject buildingInstance = Instantiate(buildingToPlace.gameObject, position, buildingToPlace.transform.rotation);

        NetworkServer.Spawn(buildingInstance, connectionToClient);

        SetResources(resources - buildingToPlace.GetPrice());
    }

    private void ServerHandleUnitSpawned(Unit unit)
    {
        if(unit.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myUnits.Add(unit);
    }

    private void ServerHandleUnitDespawned(Unit unit)
    {
        if (unit.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myUnits.Remove(unit);
    }

    private void ServerHandleBuildingSpawned(Building building)
    {
        if (building.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myBuildings.Add(building);
    }

    private void ServerHandleBuildingDespawned(Building building)
    {
        if (building.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myBuildings.Remove(building);
    }

    #region Setters
    [Server]
    public void SetResources(int _resources)
    {
        resources = _resources;
    }

    [Server]
    public void SetTeamColor(Color _color)
    {
        teamColor = _color;
    }

    [Server]
    public void SetPartyOwner(bool _newState)
    {
        isPartyOwner = _newState;
    }

    [Server]
    public void SetDisplayName(string _newDisplayName)
    {
        displayName = _newDisplayName;
    }
    #endregion

    [Command]
    public void CmdStartGame()
    {
        if(!isPartyOwner) { return; }

        ((RTSNetworkManager)NetworkManager.singleton).StartGame();
    }
    #endregion

    #region Client
    public override void OnStartAuthority()
    {
        if(NetworkServer.active) { return; }

        Unit.AuthorityOnUnitSpawned += ClientHandleUnitSpawned;
        Unit.AuthorityOnUnitDespawned += ClientHandleUnitDespawned;

        Building.AuthorityOnBuildingSpawned += ClientHandleBuildingSpawned;
        Building.AuthorityOnBuildingDespawned += ClientHandleBuildingDespawned;
    }

    public override void OnStartClient()
    {
        if (NetworkServer.active) { return; }

        DontDestroyOnLoad(gameObject);

        ((RTSNetworkManager)NetworkManager.singleton).Players.Add(this);
    }

    public override void OnStopClient()
    {
        ClientOnInfoUpdated?.Invoke();

        if (!isClientOnly) { return; }

        ((RTSNetworkManager)NetworkManager.singleton).Players.Remove(this);
        
        if(!hasAuthority) { return; }

        Unit.AuthorityOnUnitSpawned -= ClientHandleUnitSpawned;
        Unit.AuthorityOnUnitDespawned -= ClientHandleUnitDespawned;

        Building.AuthorityOnBuildingSpawned -= ClientHandleBuildingSpawned;
        Building.AuthorityOnBuildingDespawned -= ClientHandleBuildingDespawned;
    }

    private void ClientHandleUnitSpawned(Unit unit)
    {
        myUnits.Add(unit);
    }

    private void ClientHandleUnitDespawned(Unit unit)
    {
        myUnits.Remove(unit);
    }

    private void ClientHandleBuildingSpawned(Building building)
    {
        myBuildings.Add(building);
    }

    private void ClientHandleBuildingDespawned(Building building)
    {
        myBuildings.Remove(building);
    }

    private void ClientHandleResourcesUpdated(int oldResources, int newResources)
    {
        ClientOnResourcesUpdated?.Invoke(newResources);
    }

    private void AuthorityHandlePartyOwnerStateUpdated(bool _oldState, bool _newState)
    {
        if(!hasAuthority) { return; }

        AuthorityOnPartyOwnerStateUpdated?.Invoke(_newState);
    }

    private void ClientHandleDisplayNameUpdated(string _oldDisplayName, string _newDisplayName)
    {
        ClientOnInfoUpdated?.Invoke();
    }
    #endregion
}
