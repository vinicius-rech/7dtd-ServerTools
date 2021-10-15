﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace ServerTools
{
    class LevelUp
    {
        public static bool IsEnabled = false, IsRunning = false, Xml_Only = false;
        public static Dictionary<int, int> PlayerLevels = new Dictionary<int, int>();

        private static Dictionary<int, string> Dict = new Dictionary<int, string>();
        private const string file = "LevelUp.xml";
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
                                else if (line.HasAttribute("Required") && line.HasAttribute("Command"))
                                {
                                    if (!int.TryParse(line.GetAttribute("Required"), out int level))
                                    {
                                        Log.Out(string.Format("[SERVERTOOLS] Ignoring LevelUp.xml entry because of invalid (non-numeric) value for 'Required' attribute: {0}", line.OuterXml));
                                        continue;
                                    }
                                    string command = line.GetAttribute("Command");
                                    if (!Dict.ContainsKey(level))
                                    {
                                        Dict.Add(level, command);
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
                Log.Out(string.Format("[SERVERTOOLS] Error in LevelUp.LoadXml: {0}", e.Message));
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
                    sw.WriteLine("<Levels>");
                    sw.WriteLine(string.Format("<ST Version=\"{0}\" />", Config.Version));
                    sw.WriteLine("<!-- Command triggers console commands. Use ^ to separate multiple commands -->");
                    sw.WriteLine("<!-- Possible variables for commands include whisper, global, {PlayerName}, {EntityId}, {PlayerId}, {Delay} -->");
                    sw.WriteLine("<!-- <Level Required=\"300\" Command=\"global MAX LEVEL! Congratulations {PlayerName}!\" /> -->");
                    sw.WriteLine();
                    sw.WriteLine();

                    if (Dict.Count > 0)
                    {
                        foreach (KeyValuePair<int, string> kvp in Dict)
                        {
                            sw.WriteLine(string.Format("    <Level Required=\"{0}\" Command=\"{1}\"  />", kvp.Key, kvp.Value));
                        }
                    }
                    else
                    {
                        sw.WriteLine("    <!-- <Level Required=\"\" Command=\"\" /> -->");
                    }
                    sw.WriteLine("</Levels>");
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in LevelUp.UpdateXml: {0}", e.Message));
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

        public static void CheckLevel(ClientInfo _cInfo)
        {
            try
            {
                EntityPlayer player = PersistentOperations.GetEntityPlayer(_cInfo.playerId);
                if (player != null)
                {
                    if (PlayerLevels.ContainsKey(player.entityId))
                    {
                        PlayerLevels.TryGetValue(player.entityId, out int level);
                        if (player.Progression.Level > level)
                        {
                            PlayerLevels[player.entityId] = player.Progression.Level;
                            if (Xml_Only)
                            {
                                if (Dict.ContainsKey(player.Progression.Level))
                                {
                                    Dict.TryGetValue(player.Progression.Level, out string command);
                                    ProcessCommand(_cInfo, command);
                                    Phrases.Dict.TryGetValue("LevelUp1", out string phrase);
                                    phrase = phrase.Replace("{PlayerName}", _cInfo.playerName);
                                    phrase = phrase.Replace("{Value}", player.Progression.Level.ToString());
                                    ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + phrase + "[-]", -1, Config.Server_Response_Name, EChatType.Global, null);
                                }
                            }
                            else
                            {
                                if (Dict.ContainsKey(player.Progression.Level))
                                {
                                    Dict.TryGetValue(player.Progression.Level, out string command);
                                    ProcessCommand(_cInfo, command);
                                }
                                Phrases.Dict.TryGetValue("LevelUp1", out string phrase);
                                phrase = phrase.Replace("{PlayerName}", _cInfo.playerName);
                                phrase = phrase.Replace("{Value}", player.Progression.Level.ToString());
                                ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + phrase + "[-]", -1, Config.Server_Response_Name, EChatType.Global, null);
                            }
                        }
                    }
                    else
                    {
                        PlayerLevels.Add(player.entityId, player.Progression.Level);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in LevelUp.Exec: {0}", e.Message));
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
                                Timers.Level_SingleUseTimer(_time, _cInfo.playerId, _commands);
                                return;
                            }
                            else
                            {
                                Log.Out(string.Format("[SERVERTOOLS] Custom command error. Unable to commit delay with improper integer: {0}", _command));
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
                Log.Out(string.Format("[SERVERTOOLS] Error in LevelUp.ProcessCommand: {0}", e.Message));
            }
        }

        public static void LevelCommandDelayed(string _playerId, List<string> _commands)
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
                                Timers.Level_SingleUseTimer(_time, _cInfo.playerId, _commands);
                                return;
                            }
                            else
                            {
                                Log.Out(string.Format("[SERVERTOOLS] Custom command error. Unable to commit delay with improper integer: {0}", _commands));
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
                Log.Out(string.Format("[SERVERTOOLS] Error in LevelUp.LevelCommandDelayed: {0}", e.Message));
            }
        }

        private static void Command(ClientInfo _cInfo, string _command)
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
                    if (Zones.IsEnabled && Zones.ZonePlayer.ContainsKey(_cInfo.entityId))
                    {
                        Zones.ZonePlayer.Remove(_cInfo.entityId);
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
                Log.Out(string.Format("[SERVERTOOLS] Error in LevelUp.Command: {0}", e.Message));
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
                    sw.WriteLine("<Levels>");
                    sw.WriteLine(string.Format("<ST Version=\"{0}\" />", Config.Version));
                    sw.WriteLine("<!-- Command triggers console commands. Use ^ to separate multiple commands -->");
                    sw.WriteLine("<!-- Possible variables for commands include whisper, global, {PlayerName}, {EntityId}, {PlayerId}, {Delay} -->");
                    sw.WriteLine("<!-- <Level Required=\"300\" Command=\"global MAX LEVEL! Congratulations {PlayerName}!\" /> -->");
                    for (int i = 0; i < _oldChildNodes.Count; i++)
                    {
                        if (_oldChildNodes[i].NodeType == XmlNodeType.Comment && !_oldChildNodes[i].OuterXml.Contains("<!-- Command triggers console") &&
                            !_oldChildNodes[i].OuterXml.Contains("<!-- Possible variables") && !_oldChildNodes[i].OuterXml.Contains("<!-- <Level Required=\"300\"") &&
                            !_oldChildNodes[i].OuterXml.Contains("    <!-- <Level Required=\"\""))
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
                            if (line.HasAttributes && line.Name == "Level")
                            {
                                string level = "", command = "";
                                if (line.HasAttribute("Required"))
                                {
                                    level = line.GetAttribute("Required");
                                }
                                if (line.HasAttribute("Command"))
                                {
                                    command = line.GetAttribute("Command");
                                }
                                sw.WriteLine(string.Format("    <Level Required=\"\" Command=\"{1}\"  />", level, command));
                            }
                        }
                    }
                    sw.WriteLine("</Levels>");
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in LevelUp.UpgradeXml: {0}", e.Message));
            }
            FileWatcher.EnableRaisingEvents = true;
            LoadXml();
        }
    }
}
