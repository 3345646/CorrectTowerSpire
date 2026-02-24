using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Prefabs (must contain DungeonPiece component)")]
    public DungeonPiece[] pieces;

    [Header("Generation Settings")]
    public int amount = 75;
    public float tileSize = 32f;
    public bool verboseLogging = true;

    [Header("Player")]
    public Transform player;
    public float playerAmount = 1000f;

    private GameObject lastPiece;
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

    void Start()
    {
        if (pieces == null || pieces.Length == 0)
        {
            Debug.LogError("No pieces assigned.");
            return;
        }

        // FORCE first tile to be UP (Element 0)
        DungeonPiece start = pieces[0];

        if (start.Dir != DungeonPiece.ExitDir.Up)
        {
            Debug.LogWarning("Piece at index 0 is NOT set to Up!");
        }

        lastPiece = Instantiate(start.gameObject, Vector3.zero, Quaternion.identity);
        occupiedCells.Add(WorldToGrid(Vector3.zero));

        Debug.Log("Start piece forced to UP at (0,0)");

        GenerateDungeon();
    }


    void Update()
    {
        if (player.position.y > playerAmount)
        {
            
            GenerateDungeon();
            playerAmount = playerAmount + 1500;
        }
    }

    void GenerateDungeon()
    {
        for (int step = 0; step < amount; step++)
        {
            DungeonPiece.ExitDir chosenDir = GetRandomDirection_NoDown();
            DungeonPiece.ExitDir prevDir = GetDirOf(lastPiece);

            // LEFT/RIGHT -> UP → FORCE LeftUp / RightUp
            if ((prevDir == DungeonPiece.ExitDir.Left || prevDir == DungeonPiece.ExitDir.Right)
                && chosenDir == DungeonPiece.ExitDir.Up)
            {
                if (!PlaceForcedCorner(prevDir, chosenDir))
                {
                    Debug.LogError("Failed forced LeftUp/RightUp.");
                    return;
                }
                continue;
            }

            // UP -> LEFT/RIGHT → FORCE UpLeft / UpRight
            if (prevDir == DungeonPiece.ExitDir.Up &&
               (chosenDir == DungeonPiece.ExitDir.Left || chosenDir == DungeonPiece.ExitDir.Right))
            {
                if (!PlaceForcedCorner(prevDir, chosenDir))
                {
                    Debug.LogError("Failed forced UpLeft/UpRight.");
                    return;
                }
                continue;
            }

            // NORMAL placement
            Vector3 pos = GetNextPositionForDir(lastPiece.transform.position, chosenDir);
            Vector2Int grid = WorldToGrid(pos);

            if (occupiedCells.Contains(grid))
            {
                if (verboseLogging)
                    Debug.Log("Blocked — retrying another direction");
                step--;
                continue;
            }

            PlaceTile(pos, chosenDir);
        }

        Debug.Log("Generation finished.");
    }

    // ------------------------
    // Forced Corner Placement
    // ------------------------
    bool PlaceForcedCorner(DungeonPiece.ExitDir prevDir, DungeonPiece.ExitDir newDir)
    {
        Vector3 basePos = lastPiece.transform.position;

        // LEFT/RIGHT -> UP (LeftUp / RightUp)
        if ((prevDir == DungeonPiece.ExitDir.Left || prevDir == DungeonPiece.ExitDir.Right)
            && newDir == DungeonPiece.ExitDir.Up)
        {
            Vector3 cornerPos = GetNextPositionForDir(basePos, prevDir);
            Vector3 finalPos = GetNextPositionForDir(cornerPos, newDir);

            return PlaceCornerSequence(
                cornerPos,
                finalPos,
                prevDir == DungeonPiece.ExitDir.Left ? DungeonPiece.ExitDir.LeftUp : DungeonPiece.ExitDir.RightUp,
                DungeonPiece.ExitDir.Up
            );
        }

        // UP -> LEFT/RIGHT (UpLeft / UpRight)
        if (prevDir == DungeonPiece.ExitDir.Up &&
           (newDir == DungeonPiece.ExitDir.Left || newDir == DungeonPiece.ExitDir.Right))
        {
            Vector3 cornerPos = GetNextPositionForDir(basePos, DungeonPiece.ExitDir.Up);
            Vector3 finalPos = GetNextPositionForDir(cornerPos, newDir);

            return PlaceCornerSequence(
                cornerPos,
                finalPos,
                newDir == DungeonPiece.ExitDir.Left ? DungeonPiece.ExitDir.UpLeft : DungeonPiece.ExitDir.UpRight,
                newDir
            );
        }

        return false;
    }

    bool PlaceCornerSequence(Vector3 cornerPos, Vector3 finalPos,
        DungeonPiece.ExitDir cornerDir, DungeonPiece.ExitDir finalDir)
    {
        Vector2Int cGrid = WorldToGrid(cornerPos);
        Vector2Int fGrid = WorldToGrid(finalPos);

        if (occupiedCells.Contains(cGrid) || occupiedCells.Contains(fGrid))
            return false;

        DungeonPiece corner = GetRandomPieceByDir(cornerDir);
        DungeonPiece next = GetRandomPieceByDir(finalDir);

        if (corner == null || next == null)
        {
            Debug.LogError($"Missing prefab for {cornerDir} or {finalDir}");
            return false;
        }

        GameObject c = Instantiate(corner.gameObject, cornerPos, Quaternion.identity);
        GameObject n = Instantiate(next.gameObject, finalPos, Quaternion.identity);

        occupiedCells.Add(cGrid);
        occupiedCells.Add(fGrid);

        lastPiece = n;

        if (verboseLogging)
            Debug.Log($"Corner placed: {cornerDir} → {finalDir}");

        return true;
    }

    // ------------------------
    // Normal Placement
    // ------------------------
    void PlaceTile(Vector3 pos, DungeonPiece.ExitDir dir)
    {
        Vector2Int grid = WorldToGrid(pos);

        DungeonPiece prefab = GetRandomPieceByDir(dir);

        if (prefab == null)
        {
            Debug.LogError($"Missing prefab for {dir}");
            return;
        }

        GameObject obj = Instantiate(prefab.gameObject, pos, Quaternion.identity);
        occupiedCells.Add(grid);
        lastPiece = obj;

        if (verboseLogging)
            Debug.Log($"Placed {dir} at {grid}");
    }

    // ------------------------
    // Helpers
    // ------------------------
    Vector3 GetNextPositionForDir(Vector3 pos, DungeonPiece.ExitDir dir)
    {
        switch (dir)
        {
            case DungeonPiece.ExitDir.Up: return pos + Vector3.up * tileSize;
            case DungeonPiece.ExitDir.Left: return pos + Vector3.left * tileSize;
            case DungeonPiece.ExitDir.Right: return pos + Vector3.right * tileSize;
        }
        return pos;
    }

    Vector2Int WorldToGrid(Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x / tileSize);
        int y = Mathf.RoundToInt(pos.y / tileSize);
        return new Vector2Int(x, y);
    }

    DungeonPiece.ExitDir GetRandomDirection_NoDown()
    {
        DungeonPiece.ExitDir[] dirs = {
            DungeonPiece.ExitDir.Up,
            DungeonPiece.ExitDir.Left,
            DungeonPiece.ExitDir.Right
        };

        return dirs[Random.Range(0, dirs.Length)];
    }

    DungeonPiece GetRandomPieceByDir(DungeonPiece.ExitDir dir)
    {
        var matches = pieces.Where(p => p != null && p.Dir == dir).ToArray();

        if (matches.Length == 0)
        {
            Debug.LogError($"NO PREFAB WITH DIR: {dir}");
            return null;
        }

        return matches[Random.Range(0, matches.Length)];
    }

    DungeonPiece GetRandomAnyPiece()
    {
        var all = pieces.Where(p => p != null).ToArray();
        return all[Random.Range(0, all.Length)];
    }

    DungeonPiece.ExitDir GetDirOf(GameObject obj)
    {
        var dp = obj.GetComponent<DungeonPiece>();
        if (dp == null)
        {
            Debug.LogWarning("Object missing DungeonPiece component.");
            return DungeonPiece.ExitDir.Up;
        }

        return dp.Dir;
    }
}
