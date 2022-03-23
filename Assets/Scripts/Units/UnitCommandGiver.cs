using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitCommandGiver : MonoBehaviour
{
    [SerializeField] private UnitSelectionHandle unitSelectionHandler = null;
    [SerializeField] private LayerMask layerMask = new LayerMask();

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        GameOverHandler.ClientOnGameOver += ClientHandleGameOver;
    }

    private void OnDestroy()
    {
        GameOverHandler.ClientOnGameOver -= ClientHandleGameOver;
    }

    private void Update()
    {
        if(!Mouse.current.rightButton.wasPressedThisFrame) { return; }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) { return; }
        if(hit.collider.TryGetComponent<Targetable>(out Targetable _target))
        {
            if(_target.hasAuthority)
            {
                TryMove(hit.point);
                return;
            }

            TryTarget(_target);
            return;
        }

        TryMove(hit.point);
    }

    private void TryMove(Vector3 point)
    {
        foreach(Unit unit in unitSelectionHandler.selectedUnits)
        {
            unit.GetPlayerMovement().CmdMove(point);
        }
    }

    private void TryTarget(Targetable _target)
    {
        foreach (Unit unit in unitSelectionHandler.selectedUnits)
        {
            unit.GetTargeter().CmdSetTarget(_target.gameObject);
        }
    }

    private void ClientHandleGameOver(string winnerName)
    {
        enabled = false;
    }
}
