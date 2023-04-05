﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace ServerTools
{
    public class Badwords
    {
        public static bool IsEnabled = false, IsRunning = false, Invalid_Name = false;
        public static List<string> Dict = new List<string>();

        private const string file = "BadWords.xml";
        private static readonly string FilePath = string.Format("{0}/{1}", API.ConfigPath, file);
        private static FileSystemWatcher FileWatcher = new FileSystemWatcher(API.ConfigPath, file);

        private static XmlNodeList OldNodeList;

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

        private static void LoadXml()
        {
            try
            {
                if (!File.Exists(FilePath))
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
                XmlNodeList childNodes = xmlDoc.DocumentElement.ChildNodes;
                if (childNodes != null)
                {
                    Dict.Clear();
                    if (childNodes[0] != null && childNodes[0].OuterXml.Contains("Version") && childNodes[0].OuterXml.Contains(Config.Version))
                    {
                        for (int i = 0; i < childNodes.Count; i++)
                        {
                            if (childNodes[i].NodeType == XmlNodeType.Comment)
                            {
                                continue;
                            }
                            XmlElement line = (XmlElement)childNodes[i];
                            if (!line.HasAttributes || !line.HasAttribute("Word"))
                            {
                                continue;
                            }
                            string word = line.GetAttribute("Word").ToLower();
                            if (word == "")
                            {
                                continue;
                            }
                            if (!Dict.Contains(word))
                            {
                                Dict.Add(word);
                            }
                        }
                    }
                    else
                    {
                        XmlNodeList nodeList = xmlDoc.DocumentElement.ChildNodes;
                        XmlNode node = nodeList[0];
                        XmlElement line = (XmlElement)nodeList[0];
                        if (line != null)
                        {
                            if (line.HasAttributes)
                            {
                                OldNodeList = nodeList;
                                File.Delete(FilePath);
                                UpgradeXml();
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
                                        OldNodeList = nodeList;
                                        File.Delete(FilePath);
                                        UpgradeXml();
                                        return;
                                    }
                                }
                                File.Delete(FilePath);
                                UpdateXml();
                                Log.Out(string.Format("[SERVERTOOLS] The existing BadWords.xml was too old or misconfigured. File deleted and rebuilt for version {0}", Config.Version));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (e.Message == "Specified cast is not valid.")
                {
                    File.Delete(FilePath);
                    UpdateXml();
                }
                else
                {
                    Log.Out(string.Format("[SERVERTOOLS] Error in Badwords.LoadXml: {0}", e.Message));
                }
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
                    sw.WriteLine("<BadWordFilter>");
                    sw.WriteLine("    <!-- <Version=\"{0}\" /> -->", Config.Version);
                    sw.WriteLine("    <!-- <Bad Word=\"penis\" /> -->");
                    sw.WriteLine("    <Bad Word=\"\" />");
                    if (Dict.Count > 0)
                    {
                        for (int i = 0; i < Dict.Count; i++)
                        {
                            sw.WriteLine(string.Format("    <Bad Word=\"{0}\" />", Dict[i]));
                        }
                    }
                    sw.WriteLine("</BadWordFilter>");
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Badwords.UpdateXml: {0}", e.Message));
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
            if (!File.Exists(FilePath))
            {
                UpdateXml();
            }
            LoadXml();
        }

        private static void UpgradeXml()
        {
            try
            {
                FileWatcher.EnableRaisingEvents = false;
                using (StreamWriter sw = new StreamWriter(FilePath, false, Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    sw.WriteLine("<BadWordFilter>");
                    sw.WriteLine("    <!-- <Version=\"{0}\" /> -->", Config.Version);
                    sw.WriteLine("    <!-- <Bad Word=\"penis\" /> -->");
                    for (int i = 0; i < OldNodeList.Count; i++)
                    {
                        if (OldNodeList[i].NodeType == XmlNodeType.Comment && !OldNodeList[i].OuterXml.Contains("<!-- <Bad Word=\"penis") &&
                            !OldNodeList[i].OuterXml.Contains("<!-- <Version"))
                        {
                            sw.WriteLine(OldNodeList[i].OuterXml);
                        }
                    }
                    sw.WriteLine("    <Bad Word=\"\" />");
                    for (int i = 0; i < OldNodeList.Count; i++)
                    {
                        if (OldNodeList[i].NodeType != XmlNodeType.Comment)
                        {
                            XmlElement line = (XmlElement)OldNodeList[i];
                            if (line.HasAttributes && line.Name == "Bad")
                            {
                                string word = "";
                                if (line.HasAttribute("Word"))
                                {
                                    word = line.GetAttribute("Word");
                                }
                                sw.WriteLine(string.Format("    <Bad Word=\"{0}\" />", word));
                            }
                        }
                    }
                    sw.WriteLine("</BadWordFilter>");
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Badwords.UpgradeXml: {0}", e.Message));

            }
            FileWatcher.EnableRaisingEvents = true;
            LoadXml();
        }
    }
}