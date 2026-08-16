using System.Collections.Generic;
using Northbound.Dialogue;
using Northbound.Narrative;

namespace Northbound.EditorTools
{
    /// <summary>Keeps the authored chapter decisions deterministic when narrative content is rebuilt.</summary>
    internal static class MeaningfulStoryDialogueSeeder
    {
        public static bool TryBuild(DialogueAsset asset, string id)
        {
            if (asset == null)
            {
                return false;
            }

            switch (id)
            {
                case "chapter_two_rooftop":
                    asset.lines = BuildChapterTwoRooftop();
                    return true;
                case "rooftop_decision":
                    asset.lines = BuildRooftopDecision();
                    return true;
                case "before_morning_dialogue":
                    asset.lines = BuildBeforeMorning();
                    return true;
                default:
                    return false;
            }
        }

        private static List<DialogueLine> BuildChapterTwoRooftop()
        {
            var lines = new List<DialogueLine>
            {
                Line("Elias", "If we keep moving the date, it stops being a plan.", "如果我们一直改日期，这就不再是计划了。"),
                Line("Maya", "A plan should survive the people inside it changing.", "一个计划，应该容得下计划里的人发生改变。"),
                Line("Noah", "Friday is close. That does not make the answer simple.", "星期五已经很近了，但这不代表答案就变简单了。"),
                Line("Jamie", "What should matter most now?", "现在，什么才应该被放在第一位？"),
                Line("Elias", "Good. Friday stays. I can carry the work if I know the promise is still ours.", "好。星期五不变。只要这份约定还是大家的，我就能继续扛下去。", next: 7),
                Line("Maya", "Then keep the map open. Do not turn one date into a locked door.", "那就让地图继续摊开。别让一个日期变成锁死的门。", next: 7),
                Line("Maya", "Thank you. Wanting what is here does not make any of us cowards.", "谢谢。珍惜眼前的生活，并不会让任何人变成懦夫。", next: 7),
                Line("", "Elias redraws Friday in darker marker, but no one mistakes the ink for agreement.", "伊莱亚斯用更深的笔迹重新圈住星期五，但没有人再把这道墨迹当成全体同意。", narration: true),
                Line("", "Noah records the silence after Jamie's answer. Leo does not cover it with a joke.", "诺亚录下杰米回答后的沉默。利奥没有再用笑话把它盖过去。", narration: true)
            };
            lines[3].choices = new List<DialogueChoice>
            {
                Choice(
                    "Keep Friday fixed. A plan only means something if we honor it.",
                    "守住星期五。计划只有被兑现，才真正算数。",
                    "story_mark_ch2_a",
                    4,
                    Delta("tendency_commitment", 10),
                    Delta("tendency_agency", -4),
                    Delta("bond_elias", 3),
                    Delta("bond_maya", -1)),
                Choice(
                    "Keep preparing, but leave room for the people the plan is meant to serve.",
                    "继续准备，但也给计划里的人留出改变的余地。",
                    "story_mark_ch2_b",
                    5,
                    Delta("tendency_commitment", 4),
                    Delta("tendency_rootedness", 4),
                    Delta("tendency_agency", 4),
                    Delta("bond_elias", 1),
                    Delta("bond_maya", 1)),
                Choice(
                    "Stop treating the north as more real than the lives already happening here.",
                    "别再把北方看得比眼前正在发生的生活更真实。",
                    "story_mark_ch2_c",
                    6,
                    Delta("tendency_commitment", -8),
                    Delta("tendency_rootedness", 10),
                    Delta("tendency_agency", 7),
                    Delta("bond_elias", -1),
                    Delta("bond_maya", 3))
            };
            return lines;
        }

