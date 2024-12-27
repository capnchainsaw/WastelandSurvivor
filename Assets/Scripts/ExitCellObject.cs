using UnityEngine;
using UnityEngine.Tilemaps;

public class ExitCellObject : CellObject
{
    public Tile EndTile;

    public override void Init(Vector2Int coord, int prefabIndex)
    {
        base.Init(coord, prefabIndex);
        GameManager.Instance.BoardManager.SetCellTile(coord, EndTile);
    }

    public override void PlayerEntered()
    {
        GameManager.Instance.BoardManager.NextLevel();
    }
}
