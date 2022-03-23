using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TeamColorSetter : NetworkBehaviour
{
    [SerializeField] private Renderer[] colorRenderers = new Renderer[0];

    [SyncVar(hook = nameof(HandleTeamColorUpdated))]
    private Color teamColor = new Color();

    #region Server
    public override void OnStartServer()
    {
        RTSPlayer player = connectionToClient.identity.GetComponent<RTSPlayer>();

        teamColor = player.getTeamColor();
    }
    #endregion

    #region Client
    private void HandleTeamColorUpdated(Color oldColor, Color newcolor)
    {
        foreach(Renderer _renderer in colorRenderers)
        {
            _renderer.material.SetColor("_Color", newcolor);
        }
    }
    #endregion
}
