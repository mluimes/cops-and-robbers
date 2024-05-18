using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;
                    
    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }
        
    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;                
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        // Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        // Inicializar matriz a 0's
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
            }
        }

        // Rellenar la matriz con 1's para las casillas adyacentes (arriba, abajo, izquierda y derecha)
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                int currentTile = fil * Constants.TilesPerRow + col;

                if (fil > 0) // Arriba
                    matriu[currentTile, currentTile - Constants.TilesPerRow] = 1;
                if (fil < Constants.TilesPerRow - 1) // Abajo
                    matriu[currentTile, currentTile + Constants.TilesPerRow] = 1;
                if (col > 0) // Izquierda
                    matriu[currentTile, currentTile - 1] = 1;
                if (col < Constants.TilesPerRow - 1) // Derecha
                    matriu[currentTile, currentTile + 1] = 1;
            }
        }

        // Rellenar la lista "adjacency" de cada casilla con los índices de sus casillas adyacentes
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                if (matriu[i, j] == 1)
                {
                    tiles[i].adjacency.Add(j);
                }
            }
        }
    }


    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {                  
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile=tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;   
                    
                    state = Constants.TileSelected;
                }                
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {            
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }



    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        // Obtener una lista de casillas seleccionables para el primer movimiento
        List<Tile> selectableTiles = new List<Tile>();
        foreach (Tile tile in tiles)
        {
            if (tile.selectable)
            {
                selectableTiles.Add(tile);
            }
        }

        // Si no hay casillas seleccionables, no se mueve
        if (selectableTiles.Count == 0)
        {
            return;
        }

        // Encuentra la casilla más alejada de cualquier policía
        Tile bestTile = FindFurthestTile(selectableTiles);
        robber.GetComponent<RobberMove>().MoveToTile(bestTile);
        robber.GetComponent<RobberMove>().currentTile = bestTile.numTile;
    }

    private Tile FindFurthestTile(List<Tile> selectableTiles)
    {
        Tile furthestTile = null;
        int maxDistance = -1;

        foreach (Tile tile in selectableTiles)
        {
            int minDistanceToCop = int.MaxValue;
            foreach (GameObject cop in cops)
            {
                int distanceToCop = CalculateDistance(tile, tiles[cop.GetComponent<CopMove>().currentTile]);
                if (distanceToCop < minDistanceToCop)
                {
                    minDistanceToCop = distanceToCop;
                }
            }

            if (minDistanceToCop > maxDistance)
            {
                maxDistance = minDistanceToCop;
                furthestTile = tile;
            }
        }

        return furthestTile;
    }

    private int CalculateDistance(Tile startTile, Tile endTile)
    {
        // Implementación del BFS para calcular la distancia mínima entre startTile y endTile
        ResetTiles();
        Queue<Tile> queue = new Queue<Tile>();
        startTile.visited = true;
        queue.Enqueue(startTile);

        while (queue.Count > 0)
        {
            Tile t = queue.Dequeue();
            if (t == endTile)
            {
                return t.distance;
            }

            foreach (int i in t.adjacency)
            {
                Tile adjTile = tiles[i];
                if (!adjTile.visited)
                {
                    adjTile.visited = true;
                    adjTile.distance = t.distance + 1;
                    queue.Enqueue(adjTile);
                }
            }
        }

        return int.MaxValue; // En caso de que no se encuentre un camino (no debería ocurrir en un tablero conectado)
    }


    public void EndGame(bool end)
    {
        if(end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);
                
        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
         
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
        int indexcurrentTile = cop ? cops[clickedCop].GetComponent<CopMove>().currentTile : robber.GetComponent<RobberMove>().currentTile;

        // La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        // Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        // Inicializar el BFS
        Tile startTile = tiles[indexcurrentTile];
        startTile.visited = true;
        startTile.distance = 0;
        nodes.Enqueue(startTile);

        while (nodes.Count > 0)
        {
            Tile t = nodes.Dequeue();

            if (t.distance < Constants.Distance)
            {
                foreach (int i in t.adjacency)
                {
                    Tile adjTile = tiles[i];

                    if (!adjTile.visited)
                    {
                        adjTile.parent = t;
                        adjTile.visited = true;
                        adjTile.distance = t.distance + 1;
                        nodes.Enqueue(adjTile);
                        adjTile.selectable = true;
                    }
                }
            }
        }
    }

    public bool IsTileOccupiedByCop(int tileNumber)
    {
        foreach (GameObject cop in cops)
        {
            if (cop.GetComponent<CopMove>().currentTile == tileNumber)
            {
                return true;
            }
        }
        return false;
    }
}
