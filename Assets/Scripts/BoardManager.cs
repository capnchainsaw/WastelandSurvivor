using System;
using System.Xml;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class BoardManager : MonoBehaviour
{
    public class CellData
    {
        public bool Passable;
        public CellObject ContainedObject;
    }

    private CellData[,] m_BoardData;
    private Tilemap m_Tilemap;
    private Grid m_Grid;

    private int m_Width;
    public int MaxWidth;
    public int MinWidth;
    private int m_Height;
    public int MaxHeight;
    public int MinHeight;
    public Tile[] GroundTiles;
    public Tile[] WallTiles;

    public FoodObject[] FoodPrefabs;
    public int FoodGenerated;

    public WallObject WallPrefab;
    public ExitCellObject ExitCellPrefab;

    public EnemyObject EnemyPrefab;

    public ItemObject[] ItemPrefabs;

    private Label m_LevelLabel;
    private int m_Level;

    private void BaseInit()
    {
        m_Level = 0;
        m_LevelLabel = GameManager.Instance.UIDoc.rootVisualElement.Q<Label>("LevelLabel");

        m_Tilemap = GetComponentInChildren<Tilemap>();
        m_Grid = GetComponentInChildren<Grid>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Init()
    {
        BaseInit();
        AddLevelCount(1);
        CreateLevel();
    }

    public void Init(XmlReader reader)
    {
        BaseInit();
        while (reader.Read())
        {
            if (reader.IsStartElement())
            {
                switch (reader.Name.ToString())  
                {
                    case "Width":
                        m_Width = Int32.Parse(reader.ReadElementContentAsString());
                        break;
                    case "Height":
                        m_Height = Int32.Parse(reader.ReadElementContentAsString());
                        CreateBoardWithWalls();
                        break;
                    case "Wall":
                        LoadWall(reader);
                        break;
                    case "Food":
                        LoadFood(reader);
                        break;
                    case "Enemy":
                        LoadEnemy(reader);
                        break;
                    case "Item":
                        LoadPowerup(reader);
                        break;
                    case "Level":
                        AddLevelCount(Int32.Parse(reader.ReadElementContentAsString()));
                        return;
                }
            }
        }
    }

    public void AddLevelCount(int count)
    {
        m_Level += count;
        m_LevelLabel.text = "Level : " + m_Level;
    }
    
    private void CreateBoardWithWalls()
    {
        m_BoardData = new CellData[m_Width, m_Height];

        for (int y = 0; y < m_Height; ++y)
        {
            for(int x = 0; x < m_Width; ++x)
            {
                Tile tile;
                m_BoardData[x, y] = new CellData();
              
                if(x == 0 || y == 0 || x == m_Width - 1 || y == m_Height - 1)
                {
                    tile = WallTiles[UnityEngine.Random.Range(0, WallTiles.Length)];
                    m_BoardData[x, y].Passable = false;
                }
                else
                {
                    tile = GroundTiles[UnityEngine.Random.Range(0, GroundTiles.Length)];
                    m_BoardData[x, y].Passable = true;
                }

                m_Tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        CellData data = m_BoardData[m_Width-2, m_Height-2];
        ExitCellObject exit = Instantiate(ExitCellPrefab);
        Vector2Int coord = new Vector2Int(m_Width-2, m_Height-2);
        exit.Init(coord, 0);
        exit.transform.position = CellToWorld(coord);
        data.ContainedObject = exit;
    }

    private void CreateLevel()
    {
        m_Width = UnityEngine.Random.Range(MinWidth, MaxWidth + (m_Level > 10 ? 10 : m_Level));
        m_Height = UnityEngine.Random.Range(MinHeight, MaxHeight + (m_Level > 10 ? 10 : m_Level));
        CreateBoardWithWalls();

        GeneratePowerup();
        GenerateEnemies();
        GenerateWalls();
        GenerateFood();
    }

    public void ClearLevel()
    {
        for (int y = 0; y < m_Height; ++y)
        {
            for(int x = 0; x < m_Width; ++x)
            {
                // Clear tile
                m_Tilemap.SetTile(new Vector3Int(x, y, 0), null);
                // Clear data
                CellData data = m_BoardData[x, y];
                if( data.ContainedObject != null )
                {
                    Destroy(data.ContainedObject.gameObject);
                }
            }
        }
    }

    public void NextLevel()
    {
        ClearLevel();

        AddLevelCount(1);
        
        GameManager.Instance.PlayerController.MoveTo(new Vector2Int(1, 1), true);
        CreateLevel();
    }

    public int GetLevel()
    {
        return m_Level;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 CellToWorld(Vector2Int cellIndex)
    {
        return m_Grid.GetCellCenterWorld((Vector3Int)cellIndex);
    }

    public CellData GetCellData(Vector2Int cellIndex)
    {
        if (cellIndex.x < 0 || cellIndex.x >= m_Width
            || cellIndex.y < 0 || cellIndex.y >= m_Height)
        {
            return null;
        }

        return m_BoardData[cellIndex.x, cellIndex.y];
    }

    public Tile GetCellTile(Vector2Int cellIndex)
    {
        return m_Tilemap.GetTile<Tile>(new Vector3Int(cellIndex.x, cellIndex.y, 0));
    }

    public void SetCellTile(Vector2Int cellIndex, Tile tile)
    {
        m_Tilemap.SetTile(new Vector3Int(cellIndex.x, cellIndex.y, 0), tile);
    }

    void AddPowerup(int x, int y, int prefabIndex)
    {
        CellData data = m_BoardData[x, y];
        if (data.Passable && data.ContainedObject == null)
        {
            ItemObject newItem = Instantiate(ItemPrefabs[prefabIndex]);
            Vector2Int coord = new Vector2Int(x, y);
            newItem.Init(coord, prefabIndex);
            newItem.transform.position = CellToWorld(coord);
            data.ContainedObject = newItem;
        }
    }

    void GeneratePowerup()
    {
        AddPowerup(
            UnityEngine.Random.Range(2, m_Width-2),
            UnityEngine.Random.Range(2, m_Height-2),
            UnityEngine.Random.Range(0, ItemPrefabs.Length) );
    }

    void LoadPowerup(XmlReader reader)
    {
        int x = 0;
        int y = 0;
        while (reader.Read())
        {
            if (reader.IsStartElement())
            {
                switch (reader.Name.ToString())  
                {
                    case "X":
                        x = Int32.Parse(reader.ReadElementContentAsString());
                        break;
                    case "Y":
                        y = Int32.Parse(reader.ReadElementContentAsString());
                        break;
                    case "Index":
                        int prefabIndex = Int32.Parse(reader.ReadElementContentAsString());
                        AddPowerup(x, y, prefabIndex);
                        return;
                }
            }
        }
    }

    void AddEnemy(int x, int y, int health)
    {
        CellData data = m_BoardData[x, y];
        if (data.Passable && data.ContainedObject == null)
        {
            EnemyObject newEnemy = Instantiate(EnemyPrefab);
            Vector2Int coord = new Vector2Int(x, y);
            newEnemy.Init(coord, 0);
            if( health != -1 ) newEnemy.Health = health;
            newEnemy.transform.position = CellToWorld(coord);
            data.ContainedObject = newEnemy;
        }
    }

    void GenerateEnemies()
    {
        int enemyCount = UnityEngine.Random.Range(1, (m_Level > 3 ? 5 : m_Level));
        for (int i = 0; i < enemyCount; ++i)
        {
            AddEnemy(
                UnityEngine.Random.Range(2, m_Width-2),
                UnityEngine.Random.Range(2, m_Height-2),
                -1
            );
        }
    }

    void LoadEnemy(XmlReader reader)
    {
        int x = 0;
        int y = 0;
        while (reader.Read())
        {
            if (reader.IsStartElement())
            {
                switch (reader.Name.ToString())  
                {
                    case "X":
                        x = Int32.Parse(reader.ReadElementContentAsString());
                        break;
                    case "Y":
                        y = Int32.Parse(reader.ReadElementContentAsString());
                        break;
                    case "Health":
                        int health = Int32.Parse(reader.ReadElementContentAsString());
                        AddEnemy(x, y, health);
                        return;
                }
            }
        }
    }

    void AddWall(int x, int y, int health)
    {
        CellData data = m_BoardData[x, y];
        if (data.Passable && data.ContainedObject == null)
        {
            WallObject newWall = Instantiate(WallPrefab);
            Vector2Int coord = new Vector2Int(x, y);
            newWall.Init(coord, 0);
            if( health != -1 ) newWall.Health = health;
            newWall.transform.position = CellToWorld(coord);
            data.ContainedObject = newWall;
        }
    }

    void GenerateWalls()
    {
        int wallCount = UnityEngine.Random.Range(
            (MinWidth < MinHeight ? MinHeight : MinWidth), 
            (MaxWidth < MaxHeight ? MaxHeight : MaxWidth) + (m_Level > 5 ? 5 : m_Level));
        for (int i = 0; i < wallCount; ++i)
        {
            AddWall(
                UnityEngine.Random.Range(2, m_Width-1),
                UnityEngine.Random.Range(2, m_Height-1),
                -1
            );
        }
    }

    void LoadWall(XmlReader reader)
    {
        int x = 0;
        int y = 0;
        while (reader.Read())
        {
            if (reader.IsStartElement())
            {
                switch (reader.Name.ToString())  
                {
                    case "X":
                        x = Int32.Parse(reader.ReadElementContentAsString());
                        break;
                    case "Y":
                        y = Int32.Parse(reader.ReadElementContentAsString());
                        break;
                    case "Health":
                        int health = Int32.Parse(reader.ReadElementContentAsString());
                        AddWall(x, y, health);
                        return;
                }
            }
        }
    }

    void AddFood(int x, int y, int prefabIndex)
    {
        CellData data = m_BoardData[x, y];
        if (data.Passable && data.ContainedObject == null)
        {
            FoodObject newFood = Instantiate(FoodPrefabs[prefabIndex]);
            Vector2Int coord = new Vector2Int(x, y);
            newFood.Init(coord, prefabIndex);
            newFood.transform.position = CellToWorld(coord);
            data.ContainedObject = newFood;
        }
    }

    public void GenerateFood()
    {
        for (int i = 0; i < FoodGenerated; ++i)
        {
            AddFood(
                UnityEngine.Random.Range(2, m_Width-1),
                UnityEngine.Random.Range(2, m_Height-1),
                UnityEngine.Random.Range(0, FoodPrefabs.Length)
            );
        }
    }

    void LoadFood(XmlReader reader)
    {
        int x = 0;
        int y = 0;
        while (reader.Read())
        {
            if (reader.IsStartElement())
            {
                switch (reader.Name.ToString())  
                {
                    case "X":
                        x = Int32.Parse(reader.ReadElementContentAsString());
                        break;
                    case "Y":
                        y = Int32.Parse(reader.ReadElementContentAsString());
                        break;
                    case "Index":
                        int prefabIndex = Int32.Parse(reader.ReadElementContentAsString());
                        AddFood(x, y, prefabIndex);
                        return;
                }
            }
        }
    }

    public void SaveBoard(XmlWriter writer)
    {
        writer.WriteStartElement("Board");
        writer.WriteElementString("Width", m_Width.ToString());
        writer.WriteElementString("Height", m_Height.ToString());
        // Save all contained objects ignoring walls and exit
        for (int y = 1; y < m_Height-1; ++y)
        {
            for(int x = 1; x < m_Width-1; ++x)
            {
                CellData data = m_BoardData[x, y];
                if( data.ContainedObject != null )
                {
                    if( data.ContainedObject.GetType() == typeof(WallObject) )
                    {
                        writer.WriteStartElement("Wall");
                        writer.WriteElementString("X", x.ToString());
                        writer.WriteElementString("Y", y.ToString());
                        writer.WriteElementString("Health", ((WallObject) data.ContainedObject).Health.ToString());
                        writer.WriteEndElement();
                    }
                    else if( data.ContainedObject.GetType() == typeof(FoodObject) )
                    {
                        writer.WriteStartElement("Food");
                        writer.WriteElementString("X", x.ToString());
                        writer.WriteElementString("Y", y.ToString());
                        writer.WriteElementString("Index", data.ContainedObject.GetPrefabIndex().ToString());
                        writer.WriteEndElement();
                    }
                    else if( data.ContainedObject.GetType() == typeof(EnemyObject) )
                    {
                        writer.WriteStartElement("Enemy");
                        writer.WriteElementString("X", x.ToString());
                        writer.WriteElementString("Y", y.ToString());
                        writer.WriteElementString("Health", ((EnemyObject)data.ContainedObject).Health.ToString());
                        writer.WriteEndElement();
                    }
                    else if( data.ContainedObject.GetType() == typeof(ItemObject) )
                    {
                        writer.WriteStartElement("Item");
                        writer.WriteElementString("X", x.ToString());
                        writer.WriteElementString("Y", y.ToString());
                        writer.WriteElementString("Index", data.ContainedObject.GetPrefabIndex().ToString());
                        writer.WriteEndElement();
                    }
                }
            }
        }
        writer.WriteElementString("Level", m_Level.ToString());
        writer.WriteEndElement();
    }
}
