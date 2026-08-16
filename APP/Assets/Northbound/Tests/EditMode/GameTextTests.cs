using Northbound.UI;
using NUnit.Framework;

namespace Northbound.Tests
{
    public sealed class GameTextTests
    {
        [TearDown]
        public void TearDown() => GameText.Use(GameLanguage.English);

        [Test]
        public void ChineseRuntimePrompts_CoverCharactersObjectsVisitsAndRemainingObjectives()
        {
            GameText.Use(GameLanguage.SimplifiedChinese);

            Assert.That(GameText.Prompt("Begin mission"), Is.EqualTo("[E / 回车] 开始任务"));
            Assert.That(GameText.Prompt("Talk to maya"), Is.EqualTo("[E / 回车] 与玛雅交谈"));
            Assert.That(GameText.Prompt("Carry Photograph"), Is.EqualTo("[E / 回车] 带上照片"));
            Assert.That(GameText.Prompt("Carry Notebook"), Is.EqualTo("[E / 回车] 带上笔记本"));
            Assert.That(GameText.Prompt("Carry House Key"), Is.EqualTo("[E / 回车] 带上家门钥匙"));
            Assert.That(GameText.Prompt("Carry Old Map"), Is.EqualTo("[E / 回车] 带上旧地图"));
            Assert.That(GameText.Prompt("Visit Maya"), Is.EqualTo("[E / 回车] 拜访玛雅"));
            Assert.That(GameText.Prompt("Visit Noah"), Is.EqualTo("[E / 回车] 拜访诺亚"));
            Assert.That(GameText.Prompt("Visit Leo"), Is.EqualTo("[E / 回车] 拜访利奥"));
            Assert.That(GameText.Prompt("Wire the recorder"), Is.EqualTo("[E / 回车] 接好录音机线路"));
            Assert.That(GameText.Prompt("Return the table"), Is.EqualTo("[E / 回车] 收拾最后一桌"));
            Assert.That(GameText.Prompt("Remove the sign"), Is.EqualTo("[E / 回车] 拆下招牌"));
            Assert.That(GameText.Prompt("Count the inventory"), Is.EqualTo("[E / 回车] 清点库存"));
            Assert.That(GameText.Prompt("Find the spare key"), Is.EqualTo("[E / 回车] 找到备用钥匙"));
            Assert.That(GameText.ObjectiveAction("find_socket"), Is.EqualTo("拿起丢失的套筒"));
            Assert.That(GameText.ObjectiveAction("carry_recorder"), Is.EqualTo("拿起录音机"));
            Assert.That(GameText.ObjectiveAction("find_key"), Is.EqualTo("拿起备用钥匙"));
            Assert.That(GameText.Prompt("Inspect the marked object"), Is.EqualTo("[E / 回车] Inspect the marked object"));
        }

        [Test]
        public void EnglishMissionStartPrompt_DisclosesBothPrimaryKeys()
        {
            Assert.That(GameText.Prompt("Begin mission"), Is.EqualTo("[E / ENTER] Begin mission"));
        }

        [Test]
        public void Completion_UsesTheCurrentLanguageWithoutShowingTheControlHint()
        {
            Assert.That(GameText.Completion("Pack the trunk"), Is.EqualTo("Pack the trunk complete"));

            GameText.Use(GameLanguage.SimplifiedChinese);

            Assert.That(GameText.Completion("Pack the trunk"), Is.EqualTo("整理后备箱已完成"));
        }

        [Test]
        public void SaveAndQuit_HasACompleteChineseMenuLabel()
        {
            GameText.Use(GameLanguage.SimplifiedChinese);

            Assert.That(GameText.UiLabel("Save and Quit"), Is.EqualTo("保存并退出游戏"));
        }

        [Test]
        public void FinaleRouteSigns_HaveAccurateChineseDirections()
        {
            GameText.Use(GameLanguage.SimplifiedChinese);

            Assert.That(GameText.Location("Southeast - Northbound Road"), Is.EqualTo("东南 - 向北公路"));
            Assert.That(GameText.Location("Southwest - Home in Greybridge"), Is.EqualTo("西南 - 留在格雷布里奇"));
            Assert.That(GameText.Location("East - Road Without a Map"), Is.EqualTo("向东 - 没有地图的路"));
            Assert.That(GameText.Location("Northeast - Wait Until Morning"), Is.EqualTo("东北 - 等到天亮"));
        }
    }
}
