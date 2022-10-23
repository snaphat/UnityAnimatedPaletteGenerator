using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.Tilemaps;
using System.IO;
using System.Linq;

public class AnimatedPaletteGenerator
{
    const int AnimationCount = 3;
    const int Stride = AnimationCount;
    const float minSpeed = 2f;
    const float maxSpeed = minSpeed;

    [MenuItem("Assets/Create/2D/Tile Palette/Animated", priority = 0)]
    private static void Generate(MenuCommand menuCommand)
    {
        for (int i = 0; i < Selection.objects.Length; i++)
        {
            var item = Selection.objects[i];
            if (item is not Texture2D)
            {
                Debug.Log("<color=red>Animated Palette: Must use a Texture2D Object!</color>");
                return;
            }

            //  Grab path
            var srcPath = AssetDatabase.GetAssetPath(item);
            var dstPath = Path.GetDirectoryName(srcPath);
            var file = Path.GetFileNameWithoutExtension(srcPath);

            // create Palette
            GameObject tilePalette = GridPaletteUtility.CreateNewPalette(dstPath, file + ".Palette",
            GridLayout.CellLayout.Rectangle,
            GridPalette.CellSizing.Automatic, new(0.16f, 0.16f, 0.16f), GridLayout.CellSwizzle.XYZ);
            Tilemap paletteTilemap = tilePalette.GetComponentInChildren<Tilemap>();

            // Generate a folder for the tiles and grab the path Unity actually made
            dstPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(dstPath, file + ".AnimatedTiles"));

            // Load asset, order, and filter sprites
            var data = AssetDatabase.LoadAllAssetsAtPath(srcPath);
            data = data.OrderBy(x =>
            {
                if (x.name.Contains('_')) return int.Parse(x.name.Split('_')[1]);
                return -1;
            }
            ).ToArray();
            var width = (data[0] as Texture2D).width;

            data = data.Where(x => x.GetType() == typeof(Sprite)).ToArray();

            // Make sure the  parameters are sane
            if (data == null || data.Length % AnimationCount != 0)
            {
                Debug.Log("<color=red>Animated Palette: Sprite count must be a multiple of Animation Count!</color>");
                return;
            }

            // Pregenerate each animated tile and add as an AnimatedTile asset
            var totalTiles = data.Length / AnimationCount;
            var animatedTile = new AnimatedTile[totalTiles];
            for (var j = 0; j < totalTiles; j++)
            {
                animatedTile[j] = ScriptableObject.CreateInstance<AnimatedTile>();
                animatedTile[j].m_AnimatedSprites = new Sprite[AnimationCount];
                animatedTile[j].m_MinSpeed = minSpeed;
                animatedTile[j].m_MaxSpeed = maxSpeed;
                //paletteTilemap.SetTile(new Vector3Int(j % Stride, -j / Stride, 0), animatedTile[j]);
                paletteTilemap.SetTile(new Vector3Int(j % (width / (Stride * 16)), -j / (width / (Stride * 16)), 0), animatedTile[j]);
            }

            // Add sprites to animated tiles at the given stride
            var count = 0;
            foreach (Sprite sprite in data)
            {
                var k = count % Stride + Stride * (count / (Stride * Stride));
                animatedTile[k].m_AnimatedSprites[count / Stride % Stride] = sprite;
                count++;
            }

            for (var j = 0; j < totalTiles; j++)
            {
                AssetDatabase.CreateAsset(animatedTile[j], dstPath + "/" + file + "_" + j + ".asset");
            }

            // Print results
            Debug.Log("<color=yellow><color=cyan>Animated Palette:</color> Created Tile Palette and <color=red>" +
            data.Length / AnimationCount + "</color> Animated Tiles using a stride of <color=red>" + Stride +
            "</color> with <color=red>" + AnimationCount + "</color> sprites per tile.</color>");
        }

    }
}
