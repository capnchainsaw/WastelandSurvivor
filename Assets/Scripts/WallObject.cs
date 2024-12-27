using UnityEngine;
using UnityEngine.Tilemaps;

public class WallObject : CellObject 
{
    public Tile[] ObstacleTiles;
    private Tile m_OriginalTile;

    public int Health { get; set; }
    private int m_CurrentTile;
  
    public override void Init(Vector2Int cell, int prefabIndex)
    {
        base.Init(cell, prefabIndex);
        Health = Random.Range(4,6);
        m_OriginalTile = GameManager.Instance.BoardManager.GetCellTile(m_Cell);
        m_CurrentTile = Random.Range(0, ObstacleTiles.Length);
        Tile tile = ObstacleTiles[m_CurrentTile];
        GameManager.Instance.BoardManager.SetCellTile(m_Cell, tile);
    }

    public override bool PlayerWantsToEnter(int Strength)
    {
        Health -= 1 + Random.Range(0, Strength);;
        if( Health > 0 )
        {
            int NewTile = 0;
            do
            {
                NewTile = Random.Range(0, ObstacleTiles.Length);
            }
            while(m_CurrentTile == NewTile);
            Tile tile = ObstacleTiles[NewTile];
            GameManager.Instance.BoardManager.SetCellTile(m_Cell, tile);
            return false;
        }

        GameManager.Instance.BoardManager.SetCellTile(m_Cell, m_OriginalTile);
        Destroy(gameObject);
        return true;
    }

    public override bool PlayerAttacks()
    {
        return true;
    }
}
