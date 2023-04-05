﻿using System.Linq;

namespace ServerTools
{
    public class Whisper
    {
        public static bool IsEnabled = false;
        public static string Command_pmessage = "pmessage", Command_pm = "pm", Command_rmessage = "rmessage", Command_rm = "rm";

        public static void Send(ClientInfo _cInfo, string _message)
        {
            if (_message.StartsWith(Command_pmessage + " "))
            {
                _message = _message.Replace(Command_pmessage + " ", "");
            }
            if (_message.StartsWith(Command_pm + " "))
            {
                _message = _message.Replace(Command_pm + " ", "");
            }
            string _nameId = _message.Split(' ').First();
            _message = _message.Replace(_nameId, "");
            if (string.IsNullOrEmpty(_nameId))
            {
                Phrases.Dict.TryGetValue("Whisper1", out string phrase1);
                phrase1 = phrase1.Replace("{PlayerName}", _nameId);
                ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + phrase1 + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                return;
            }
            if (string.IsNullOrEmpty(_message))
            {
                Phrases.Dict.TryGetValue("Whisper3", out string phrase3);
                ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + phrase3 + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                return;
            }
            ClientInfo _recipientInfo = ConsoleHelper.ParseParamIdOrName(_nameId);
            if (_recipientInfo == null)
            {
                Phrases.Dict.TryGetValue("Whisper1", out string phrase1);
                phrase1 = phrase1.Replace("{PlayerName}", _nameId);
                ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + phrase1 + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
            }
            else
            {
                PersistentContainer.Instance.Players[_recipientInfo.CrossplatformId.CombinedString].LastWhisper = _cInfo.CrossplatformId.CombinedString;
                PersistentContainer.DataChange = true;
                ChatHook.ChatMessage(_recipientInfo, Config.Chat_Response_Color + "(Whisper) " + _message + "[-]", -1, _cInfo.playerName, EChatType.Whisper, null);
            }
        }

        public static void Reply(ClientInfo _cInfo, string _message)
        {
            if (_message.StartsWith(Command_rmessage + " "))
            {
                _message = _message.Replace(Command_rmessage + " ", "");
            }
            if (_message.StartsWith(Command_rm + " "))
            {
                _message = _message.Replace(Command_rm + " ", "");
            }
            string lastwhisper = PersistentContainer.Instance.Players[_cInfo.CrossplatformId.CombinedString].LastWhisper;
            if (string.IsNullOrEmpty(lastwhisper))
            {
                Phrases.Dict.TryGetValue("Whisper2", out string _phrase2);
                ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + _phrase2 + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
            }
            else
            {
                ClientInfo cInfo2 = GeneralOperations.GetClientInfoFromNameOrId(lastwhisper);
                if (cInfo2 == null)
                {
                    Phrases.Dict.TryGetValue("Whisper4", out string _phrase4);
                    ChatHook.ChatMessage(_cInfo, Config.Chat_Response_Color + _phrase4 + "[-]", -1, Config.Server_Response_Name, EChatType.Whisper, null);
                }
                else
                {
                    PersistentContainer.Instance.Players[cInfo2.CrossplatformId.CombinedString].LastWhisper = _cInfo.CrossplatformId.CombinedString;
                    PersistentContainer.DataChange = true;
                    ChatHook.ChatMessage(cInfo2, Config.Chat_Response_Color + "(Whisper) " + _message + "[-]", -1, _cInfo.playerName, EChatType.Whisper, null);
                }
            }
        }
    }
}