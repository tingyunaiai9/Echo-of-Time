using UnityEngine;

public class Cells : MonoBehaviour
{
    [Header("Cell Settings")]
    [SerializeField] private int columns = 18;
    [SerializeField] private int rows = 10;
    [SerializeField] private float cellSize = 0.9f;
    
    void Start()
    {
        GenerateCells();
    }

    private void GenerateCells()
    {
        int totalCells = columns * rows;
        
        for (int i = 0; i < totalCells; i++)
        {
            // 创建新的GameObject
            GameObject cell = new GameObject($"cell_{i}");
            
            // 设置父对象为当前对象（Cells）
            cell.transform.SetParent(transform);
            cell.transform.localScale = Vector3.one;
            
            // 添加SpriteRenderer组件
            SpriteRenderer spriteRenderer = cell.AddComponent<SpriteRenderer>();
            
            // 创建白色方形sprite
            spriteRenderer.sprite = CreateSquareSprite();
            spriteRenderer.color = Color.white;
            
            // 设置大小
            cell.transform.localScale = new Vector3(cellSize, cellSize, 1f);
        }
    }

    private Sprite CreateSquareSprite()
    {
        // 创建一个简单的白色方形纹理
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        
        // 从纹理创建sprite
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );
        
        return sprite;
    }
}