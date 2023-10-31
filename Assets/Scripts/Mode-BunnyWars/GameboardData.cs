using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameboardData
{
    public class HexagonCase
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public GameObject GameObject { get; set; }
        public bool IsSelected { get => IsSelectedByPlayerId != -1; }
        public int IsSelectedByPlayerId { get; set; }

        public HexagonCase(int x, int y)
        {
            X = x;
            Y = y;
            IsSelectedByPlayerId = -1;
        }
    }

    public HexagonCase[,] hexagon;

    public GameboardData(int size)
    {
        int gridSize = size * 2 - 1;
        hexagon = new HexagonCase[gridSize, gridSize];

        for (int q = -size + 1; q < size; q++)
        {
            int r1 = Mathf.Max(-size + 1, -q - size + 1);
            int r2 = Mathf.Min(size - 1, -q + size - 1);
            for (int r = r1; r <= r2; r++)
            {
                int x = q + size - 1;
                int y = r + size - 1;
                hexagon[x, y] = new HexagonCase(q, r);
            }
        }
    }

    public override string ToString()
    {
        string result = "";
        for (int q = 0; q < hexagon.GetLength(0); q++)
        {
            int offset = Mathf.Abs(2 - q);
            //result += new string(' ', offset * 4);
            for (int r = 0; r < hexagon.GetLength(1); r++)
            {
                HexagonCase currentCase = GetCase(q, r);
                if (currentCase != null)
                {
                    result += $"({currentCase.X},{currentCase.Y}) ";
                }
                else
                {
                    result += "       "; // 7 espaces pour aligner correctement
                }
            }
            result += "\n";
        }
        return result;
    }

    public HexagonCase GetCase(int x, int y)
    {
        if (x < 0 || x >= hexagon.GetLength(0) || y < 0 || y >= hexagon.GetLength(1))
        {
            return null;
        }

        return hexagon[x, y];
    }

    public HexagonCase GetCaseByCoordinates(int x, int y)
    {
        foreach (var hexCase in hexagon)
        {
            if (hexCase != null && hexCase.X == x && hexCase.Y == y)
            {
                return hexCase;
            }
        }
        return null; // Retourne null si la case n'est pas trouvée
    }

    public List<HexagonCase> GetRow(int row)
    {
        List<HexagonCase> cases = new List<HexagonCase>();

        foreach (var hexCase in hexagon)
        {
            if (hexCase != null && hexCase.Y == row)
            {
                cases.Add(hexCase);
            }
        }

        return cases;
    }

    public List<HexagonCase> GetDiagonalByCoordinate(int x, int y, bool isAscending)
    {
        if (!isAscending)
        {
            List<HexagonCase> cases = new List<HexagonCase>();

            foreach (var hexCase in hexagon)
            {
                if (hexCase != null && hexCase.X == x)
                {
                    cases.Add(hexCase);
                }
            }

            return cases;
        }
        else
        {

            List<HexagonCase> cases = new List<HexagonCase>
            {
                GetCaseByCoordinates(x, y)
            };

            HexagonCase upCase = GetCaseByCoordinates(x - 1, y + 1);
            while (upCase != null)
            {
                cases.Add(upCase);
                upCase = GetCaseByCoordinates(upCase.X - 1, upCase.Y + 1);
            }

            HexagonCase downCase = GetCaseByCoordinates(x + 1, y - 1);
            while (downCase != null)
            {
                cases.Add(downCase);
                downCase = GetCaseByCoordinates(downCase.X + 1, downCase.Y - 1);
            }

            cases = cases.OrderByDescending(x => x.X).ToList();

            return cases;
        }

    }

    public List<HexagonCase> GetDiagonal(int x, int y, bool isAscending)
    {
        List<HexagonCase> cases = new List<HexagonCase>();
        for (int i = 0; i < hexagon.GetLength(0); i++)
        {
            int newX = isAscending ? x + i : x - i;
            int newY = y + i;

            if (newX >= 0 && newX < hexagon.GetLength(0) && newY >= 0 && newY < hexagon.GetLength(1))
            {
                HexagonCase currentCase = GetCase(newX, newY);
                if (currentCase != null)
                {
                    cases.Add(currentCase);
                }
            }
        }
        return cases;
    }
}

