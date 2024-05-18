using System.Collections.Generic;
using UnityEngine;

public class CopMove : Movement
{
    public GameObject controller;

    void Update()
    {
        if (moving)
        {
            Move();
        }
    }

    public void Restart(Tile t)
    {
        currentTile = t.numTile;
        MoveToTile(t);
    }

    private void OnMouseDown()
    {
        controller.GetComponent<Controller>().ClickOnCop(id);
    }

    public void Move()
    {
        if (path.Count > 0)
        {
            DoMove();
        }
        else
        {
            moving = false;
            controller.GetComponent<Controller>().FinishTurn();
        }
    }
}
