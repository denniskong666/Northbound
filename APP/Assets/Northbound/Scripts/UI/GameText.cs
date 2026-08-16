using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Northbound.UI
{
    public enum GameLanguage { English, SimplifiedChinese }

    /// <summary>Small runtime localization layer for the player-facing navigation and control UI.</summary>
    public static class GameText
    {
        private static Font chineseFont;
        public static GameLanguage Language { get; private set; } = GameLanguage.English;
        public static bool IsChinese => Language == GameLanguage.SimplifiedChinese;
        public static event Action LanguageChanged;

        public static void Use(GameLanguage language)
        {
            if (Language == language) return;
            Language = language;
            LanguageChanged?.Invoke();
        }

        public static string T(string english, string chinese) => IsChinese ? chinese : english;

        public static string Location(string english) => !IsChinese ? english : english switch
        {
            "Greybridge" => "格雷布里奇街区",
            "Ruth's Diner" => "露丝餐馆",
            "Vale Auto Garage" => "维尔汽车修理厂",
            "Maya's Studio" => "玛雅工作室",
            "Noah's Electronics" => "诺亚电子店",
            "Rooftop Overlook" => "屋顶瞭望台",
            "Jamie's Home" => "杰米的家",
            "Finale Gathering" => "终章集合点",
            "Southeast - Northbound Road" => "东南 - 向北公路",
            "Southwest - Home in Greybridge" => "西南 - 留在格雷布里奇",
            "East - Road Without a Map" => "向东 - 没有地图的路",
            "Northeast - Wait Until Morning" => "东北 - 等到天亮",
            _ => english
        };

        public static string Objective(string english)
        {
            if (!IsChinese) return english;
            var isStart = english.StartsWith("Start ", StringComparison.Ordinal);
            var title = isStart ? english.Substring(6) : english;
            var translated = title switch
            {
                "Find Your Footing" or "Find your footing" => "熟悉行动",
                "Clock In" => "开始上班",
                "Missing Socket" => "丢失的套筒",
                "Parts Future" => "未来的零件",
                "Rooftop Inventory" => "屋顶清点",
                "Last Sign" => "最后的招牌",
                "Dead Air" => "无声电波",
                "One More Table" => "再服务一桌",
                "Alternator" => "交流发电机",
                "First Light" => "第一束光",
                "Road Test" => "道路测试",
                "Static" => "静电杂音",
                "Pack Trunk" => "整理后备箱",
                "Last Night Open" => "最后营业之夜",
                "Things We Leave" => "我们留下的东西",
                "Spare Key" => "备用钥匙",
                "Before Morning" => "天亮之前",
                "Meet The People Of Greybridge" or "Meet the people of Greybridge" => "去见格雷布里奇的人们",
                "Meet At The Wagon" or "Meet at the wagon" => "在旅行车旁集合",
                "Choose Your Direction" or "Choose your direction" => "选择你的方向",
                "Journey Complete" or "Journey complete" => "旅程完成",
                "Wait For The Final Memory" or "Wait for the final memory" => "等待最后的回忆",
                _ => title
            };
            return isStart ? $"开始：{translated}" : translated;
        }

        public static string Prompt(string english)
        {
            if (string.IsNullOrWhiteSpace(english)) return english;
            if (!IsChinese)
            {
                if (english.StartsWith("[E / ENTER] ", StringComparison.Ordinal)) return english;
                return english.StartsWith("[E] ", StringComparison.Ordinal)
                    ? english.Replace("[E] ", "[E / ENTER] ")
                    : $"[E / ENTER] {english}";
            }
            if (english.Contains("Return to Greybridge")) return "[E / 回车] 返回格雷布里奇";
            if (english.Contains("Enter Vale Auto Garage")) return "[E / 回车] 进入维尔汽车修理厂";
            if (english.Contains("Enter Ruth's Diner")) return "[E / 回车] 进入露丝餐馆";
            if (english.Contains("Enter Jamie's Home")) return "[E / 回车] 进入杰米的家";
            if (english.Contains("Enter Maya's Studio")) return "[E / 回车] 进入玛雅工作室";
            if (english.Contains("Enter Noah's Electronics")) return "[E / 回车] 进入诺亚电子店";
            if (english.Contains("Climb to Rooftop Overlook")) return "[E / 回车] 前往屋顶瞭望台";
            if (english.StartsWith("Talk to ", StringComparison.Ordinal))
            {
                return $"[E / 回车] 与{CharacterName(english.Substring(8))}交谈";
            }
            var translated = english switch
            {
                "Begin mission" => "[E / 回车] 开始任务",
                "Watch memory" => "[E] 观看回忆",
                "Talk" => "[E] 交谈",
                "Serve the diner shift" => "[E] 开始餐馆值班",
                "Find the missing socket" => "[E] 寻找丢失的套筒",
                "Fit the battery" => "[E] 安装电池",
                "Pick up the fan belt" => "[E] 拿起风扇皮带",
                "Pick up the fuses" => "[E] 拿起保险丝",
                "Pick up the toolbox" => "[E] 拿起工具箱",
                "Hang Maya's painting" => "[E] 挂起玛雅的画",
                "Set the gallery lights" => "[E] 调整展厅灯光",
                "Open the exhibition" => "[E] 开启展览",
                "Lift the alternator" => "[E] 抬起交流发电机",
                "Connect the belt" => "[E] 连接皮带",
                "Test the charge" => "[E] 测试充电",
                "Start the road test" => "[E] 开始道路测试",
                "Push the stalled wagon" => "[E] 推动抛锚旅行车",
                "Return the wagon" => "[E] 将旅行车开回车库",
                "Carry the recorder" => "[E] 拿起录音机",
                "Deliver the radio case" => "[E] 送达收音机箱",
                "Choose one object" => "[E] 选择一件带走的物品",
                "Carry Photograph" => "[E] 带上照片",
                "Carry Notebook" => "[E] 带上笔记本",
                "Carry House Key" => "[E] 带上家门钥匙",
                "Carry Old Map" => "[E] 带上旧地图",
                "Visit Maya" => "[E] 拜访玛雅",
                "Visit Noah" => "[E] 拜访诺亚",
                "Visit Leo" => "[E] 拜访利奥",
                "Review the available routes" => "[E / 回车] 查看可选路线",
                "Wire the recorder" => "[E] 接好录音机线路",
                "Pick up the missing socket" => "[E] 拿起丢失的套筒",
                "Pick up the recorder" => "[E] 拿起录音机",
                "Pick up the spare key" => "[E] 拿起备用钥匙",
                "Return the table" => "[E] 收拾最后一桌",
                "Remove the sign" => "[E] 拆下招牌",
                "Count the inventory" => "[E] 清点库存",
                "Find the spare key" => "[E] 找到备用钥匙",
                "Pack the trunk" => "[E] 整理后备箱",
                "Serve the final tables" => "[E] 服务最后几桌",
                "Complete the task" => "[E] 完成任务",
                _ => english
            };
            if (translated.StartsWith("[E / 回车] ", StringComparison.Ordinal)) return translated;
            if (translated.StartsWith("[E / ENTER] ", StringComparison.Ordinal))
                return "[E / 回车] " + translated.Substring("[E / ENTER] ".Length);
            return translated.StartsWith("[E] ", StringComparison.Ordinal)
                ? translated.Replace("[E] ", "[E / 回车] ")
                : $"[E / 回车] {translated}";
        }

        public static string Completion(string englishPrompt)
        {
            if (!IsChinese) return $"{englishPrompt} complete";
            var localizedPrompt = StripInteractionKey(Prompt(englishPrompt));
            return $"{localizedPrompt}已完成";
        }

        public static string ObjectivePrompt(string objectiveId) => objectiveId switch
        {
            "serve_tables" => "Serve the diner shift", "find_socket" => "Pick up the missing socket", "fit_battery" => "Fit the battery",
            "collect_belt" => "Pick up the fan belt", "collect_fuses" => "Pick up the fuses", "collect_toolbox" => "Pick up the toolbox",
            "hang_painting" => "Hang Maya's painting", "set_lights" => "Set the gallery lights", "open_exhibition" => "Open the exhibition",
            "lift_alternator" => "Lift the alternator", "connect_belt" => "Connect the belt", "test_charge" => "Test the charge",
            "drive_service_road" => "Start the road test", "push_wagon" => "Push the stalled wagon", "return_garage" => "Return the wagon",
            "wire_recorder" => "Wire the recorder", "carry_recorder" => "Pick up the recorder", "deliver_radio_case" => "Deliver the radio case",
            "return_table" => "Return the table", "remove_sign" => "Remove the sign", "count_inventory" => "Count the inventory",
            "find_key" => "Pick up the spare key", "choose_carried_object" => "Choose one object to carry",
            "visit_maya" => "Visit Maya", "visit_noah" => "Visit Noah", "visit_leo" => "Visit Leo", "pack_trunk" => "Pack the trunk",
            "close_diner" => "Serve the final tables", _ => "Complete the task"
        };

        public static string ObjectiveAction(string objectiveId)
        {
            var prompt = ObjectivePrompt(objectiveId);
            return IsChinese ? StripInteractionKey(Prompt(prompt)) : prompt;
        }

        public static string ObjectiveInstruction(string objectiveId)
        {
            var action = ObjectiveAction(objectiveId);
            if (IsChinese)
                return $"下一步：前往金色描边的目标旁，按 E / 回车：{action}。";
            var lowerAction = char.ToLowerInvariant(action[0]) + action.Substring(1);
            return $"NEXT: At the gold-outlined target, press E / Enter to {lowerAction}.";
        }

        public static string NavigationAction(string english)
        {
            if (!IsChinese) return english;
            return english switch
            {
                "MOVE: Use WASD or arrow keys." => "移动：使用 WASD 或方向键。",
                "NEXT: Press E at the gold star." => "下一步：到金色标记处按 E。",
                "ENTER: Press E / Enter at the marked door." => "进入：到标记的门口按 E / 回车。",
                "INTERACT: Press E / Enter at the marked door." => "交互：到标记的门口按 E / 回车。",
                "WAIT: Watch the final memory." => "等待：观看最后的回忆。",
                "NEXT: Press E at the gathering point." => "下一步：到集合点按 E。",
                "CHOOSE: Walk toward one of the four route regions." => "选择：向四个路线区域中的一个持续前进。",
                "ROUTES: Southeast Northbound | Southwest Home | East No Map | Northeast Wait." => "路线：东南向北公路 | 西南回家 | 向东无名路 | 东北等到天亮。",
                _ => english
            };
        }

        public static string UiLabel(string english) => !IsChinese ? english : english switch
        {
            "New Game" => "新游戏", "Continue" => "继续游戏", "Settings" => "设置", "Credits" => "制作人员",
            "Confirm New Game" => "确认新游戏", "Cancel New Game" => "取消", "Resume" => "继续",
            "Return to Title" => "返回标题", "Save and Quit" => "保存并退出游戏", "Apply" => "应用", "Back" => "返回",
            "Master Volume" => "主音量", "Music Volume" => "音乐音量", "SFX Volume" => "音效音量", "Voice Volume" => "语音音量",
            "Subtitle Scale" => "字幕大小", "Subtitle Background Opacity" => "字幕背景透明度",
            "Interaction Time Multiplier" => "交互时间", "Reduced Motion" => "减少动态效果", "Skip Minigames" => "跳过小游戏",
            _ => english
        };

        public static void ApplyFont(Text text)
        {
            if (text == null || !IsChinese) return;
            chineseFont ??= Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Arial Unicode MS", "Arial" }, Mathf.Max(14, text.fontSize));
            if (chineseFont != null) text.font = chineseFont;
        }

        public static string CharacterName(string idOrName) => idOrName.Trim().ToLowerInvariant() switch
        {
            "elias" => "伊莱亚斯",
            "maya" => "玛雅",
            "noah" => "诺亚",
            "leo" => "利奥",
            "jamie" => "杰米",
            _ => idOrName
        };

        private static string StripInteractionKey(string value)
        {
            foreach (var prefix in new[] { "[E / ENTER] ", "[E / 回车] ", "[E] " })
                if (value.StartsWith(prefix, StringComparison.Ordinal)) return value.Substring(prefix.Length);
            return value;
        }
    }
}
