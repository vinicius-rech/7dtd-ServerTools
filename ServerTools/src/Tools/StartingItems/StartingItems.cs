﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using UnityEngine;

namespace ServerTools
{
    class StartingItems
    {
        public static bool IsEnabled = false, IsRunning = false;
        public static Dictionary<string, int[]> Dict = new Dictionary<string, int[]>();

        private const string file = "StartingItems.xml";
        private static readonly string FilePath = string.Format("{0}/{1}", API.ConfigPath, file);
        private static FileSystemWatcher FileWatcher = new FileSystemWatcher(API.ConfigPath, file);

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
                                else if (line.HasAttribute("Name") && line.HasAttribute("Count") && line.HasAttribute("Quality"))
                                {
                                    if (!int.TryParse(line.GetAttribute("Count"), out int count))
                                    {
                                        Log.Out(string.Format("[SERVERTOOLS] Ignoring StartingItems.xml entry. Invalid (non-numeric) value for 'Count' attribute: {0}", line.OuterXml));
                                        continue;
                                    }
                                    if (!int.TryParse(line.GetAttribute("Quality"), out int quality))
                                    {
                                        Log.Out(string.Format("[SERVERTOOLS] Ignoring StartingItems.xml entry. Invalid (non-numeric) value for 'Quality' attribute: {0}", line.OuterXml));
                                        continue;
                                    }
                                    string item = line.GetAttribute("Name");
                                    if (item == "WalletCoin" || item == "walletCoin" || item == "walletcoin")
                                    {
                                        if (Wallet.IsEnabled)
                                        {
                                            if (count < 1)
                                            {
                                                count = 1;
                                            }
                                        }
                                        else
                                        {
                                            Log.Out(string.Format("[SERVERTOOLS] Ignoring StartingItems.xml entry. Wallet tool is not enabled: {0}", line.OuterXml));
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        ItemValue itemValue = ItemClass.GetItem(item, false);
                                        if (itemValue.type == ItemValue.None.type)
                                        {
                                            Log.Out(string.Format("[SERVERTOOLS] Ignoring StartingItems.xml entry. Item not found: {0}", item));
                                            continue;
                                        }
                                        if (count > itemValue.ItemClass.Stacknumber.Value)
                                        {
                                            count = itemValue.ItemClass.Stacknumber.Value;
                                            Log.Out(string.Format("[SERVERTOOLS] StartingItems.xml entry {0} was set above the max stack value. It has been reduced to the maximum of {1}", item, count));
                                        }
                                        if (Dict.ContainsKey(item))
                                        {
                                            Log.Out(string.Format("[SERVERTOOLS] StartingItems.xml entry {0} has a duplicate entry", item));
                                        }
                                        if (count > itemValue.ItemClass.Stacknumber.Value)
                                        {
                                            count = itemValue.ItemClass.Stacknumber.Value;
                                        }
                                        else if (count < 1)
                                        {
                                            count = 1;
                                        }
                                    }
                                    if (quality < 1)
                                    {
                                        quality = 1;
                                    }
                                    int[] c = new int[] { count, quality };
                                    if (!Dict.ContainsKey(item))
                                    {
                                        Dict.Add(item, c);
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
                Log.Out(string.Format("[SERVERTOOLS] Error in StartingItems.LoadXml: {0}", e.Message));
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
                    sw.WriteLine("<StartingItems>");
                    sw.WriteLine(string.Format("<ST Version=\"{0}\" />", Config.Version));
                    sw.WriteLine("<!-- Use WalletCoin for the Name if you want to give currency to their wallet -->");
                    sw.WriteLine("<!-- <Item Name=\"foodCanChili\" Count=\"1\" Quality=\"1\" /> -->");
                    sw.WriteLine();
                    sw.WriteLine();
                    if (Dict.Count > 0)
                    {
                        foreach (KeyValuePair<string, int[]> kvp in Dict)
                        {
                            sw.WriteLine(string.Format("    <Item Name=\"{0}\" Count=\"{1}\" Quality=\"{2}\" />", kvp.Key, kvp.Value[0], kvp.Value[1]));
                        }
                    }
                    else
                    {
                        sw.WriteLine("    <!-- <Item Name=\"\" Count=\"\" Quality=\"\" /> -->");
                    }
                    sw.WriteLine("</StartingItems>");
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in StartingItems.UpdateXml: {0}", e.Message));
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

        public static void Exec(ClientInfo _cInfo)
        {
            if (Dict.Count > 0)
            {
                if (!PersistentContainer.Instance.Players[_cInfo.playerId].StartingItems)
                {
                    SpawnItems(_cInfo);
                }
                else
                {
                    Log.Out(string.Format("[SERVERTOOLS] Starting items have already been spawned for player {0} with steam id {1}", _cInfo.playerName, _cInfo.playerId));
                }
            }
            else
            {
                Log.Out("[SERVERTOOLS] Starting items list empty. Check the StartingItems.xml file for entries or mistakes.");
            }
        }

        public static void SpawnItems(ClientInfo _cInfo)
        {
            try
            {
                if (Dict.Count > 0)
                {
                    PersistentContainer.Instance.Players[_cInfo.playerId].StartingItems = true;
                    World world = GameManager.Instance.World;
                    List<string> _itemList = Dict.Keys.ToList();
                    for (int i = 0; i < _itemList.Count; i++)
                    {
                        string _item = _itemList[i];
                        if (Dict.TryGetValue(_item, out int[] _itemData))
                        {
                            if (_item == "WalletCoin" || _item == "walletCoin" || _item == "walletcoin")
                            {
                                if (Wallet.IsEnabled)
                                {
                                    Wallet.AddCoinsToWallet(_cInfo.playerId, _itemData[0]);
                                }
                                else
                                {
                                    Phrases.Dict.TryGetValue("VoteReward12", out string _phrase);
                                    Log.Out(string.Format("[SERVERTOOLS] {0}", _phrase));
                                }
                            }
                            else
                            {
                                ItemValue _itemValue = new ItemValue(ItemClass.GetItem(_item, false).type, false);
                                if (_itemValue.HasQuality)
                                {
                                    _itemValue.Quality = _itemData[1];
                                }
                                EntityItem entityItem = new EntityItem();
                                entityItem = (EntityItem)EntityFactory.CreateEntity(new EntityCreationData
                                {
                                    entityClass = EntityClass.FromString("item"),
                                    id = EntityFactory.nextEntityID++,
                                    itemStack = new ItemStack(_itemValue, _itemData[0]),
                                    pos = world.Players.dict[_cInfo.entityId].position,
                                    rot = new Vector3(20f, 0f, 20f),
                                    lifetime = 60f,
                                    belongsPlayerId = _cInfo.entityId
                                });
                                world.SpawnEntityInWorld(entityItem);
                                _cInfo.SendPackage(NetPackageManager.GetPackage<NetPackageEntityCollect>().Setup(entityItem.entityId, _cInfo.entityId));
                                world.RemoveEntity(entityItem.entityId, EnumRemoveEntityReason.Despawned);
                                Thread.Sleep(TimeSpan.FromSeconds(1));
                            }
                        }
                    }
                    PersistentContainer.DataChange = true;
                    Log.Out(string.Format("[SERVERTOOLS] {0} with steam id {1} received their starting items", _cInfo.playerName, _cInfo.playerId));
                    SdtdConsole.Instance.Output(string.Format("[SERVERTOOLS] {0} with steam id {1} received their starting items", _cInfo.playerName, _cInfo.playerId));
                    Phrases.Dict.TryGetValue("StartingItems1", out string _phrase1);
                    ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + _phrase1 + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in StartingItems.SpawnItems: {0}", e.Message));
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
                    sw.WriteLine("<StartingItems>");
                    sw.WriteLine(string.Format("<ST Version=\"{0}\" />", Config.Version));
                    sw.WriteLine("<!-- Use WalletCoin for the Name if you want to give currency to their wallet -->");
                    sw.WriteLine("<!-- <Item Name=\"foodCanChili\" Count=\"1\" Quality=\"1\" /> -->");
                    for (int i = 0; i < _oldChildNodes.Count; i++)
                    {
                        if (_oldChildNodes[i].NodeType == XmlNodeType.Comment && !_oldChildNodes[i].OuterXml.Contains("<!-- Use WalletCoin for") &&
                            !_oldChildNodes[i].OuterXml.Contains("<!-- <Item Name=\"foodCanChili\"") && !_oldChildNodes[i].OuterXml.Contains("    <!-- <Item Name=\"\""))
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
                                string name = "", count = "", quality = "";
                                if (line.HasAttribute("Name"))
                                {
                                    name = line.GetAttribute("Name");
                                }
                                if (line.HasAttribute("Count"))
                                {
                                    count = line.GetAttribute("Count");
                                }
                                if (line.HasAttribute("Quality"))
                                {
                                    quality = line.GetAttribute("Quality");
                                }
                                sw.WriteLine(string.Format("    <Item Name=\"{0}\" Count=\"{1}\" Quality=\"{2}\" />", name, count, quality));
                            }
                        }
                    }
                    sw.WriteLine("</StartingItems>");
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in StartingItems.UpgradeXml: {0}", e.Message));
            }
            FileWatcher.EnableRaisingEvents = true;
            LoadXml();
        }
    }
}

