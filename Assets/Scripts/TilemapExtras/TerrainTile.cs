using System;
using System.Collections;
using System.Collections.Generic;

using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace UnityEngine.Tilemaps
{
	[Serializable]
	public class TerrainTile : Tile
	{
		// What to call GameObjects that are children of these tiles that
		// contain a SpriteRenderer for the inner corner overlays
		private const string OverlayGOName = "Overlay_";
	
		[SerializeField]
		public new Tile.ColliderType ColliderType = Tile.ColliderType.Grid;

		[SerializeField]
		public Sprite[] tileSprites;

		[SerializeField]
		public Sprite[] nonAutoInnerSprites;

		public override void RefreshTile(Vector3Int position, ITilemap tilemap)
		{
			for (int yd = -1; yd <= 1; yd++)
			{
				for (int xd = -1; xd <= 1; xd++)
				{
                    Vector3Int tempPosition = new Vector3Int(position.x + xd, position.y + yd, position.z);
					if (TileIsThisTile(tilemap, tempPosition))
					{
						tilemap.RefreshTile(tempPosition);
					}
				}
			}
		}

		public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
		{
			UpdateTile(position, tilemap, ref tileData);
		}

		private static Vector3Int TopLeft = new Vector3Int (-1, 1, 0);
		private static Vector3Int Top = new Vector3Int (0, 1, 0);
		private static Vector3Int TopRight = new Vector3Int (1, 1, 0);

		private static Vector3Int Left = new Vector3Int (-1, 0, 0);
		private static Vector3Int Right = new Vector3Int (1, 0, 0);

		private static Vector3Int BottomLeft = new Vector3Int (-1, -1, 0);
		private static Vector3Int Bottom = new Vector3Int (0, -1, 0);
		private static Vector3Int BottomRight = new Vector3Int (1, -1, 0);

		private void UpdateTile(Vector3Int location, ITilemap tileMap, ref TileData tileData)
		{
			tileData.transform = Matrix4x4.identity;
			tileData.color = Color.white;

			// Build a mask based on the surrounding 8 tiles
			// Top left is 1, top is 2, top right is 4, etc.
			// This builds us a bytes-worth of data that we can use
			// to quickly look up the appropriate value for the current tile

			byte mask = GetTileMask(tileMap, location);
			int index = GetIndex ((byte)mask);

//			if (!(index >= 0 && index < tileSprites.Length && TileIsThisTile (tileMap, location))) {
//				Debug.LogError ("Unknown index returned from GetIndex. That's strange." + index.ToString());
//			}

			if (!(index >= 0 && index < tileSprites.Length)) {
				Debug.LogError ("Unknown index returned from GetIndex. That's strange." + index.ToString());
			}

			tileData.sprite = tileSprites[index];

			tileData.flags = TileFlags.LockTransform | TileFlags.LockColor;
			tileData.colliderType = Tile.ColliderType.Grid;
		}

		public byte GetTileMask(ITilemap tileMap, Vector3Int location)
		{
			int mask = TileIsThisTile(tileMap, location + TopLeft) ? 1 : 0;
			mask += TileIsThisTile(tileMap, location + Top) ? 2 : 0;
			mask += TileIsThisTile(tileMap, location + TopRight) ? 4 : 0;

			mask += TileIsThisTile(tileMap, location + Left) ? 8 : 0;
			mask += TileIsThisTile(tileMap, location + Right) ? 16 : 0;

			mask += TileIsThisTile(tileMap, location + BottomLeft) ? 32 : 0;
			mask += TileIsThisTile(tileMap, location + Bottom) ? 64 : 0;
			mask += TileIsThisTile(tileMap, location + BottomRight) ? 128 : 0;

			return (byte)mask;
		}

		private bool TileIsThisTile(ITilemap tileMap, Vector3Int position)
		{
			TileBase tile = tileMap.GetTile(position);
			if (tile == null)
			{
				return false;
			}

            if (tile == this)
            {
                return true;
            }

			Sprite s = tileMap.GetSprite (position);

			if (s == null || !s)
			{
				return false;
			}

			// Next: Check if it's in a decoration list
			if (nonAutoInnerSprites.Contains(s))
			{
				//Debug.Log ("It worked? Or did it?");
				return true;
			}

			return false;
		}

		// Use for when we want an inner corner cap, check to see if the tile is empty before filling it.
		private bool TileIsEmpty(ITilemap Tilemap, Vector3Int position)
		{
			return Tilemap.GetTile (position) == null;
		}

		//	1	2	4
		//	8		16
		//	32	64	128
		private int GetIndex(byte mask)
		{
			switch (mask)
			{
			case 0: 
				return 15; // Standalone block is standing alone
			case 1:
			case 4:
			case 32:
			case 128:
			case 4+1:
			case 32+1:
			case 128+1:
			case 32+4:
			case 128+32:
			case 32+4+1:
			case 128+32+1:
			case 128+32+4:
			case 128+32+4+1:	
				return 15; // Blocks just in corners = still standing alone
			
			case 2+1:
			case 4+2:
			case 4+2+1:
				return 14; // Coming from top only = vertical bottom cap

			case 4+16:
			case 16+128:
			case 4+16+128:
				return 9; // Coming from right only = horizontal left cap

			case 32+64:
			case 32+64+128:
			case 128+64:
				return 12; // Coming from bottom only = vertical top cap

			case 1+8:
			case 1+8+32:
			case 8+32:
				return 11; // Coming from left only = horizontal right cap
				

			case 8+2+1:
			case 8+2+1+4:
				return 8; // Top left, top, and left = bottom right cap

			case 16+4+2:
			case 16+4+2+1:
				return 6; // Top right, top, and right = bottom left cap

			case 64+8+32:
			case 64+8+32+128:
				return 2; // Bottom left, bottom, and left = top right cap

			case 16+128+64:
			case 16+128+64+32:
				return 0; // Bottom right, right, bottom = top left cap

				// BELOW: Edges. Bottom, right, left, top.
			case 1+2+4+8+16:
			case 2+4+8+16:
			case 1+2+8+16:
			case 16+2+8:
				return 7;
			case 1+2+8+32+64:
			case 1+2+8+64:
			case 8+2+32+64:
			case 8+2+64:
				return 4;
			case 2+4+16+128+64:
			case 2+16+128+64:
			case 2+4+16+64:
			case 2+16+64:
				return 3;
			case 8+16+32+64+128:
			case 8+64+128+16:
			case 8+16+32+64:
			case 8+16+64:
				return 1;
			
				// Horizontal and vertical paths
			case 2+64:
			case 2+1+64:
			case 2+4+64:
			case 32+64+2:
			case 2+64+128:
				return 13;
			case 8+16:
			case 1+8+16:
			case 8+32+16:
			case 4+16+8:
			case 16+128+8:
				return 10;

				// Horizontal and vertical caps
			case 2:
				return 14;
			case 8:
				return 11;
			case 16:
				return 9;
			case 64: 
				return 12;

				// T-joints
			case 2+32+64+128:
			case 1+2+4+64:
				return 13;
			case 8+4+16+128:
			case 16+1+8+32:
				return 10;

				//Basic corners
			case 2+8:
				return 8;

			}

			// Catch-alls!

			// Top, bottom, left, right = use inner fill
			if (CheckMask(90, mask))
			{
				return 5;
			}

			// T-join edges with extra shit surrounding
			// Top edge
			if (CheckMask(8+16+64, mask))
			{
				return 1;
			}

			// Right edge
			if (CheckMask(2+8+64, mask))
			{
				return 4;
			}

			// Left edge
			if (CheckMask(2+16+64, mask))
			{
				return 3;
			}

			// Bottom edge
			if (CheckMask(8+2+16, mask))
			{
				return 7;
			}

			// Top left corner
			if (CheckMask(64+16, mask))
			{
				return 0;
			}

			// Top right corner
			if (CheckMask(8+64, mask))
			{
				return 2;
			}
				
			// Bottom left corner
			if (CheckMask(2+16, mask))
			{
				return 6;
			}
				
			// Bottom right corner
			if (CheckMask(2+8, mask))
			{
				return 8;
			}

			// Catch-all for left and right (low priority!)
			if (CheckMask(8+16, mask))
			{
				return 10;
			}

			if (CheckMask(2+64, mask))
			{
				return 13;
			}

			// Lowest priority = check for cardinals, ignore diags
			// (provide cardinal caps) (not baseball)
			if (CheckMask(2, mask))
			{
				return 14;
			}
			if (CheckMask(8, mask))
			{
				return 11;
			}
			if (CheckMask(16, mask))
			{
				return 9;
			}
			if (CheckMask(64, mask))
			{
				return 12;
			}

			// Default to standalone block for now
			return 15;

		}

		// Check if we need any inner corner overlays
		// Can have up to four
		private int[] GetOverlayIndex(byte mask)
		{
			int[] overlays = new int[4]{0, 0, 0, 0};

			if (CheckMask(2+8, mask) && Not(1, mask))
			{
				overlays [0] = 19;
			}

			if (CheckMask(2+16, mask) && Not(4, mask))
			{
				overlays [1] = 18;
			}

			if (CheckMask(16+64, mask) && Not(128, mask))
			{
				overlays [2] = 16;
			}

			if (CheckMask(8+64, mask) && Not(32, mask))
			{
				overlays [3] = 17;
			}

			return overlays;
		}

		private bool CheckMask(byte b, byte mask)
		{
			return (byte)((byte)b & mask) == b;
		}

		private bool Not(byte b, byte mask)
		{
			return (byte)((byte)b & mask) == 0;
		}


		#if UNITY_EDITOR
		[MenuItem("Assets/Create/Terrain Tile")]
		public static void CreateTerrainTile()
		{
			string path = EditorUtility.SaveFilePanelInProject("Save Terrain Tile", "New Terrain Tile", "asset", "Save Terrain Tile", "Assets");

			if (path == "")
				return;

			AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TerrainTile>(), path);
		}

		private static Tilemap OverlaysBR;
		private static Tilemap OverlaysBL;
		private static Tilemap OverlaysUL;
		private static Tilemap OverlaysUR;

		private static Tilemap[] OverlayLayers;
		private static Tilemap[] GetOverlayLayers()
		{
			if (OverlayLayers == null || OverlayLayers.Length == 0)
			{
				OverlaysBR = GameObject.Find ("Overlays_BR").GetComponent<Tilemap>();
				OverlaysBL = GameObject.Find ("Overlays_BL").GetComponent<Tilemap>();
				OverlaysUL = GameObject.Find ("Overlays_UL").GetComponent<Tilemap>();
				OverlaysUR = GameObject.Find ("Overlays_UR").GetComponent<Tilemap>();

				OverlayLayers = new Tilemap[4]{ OverlaysBR, OverlaysBL, OverlaysUR, OverlaysUL };
			}

			return OverlayLayers;
		}

		public void SetOverlaysForMask(Vector3Int location, byte mask)
		{
			int[] overlays = GetOverlayIndex ((byte)mask);

			for (int i=0;i<4;i++)
			{
				Tilemap Layer = GetOverlayLayers() [i];
				Tile tile = ScriptableObject.CreateInstance<Tile> ();
				tile.sprite = (overlays [i] == 0) ? null : tileSprites[overlays [i]];
				Layer.SetTile (location, tile);
			}
		}

		[MenuItem("Moustachevania/Clear Corner Overlays")]
		public static void ClearCornerOverlays()
		{
			foreach (Tilemap overlay in GetOverlayLayers())
			{

                BoundsInt bounds = overlay.cellBounds;
                for (int x = bounds.xMin; x <= bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y <= bounds.yMax; y++)
                    {
                        Vector3Int currentLocation = new Vector3Int(x, y, 0);
                        //TileBase tile = overlay.GetTile(currentLocation);
                        //if (tile == null || !(tile is TerrainTile)) continue;
                        //TerrainTile tt = (TerrainTile)tile;
                        //tt.sprite = null;
                        overlay.SetTile(currentLocation, null);
                    }
                }

                overlay.RefreshAllTiles();
			}
		}

		[MenuItem("Moustachevania/Add Corner Overlays")]
		public static void AddCornerOverlays()
		{
			Tilemap GroundLayer = GameObject.Find ("AGround Layer").GetComponent<Tilemap> ();
            BoundsInt bounds = GroundLayer.cellBounds;

            for (int x = bounds.xMin; x <= bounds.xMax; x++){
                for (int y = bounds.yMin; y <= bounds.yMax; y++){
                    Vector3Int currentLocation = new Vector3Int(x, y, 0);
                    TileBase tile = GroundLayer.GetTile(currentLocation);
                    if (tile == null || !(tile is TerrainTile)) continue;

                    byte mask = StaticGetTileMask(GroundLayer, currentLocation);
                    //Debug.Log(mask);

                    if (mask != 0){
                        TerrainTile tt = (TerrainTile)tile;
                        tt.SetOverlaysForMask(currentLocation, mask);
                    }
                }
            }
		}

		public static byte StaticGetTileMask(Tilemap tileMap, Vector3Int location)
		{
			int mask = StaticTileIsThisTile(tileMap, location + TopLeft) ? 1 : 0;
			mask += StaticTileIsThisTile(tileMap, location + Top) ? 2 : 0;
			mask += StaticTileIsThisTile(tileMap, location + TopRight) ? 4 : 0;

			mask += StaticTileIsThisTile(tileMap, location + Left) ? 8 : 0;
			mask += StaticTileIsThisTile(tileMap, location + Right) ? 16 : 0;

			mask += StaticTileIsThisTile(tileMap, location + BottomLeft) ? 32 : 0;
			mask += StaticTileIsThisTile(tileMap, location + Bottom) ? 64 : 0;
			mask += StaticTileIsThisTile(tileMap, location + BottomRight) ? 128 : 0;

            //Debug.Log(mask);

			return (byte)mask;
		}

		private static bool StaticTileIsThisTile(Tilemap tileMap, Vector3Int position)
		{
			TileBase tile = tileMap.GetTile(position);

			if (tile == null)
			{
				return false;
			}

			//Debug.Log (tile);

			if (tile != null && tile is TerrainTile)
			{
				return true;
			}

			Sprite s = tileMap.GetSprite (position);

			if (s == null)
			{
				return false;
			}

            if (!(tile is TerrainTile)){
                return false;
            }

			TerrainTile terrain = (TerrainTile)tile;

			// Next: Check if it's in a decoration list
			if (terrain.nonAutoInnerSprites.Contains(s))
			{
				//Debug.Log ("It worked? Or did it?");
				return true;
			}

			return false;
		}

		// Use for when we want an inner corner cap, check to see if the tile is empty before filling it.
		private static bool StaticTileIsEmpty(Tilemap Tilemap, Vector3Int position)
		{
			return Tilemap.GetTile (position) == null;
		}
			


		#endif
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(TerrainTile))]
	public class TerrainTileEditor : Editor
	{
		private TerrainTile tile { get { return (target as TerrainTile); } }

		SerializedProperty NonAutoInnerSprites;

		public void OnEnable()
		{
			if (tile.tileSprites == null || tile.tileSprites.Length != 20)
				tile.tileSprites = new Sprite[20];

			NonAutoInnerSprites = serializedObject.FindProperty ("nonAutoInnerSprites");
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.LabelField("96x96 Block - Corners, edges, and inner.");
			EditorGUILayout.Space();

			EditorGUI.BeginChangeCheck();
			tile.tileSprites[0] = (Sprite) EditorGUILayout.ObjectField("Upper Left", tile.tileSprites[0], typeof(Sprite), false, null);
			tile.tileSprites[1] = (Sprite) EditorGUILayout.ObjectField("Upper Edge", tile.tileSprites[1], typeof(Sprite), false, null);
			tile.tileSprites[2] = (Sprite) EditorGUILayout.ObjectField("Upper Right", tile.tileSprites[2], typeof(Sprite), false, null);
			tile.tileSprites[3] = (Sprite) EditorGUILayout.ObjectField("Left Edge", tile.tileSprites[3], typeof(Sprite), false, null);
			tile.tileSprites[4] = (Sprite) EditorGUILayout.ObjectField("Right Edge", tile.tileSprites[4], typeof(Sprite), false, null);
			tile.tileSprites[5] = (Sprite) EditorGUILayout.ObjectField("Inner", tile.tileSprites[5], typeof(Sprite), false, null);
			tile.tileSprites[6] = (Sprite) EditorGUILayout.ObjectField("Bottom Left", tile.tileSprites[6], typeof(Sprite), false, null);
			tile.tileSprites[7] = (Sprite) EditorGUILayout.ObjectField("Bottom Edge", tile.tileSprites[7], typeof(Sprite), false, null);
			tile.tileSprites[8] = (Sprite) EditorGUILayout.ObjectField("Bottom Right", tile.tileSprites[8], typeof(Sprite), false, null);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Single-width - Caps + straight segments");

			tile.tileSprites[9] = (Sprite) EditorGUILayout.ObjectField("Horizontal Left", tile.tileSprites[9], typeof(Sprite), false, null);
			tile.tileSprites[10] = (Sprite) EditorGUILayout.ObjectField("Horizontal Mid", tile.tileSprites[10], typeof(Sprite), false, null);
			tile.tileSprites[11] = (Sprite) EditorGUILayout.ObjectField("Horizontal Right", tile.tileSprites[11], typeof(Sprite), false, null);
			tile.tileSprites[12] = (Sprite) EditorGUILayout.ObjectField("Vertical Top", tile.tileSprites[12], typeof(Sprite), false, null);
			tile.tileSprites[13] = (Sprite) EditorGUILayout.ObjectField("Vertical Mid", tile.tileSprites[13], typeof(Sprite), false, null);
			tile.tileSprites[14] = (Sprite) EditorGUILayout.ObjectField("Vertical Bottom", tile.tileSprites[14], typeof(Sprite), false, null);


			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Standalone Block");

			tile.tileSprites[15] = (Sprite) EditorGUILayout.ObjectField("Standalone Block", tile.tileSprites[15], typeof(Sprite), false, null);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Inner caps for 90 degree turns");

			tile.tileSprites[16] = (Sprite) EditorGUILayout.ObjectField("Lower Right Cap", tile.tileSprites[16], typeof(Sprite), false, null);
			tile.tileSprites[17] = (Sprite) EditorGUILayout.ObjectField("Lower Left Cap", tile.tileSprites[17], typeof(Sprite), false, null);
			tile.tileSprites[18] = (Sprite) EditorGUILayout.ObjectField("Top Right Cap", tile.tileSprites[18], typeof(Sprite), false, null);
			tile.tileSprites[19] = (Sprite) EditorGUILayout.ObjectField("Top Left Cap", tile.tileSprites[19], typeof(Sprite), false, null);

			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(tile);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField ("Sprites that are considered 'inner' tileset for the purpose of auto-paint, but don't appear automatically when painting.");

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField (NonAutoInnerSprites, true);
			if (EditorGUI.EndChangeCheck ())
			{
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty (tile);
			}


		}
	}
	#endif
}