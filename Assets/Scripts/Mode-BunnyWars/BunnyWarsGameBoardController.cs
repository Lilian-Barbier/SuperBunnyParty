using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BunnyWarsPlateController : MonoBehaviour
{

    public class InGridObject
    {
        public GameObject gameObject;
        public bool isSelected;
        public int isSelectedByPlayerId;

        public InGridObject(GameObject gameObject)
        {
            this.gameObject = gameObject;
            isSelected = false;
            isSelectedByPlayerId = -1;
        }
    }


    //Contains all the hexagons in the grid
    List<List<InGridObject>> gridList = new();

    [SerializeField] int numRows = 5;
    [SerializeField] int startNumColumns = 3;

    [SerializeField] GameObject inGridObject;

    [SerializeField] float hexagonWidht = 1;
    [SerializeField] float hexagonHeight = 1;


    // Start is called before the first frame update
    void Start()
    {
        CreateHexagonalGrid();
    }

    void CreateHexagonalGrid()
    {
        int currentNumColumns = startNumColumns;
        for (int i = 0; i < numRows; i++)
        {
            var line = new List<InGridObject>();
            for (int j = 0; j < currentNumColumns; j++)
            {
                Vector3 position = new(i * hexagonWidht, 0, j * hexagonHeight + (startNumColumns - currentNumColumns) * hexagonHeight / 2);
                GameObject hexagon = Instantiate(inGridObject, position, new Quaternion { eulerAngles = new Vector3(-90, 0, 30) });
                hexagon.transform.SetParent(transform);
                line.Add(new InGridObject(hexagon));
            }

            gridList.Add(line);

            if (i < numRows / 2)
            {
                currentNumColumns += 1;
            }
            else
            {
                currentNumColumns -= 1;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