        private static List<DialogueLine> BuildRooftopDecision()
        {
            var lines = new List<DialogueLine>
            {
                Line("", "Jamie defended Friday before. Elias arrives expecting that answer to hold.", "杰米曾经替星期五的计划说过话。伊莱亚斯来到屋顶时，仍以为那个答案不会改变。", "story_mark_ch2_a", 3, true),
                Line("", "Jamie asked the plan to leave room for people. Tonight, both sides expect that balance again.", "杰米曾要求计划给人留下改变的余地。今晚，双方都在等待杰米再次维持平衡。", "story_mark_ch2_b", 3, true),
                Line("", "Jamie put the lives in Greybridge before the timetable. Maya has not forgotten it.", "杰米曾把格雷布里奇正在发生的生活放在时间表之前。玛雅一直记得。", "story_mark_ch2_c", 3, true),
                Line("Elias", "We made one promise. It cannot mean nothing the moment keeping it gets hard.", "我们许过同一个承诺。不能一到兑现它变难的时候，这个承诺就什么都不算了。"),
                Line("Maya", "We were twelve. A promise made by children cannot own every adult we become.", "那时我们才十二岁。孩子许下的承诺，不能占有我们长大后的每一种人生。"),
                Line("Noah", "The promise brought us this far. It still does not get to answer for us.", "那份承诺把我们带到了这里，但它仍然不能替我们回答。"),
                Line("Jamie", "What should the old promise mean now?", "到了现在，那份旧约定究竟还应该意味着什么？"),
                Line("Elias", "Then say it plainly. We leave Friday, and the promise still belongs to all of us.", "那就说清楚。星期五出发，这份约定依然属于我们所有人。", next: 10),
                Line("Maya", "That is the first version of the promise that leaves room for us to be people.", "这是那份约定第一次给我们留出了做自己的空间。", next: 10),
                Line("Noah", "I can live with a promise that does not require anyone to disappear inside it.", "如果一份承诺不要求任何人把自己藏起来，我愿意继续带着它生活。", next: 10),
                Line("", "The chalk arrow has faded until it points nowhere in particular. Elias leaves the map beneath a loose brick.", "粉笔箭头已经淡得几乎不再指向任何地方。伊莱亚斯把地图压在一块松动的砖下，独自离开。", narration: true)
            };
            lines[6].choices = new List<DialogueChoice>
            {
                Choice(
                    "A promise matters because we keep it, especially when it gets hard.",
                    "正因为兑现很难，守住承诺才有意义。",
                    "story_mark_ch3_a",
                    7,
                    Delta("tendency_commitment", 12),
                    Delta("tendency_agency", -4),
                    Delta("bond_elias", 3),
                    Delta("bond_maya", -1)),
                Choice(
                    "Keep the promise to each other, not to one date or one road.",
                    "守住对彼此的承诺，而不是死守某个日期或某一条路。",
                    "story_mark_ch3_b",
                    8,
                    Delta("tendency_commitment", 3),
                    Delta("tendency_rootedness", 3),
                    Delta("tendency_agency", 6),
                    Delta("bond_elias", 1),
                    Delta("bond_maya", 1),
                    Delta("bond_noah", 1),
                    Delta("bond_leo", 1)),
                Choice(
                    "No one owes the group a life they no longer choose.",
                    "谁都不欠这个小团体一种自己已经不再选择的人生。",
                    "story_mark_ch3_c",
                    9,
                    Delta("tendency_commitment", -8),
                    Delta("tendency_rootedness", 2),
                    Delta("tendency_agency", 12),
                    Delta("bond_elias", -2),
                    Delta("bond_maya", 3),
                    Delta("bond_noah", 2))
            };
            return lines;
        }

