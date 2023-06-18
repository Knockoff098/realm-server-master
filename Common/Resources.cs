﻿using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RotMG.Game;

namespace RotMG.Common
{
    public static class Resources
    {
        public static Dictionary<ushort, ObjectDesc> Type2Object = new Dictionary<ushort, ObjectDesc>();
        public static Dictionary<string, ObjectDesc> Id2Object = new Dictionary<string, ObjectDesc>();
        public static Dictionary<string, ObjectDesc> IdLower2Object = new Dictionary<string, ObjectDesc>();

        public static Dictionary<ushort, PlayerDesc> Type2Player = new Dictionary<ushort, PlayerDesc>();
        public static Dictionary<string, PlayerDesc> Id2Player = new Dictionary<string, PlayerDesc>();

        public static Dictionary<ushort, SkinDesc> Type2Skin = new Dictionary<ushort, SkinDesc>();
        public static Dictionary<string, SkinDesc> Id2Skin = new Dictionary<string, SkinDesc>();

        public static Dictionary<ushort, TileDesc> Type2Tile = new Dictionary<ushort, TileDesc>();
        public static Dictionary<string, TileDesc> Id2Tile = new Dictionary<string, TileDesc>();

        public static Dictionary<ushort, ItemDesc> Type2Item = new Dictionary<ushort, ItemDesc>();
        public static Dictionary<string, ItemDesc> Id2Item = new Dictionary<string, ItemDesc>();
        public static Dictionary<string, ItemDesc> IdLower2Item = new Dictionary<string, ItemDesc>();

        public static Dictionary<string, WorldDesc> Worlds = new Dictionary<string, WorldDesc>();
        public static Dictionary<ushort, WorldDesc> PortalId2World = new Dictionary<ushort, WorldDesc>();
        public static Dictionary<string, Map> SetPieces = new Dictionary<string, Map>();

        public static Dictionary<ushort, QuestDesc> Quests = new Dictionary<ushort, QuestDesc>();

        public static Dictionary<string, byte[]> WebFiles = new Dictionary<string, byte[]>();


        public static List<XElement> News = new List<XElement>();

        public static string CombineResourcePath(string path)
        {
            return $"{Settings.ResourceDirectory}/{path}";
        }

        public static void Init()
        {
            LoadGameData();
            LoadQuests();
            LoadWorlds();
            LoadSetPieces();
            LoadWebFiles();
            LoadNews();
        }

        private static void LoadGameData()
        {
            var paths = Directory.EnumerateFiles(CombineResourcePath("GameData/"), "*.xml", SearchOption.TopDirectoryOnly).ToArray();
            for (var i = 0; i < paths.Length; i++)
            {

#if DEBUG
                Program.Print(PrintType.Debug, $"Parsing GameData <{paths[i].Split('/').Last()}>");
#endif
                var data = XElement.Parse(File.ReadAllText(paths[i]));

                foreach (var e in data.Elements("Object"))
                {
                    var id = e.ParseString("@id");
                    var type = e.ParseUshort("@type");
#if DEBUG
                    if (string.IsNullOrWhiteSpace(id))
                        throw new Exception("Invalid ID.");
                    if (Type2Object.ContainsKey(type) || Type2Item.ContainsKey(type))
                        throw new Exception($"Duplicate type <{id}:{type}>");
                    if (Id2Object.ContainsKey(id) || Id2Item.ContainsKey(id))
                        throw new Exception($"Duplicate ID <{id}:{type}>");
#endif

                    switch (e.ParseString("Class"))
                    {
                        case "Skin":
                            Id2Skin[id] = Type2Skin[type] = new SkinDesc(e, id, type);
                            break;
                        case "Player":
                            Id2Object[id] = IdLower2Object[id.ToLower()] = Type2Object[type] = Id2Player[id] = Type2Player[type] = new PlayerDesc(e, id, type);
                            break;
                        case "Equipment":
                        case "Dye":
                            Id2Item[id] = IdLower2Item[id.ToLower()] = Type2Item[type] = new ItemDesc(e, id, type);
                            break;
                        default:
                            Id2Object[id] = IdLower2Object[id.ToLower()] = Type2Object[type] = new ObjectDesc(e, id, type);
                            break;
                    }
                }

                foreach (var e in data.Elements("Ground"))
                {
                    var id = e.ParseString("@id");
                    var type = e.ParseUshort("@type"); 
#if DEBUG
                    if (string.IsNullOrWhiteSpace(id))
                        throw new Exception("Invalid ID.");
                    if (Type2Tile.ContainsKey(type))
                        throw new Exception($"Duplicate type <{id}:{type}>");
                    if (Id2Tile.ContainsKey(id)) 
                        throw new Exception($"Duplicate ID <{id}:{type}>");
#endif

                    Id2Tile[id] = Type2Tile[type] = new TileDesc(e, id, type);
                }
            }

#if DEBUG
            Program.Print(PrintType.Debug, $"Parsed <{Type2Object.Count}> Objects");
            Program.Print(PrintType.Debug, $"Parsed <{Type2Player.Count}> Player Classes");
            Program.Print(PrintType.Debug, $"Parsed <{Type2Skin.Count}> Skins");
            Program.Print(PrintType.Debug, $"Parsed <{Type2Item.Count}> Items");
            Program.Print(PrintType.Debug, $"Parsed <{Type2Tile.Count}> Tiles");
#endif
        }

