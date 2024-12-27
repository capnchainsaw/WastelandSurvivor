using UnityEngine;

public class CellObject : MonoBehaviour
{
    protected Vector2Int m_Cell;

    private int m_PrefabIndex;

    public virtual void Init(Vector2Int cell, int prefabIndex)
    {
        m_Cell = cell;
        m_PrefabIndex = prefabIndex;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public int GetPrefabIndex()
    {
        return m_PrefabIndex;
    }

    public virtual bool PlayerWantsToEnter(int Strength)
    {
        return true;
    }

    public virtual bool PlayerAttacks()
    {
        return false;
    }

    public virtual void PlayerEntered()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
