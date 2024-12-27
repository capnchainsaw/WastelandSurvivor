using UnityEngine;

public class TurnManager
{
    public event System.Action OnTick;
    private int m_TurnCount;

    public TurnManager(int startingTurn)
    {
        m_TurnCount = startingTurn;
    }

    public int GetTurnCount()
    {
        return m_TurnCount;
    }

    public void Tick()
    {
        m_TurnCount += 1;
        OnTick?.Invoke();
    }
}
