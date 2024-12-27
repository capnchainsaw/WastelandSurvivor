using Unity.VisualScripting;
using UnityEngine;

public class EnemyObject : CellObject
{
    private BoardManager m_Board;
    public int Health { get; set; }
    public int Damage;
    private bool m_Moving = false;
    public float MovementSpeed;
    private Vector3 m_TargetCell;
    private Animator m_Animator;
    private int MovingHash = Animator.StringToHash("Moving");
    private int AttackHash = Animator.StringToHash("Attack");

    public override void Init(Vector2Int cell, int prefabIndex)
    {
        base.Init(cell, prefabIndex);
        m_Board = GameManager.Instance.BoardManager;
        Health = Random.Range(2,3) + Random.Range(0, m_Board.GetLevel());
        m_Animator = GetComponent<Animator>();

        GameManager.Instance.TurnManager.OnTick += OnTurnHappen;
    }

    private void OnDestroy()
    {
        GameManager.Instance.TurnManager.OnTick -= OnTurnHappen;
    }

    public void MoveTo(Vector2Int cell)
    {
        BoardManager.CellData targetCell = m_Board.GetCellData(cell);

        if( targetCell != null && targetCell.Passable )
        {
            if( targetCell.ContainedObject != null )
            {
                // Gobble up any food but dont move into anything else
                if( targetCell.ContainedObject.GetType() != typeof(FoodObject) )
                {
                    return;
                }
                Destroy(targetCell.ContainedObject.gameObject);
            }

            // Remove from current cell
            BoardManager.CellData currentCell = m_Board.GetCellData(m_Cell);
            currentCell.ContainedObject = null;

            // Move to new cell
            m_Cell = cell;
            targetCell.ContainedObject = this;
            m_TargetCell = m_Board.CellToWorld(m_Cell);
            m_Moving = true;
            m_Animator.SetBool(MovingHash, m_Moving);
        }
    }

        // Update is called once per frame
    void Update()
    {
        // If in motion handle movement.
        if( m_Moving )
        {
            float deltaX = m_TargetCell.x - transform.position.x;
            float deltaY = m_TargetCell.y - transform.position.y;
            if( deltaX != 0 || deltaY != 0 )
            {
                transform.position = new Vector3(
                    transform.position.x + (deltaX > MovementSpeed ? MovementSpeed : deltaX), 
                    transform.position.y + (deltaY > MovementSpeed ? MovementSpeed : deltaY), 
                    0
                );
            }
            else
            {
                m_Moving = false;
                m_Animator.SetBool(MovingHash, m_Moving);
            }
            return;
        }
    }

    void OnTurnHappen()
    {
        // Move towards player
        Vector2Int playerCell = GameManager.Instance.PlayerController.Cell();
        int deltaX = playerCell.x - m_Cell.x;
        int deltaY = playerCell.y - m_Cell.y;
        int absDeltaX = Mathf.Abs(deltaX);
        int absDeltaY = Mathf.Abs(deltaY);

        // Attack if player in adjacent cell
        if( (deltaX == 0 && absDeltaY == 1) ||
            (deltaY == 0 && absDeltaX == 1) )
        {
            int TotalDamage = Damage - Random.Range(0, m_Board.GetLevel());
            GameManager.Instance.PlayerController.TakeDamage(Damage);
            m_Animator.SetTrigger(AttackHash);
            return;
        }

        // Otherwise try to move into cell
        if( deltaX > 0 ) MoveTo(m_Cell + Vector2Int.right);
        else if( deltaX < 0 ) MoveTo(m_Cell + Vector2Int.left);
        else if( deltaY > 0 ) MoveTo(m_Cell + Vector2Int.up);
        else if( deltaY < 0 ) MoveTo(m_Cell + Vector2Int.down);
    }

    public override bool PlayerWantsToEnter(int Strength)
    {
        Health -= 1 + Random.Range(0, Strength);
        if( Health > 0 )
        {
            return false;
        }

        Destroy(gameObject);
        return true;
    }

    public override bool PlayerAttacks()
    {
        return true;
    }
}
