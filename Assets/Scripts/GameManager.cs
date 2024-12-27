using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public BoardManager BoardManager;
    public PlayerController PlayerController;
    public TurnManager TurnManager { get; private set; }
    public UIDocument UIDoc;

    private VisualElement m_MenuPanel;
    private Label m_MenuLabel;
    private Button m_ContinueButton;
    private Button m_ExitButton;
    private Label m_FoodLabel;
    private int m_FoodAmount = 0;
    public int StartingFood;
    public string SaveGamePath;

    private void Awake()
    {
       if (Instance != null)
       {
           Destroy(gameObject);
           return;
       }
      
       Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_MenuPanel = UIDoc.rootVisualElement.Q<VisualElement>("MenuPanel");
        m_MenuLabel = m_MenuPanel.Q<Label>("MenuLabel");
        m_ContinueButton = m_MenuPanel.Q<Button>("ContinueButton");
        m_ContinueButton.clicked += HideMenu;
        m_MenuPanel.Q<Button>("NewGameButton").clicked += RestartGame;
        m_ExitButton = m_MenuPanel.Q<Button>("ExitButton");
        m_ExitButton.clicked += ExitGame;

        m_FoodLabel = UIDoc.rootVisualElement.Q<Label>("FoodLabel");
        
        if( File.Exists(SaveGamePath) )
        {
            try
            {
                LoadSavedGame();
                m_ContinueButton.style.visibility = Visibility.Visible;
                ShowMenu("Wasteland Survivor", "Save & Exit");
                return;
            }
            catch(Exception e)
            {
                Debug.Log(e.Message);
                ShowMenu("Failed to Load", "Exit");
            }
        }
        else
        {
            ShowMenu("Wasteland Survivor", "Exit");
        }

        TurnManager = new TurnManager(1);
        TurnManager.OnTick += OnTurnHappen;

        BoardManager.Init();
        PlayerController.Spawn(this, BoardManager, new Vector2Int(1,1));
        AdjustFoodAmount(StartingFood);
    }

    public void RestartGame()
    {
        HideMenu();
        BoardManager.ClearLevel();
        PlayerController.Spawn(this, BoardManager, new Vector2Int(1, 1));
        AdjustFoodAmount(StartingFood);
        BoardManager.Init();
    }

    public void ExitGame()
    {
        if( !PlayerController.IsGameOver() )
        {
            try
            {
                SaveGame();
            }
            catch(Exception e)
            {
                Debug.Log(e.Message);
            }
        }
        Application.Quit();
    }

    private void SaveGame()
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Indent = true;
        XmlWriter writer = XmlWriter.Create(@SaveGamePath, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement("Game");
        writer.WriteElementString("Food", m_FoodAmount.ToString());
        writer.WriteElementString("Turn", TurnManager.GetTurnCount().ToString());
        BoardManager.SaveBoard(writer);
        PlayerController.SavePlayer(writer);
        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Flush();
        writer.Close();
    }

    private void LoadSavedGame()
    {
        using (XmlReader reader = XmlReader.Create(SaveGamePath))
        {
            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    switch (reader.Name.ToString())  
                    {
                        case "Food":
                            AdjustFoodAmount(Int32.Parse(reader.ReadElementContentAsString()));
                            break;
                        case "Turn":
                            int startingTurn = Int32.Parse(reader.ReadElementContentAsString());
                            TurnManager = new TurnManager(startingTurn);
                            TurnManager.OnTick += OnTurnHappen;
                            break;
                        case "Board":
                            BoardManager.Init(reader);
                            break;
                        case "Player":
                            PlayerController.Spawn(this, BoardManager, reader);
                            break;
                    }
                }
            }
        }
    }

    public void ShowMenu(string MenuLabelText, string ExitButtonText)
    {
        m_MenuLabel.text = MenuLabelText;
        m_ExitButton.text = ExitButtonText;
        m_MenuPanel.style.visibility = Visibility.Visible;
    }

    public void HideMenu()
    {
        m_MenuPanel.style.visibility = Visibility.Hidden;
        m_ContinueButton.style.visibility = Visibility.Hidden;
    }

    public bool InMenu()
    {
        return m_MenuPanel.style.visibility == Visibility.Visible;
    }

    void OnTurnHappen()
    {
        AdjustFoodAmount(-1);

        if(TurnManager.GetTurnCount() % 15 == 0)
        {
            BoardManager.GenerateFood();
        }
    }

    public void AdjustFoodAmount(int amount)
    {
        m_FoodAmount += amount;
        m_FoodLabel.text = "Food : " + m_FoodAmount;

        if( m_FoodAmount <= 0)
        {
            PlayerController.SetGameOver(true);
            if( File.Exists(SaveGamePath) ) File.Delete(SaveGamePath);
            ShowMenu("Game Over", "Exit");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
