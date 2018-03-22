﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using UnityEngine;

namespace ServerTools
{
    class VoteReward
    {
        public static bool IsEnabled = false, IsRunning = false, RandomListRunning = false, Reward_Entity = false,
            RewardOpen = true;
        public static int Reward_Count = 1, Delay_Between_Uses = 24, Entity_Id = 74, _counter = 0;
        public static string Your_Voting_Site = ("https://7daystodie-servers.com/server/12345"), API_Key = ("xxxxxxxx");
        private const string file = "VoteReward.xml";
        private static string filePath = string.Format("{0}/{1}", API.ConfigPath, file);
        private static Dictionary<string, int[]> dict = new Dictionary<string, int[]>();
        private static List<string> list = new List<string>();
        private static FileSystemWatcher fileWatcher = new FileSystemWatcher(API.ConfigPath, file);
        private static bool updateConfig = false, posFound = false;
        public static System.Random rnd = new System.Random();

        public static void Load()
        {
            if (IsEnabled && !IsRunning)
            {
                LoadXml();
                InitFileWatcher();
            }
        }

        public static void Unload()
        {
            fileWatcher.Dispose();
            IsRunning = false;
        }

        public static void LoadXml()
        {
            if (!Utils.FileExists(filePath))
            {
                UpdateXml();
            }
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(filePath);
            }
            catch (XmlException e)
            {
                Log.Error(string.Format("[SERVERTOOLS] Failed loading {0}: {1}", file, e.Message));
                return;
            }            
            XmlNode _XmlNode = xmlDoc.DocumentElement;
            foreach (XmlNode childNode in _XmlNode.ChildNodes)
            {
                if (childNode.Name == "Rewards")
                {
                    dict.Clear();
                    list.Clear();
                    foreach (XmlNode subChild in childNode.ChildNodes)
                    {
                        if (subChild.NodeType == XmlNodeType.Comment)
                        {
                            continue;
                        }
                        if (subChild.NodeType != XmlNodeType.Element)
                        {
                            Log.Warning(string.Format("[SERVERTOOLS] Unexpected XML node found in 'Rewards' section: {0}", subChild.OuterXml));
                            continue;
                        }
                        XmlElement _line = (XmlElement)subChild;
                        if (!_line.HasAttribute("itemOrBlock"))
                        {
                            Log.Warning(string.Format("[SERVERTOOLS] Ignoring reward entry because of missing itemOrBlock attribute: {0}", subChild.OuterXml));
                            continue;
                        }
                        if (!_line.HasAttribute("countMin"))
                        {
                            Log.Warning(string.Format("[SERVERTOOLS] Ignoring reward entry because of missing countMin attribute: {0}", subChild.OuterXml));
                            continue;
                        }
                        if (!_line.HasAttribute("countMax"))
                        {
                            Log.Warning(string.Format("[SERVERTOOLS] Ignoring reward entry because of missing countMax attribute: {0}", subChild.OuterXml));
                            continue;
                        }
                        if (!_line.HasAttribute("qualityMin"))
                        {
                            Log.Warning(string.Format("[SERVERTOOLS] Ignoring reward entry because of missing qualityMin attribute: {0}", subChild.OuterXml));
                            continue;
                        }
                        if (!_line.HasAttribute("qualityMax"))
                        {
                            Log.Warning(string.Format("[SERVERTOOLS] Ignoring reward entry because of missing qualityMax attribute: {0}", subChild.OuterXml));
                            continue;
                        }
                        int _countMin = 1;
                        int _countMax = 1;
                        int _qualityMin = 1;
                        int _qualityMax = 1;
                        if (!int.TryParse(_line.GetAttribute("countMin"), out _countMin))
                        {
                            Log.Out(string.Format("[SERVERTOOLS] Ignoring reward entry because of invalid (non-numeric) value for 'count' attribute: {0}", subChild.OuterXml));
                            continue;
                        }
                        if (!int.TryParse(_line.GetAttribute("countMax"), out _countMax))
                        {
                            Log.Out(string.Format("[SERVERTOOLS] Ignoring reward entry because of invalid (non-numeric) value for 'count' attribute: {0}", subChild.OuterXml));
                            continue;
                        }
                        if (!int.TryParse(_line.GetAttribute("qualityMin"), out _qualityMin))
                        {
                            Log.Out(string.Format("[SERVERTOOLS] Ignoring reward entry because of invalid (non-numeric) value for 'quality' attribute: {0}", subChild.OuterXml));
                            continue;
                        }
                        if (!int.TryParse(_line.GetAttribute("qualityMax"), out _qualityMax))
                        {
                            Log.Out(string.Format("[SERVERTOOLS] Ignoring reward entry because of invalid (non-numeric) value for 'quality' attribute: {0}", subChild.OuterXml));
                            continue;
                        }
                        string item = _line.GetAttribute("itemOrBlock");
                        if (!dict.ContainsKey(item))
                        {
                            int[] _c = new int[] { _countMin, _countMax, _qualityMin, _qualityMax };
                            dict.Add(item, _c);
                        }                                                                      
                    }
                }
            }
            list = new List<string>(dict.Keys);
            if (updateConfig)
            {
                updateConfig = false;
                UpdateXml();
            }
        }