        private static List<DialogueLine> BuildBeforeMorning()
        {
            var lines = new List<DialogueLine>
            {
                Line("", "On the rooftop, Jamie chose the promise. The second key now feels like its weight made metal.", "在屋顶上，杰米选择了守约。如今，第二把钥匙像是把那份承诺的重量变成了金属。", "story_mark_ch3_a", 3, true),
                Line("", "On the rooftop, Jamie refused to make either side surrender. Three lit doorways still ask what that balance costs.", "在屋顶上，杰米拒绝逼任何一边投降。三扇亮着灯的门仍在追问，这份平衡需要付出什么。", "story_mark_ch3_b", 3, true),
                Line("", "On the rooftop, Jamie defended everyone's right to change. Now Jamie has to claim that right too.", "在屋顶上，杰米捍卫了每个人改变心意的权利。现在，杰米也必须为自己作出选择。", "story_mark_ch3_c", 3, true),
                Line("", "Maya's studio, Noah's radio shop, and Leo's diner are still lit. There is time to visit, but not to make anyone else's decision.", "玛雅的工作室、诺亚的电器店和利奥的餐馆仍亮着灯。还有时间去见他们，但不能替任何人做决定。", narration: true),
                Line("", "What will Jamie carry into these final visits?", "杰米将带着怎样的答案，走进天亮前最后的三次见面？", narration: true),
                Line("Jamie", "Then I will say goodbye without pretending the plan disappeared.", "那我会认真道别，也不会假装原来的计划已经消失。", next: 8),
                Line("", "The northbound road and the three lit doorways remain visible at the same time.", "向北的路和三扇亮着灯的门，同时留在杰米的视野里。", next: 8, narration: true),
                Line("Jamie", "Then these visits are not votes. They are a chance to hear what each person actually wants.", "那么，这三次见面就不是投票，而是一次真正听见每个人想要什么的机会。", next: 8),
                Line("", "Jamie starts with the nearest light. Morning can wait until every friend has answered for themselves.", "杰米走向最近的那盏灯。等每个朋友都替自己回答以后，清晨再来。", narration: true)
            };
            lines[4].choices = new List<DialogueChoice>
            {
                Choice(
                    "I am still going north. I will ask them to keep the promise with me.",
                    "我仍然要去北方。我会请他们和我一起守住约定。",
                    "story_mark_ch4_a",
                    5,
                    Delta("tendency_commitment", 12),
                    Delta("tendency_rootedness", -4),
                    Delta("tendency_agency", -2),
                    Delta("bond_elias", 3)),
                Choice(
                    "Staying and leaving are not opposite answers to a test. I will not call either one wrong.",
                    "留下和离开，不是一道题的正反答案。我不会把任何一种选择说成错误。",
                    "story_mark_ch4_b",
                    6,
                    Delta("tendency_commitment", 3),
                    Delta("tendency_rootedness", 5),
                    Delta("tendency_agency", 5),
                    Delta("bond_elias", 1),
                    Delta("bond_maya", 1),
                    Delta("bond_noah", 1),
                    Delta("bond_leo", 1)),
                Choice(
                    "Each of us should choose the life that fits, even if our roads separate.",
                    "每个人都该选择真正适合自己的生活，哪怕我们的路会因此分开。",
                    "story_mark_ch4_c",
                    7,
                    Delta("tendency_commitment", -6),
                    Delta("tendency_rootedness", 2),
                    Delta("tendency_agency", 12),
                    Delta("bond_maya", 2),
                    Delta("bond_noah", 2),
                    Delta("bond_leo", 2))
            };
            return lines;
        }

        private static DialogueLine Line(
            string speaker,
            string english,
            string chinese,
            string requiredFact = "",
            int next = -1,
            bool narration = false)
        {
            return new DialogueLine
            {
                speakerId = speaker,
                presentation = narration ? DialoguePresentation.Narration : DialoguePresentation.Character,
                text = english,
                textChinese = chinese,
                requiredFact = requiredFact,
                nextLineIndex = next
            };
        }

        private static DialogueChoice Choice(
            string english,
            string chinese,
            string fact,
            int next,
            params NarrativeCounterDelta[] deltas)
        {
            return new DialogueChoice
            {
                text = english,
                textChinese = chinese,
                grantedFact = fact,
                nextLineIndex = next,
                counterDeltas = new List<NarrativeCounterDelta>(deltas)
            };
        }

        private static NarrativeCounterDelta Delta(string id, int amount)
        {
            return new NarrativeCounterDelta { id = id, amount = amount };
        }
    }
}
