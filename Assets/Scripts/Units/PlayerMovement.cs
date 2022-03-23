using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private NavMeshAgent agent = null;
    [SerializeField] private Targeter targeter = null;
    [SerializeField] private float chaseRange = 10f;

    #region Server
    public override void OnStartServer()
    {
        GameOverHandler.ServerOnGameOver += ServerHandleGameOver;
    }

    public override void OnStopServer()
    {
        GameOverHandler.ServerOnGameOver -= ServerHandleGameOver;
    }

    [ServerCallback]
    private void Update()
    {
        Targetable target = targeter.GetTarget();

        if(target != null)
        {
            if((target.transform.position - transform.position).sqrMagnitude > chaseRange * chaseRange)
            {
                agent.SetDestination(targeter.GetTarget().transform.position);
            }
            else if(agent.hasPath)
            {
                agent.ResetPath();
            }

            return;
        }

        if(!agent.hasPath) { return; }
        if(agent.remainingDistance > agent.stoppingDistance) { return; }
        agent.ResetPath();
    }

    //El command es el endpoint al cual el cliente puede llamar
    //Es necesario para ser llamadao desde el server y el cliente
    [Command]
    public void CmdMove(Vector3 _position)
    {
        ServerMove(_position);
    }

    [Server]
    public void ServerMove(Vector3 _position)
    {
        targeter.ClearTarget();
        //https://docs.unity3d.com/ScriptReference/AI.NavMesh.SamplePosition.html
        //Si es una posicion valida sigue, si no retorna
        if (!NavMesh.SamplePosition(_position, out NavMeshHit hit, 100f, NavMesh.AllAreas)) { Debug.Log("Caminando por la vida"); return; }

        agent.SetDestination(_position);
    }

    [Server]
    private void ServerHandleGameOver()
    {
        agent.ResetPath();
    }
    #endregion


}
