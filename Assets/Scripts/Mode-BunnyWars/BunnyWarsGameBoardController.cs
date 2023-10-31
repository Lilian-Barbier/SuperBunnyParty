using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static GameboardData;

public class BunnyWarsPlateController : MonoBehaviour
{

    [SerializeField] int gameboardSize = 3;

    [SerializeField] GameObject inGridObject;
    [SerializeField] GameObject selectorObject;

    [SerializeField] float hexagonWidth = 1;
    [SerializeField] float hexagonHeight = 1;

    GameboardData gameboard;

    //Un Vector2 est utilisé pour contenir la position actuelle de la sélection au sein de la grille gameboard.
    Vector2Int currentSelectorPosition = new Vector2Int(0, 0);
    GameObject selector;
    Controls playerInput;

    int currentPlayerId = 0;

    void Awake()
    {
        playerInput = new Controls();
    }

    void OnDisable()
    {
        playerInput.Disable();
    }

    void OnEnable()
    {
        playerInput.Enable();
    }


    // Start is called before the first frame update
    void Start()
    {
        gameboard = new GameboardData(gameboardSize);
        InstantiateGameboardCase();

        var defaultCase = gameboard.GetCaseByCoordinates(0, 0);
        Vector3 selectorPosition = defaultCase.GameObject.transform.position;
        selectorPosition.y += 0.4f;
        selector = Instantiate(selectorObject, selectorPosition, new Quaternion { eulerAngles = new Vector3(-90, 0, 0) });

        playerInput.Control.Movement.performed += ctx => Move(ctx.ReadValue<Vector2>());
        playerInput.Control.ActionA.performed += ctx => SelectTile();
    }

    void InstantiateGameboardCase()
    {
        var hexagon = gameboard.hexagon;
        int currentOffset = gameboardSize - 1;

        for (int y = 0; y < hexagon.GetLength(1); y++)
        {
            for (int x = 0; x < hexagon.GetLength(0); x++)
            {
                HexagonCase currentCase = gameboard.GetCase(x, y);
                if (currentCase != null)
                {
                    Vector3 position = new Vector3(currentCase.X * hexagonWidth + currentOffset * hexagonWidth / 2, 0, currentCase.Y * hexagonHeight);
                    GameObject hexInstance = Instantiate(inGridObject, position, Quaternion.Euler(-90, 0, 0));
                    hexInstance.transform.SetParent(transform);
                    currentCase.GameObject = hexInstance;
                }
            }

            //vu que les valeur au sein de la grille peuvent être négative l'offset n'a pas besoin d'être décrémenté
            currentOffset++;
        }
    }

    void Move(Vector2 direction)
    {
        Vector2Int futurPosition = currentSelectorPosition + Vector2Int.FloorToInt(direction);
        var futurCase = gameboard.GetCaseByCoordinates(futurPosition.x, futurPosition.y);

        if (futurCase == null)
        {
            return;
        }

        Vector3 selectorPosition = futurCase.GameObject.transform.position;
        selectorPosition.y += 0.4f;

        selector.transform.position = selectorPosition;
        currentSelectorPosition = futurPosition;
    }

    void SelectTile()
    {
        var selectedCase = gameboard.GetCaseByCoordinates(currentSelectorPosition.x, currentSelectorPosition.y);
        if (selectedCase == null)
        {
            return;
        }

        if (!selectedCase.IsSelected)
        {
            selectedCase.IsSelectedByPlayerId = currentPlayerId;
            selectedCase.GameObject.GetComponent<MeshRenderer>().material.color = currentPlayerId == 0 ? Color.yellow : Color.blue;

            //apply rules of reversi
            var row = gameboard.GetRow(selectedCase.Y);
            ReverseCase(selectedCase, row);

            var diagonalUp = gameboard.GetDiagonalByCoordinate(selectedCase.X, selectedCase.Y, false);
            ReverseCase(selectedCase, diagonalUp);

            var diagonalDown = gameboard.GetDiagonalByCoordinate(selectedCase.X, selectedCase.Y, true);
            ReverseCase(selectedCase, diagonalDown);

            //change player for test
            currentPlayerId = currentPlayerId == 0 ? 1 : 0;
            selector.GetComponent<MeshRenderer>().material.color = currentPlayerId == 0 ? Color.yellow : Color.blue;
        }
    }

    void ReverseCase(HexagonCase selectedCase, List<HexagonCase> row)
    {

        //check left
        int indexInRow = row.IndexOf(selectedCase);
        if (indexInRow > 1)
        {
            for (int i = indexInRow - 2; i >= 0; i--)
            {
                var currentCase = row[i];
                if (currentCase != null)
                {
                    if (currentCase.IsSelected && currentCase.IsSelectedByPlayerId == currentPlayerId)
                    {
                        for (int j = i; j < indexInRow; j++)
                        {
                            row[j].IsSelectedByPlayerId = currentPlayerId;
                            row[j].GameObject.GetComponent<MeshRenderer>().material.color = currentPlayerId == 0 ? Color.yellow : Color.blue;
                        }
                        break;
                    }
                }
            }

        }

        //check right
        if (indexInRow < row.Count - 2)
        {
            for (int i = indexInRow + 2; i < row.Count; i++)
            {
                var currentCase = row[i];
                if (currentCase != null)
                {
                    if (currentCase.IsSelected && currentCase.IsSelectedByPlayerId == currentPlayerId)
                    {
                        for (int j = i; j > indexInRow; j--)
                        {
                            row[j].IsSelectedByPlayerId = currentPlayerId;
                            row[j].GameObject.GetComponent<MeshRenderer>().material.color = currentPlayerId == 0 ? Color.yellow : Color.blue;
                        }
                        break;
                    }
                }
            }

        }
    }

}