        private static void LoadQuests()
        {
            foreach (var desc in Type2Object.Values)
            {
                if (!desc.Quest) continue;
                var priority = desc.Level;
                if (desc.Hero) priority += 1000;
                Quests[desc.Type] = new QuestDesc(desc.Level, priority);
            }
        }

        private static void LoadWorlds()
        {
            foreach (var e in XElement.Parse(File.ReadAllText(CombineResourcePath("Worlds/Worlds.xml"))).Elements("World"))
            {
#if DEBUG
                Program.Print(PrintType.Debug, $"Parsing World <{e.ParseString("@name")}>");
#endif
                var desc = new WorldDesc(e);
                Worlds[desc.Name] = desc;
                foreach (var portal in desc.Portals)
                    PortalId2World[portal] = desc;
            }
        }

        private static void LoadSetPieces()
        {
            foreach (var e in XElement.Parse(File.ReadAllText(CombineResourcePath("SetPieces/SetPieces.xml"))).Elements("SetPiece"))
            {
#if DEBUG
                Program.Print(PrintType.Debug, $"Parsing World <{e.ParseString("@id")}>");
#endif
                var name = e.ParseString("Map");
                Map map;
                if (name.EndsWith("wmap"))
                    map = new WMap(File.ReadAllBytes(CombineResourcePath($"SetPieces/{name}")));
                else
                    map = new JSMap(File.ReadAllText(CombineResourcePath($"SetPieces/{name}")));

                SetPieces[e.ParseString("@id")] = map;
            }
        }
        
        private static void LoadWebFiles()
        {
            var paths = Directory.EnumerateFiles(CombineResourcePath("Web/"), "*", SearchOption.AllDirectories).ToArray();
            for (var i = 0; i < paths.Length; i++)
            {
                var display = '/' + paths[i].Split('/')[2].Replace(@"\", "/");
#if DEBUG
                Program.Print(PrintType.Debug, $"Loading Web File <{display}>");
#endif
                WebFiles[display] = File.ReadAllBytes(paths[i]);
            }
        }

        private static void LoadNews()
        {
            News.Clear();
            var data = File.ReadAllText(CombineResourcePath("News.xml"));
            if (!string.IsNullOrWhiteSpace(data))
            {
                var news = XElement.Parse(data);
                foreach (var item in news.Elements("Item").OrderByDescending(k => k.ParseInt("Date", Database.UnixTime())))
                    News.Add(item);
            }
        }
    }
}