        private static void UpdateXml()
        {
            fileWatcher.EnableRaisingEvents = false;
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sw.WriteLine("<RewardItems>");
                sw.WriteLine("    <Rewards>");
                if (dict.Count > 0)
                {
                    foreach (KeyValuePair<string, int[]> kvp in dict)
                    {
                        sw.WriteLine(string.Format("        <reward itemOrBlock=\"{0}\" countMin=\"{1}\" countMax=\"{2}\" qualityMin=\"{3}\" qualityMax=\"{4}\" />", kvp.Key, kvp.Value[0], kvp.Value[1], kvp.Value[2], kvp.Value[3]));
                    }
                }
                else
                {
                    sw.WriteLine("        <reward itemOrBlock=\"torch\" countMin=\"5\" countMax=\"10\" qualityMin=\"1\" qualityMax=\"1\" />");
                    sw.WriteLine("        <reward itemOrBlock=\"9mmBullet\" countMin=\"10\" countMax=\"30\" qualityMin=\"1\" qualityMax=\"1\" />");
                    sw.WriteLine("        <reward itemOrBlock=\"44MagBullet\" countMin=\"5\" countMax=\"10\" qualityMin=\"1\" qualityMax=\"1\" />");
                    sw.WriteLine("        <reward itemOrBlock=\"ironChestArmor\" countMin=\"1\" countMax=\"1\" qualityMin=\"1\" qualityMax=\"600\" />");
                    sw.WriteLine("        <reward itemOrBlock=\"plantedCorn1\" countMin=\"5\" countMax=\"10\" qualityMin=\"1\" qualityMax=\"1\" />");
                    sw.WriteLine("        <reward itemOrBlock=\"sand\" countMin=\"5\" countMax=\"10\" qualityMin=\"1\" qualityMax=\"1\" />");
                    sw.WriteLine("        <reward itemOrBlock=\"snow\" countMin=\"2\" countMax=\"10\" qualityMin=\"1\" qualityMax=\"1\" />");
                }
                sw.WriteLine("    </Rewards>");
                sw.WriteLine("</RewardItems>");
                sw.Flush();
                sw.Close();
            }
            fileWatcher.EnableRaisingEvents = true;
        }

        private static void InitFileWatcher()
        {
            fileWatcher.Changed += new FileSystemEventHandler(OnFileChanged);
            fileWatcher.Created += new FileSystemEventHandler(OnFileChanged);
            fileWatcher.Deleted += new FileSystemEventHandler(OnFileChanged);
            fileWatcher.EnableRaisingEvents = true;
            IsRunning = true;
        }

        private static void OnFileChanged(object source, FileSystemEventArgs e)
        {
            if (!Utils.FileExists(filePath))
            {
                UpdateXml();
            }
            LoadXml();
        }

        public static void CheckReward(ClientInfo _cInfo)
        {
            if (Delay_Between_Uses == 0)
            {
                Execute(_cInfo);
            }
            else
            {
                Player p = PersistentContainer.Instance.Players[_cInfo.playerId, false];
                if (p == null || p.LastVoteReward == null)
                {
                    Execute(_cInfo);
                }
                else
                {
                    TimeSpan varTime = DateTime.Now - p.LastVoteReward;
                    double fractionalHours = varTime.TotalHours;
                    int _timepassed = (int)fractionalHours;
                    if (_timepassed >= Delay_Between_Uses)
                    {
                        Execute(_cInfo);
                    }
                    else
                    {
                        int _timeleft = Delay_Between_Uses - _timepassed;
                        string _phrase602;
                        if (!Phrases.Dict.TryGetValue(602, out _phrase602))
                        {
                            _phrase602 = "{PlayerName} you can only use /reward once every {DelayBetweenRewards} hours. Time remaining: {TimeRemaining} hour(s).";
                        }
                        string cinfoName = _cInfo.playerName;
                        _phrase602 = _phrase602.Replace("{PlayerName}", cinfoName);
                        _phrase602 = _phrase602.Replace("{DelayBetweenRewards}", Delay_Between_Uses.ToString());
                        _phrase602 = _phrase602.Replace("{TimeRemaining}", _timeleft.ToString());
                        {
                            _cInfo.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, string.Format("{0}{1}[-]", Config.Chat_Response_Color, _phrase602), "Server", false, "", false));
                        }
                    }
                }
            }
        }

        private static void Execute(ClientInfo _cInfo)
        {
            if (RewardOpen)
            {
                RewardOpen = false;
                ServicePointManager.ServerCertificateValidationCallback += (send, certificate, chain, sslPolicyErrors) => { return true; };
                var VoteUrl = string.Format("https://7daystodie-servers.com/api/?object=votes&element=claim&key={0}&username={1}", Uri.EscapeUriString(API_Key), Uri.EscapeUriString(_cInfo.playerName));
                using (var NewVote = new WebClient())
                {
                    var VoteResult = string.Empty;
                    VoteResult = NewVote.DownloadString(VoteUrl);
                    if (VoteResult == "0")
                    {
                        RewardOpen = true;
                        string _phrase700;
                        if (!Phrases.Dict.TryGetValue(700, out _phrase700))
                        {
                            _phrase700 = "Your vote has not been located {PlayerName}. Make sure you voted @ {VoteSite} and try again.";
                        }
                        _phrase700 = _phrase700.Replace("{PlayerName}", _cInfo.playerName);
                        _phrase700 = _phrase700.Replace("{VoteSite}", Your_Voting_Site);
                        _cInfo.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, string.Format("{0}{1}[-]", Config.Chat_Response_Color, _phrase700), "Server", false, "", false));
                    }
                    if (VoteResult == "1")
                    {                        
                        string _phrase701;
                        if (!Phrases.Dict.TryGetValue(701, out _phrase701))
                        {
                            _phrase701 = "Thank you for your vote {PlayerName}. You can vote and receive another reward in {VoteDelay} hours.";
                        }
                        _phrase701 = _phrase701.Replace("{PlayerName}", _cInfo.playerName);
                        _phrase701 = _phrase701.Replace("{VoteDelay}", Delay_Between_Uses.ToString());
                        _cInfo.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, string.Format("{0}{1}[-]", Config.Chat_Response_Color, _phrase701), "Server", false, "", false));
                        if (!Reward_Entity)
                        {
                            if (dict.Count > 0 && Reward_Count > 0)
                            {
                                ItemOrBlock(_cInfo);
                            }
                        }
                        else
                        {
                            Entity(_cInfo);
                        }
                    }
                }
            }
            else
            {
                _cInfo.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, string.Format("{0}Another player is receiving their reward. Please try again.[-]", Config.Chat_Response_Color), "Server", false, "", false));
            }
        }

        private static void ItemOrBlock(ClientInfo _cInfo)
        {
            if (Reward_Count > dict.Count)
            {
                Reward_Count = dict.Count;
            }
            string _item = list.RandomObject();
            int[] _values;
            if (dict.TryGetValue(_item, out _values))
            {
                int count = 0;
                if (_values[0] != _values[1])
                {
                    count = rnd.Next(_values[0], _values[1] + 1);
                }
                else
                {
                    count = _values[0];
                }
                if (count > 0)
                {
                    int quality = rnd.Next(_values[2], _values[3] + 1);
                    if (quality < 1 || quality > 600)
                    {
                        quality = rnd.Next(1, 601);
                    }
                    ItemValue itemValue;
                    var itemId = 4096;
                    int _itemId;
                    if (int.TryParse(_item, out _itemId))
                    {
                        int calc = (_itemId + 4096);
                        itemId = calc;
                        itemValue = ItemClass.list[itemId] == null ? ItemValue.None : new ItemValue(itemId, quality, quality, true);
                    }
                    else
                    {
                        ItemValue _itemValue = ItemClass.GetItem(_item, true);
                        if (_itemValue.type == ItemValue.None.type)
                        {
                            Log.Warning(string.Format("[SERVERTOOLS] Item or block not found: {0}. Item or block was not given as a reward.", _item));
                            list.Remove(_item);
                            ItemOrBlock(_cInfo);
                            return;
                        }
                        else
                        {
                            itemValue = new ItemValue(ItemClass.GetItem(_item).type, quality, quality, true);
                        }
                    }
                    World world = GameManager.Instance.World;
                    if (world.Players.dict[_cInfo.entityId].IsSpawned())
                    {
                        var entityItem = (EntityItem)EntityFactory.CreateEntity(new EntityCreationData
                        {
                            entityClass = EntityClass.FromString("item"),
                            id = EntityFactory.nextEntityID++,
                            itemStack = new ItemStack(itemValue, count),
                            pos = world.Players.dict[_cInfo.entityId].position,
                            rot = new Vector3(20f, 0f, 20f),
                            lifetime = 60f,
                            belongsPlayerId = _cInfo.entityId
                        });
                        world.SpawnEntityInWorld(entityItem);
                        _cInfo.SendPackage(new NetPackageEntityCollect(entityItem.entityId, _cInfo.entityId));
                        world.RemoveEntity(entityItem.entityId, EnumRemoveEntityReason.Killed);
                        _counter++;
                    }
                    if (_counter != Reward_Count)
                    {
                        ItemOrBlock(_cInfo);
                    }
                    else
                    {
                        list.Clear();
                        list = new List<string>(dict.Keys);
                        _cInfo.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, string.Format("{0}Reward items sent to your inventory. If it is full, check the ground.[-]", Config.Chat_Response_Color), "Server", false, "", false));
                        PersistentContainer.Instance.Players[_cInfo.playerId, true].LastVoteReward = DateTime.Now;
                        PersistentContainer.Instance.Save();
                        _counter = 0;
                        RewardOpen = true;
                    }
                }
                else
                {
                    list.Remove(_item);
                    ItemOrBlock(_cInfo);
                }
            }
        }

        private static void Entity(ClientInfo _cInfo)
        {
            EntityPlayer _player = GameManager.Instance.World.Players.dict[_cInfo.entityId];
            Vector3 pos = _player.GetPosition();
            float x = pos.x;
            float y = pos.y;
            float z = pos.z;
            int _x, _y, _z;
            posFound = true;
            posFound = GameManager.Instance.World.FindRandomSpawnPointNearPosition(new Vector3((float)x, (float)y, (float)z), 15, out _x, out _y, out _z, new Vector3((float)5, (float)5, (float)5), true);
            if (!posFound)
            {
                posFound = GameManager.Instance.World.FindRandomSpawnPointNearPosition(new Vector3((float)x, (float)y, (float)z), 15, out _x, out _y, out _z, new Vector3((float)5 + 5, (float)5 + 10, (float)5 + 5), true);
            }
            if (posFound)
            {
                EntityClass eClass = EntityClass.list[Entity_Id];
                if (eClass.bAllowUserInstantiate)
                {
                    RewardOpen = true;
                    Entity entity = EntityFactory.CreateEntity(Entity_Id, new Vector3((float)_x, (float)_y, (float)_z));
                    GameManager.Instance.World.SpawnEntityInWorld(entity);
                    Log.Out(string.Format("[SERVERTOOLS] Spawned an entity reward {0} at {1} x, {2} y, {3} z for {4}", eClass.entityClassName, _x, _y, _z, _cInfo.playerName));
                }
            }
            else
            {
                RewardOpen = true;
                _cInfo.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, string.Format("{0}No spawn point was found near you. Please move locations and try again.[-]", Config.Chat_Response_Color), "Server", false, "", false));
            }
        }
    }
}

