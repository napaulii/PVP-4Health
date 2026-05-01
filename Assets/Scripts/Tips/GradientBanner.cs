// GradientBanner.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GradientBanner : MonoBehaviour
{
    public Color topColor = new Color(0.3f, 0.5f, 0.8f, 0.9f);
    public Color bottomColor = new Color(0.1f, 0.2f, 0.4f, 0.9f);

    void Start()
    {
        CreateGradientTexture();
    }

    void CreateGradientTexture()
    {
        Texture2D texture = new Texture2D(1, 256);
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < 256; y++)
        {
            float t = y / 255f;
            texture.SetPixel(0, y, Color.Lerp(bottomColor, topColor, t));
        }
        texture.Apply();

        Image image = GetComponent<Image>();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 256), new Vector2(0.5f, 0.5f));
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
    }
}