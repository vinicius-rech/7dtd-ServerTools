﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;

namespace ServerTools
{
    class BloodmoonWarrior
    {
        public static bool IsEnabled = false, IsRunning = false, BloodmoonStarted = false, Reduce_Death_Count = false;
        public static int Zombie_Kills = 10, Chance = 50, Reward_Count = 1;
        public static List<string> WarriorList = new List<string>();
        public static Dictionary<string, int> KilledZombies = new Dictionary<string, int>();

        private const string file = "BloodmoonWarrior.xml";
        private static readonly string FilePath = string.Format("{0}/{1}", API.ConfigPath, file);
        private static readonly Dictionary<string, string[]> Dict = new Dictionary<string, string[]>();
        private static readonly System.Random Random = new System.Random();
        private static FileSystemWatcher FileWatcher = new FileSystemWatcher(API.ConfigPath, file);

        private static List<string> List
        {
            get { return new List<string>(Dict.Keys); }
        }

        public static void Load()
        {
            LoadXml();
            InitFileWatcher();
        }

        public static void Unload()
        {
            Dict.Clear();
            FileWatcher.Dispose();
            IsRunning = false;
        }

        public static void LoadXml()
        {
            try
            {
                if (!Utils.FileExists(FilePath))
                {
                    UpdateXml();
                }
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.Load(FilePath);
                }
                catch (XmlException e)
                {
                    Log.Error(string.Format("[SERVERTOOLS] Failed loading {0}: {1}", file, e.Message));
                    return;
                }
                bool upgrade = true;
                XmlNodeList childNodes = xmlDoc.DocumentElement.ChildNodes;
                if (childNodes != null)
                {
                    Dict.Clear();
                    for (int i = 0; i < childNodes.Count; i++)
                    {
                        if (childNodes[i].NodeType != XmlNodeType.Comment)
                        {
                            XmlElement line = (XmlElement)childNodes[i];
                            if (line.HasAttributes)
                            {
                                if (line.HasAttribute("Version") && line.GetAttribute("Version") == Config.Version)
                                {
                                    upgrade = false;
                                    continue;
                                }
                                else if (line.HasAttribute("Name") && line.HasAttribute("SecondaryName") && line.HasAttribute("MinCount") && line.HasAttribute("MaxCount") &&
                                    line.HasAttribute("MinQuality") && line.HasAttribute("MaxQuality"))
                                {
                                    if (!int.TryParse(line.GetAttribute("MinCount"), out int minCount))
                                    {
                                        Log.Out(string.Format("[SERVERTOOLS] Ignoring BloodmoonWarrior.xml entry because of invalid (non-numeric) value for 'MinCount' attribute: {0}", line.OuterXml));
                                        continue;
                                    }
                                    if (!int.TryParse(line.GetAttribute("MaxCount"), out int maxCount))
                                    {
                                        Log.Out(string.Format("[SERVERTOOLS] Ignoring BloodmoonWarrior.xml entry because of invalid (non-numeric) value for 'MaxCount' attribute: {0}", line.OuterXml));
                                        continue;
                                    }
                                    if (!int.TryParse(line.GetAttribute("MinQuality"), out int minQuality))
                                    {
                                        Log.Out(string.Format("[SERVERTOOLS] Ignoring BloodmoonWarrior.xml entry because of invalid (non-numeric) value for 'MinQuality' attribute: {0}", line.OuterXml));
                                        continue;
                                    }
                                    if (!int.TryParse(line.GetAttribute("MaxQuality"), out int maxQuality))
                                    {
                                        Log.Out(string.Format("[SERVERTOOLS] Ignoring BloodmoonWarrior.xml entry because of invalid (non-numeric) value for 'MaxQuality' attribute: {0}", line.OuterXml));
                                        continue;
                                    }
                                    string name = line.GetAttribute("Name");
                                    ItemClass _class = ItemClass.GetItemClass(name, true);
                                    Block _block = Block.GetBlockByName(name, true);
                                    if (_class == null && _block == null)
                                    {
                                        Log.Out(string.Format("[SERVERTOOLS] Ignoring BloodmoonWarrior.xml entry. Item name not found: {0}", name));
                                        continue;
                                    }
                                    string secondaryname;
                                    if (line.HasAttribute("SecondaryName"))
                                    {
                                        secondaryname = line.GetAttribute("SecondaryName");
                                    }
                                    else
                                    {
                                        secondaryname = name;
                                    }
                                    if (!Dict.ContainsKey(name))
                                    {
                                        string[] c = new string[] { secondaryname, minCount.ToString(), maxCount.ToString(), minQuality.ToString(), maxQuality.ToString() };
                                        Dict.Add(name, c);
                                    }
                                }
                            }
                        }
                    }
                }
                if (upgrade)
                {
                    XmlNodeList nodeList = xmlDoc.DocumentElement.ChildNodes;
                    XmlNode node = nodeList[0];
                    XmlElement line = (XmlElement)nodeList[0];
                    if (line != null)
                    {
                        if (line.HasAttributes)
                        {
                            UpgradeXml(nodeList);
                            return;
                        }
                        else
                        {
                            nodeList = node.ChildNodes;
                            line = (XmlElement)nodeList[0];
                            if (line != null)
                            {
                                if (line.HasAttributes)
                                {
                                    UpgradeXml(nodeList);
                                    return;
                                }
                            }
                        }
                    }
                    UpgradeXml(null);
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in BloodmoonWarrior.LoadXml: {0}", e.Message));
            }
        }

