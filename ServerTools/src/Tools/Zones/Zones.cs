﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;

namespace ServerTools
{
    class Zones
    {
        public static bool IsEnabled = false, IsRunning = false, Zone_Message = false, Set_Home = false;
        public static string Reminder_Delay = "20";
        public static Dictionary<int, DateTime> Reminder = new Dictionary<int, DateTime>();
        public static Dictionary<int, string[]> ZonePlayer = new Dictionary<int, string[]>();
        public static List<string[]> ZoneList = new List<string[]>();
        public static Dictionary<int, string[]> ZoneSetup = new Dictionary<int, string[]>();

        private const string file = "Zones.xml";
        private static readonly string FilePath = string.Format("{0}/{1}", API.ConfigPath, file);
        private static FileSystemWatcher FileWatcher = new FileSystemWatcher(API.ConfigPath, file);

        public static void Load()
        {
            LoadXml();
            InitFileWatcher();
        }

        public static void Unload()
        {
            ZoneList.Clear();
            Reminder.Clear();
            ZonePlayer.Clear();
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
                    ZoneList.Clear();
                    Reminder.Clear();
                    ZonePlayer.Clear();
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
                                else if (line.HasAttribute("Name") && line.HasAttribute("Corner1") && line.HasAttribute("Corner2") && line.HasAttribute("Circle") &&
                                    line.HasAttribute("EntryMessage") && line.HasAttribute("ExitMessage") && line.HasAttribute("EntryCommand") && line.HasAttribute("ExitCommand") &&
                                    line.HasAttribute("ReminderNotice") && line.HasAttribute("PvPvE") && line.HasAttribute("NoZombie"))
                                {
                                    string[] zone = { line.GetAttribute("Name"), line.GetAttribute("Corner1"), line.GetAttribute("Corner2"), line.GetAttribute("Circle"),
                                line.GetAttribute("EntryMessage"), line.GetAttribute("ExitMessage"), line.GetAttribute("EntryCommand"),line.GetAttribute("ExitCommand"),
                                line.GetAttribute("ReminderNotice"), line.GetAttribute("PvPvE"), line.GetAttribute("NoZombie") };
                                    if (zone[6] == "")
                                    {
                                        zone[6] = "";
                                    }
                                    if (zone[7] == "")
                                    {
                                        zone[7] = "";
                                    }
                                    if (zone[3].ToLower() == "false")
                                    {
                                        if (zone[1].Contains(",") && zone[2].Contains(","))
                                        {
                                            string[] _corner1 = zone[1].Split(',');
                                            string[] _corner2 = zone[2].Split(',');
                                            int.TryParse(_corner1[0], out int x1);
                                            int.TryParse(_corner1[1], out int y1);
                                            int.TryParse(_corner1[2], out int z1);
                                            int.TryParse(_corner2[0], out int x2);
                                            int.TryParse(_corner2[1], out int y2);
                                            int.TryParse(_corner2[2], out int z2);
                                            int alt;
                                            if (x1 > x2)
                                            {
                                                alt = x2;
                                                x2 = x1;
                                                x1 = alt;
                                            }
                                            if (y1 > y2)
                                            {
                                                alt = y2;
                                                y2 = y1;
                                                y1 = alt;
                                            }
                                            else if (y1 == y2)
                                            {
                                                y2++;
                                            }
                                            if (z1 > z2)
                                            {
                                                alt = z2;
                                                z2 = z1;
                                                z1 = alt;
                                            }
                                            zone[1] = x1 + "," + y1 + "," + z1;
                                            zone[2] = x2 + "," + y2 + "," + z2;
                                        }
                                        else
                                        {
                                            Log.Out(string.Format("[SERVERTOOLS] Ignoring Zones.xml entry. Improper format in corner1 or corner2 attribute: {0}", line.OuterXml));
                                            continue;
                                        }
                                    }
                                    if (!ZoneList.Contains(zone))
                                    {
                                        ZoneList.Add(zone);
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
                Log.Out(string.Format("[SERVERTOOLS] Error in Zones.LoadXml: {0}", e.Message));
            }
        }

        public static void UpdateXml()
        {
            try
            {
                FileWatcher.EnableRaisingEvents = false;
                using (StreamWriter sw = new StreamWriter(FilePath, false, Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    sw.WriteLine("<Zones>");
                    sw.WriteLine(string.Format("<ST Version=\"{0}\" />", Config.Version));
                    sw.WriteLine("<!-- Do not use decimals in the corner positions -->");
                    sw.WriteLine("<!-- Overlapping zones: the first zone listed that is overlapping will take priority -->");
                    sw.WriteLine("<!-- PvPvE: 0 = No Killing, 1 = Kill Allies Only, 2 = Kill Strangers Only, 3 = Kill Everyone -->");
                    sw.WriteLine("<!-- EntryCommand and ExitCommand trigger console commands. Use ^ to separate multiple commands -->");
                    sw.WriteLine("<!-- Possible variables for commands include {PlayerName}, {EntityId}, {PlayerId}, {Delay}, whisper, global -->");
                    sw.WriteLine("<!-- <Zone Name=\"Example\" Corner1=\"1,2,3\" Corner2=\"-3,4,-5\" Circle=\"false\" EntryMessage=\"You have entered example\" ExitMessage=\"You have exited example\" EntryCommand=\"whisper This is a pve space\" ExitCommand=\"\" ReminderNotice=\"You are still in example\" PvPvE=\"0\" NoZombie=\"True\" /> -->");
                    sw.WriteLine();
                    sw.WriteLine();
                    if (ZoneList.Count > 0)
                    {
                        for (int i = 0; i < ZoneList.Count; i++)
                        {
                            string[] _zone = ZoneList[i];
                            sw.WriteLine(string.Format("    <Zone Name=\"{0}\" Corner1=\"{1}\" Corner2=\"{2}\" Circle=\"{3}\" EntryMessage=\"{4}\" ExitMessage=\"{5}\" EntryCommand=\"{6}\" ExitCommand=\"{7}\" ReminderNotice=\"{8}\" PvPvE=\"{9}\" NoZombie=\"{10}\" />", _zone[0], _zone[1], _zone[2], _zone[3], _zone[4], _zone[5], _zone[6], _zone[7], _zone[8], _zone[9], _zone[10]));
                        }
                    }
                    sw.WriteLine("</Zones>");
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Zones.UpdateXml: {0}", e.Message));
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

        public static void SetDelay()
        {
            if (EventSchedule.zones != Reminder_Delay)
            {
                EventSchedule.zones = Reminder_Delay;
                if (Reminder_Delay.Contains(",") && Reminder_Delay.Contains(":"))
                {
                    string[] times = Reminder_Delay.Split(',');
                    for (int i = 0; i < times.Length; i++)
                    {
                        if (DateTime.TryParse(DateTime.Today.ToString("d") + " " + times[i] + ":00", out DateTime time))
                        {
                            if (DateTime.Now < time)
                            {
                                EventSchedule.Add("Zones", time);
                                return;
                            }
                        }
                    }
                    for (int i = 0; i < times.Length; i++)
                    {
                        if (DateTime.TryParse(DateTime.Today.AddDays(1).ToString("d") + " " + times[i] + ":00", out DateTime time))
                        {
                            if (DateTime.Now < time)
                            {
                                EventSchedule.Add("Zones", time);
                                return;
                            }
                        }
                    }
                }
                else if (Reminder_Delay.Contains(":"))
                {
                    if (DateTime.TryParse(DateTime.Today.ToString("d") + " " + Reminder_Delay + ":00", out DateTime time))
                    {
                        if (DateTime.Now < time)
                        {
                            EventSchedule.Add("Zones", time);
                        }
                        else if (DateTime.TryParse(DateTime.Today.AddDays(1).ToString("d") + " " + Reminder_Delay + ":00", out DateTime secondaryTime))
                        {
                            EventSchedule.Add("Zones", secondaryTime);
                        }
                    }
                }
                else
                {
                    if (int.TryParse(Reminder_Delay, out int delay))
                    {
                        EventSchedule.Add("Zones", DateTime.Now.AddMinutes(delay));
                    }
                    else
                    {
                        Log.Out("[SERVERTOOLS] Invalid Zones Reminder_Delay detected. Use a single integer, 24h time or multiple 24h time entries");
                        Log.Out("[SERVERTOOLS] Example: 120 or 03:00 or 03:00, 06:00, 09:00");
                    }
                }
            }
        }

        public static void ZoneCheck(ClientInfo _cInfo, EntityAlive _player)
        {
            try
            {
                if (ZoneList.Count > 0)
                {
                    for (int i = 0; i < ZoneList.Count; i++)
                    {
                        string[] zone = ZoneList[i];
                        if (InsideZone(zone, (int)_player.position.x, (int)_player.position.y, (int)_player.position.z))
                        {
                            if (ZonePlayer.ContainsKey(_player.entityId))
                            {
                                ZonePlayer.TryGetValue(_player.entityId, out string[] info);
                                if (info != zone)
                                {
                                    if (Zone_Message)
                                    {
                                        ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + zone[4] + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                                    }
                                    if (zone[6] != "")
                                    {
                                        ProcessCommand(_cInfo, zone[6]);
                                    }
                                    ZonePlayer[_player.entityId] = zone;
                                    Reminder[_player.entityId] = DateTime.Now;
                                }
                            }
                            else
                            {
                                if (Zone_Message)
                                {
                                    ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + zone[4] + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                                }
                                if (zone[6] != "")
                                {
                                    ProcessCommand(_cInfo, zone[6]);
                                }
                                ZonePlayer.Add(_player.entityId, zone);
                                Reminder.Add(_player.entityId, DateTime.Now);
                            }
                            return;
                        }
                    }
                }
                if (ZonePlayer.ContainsKey(_player.entityId))
                {
                    ZonePlayer.TryGetValue(_player.entityId, out string[] zone);
                    if (Zone_Message && zone[5] != "")
                    {
                        ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + zone[5] + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                    }
                    if (zone[7] != "")
                    {
                        ProcessCommand(_cInfo, zone[7]);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Zones.ZoneCheck: {0}", e.Message));
            }
        }

        private static void ProcessCommand(ClientInfo _cInfo, string _command)
        {
            try
            {
                if (_command.Contains("^"))
                {
                    List<string> _commands = _command.Split('^').ToList();
                    for (int i = 0; i < _commands.Count; i++)
                    {
                        string _commandTrimmed = _commands[i].Trim();
                        if (_commandTrimmed.StartsWith("{Delay}"))
                        {
                            string[] _commandSplit = _commandTrimmed.Split(' ');
                            if (int.TryParse(_commandSplit[1], out int _time))
                            {
                                _commands.RemoveRange(0, i + 1);
                                Timers.Zone_SingleUseTimer(_time, _cInfo.playerId, _commands);
                                return;
                            }
                            else
                            {
                                Log.Out(string.Format("[SERVERTOOLS] Zone command error. Unable to commit delay with improper integer: {0}", _command));
                            }
                        }
                        else
                        {
                            Command(_cInfo, _commandTrimmed);
                        }
                    }
                }
                else
                {
                    Command(_cInfo, _command);
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Zones.ProcessCommand: {0}", e.Message));
            }
        }

        public static void ZoneCommandDelayed(string _playerId, List<string> _commands)
        {
            try
            {
                ClientInfo _cInfo = PersistentOperations.GetClientInfoFromSteamId(_playerId);
                if (_cInfo != null)
                {
                    for (int i = 0; i < _commands.Count; i++)
                    {
                        string _commandTrimmed = _commands[i].Trim();
                        if (_commandTrimmed.StartsWith("{Delay}"))
                        {
                            string[] _commandSplit = _commandTrimmed.Split(' ');
                            if (int.TryParse(_commandSplit[1], out int _time))
                            {
                                _commands.RemoveRange(0, i + 1);
                                Timers.Zone_SingleUseTimer(_time, _cInfo.playerId, _commands);
                                return;
                            }
                            else
                            {
                                Log.Out(string.Format("[SERVERTOOLS] Zone command error. Unable to commit delay with improper integer: {0}", _commands));
                            }
                        }
                        else
                        {
                            Command(_cInfo, _commandTrimmed);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Zones.ZoneCommandDelayed: {0}", e.Message));
            }
        }

        public static void Command(ClientInfo _cInfo, string _command)
        {
            try
            {
                _command = _command.Replace("{EntityId}", _cInfo.entityId.ToString());
                _command = _command.Replace("{SteamId}", _cInfo.playerId);
                _command = _command.Replace("{PlayerName}", _cInfo.playerName);
                if (_command.ToLower().StartsWith("global "))
                {
                    _command = _command.Replace("Global ", "");
                    _command = _command.Replace("global ", "");
                    ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + _command + "[-]", -1, Config.Server_Response_Name, EChatType.Global, null);
                }
                else if (_command.ToLower().StartsWith("whisper "))
                {
                    _command = _command.Replace("Whisper ", "");
                    _command = _command.Replace("whisper ", "");
                    ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + _command + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                }
                else if (_command.StartsWith("tele ") || _command.StartsWith("tp ") || _command.StartsWith("teleportplayer "))
                {
                    if (IsEnabled && ZonePlayer.ContainsKey(_cInfo.entityId))
                    {
                        ZonePlayer.Remove(_cInfo.entityId);
                    }
                    SdtdConsole.Instance.ExecuteSync(_command, null);
                }
                else
                {
                    SdtdConsole.Instance.ExecuteSync(_command, null);
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Zones.Command: {0}", e.Message));
            }
        }

        public static bool InsideZone(string[] _zone, float _X, float _Y, float _Z)
        {
            string[] _corner1 = _zone[1].Split(',');
            float.TryParse(_corner1[0], out float xMin);
            float.TryParse(_corner1[1], out float yMin);
            float.TryParse(_corner1[2], out float zMin);
            if (_zone[3].ToLower() == "true")
            {
                if (int.TryParse(_zone[2], out int _radius))
                {
                    if (VectorCircle(xMin, zMin, _X, _Z, _radius))
                    {
                        return true;
                    }
                }
            }
            else
            {
                string[] _corner2 = _zone[2].Split(',');
                float.TryParse(_corner2[0], out float xMax);
                float.TryParse(_corner2[1], out float yMax);
                float.TryParse(_corner2[2], out float zMax);
                if (VectorBox(xMin, yMin, zMin, xMax, yMax, zMax, _X, _Y, _Z))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool VectorCircle(float xMin, float zMin, float _X, float _Z, int _radius)
        {
            if ((xMin - _X) * (xMin - _X) + (zMin - _Z) * (zMin - _Z) <= _radius * _radius)
            {
                return true;
            }
            return false;
        }

        public static bool VectorBox(float xMin, float yMin, float zMin, float xMax, float yMax, float zMax, float _X, float _Y, float _Z)
        {
            if (_X >= xMin && _X <= xMax && _Y >= yMin && _Y <= yMax && _Z >= zMin && _Z <= zMax)
            {
                return true;
            }
            return false;
        }

        public static void HostileCheck()
        {
            try
            {
                if (ZoneList.Count > 0)
                {
                    for (int i = 0; i < ZoneList.Count; i++)
                    {
                        string[] zone = ZoneList[i];
                        if (zone[10].ToLower() == "true")
                        {
                            List<Entity> Entities = GameManager.Instance.World.Entities.list;
                            for (int j = 0; j < Entities.Count; j++)
                            {
                                Entity entity = Entities[j];
                                if (entity != null && !entity.IsDead())
                                {
                                    if (entity is EntityZombie || entity is EntityEnemyAnimal)
                                    {
                                        Vector3 vec = entity.position;
                                        int X = (int)entity.position.x;
                                        int Y = (int)entity.position.y;
                                        int Z = (int)entity.position.z;
                                        string[] corner1 = zone[1].Split(',');
                                        int.TryParse(corner1[0], out int xMin);
                                        int.TryParse(corner1[1], out int yMin);
                                        int.TryParse(corner1[2], out int zMin);
                                        if (zone[3].ToLower() == "true")
                                        {
                                            if (int.TryParse(zone[2], out int radius))
                                            {
                                                if (VectorCircle(xMin, zMin, X, Z, radius))
                                                {
                                                    GameManager.Instance.World.RemoveEntity(entity.entityId, EnumRemoveEntityReason.Despawned);
                                                    Log.Out(string.Format("[SERVERTOOLS] Entity cleanup: Removed {0} from zone {1} @ {2} {3} {4}",
                                                        EntityClass.list[entity.entityClass].entityClassName, zone[0], X, Y, Z));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            string[] corner2 = zone[2].Split(',');
                                            int.TryParse(corner2[0], out int xMax);
                                            int.TryParse(corner2[1], out int yMax);
                                            int.TryParse(corner2[2], out int zMax);
                                            if (VectorBox(xMin, yMin, zMin, xMax, yMax, zMax, X, Y, Z))
                                            {
                                                GameManager.Instance.World.RemoveEntity(entity.entityId, EnumRemoveEntityReason.Despawned);
                                                Log.Out(string.Format("[SERVERTOOLS] Entity cleanup: Removed {0} from zone {1} @ {2} {3} {4}",
                                                    EntityClass.list[entity.entityClass].entityClassName, zone[0], X, Y, Z));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Zones.HostileCheck: {0}", e.Message));
            }
        }

        public static void ReminderExec()
        {
            try
            {
                foreach (KeyValuePair<int, DateTime> time in Reminder.ToArray())
                {
                    ClientInfo cInfo = PersistentOperations.GetClientInfoFromEntityId(time.Key);
                    if (cInfo != null)
                    {
                        DateTime dt = time.Value;
                        TimeSpan varTime = DateTime.Now - dt;
                        double fractionalMinutes = varTime.TotalMinutes;
                        int timepassed = (int)fractionalMinutes;
                        int.TryParse(Reminder_Delay, out int delay);
                        if (timepassed >= delay)
                        {
                            ZonePlayer.TryGetValue(cInfo.entityId, out string[] zone);
                            if (zone != null && zone[8] != "")
                            {
                                ChatHook.ChatMessage(cInfo, Config.Chat_Response_Color + zone[8] + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                                Reminder[cInfo.entityId] = DateTime.Now;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Zones.ReminderExec: {0}", e.Message));
            }
        }

        public static bool IsValid(ClientInfo _cInfo1, ClientInfo _cInfo2)
        {
            try
            {
                if (ZonePlayer.ContainsKey(_cInfo1.entityId))
                {
                    ZonePlayer.TryGetValue(_cInfo1.entityId, out string[] zone1);
                    if (ZonePlayer.ContainsKey(_cInfo2.entityId))
                    {
                        ZonePlayer.TryGetValue(_cInfo2.entityId, out string[] zone2);
                        if (zone1[9] != zone2[9])
                        {
                            Phrases.Dict.TryGetValue("Zones3", out string phrase);
                            ChatHook.ChatMessage(_cInfo2, Config.Chat_Response_Color + phrase + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                            return false;
                        }
                        else if (zone1[9] == "0")
                        {
                            Phrases.Dict.TryGetValue("Zones9", out string phrase);
                            ChatHook.ChatMessage(_cInfo2, Config.Chat_Response_Color + phrase + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                            return false;
                        }
                        else if (zone1[9] == "1")
                        {
                            PersistentPlayerData ppd1 = PersistentOperations.GetPersistentPlayerDataFromSteamId(_cInfo1.playerId);
                            PersistentPlayerData ppd2 = PersistentOperations.GetPersistentPlayerDataFromSteamId(_cInfo2.playerId);
                            if ((ppd1 != null && ppd1.ACL != null && !ppd1.ACL.Contains(_cInfo2.playerId)) || (ppd2 != null && ppd2.ACL != null && !ppd2.ACL.Contains(_cInfo1.playerId)))
                            {
                                Phrases.Dict.TryGetValue("Zones10", out string phrase);
                                ChatHook.ChatMessage(_cInfo2, Config.Chat_Response_Color + phrase + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                                return false;
                            }
                        }
                        else if (zone1[9] == "2")
                        {
                            PersistentPlayerData ppd1 = PersistentOperations.GetPersistentPlayerDataFromSteamId(_cInfo1.playerId);
                            PersistentPlayerData ppd2 = PersistentOperations.GetPersistentPlayerDataFromSteamId(_cInfo2.playerId);
                            if ((ppd1 != null && ppd1.ACL != null && ppd1.ACL.Contains(_cInfo2.playerId)) || (ppd2 != null && ppd2.ACL != null && ppd2.ACL.Contains(_cInfo1.playerId)))
                            {
                                Phrases.Dict.TryGetValue("Zones11", out string phrase);
                                ChatHook.ChatMessage(_cInfo2, Config.Chat_Response_Color + phrase + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                                return false;
                            }
                        }
                    }
                    else if (zone1[9] != PersistentOperations.Player_Killing_Mode.ToString())
                    {
                        Phrases.Dict.TryGetValue("Zones3", out string phrase);
                        ChatHook.ChatMessage(_cInfo2, Config.Chat_Response_Color + phrase + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                        return false;
                    }
                }
                else if (ZonePlayer.ContainsKey(_cInfo2.entityId))
                {
                    ZonePlayer.TryGetValue(_cInfo2.entityId, out string[] zone);
                    if (zone[9] != PersistentOperations.Player_Killing_Mode.ToString())
                    {
                        Phrases.Dict.TryGetValue("Zones3", out string phrase);
                        ChatHook.ChatMessage(_cInfo2, Config.Chat_Response_Color + phrase + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Zones.IsValid: {0}", e.Message));
            }
            return true;
        }

        private static void UpgradeXml(XmlNodeList _oldChildNodes)
        {
            try
            {
                FileWatcher.EnableRaisingEvents = false;
                using (StreamWriter sw = new StreamWriter(FilePath, false, Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    sw.WriteLine("<Zones>");
                    sw.WriteLine(string.Format("<ST Version=\"{0}\" />", Config.Version));
                    sw.WriteLine("<!-- Do not use decimals in the corner positions -->");
                    sw.WriteLine("<!-- Overlapping zones: the first zone listed that is overlapping will take priority -->");
                    sw.WriteLine("<!-- PvPvE: 0 = No Killing, 1 = Kill Allies Only, 2 = Kill Strangers Only, 3 = Kill Everyone -->");
                    sw.WriteLine("<!-- EntryCommand and ExitCommand trigger console commands. Use ^ to separate multiple commands -->");
                    sw.WriteLine("<!-- Possible variables for commands include {PlayerName}, {EntityId}, {PlayerId}, {Delay}, whisper, global -->");
                    sw.WriteLine("<!-- <Zone Name=\"Example\" Corner1=\"1,2,3\" Corner2=\"-3,4,-5\" Circle=\"false\" EntryMessage=\"You have entered example\" ExitMessage=\"You have exited example\" EntryCommand=\"whisper This is a pve space\" ExitCommand=\"\" ReminderNotice=\"You are still in example\" PvPvE=\"0\" NoZombie=\"True\" /> -->");
                    for (int i = 0; i < _oldChildNodes.Count; i++)
                    {
                        if (_oldChildNodes[i].NodeType == XmlNodeType.Comment && !_oldChildNodes[i].OuterXml.Contains("<!-- Do not use decimals") &&
                            !_oldChildNodes[i].OuterXml.Contains("<!-- Overlapping zones:") && !_oldChildNodes[i].OuterXml.Contains("<!-- PvPvE: 0 = No Killing") &&
                            !_oldChildNodes[i].OuterXml.Contains("<!-- EntryCommand and ExitCommand") && !_oldChildNodes[i].OuterXml.Contains("<!-- Possible variables for commands") &&
                            !_oldChildNodes[i].OuterXml.Contains("<!-- <Zone Name=\"Example\"") && !_oldChildNodes[i].OuterXml.Contains("    <!-- <Zone Name=\"\""))
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
                            if (line.HasAttributes && line.Name == "Zone")
                            {
                                string name = "", corner1 = "", corner2 = "", circle = "", entryMessage = "", exitMessage = "", entryCommand = "",
                                    exitCommand = "", reminder = "", pvpve = "", noZ = "";
                                if (line.HasAttribute("Name"))
                                {
                                    name = line.GetAttribute("Name");
                                }
                                if (line.HasAttribute("Corner1"))
                                {
                                    corner1 = line.GetAttribute("Corner1");
                                }
                                if (line.HasAttribute("Corner2"))
                                {
                                    corner2 = line.GetAttribute("Corner2");
                                }
                                if (line.HasAttribute("Circle"))
                                {
                                    circle = line.GetAttribute("Circle");
                                }
                                if (line.HasAttribute("EntryMessage"))
                                {
                                    entryMessage = line.GetAttribute("EntryMessage");
                                }
                                if (line.HasAttribute("ExitMessage"))
                                {
                                    exitMessage = line.GetAttribute("ExitMessage");
                                }
                                if (line.HasAttribute("EntryCommand"))
                                {
                                    entryCommand = line.GetAttribute("EntryCommand");
                                }
                                if (line.HasAttribute("ExitCommand"))
                                {
                                    exitCommand = line.GetAttribute("ExitCommand");
                                }
                                if (line.HasAttribute("ReminderNotice"))
                                {
                                    reminder = line.GetAttribute("ReminderNotice");
                                }
                                if (line.HasAttribute("PvPvE"))
                                {
                                    pvpve = line.GetAttribute("PvPvE");
                                }
                                if (line.HasAttribute("NoZombie"))
                                {
                                    noZ = line.GetAttribute("NoZombie");
                                }
                                sw.WriteLine(string.Format("    <Zone Name=\"{0}\" Corner1=\"{1}\" Corner2=\"{2}\" Circle=\"{3}\" EntryMessage=\"{4}\" ExitMessage=\"{5}\" EntryCommand=\"{6}\" ExitCommand=\"{7}\" ReminderNotice=\"{8}\" PvPvE=\"{9}\" NoZombie=\"{10}\" />", name, corner1, corner2, circle, entryMessage, exitMessage, entryCommand, exitCommand, reminder, pvpve, noZ));
                            }
                        }
                    }
                    sw.WriteLine("</Zones>");
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Zones.UpgradeXml: {0}", e.Message));
            }
            FileWatcher.EnableRaisingEvents = true;
            LoadXml();
        }
    }
}
