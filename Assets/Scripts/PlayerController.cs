using System;
using System.Xml;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    private GameManager m_Game;
    private BoardManager m_Board;
    private Vector2Int m_CellPosition;
    private bool m_GameOver = false;
    private bool m_Moving = false;
    public float MovementSpeed;
    private Vector3 m_TargetCell;

    private Animator m_Animator;
    private int MovingHash = Animator.StringToHash("Moving");
    private int AttackHash = Animator.StringToHash("Attack");
    private int OuchHash = Animator.StringToHash("Ouch");

    private int m_Strength;
    private Label m_StrengthLabel;
    private int m_Defense;
    private Label m_DefenseLabel;
    private int m_Stamina;
    private int m_CurrentStamina;
    private Label m_StaminaLabel;


    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void SpawnBase(GameManager gameManager, BoardManager boardManager)
    {
        m_Game = gameManager;
        m_Board = boardManager;
        m_GameOver = false;
        m_Strength = 0;
        m_Defense = 0;
        m_Stamina = 0;
        m_CurrentStamina = 0;
        m_StrengthLabel = m_Game.UIDoc.rootVisualElement.Q<Label>("StrengthLabel");
        m_DefenseLabel = m_Game.UIDoc.rootVisualElement.Q<Label>("DefenseLabel");
        m_StaminaLabel = m_Game.UIDoc.rootVisualElement.Q<Label>("StaminaLabel");
    }

    public void Spawn(GameManager gameManager, BoardManager boardManager, Vector2Int cell)
    {
        SpawnBase(gameManager, boardManager);
        AdjustStrength(1);
        AdjustDefense(1);
        AdjustStamina(1, 1);
        MoveTo(cell, true);
    }

    public void Spawn(GameManager gameManager, BoardManager boardManager, XmlReader reader)
    {
        SpawnBase(gameManager, boardManager);
        int x = 0;
        while (reader.Read())
        {
            if (reader.IsStartElement())
            {
                switch (reader.Name.ToString())  
                {
                    case "Strength":
                        AdjustStrength(Int32.Parse(reader.ReadElementContentAsString()));
                        break;
                    case "Defense":
                        AdjustDefense(Int32.Parse(reader.ReadElementContentAsString()));
                        break;
                    case "CurrentStamina":
                        AdjustStamina(0, Int32.Parse(reader.ReadElementContentAsString()));
                        break;
                    case "Stamina":
                        AdjustStamina(Int32.Parse(reader.ReadElementContentAsString()), 0);
                        break;
                    case "X":
                        x = Int32.Parse(reader.ReadElementContentAsString());
                        break;
                    case "Y":
                        Vector2Int cell = new Vector2Int(x, Int32.Parse(reader.ReadElementContentAsString()));
                        MoveTo(cell, true);
                        return;
                }
            }
        }
    }

    public bool IsGameOver()
    {
        return m_GameOver;
    }

    public void SetGameOver(bool GameOver)
    {
        m_GameOver = GameOver;
    }

    public Vector2Int Cell()
    {
        return m_CellPosition;
    }

    public void MoveTo(Vector2Int cell, bool immediate)
    {
        m_CellPosition = cell;
        if(immediate)
        {
            transform.position = m_Board.CellToWorld(m_CellPosition);
            m_Moving = false;
        }
        else
        {
            m_TargetCell = m_Board.CellToWorld(m_CellPosition);
            m_Moving = true;
        }

        m_Animator.SetBool(MovingHash, m_Moving);
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if( m_Game.InMenu() )
            {
                if( m_GameOver ) m_Game.RestartGame();
                else m_Game.HideMenu();
            }
            else
            {
                m_Game.ShowMenu("Game Paused", "Save & Exit");
            }
        }

        // Aside from escape key Menu means game paused
        if( m_Game.InMenu() )
        {
            return;
        }

        // If in motion handle movement.
        if( m_Moving )
        {
            float deltaX = m_TargetCell.x - transform.position.x;
            float deltaY = m_TargetCell.y - transform.position.y;
            if( deltaX != 0 || deltaY != 0 )
            {
                transform.position = new Vector3(
                    transform.position.x + (Math.Abs(deltaX) > MovementSpeed ? deltaX < 0 ? -1 * MovementSpeed : MovementSpeed : deltaX), 
                    transform.position.y + (Math.Abs(deltaY) > MovementSpeed ? deltaY < 0 ? -1 * MovementSpeed : MovementSpeed : deltaY), 
                    0
                );
            }
            else
            {
                m_Moving = false;
                m_Animator.SetBool(MovingHash, m_Moving);
                var cellData = m_Board.GetCellData(m_CellPosition);
                if(cellData.ContainedObject != null)
                    cellData.ContainedObject.PlayerEntered();
            }
            return;
        }

        // If game over then handle any key and restart game
        if( m_GameOver )
        {
            if( Keyboard.current.anyKey.wasPressedThisFrame)
            {
                m_Game.RestartGame();
            }
            return;
        }

        Vector2Int newCellTarget = m_CellPosition;
        bool hasMoved = false;

        if(Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            AdjustStamina(0, -1);
            hasMoved = false;
        }
        else if(Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            newCellTarget.y += 1;
            hasMoved = true;
        }
        else if(Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            newCellTarget.y -= 1;
            hasMoved = true;
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            newCellTarget.x += 1;
            hasMoved = true;
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            newCellTarget.x -= 1;
            hasMoved = true;
        }

        if(hasMoved)
        {
            //check if the new position is passable, then move there if it is.
            BoardManager.CellData cellData = m_Board.GetCellData(newCellTarget);

            if(cellData != null && cellData.Passable)
            {
                if (cellData.ContainedObject == null)
                {
                    MoveTo(newCellTarget, false);
                }
                else 
                {
                    if( cellData.ContainedObject.PlayerAttacks() )
                    {
                        m_Animator.SetTrigger(AttackHash);
                    }
                    if (cellData.ContainedObject.PlayerWantsToEnter(m_Strength))
                    {
                        MoveTo(newCellTarget, false);
                    }
                }

                AdjustStamina(0, -1);
            }
        }
    }

    public void TakeDamage(int Damage)
    {
        Damage += UnityEngine.Random.Range(0, m_Defense);
        if( Damage < 0 )
        {
            m_Game.AdjustFoodAmount(Damage);
            m_Animator.SetTrigger(OuchHash);
        }
    }

    public void AdjustStrength(int StrengthAmount)
    {
        m_Strength += StrengthAmount;
        m_StrengthLabel.text = "STR : " + m_Strength;
    }

    public void AdjustDefense(int DefenseAmount)
    {
        m_Defense += DefenseAmount;
        m_DefenseLabel.text = "DEF : " + m_Defense;
    }

    public void AdjustStamina(int StaminaAmount, int CurrentStaminaAmount)
    {
        m_Stamina += StaminaAmount;
        m_CurrentStamina += CurrentStaminaAmount;

        if( m_CurrentStamina <= 0 )
        {
            m_Game.TurnManager.Tick();
            m_CurrentStamina = m_Stamina;
        }
        m_StaminaLabel.text = "STA : " + m_CurrentStamina + " / " + m_Stamina;
    }

    public void SavePlayer(XmlWriter writer)
    {
        writer.WriteStartElement("Player");
        writer.WriteElementString("Strength", m_Strength.ToString());
        writer.WriteElementString("Defense", m_Defense.ToString());
        writer.WriteElementString("CurrentStamina", m_CurrentStamina.ToString());
        writer.WriteElementString("Stamina", m_Stamina.ToString());
        writer.WriteElementString("X", m_CellPosition.x.ToString());
        writer.WriteElementString("Y", m_CellPosition.y.ToString());
        writer.WriteEndElement();
    }
}