        private static void UpdateXml()
        {
            try
            {
                FileWatcher.EnableRaisingEvents = false;
                using (StreamWriter sw = new StreamWriter(FilePath, false, Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    sw.WriteLine("<BloodmoonWarrior>");
                    sw.WriteLine(string.Format("<ST Version=\"{0}\" />", Config.Version));
                    sw.WriteLine("    <!-- <Item Name=\"gunPistolExample\" SecondaryName=\"pistol\" MinCount=\"1\" MaxCount=\"1\" MinQuality=\"3\" MaxQuality=\"3\" /> -->");
                    sw.WriteLine();
                    sw.WriteLine();
                    if (Dict.Count > 0)
                    {
                        foreach (KeyValuePair<string, string[]> kvp in Dict)
                        {
                            sw.WriteLine(string.Format("    <Item Name=\"{0}\" SecondaryName=\"{1}\" MinCount=\"{2}\" MaxCount=\"{3}\" MinQuality=\"{4}\" MaxQuality=\"{5}\" />", kvp.Key, kvp.Value[0], kvp.Value[1], kvp.Value[2], kvp.Value[3], kvp.Value[4]));
                        }
                    }
                    sw.WriteLine("</BloodmoonWarrior>");
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in BloodmoonWarrior.UpdateXml: {0}", e.Message));
            }
            FileWatcher.EnableRaisingEvents = true;
        }

        private static void InitFileWatcher()
        {
            FileWatcher.Changed += new FileSystemEventHandler(OnFileChanged);
            FileWatcher.Created += new FileSystemEventHandler(OnFileChanged);
            FileWatcher.Deleted += new FileSystemEventHandler(OnFileChanged);
            FileWatcher.EnableRaisingEvents = true;
            IsRunning = true;
        }

        private static void OnFileChanged(object source, FileSystemEventArgs e)
        {
            if (!Utils.FileExists(FilePath))
            {
                UpdateXml();
            }
            LoadXml();
        }

        public static void Exec()
        {
            try
            {
                if (!BloodmoonStarted)
                {
                    if (PersistentOperations.IsBloodmoon())
                    {
                        BloodmoonStarted = true;
                        List<ClientInfo> _cInfoList = PersistentOperations.ClientList();
                        if (_cInfoList != null)
                        {
                            for (int i = 0; i < _cInfoList.Count; i++)
                            {
                                ClientInfo _cInfo = _cInfoList[i];
                                if (_cInfo != null)
                                {
                                    if (GameManager.Instance.World.Players.dict.ContainsKey(_cInfo.entityId))
                                    {
                                        EntityPlayer _player = PersistentOperations.GetEntityPlayer(_cInfo.playerId);
                                        if (_player != null && _player.IsSpawned() && _player.IsAlive() && _player.Died > 0 && _player.Progression.GetLevel() >= 10 && Random.Next(0, 100) < Chance)
                                        {
                                            WarriorList.Add(_cInfo.playerId);
                                            KilledZombies.Add(_cInfo.playerId, 0);
                                            Phrases.Dict.TryGetValue("BloodmoonWarrior1", out string _phrase);
                                            _phrase = _phrase.Replace("{Count}", Zombie_Kills.ToString());
                                            ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + _phrase + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (!PersistentOperations.IsBloodmoon())
                {
                    BloodmoonStarted = false;
                    RewardWarriors();
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in BloodmoonWarrior.Exec: {0}", e.Message));
            }
        }

        public static void RewardWarriors()
        {
            try
            {
                List<string> _warriors = WarriorList;
                for (int i = 0; i < _warriors.Count; i++)
                {
                    string _warrior = _warriors[i];
                    EntityPlayer _player = PersistentOperations.GetEntityPlayer(_warrior);
                    if (_player != null && _player.IsAlive())
                    {
                        if (KilledZombies.TryGetValue(_warrior, out int _killedZ))
                        {
                            if (_killedZ >= Zombie_Kills)
                            {
                                ClientInfo _cInfo = PersistentOperations.GetClientInfoFromSteamId(_warrior);
                                if (_cInfo != null)
                                {
                                    Counter(_cInfo, Reward_Count);
                                    if (Reduce_Death_Count)
                                    {
                                        int _deathCount = _player.Died - 1;
                                        _player.Died = _deathCount;
                                        _player.bPlayerStatsChanged = true;
                                        _cInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerStats>().Setup(_player));
                                        Phrases.Dict.TryGetValue("BloodmoonWarrior2", out string _phrase);
                                        ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + _phrase + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                                    }
                                    else
                                    {
                                        Phrases.Dict.TryGetValue("BloodmoonWarrior3", out string _phrase);
                                        ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + _phrase + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in BloodmoonWarrior.RewardWarriors: {0}", e.Message));
            }
            WarriorList.Clear();
            KilledZombies.Clear();
        }

        private static void Counter(ClientInfo _cInfo, int _counter)
        {
            RandomItem(_cInfo);
            _counter--;
            if (_counter != 0)
            {
                RandomItem(_cInfo);
            }
        }

        private static void RandomItem(ClientInfo _cInfo)
        {
            try
            {
                string _randomItem = List.RandomObject();
                if (Dict.TryGetValue(_randomItem, out string[]  _item))
                {
                    int _minCount = int.Parse(_item[1]);
                    int _maxCount = int.Parse(_item[2]);
                    int _minQuality = int.Parse(_item[3]);
                    int _maxQuality = int.Parse(_item[4]);
                    int _count = Random.Next(_minCount, _maxCount + 1);
                    int _quality = Random.Next(_minCount, _maxCount + 1);
                    ItemValue _itemValue = new ItemValue(ItemClass.GetItem(_randomItem, false).type, _quality, _quality, false, null);
                    World world = GameManager.Instance.World;
                    var entityItem = (EntityItem)EntityFactory.CreateEntity(new EntityCreationData
                    {
                        entityClass = EntityClass.FromString("item"),
                        id = EntityFactory.nextEntityID++,
                        itemStack = new ItemStack(_itemValue, _count),
                        pos = world.Players.dict[_cInfo.entityId].position,
                        rot = new Vector3(20f, 0f, 20f),
                        lifetime = 60f,
                        belongsPlayerId = _cInfo.entityId
                    });
                    world.SpawnEntityInWorld(entityItem);
                    _cInfo.SendPackage(NetPackageManager.GetPackage<NetPackageEntityCollect>().Setup(entityItem.entityId, _cInfo.entityId));
                    world.RemoveEntity(entityItem.entityId, EnumRemoveEntityReason.Despawned);
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in BloodmoonWarrior.RandomItem: {0}", e.Message));
            }
        }

        private static void UpgradeXml(XmlNodeList _oldChildNodes)
        {
            try
            {
                FileWatcher.EnableRaisingEvents = false;
                using (StreamWriter sw = new StreamWriter(FilePath, false, Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    sw.WriteLine("<BloodmoonWarrior>");
                    sw.WriteLine(string.Format("<ST Version=\"{0}\" />", Config.Version));
                    sw.WriteLine("<!-- <Item Name=\"gunPistolExample\" SecondaryName=\"pistol\" MinCount=\"1\" MaxCount=\"1\" MinQuality=\"3\" MaxQuality=\"3\" /> -->");
                    for (int i = 0; i < _oldChildNodes.Count; i++)
                    {
                        if (_oldChildNodes[i].NodeType == XmlNodeType.Comment && !_oldChildNodes[i].OuterXml.StartsWith("<!-- <Item Name=\"gunPistolExample\"") && 
                            !_oldChildNodes[i].OuterXml.StartsWith("<!-- <Item Name=\"\""))
                        {
                            sw.WriteLine(_oldChildNodes[i].OuterXml);
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine();
                    for (int i = 0; i < _oldChildNodes.Count; i++)
                    {
                        if (_oldChildNodes[i].NodeType != XmlNodeType.Comment)
                        {
                            XmlElement line = (XmlElement)_oldChildNodes[i];
                            if (line.HasAttributes && line.Name == "Item")
                            {
                                string name = "", secondaryName = "", minCount = "", maxCount = "", minQuality = "", maxQuality = "";
                                if (line.HasAttribute("Name"))
                                {
                                    name = line.GetAttribute("Name");
                                }
                                if (line.HasAttribute("SecondaryName"))
                                {
                                    secondaryName = line.GetAttribute("SecondaryName");
                                }
                                if (line.HasAttribute("MinCount"))
                                {
                                    minCount = line.GetAttribute("MinCount");
                                }
                                if (line.HasAttribute("MaxCount"))
                                {
                                    maxCount = line.GetAttribute("MaxCount");
                                }
                                if (line.HasAttribute("MinQuality"))
                                {
                                    minQuality = line.GetAttribute("MinQuality");
                                }
                                if (line.HasAttribute("MaxQuality"))
                                {
                                    maxQuality = line.GetAttribute("MaxQuality");
                                }
                                sw.WriteLine(string.Format("    <Item Name=\"{0}\" SecondaryName=\"{1}\" MinCount=\"{2}\" MaxCount=\"{3}\" MinQuality=\"{4}\" MaxQuality=\"{5}\" />", name, secondaryName, minCount, maxCount, minQuality, maxQuality));
                            }
                        }
                    }
                    sw.WriteLine("</BloodmoonWarrior>");
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in BloodmoonWarrior.UpgradeXml: {0}", e.Message));
            }
            FileWatcher.EnableRaisingEvents = true;
            LoadXml();
        }
    }
}
