using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Northbound.Cinematics;
using Northbound.Content;
using Northbound.Dialogue;
using Northbound.Endings;
using Northbound.Quests;
using UnityEditor;
using UnityEngine;

namespace Northbound.EditorTools
{
    /// <summary>Rebuilds the approved, reviewable narrative assets from the locked story specification.</summary>
    public static class NarrativeContentSeeder
    {
        private const string DataRoot = "Assets/Northbound/Data";
        private const string ResourceRoot = "Assets/Northbound/Resources/Northbound";

        private sealed class QuestSpec
        {
            public readonly string id, title, hint, objective, dialogue, pair;
            public readonly string[] facts, next;
            public QuestSpec(string id, string title, string hint, string objective, string dialogue, string pair = "", string[] facts = null, string[] next = null)
            { this.id = id; this.title = title; this.hint = hint; this.objective = objective; this.dialogue = dialogue; this.pair = pair; this.facts = facts ?? Array.Empty<string>(); this.next = next ?? Array.Empty<string>(); }
        }

        private sealed class DialogueSpec
        {
            public readonly string id, kind;
            public readonly string[] speakers, lines;
            public DialogueSpec(string id, string kind, string[] speakers, string[] lines) { this.id = id; this.kind = kind; this.speakers = speakers; this.lines = lines; }
        }

        [MenuItem("Northbound/Rebuild Approved Narrative Content")]
        public static void RebuildApprovedContent()
        {
            EnsureFolder("Assets/Northbound/Resources");
            EnsureFolder(ResourceRoot);
            EnsureFolder(DataRoot + "/Quests");
            EnsureFolder(DataRoot + "/Dialogue");
            EnsureFolder(DataRoot + "/Endings");
            EnsureFolder("Assets/Northbound/Prefabs/Characters");
            EnsureFolder("Assets/Northbound/Prefabs/Triggers");

            var quests = QuestSpecs();
            var dialogues = DialogueSpecs();
            var questAssets = quests.Select(CreateQuest).ToArray();
            var dialogueAssets = dialogues.Select(CreateDialogue).ToArray();
            var endingAssets = EndingSpecs().Select(CreateEnding).ToArray();
            var cinematicAssets = new[]
            {
                Load<CinematicAsset>(DataRoot + "/Cinematics/Opening.asset"), Load<CinematicAsset>(DataRoot + "/Cinematics/MayaHighlight.asset"),
                Load<CinematicAsset>(DataRoot + "/Cinematics/NoahHighlight.asset"), Load<CinematicAsset>(DataRoot + "/Cinematics/LeoHighlight.asset"),
                Load<CinematicAsset>(DataRoot + "/Cinematics/Rooftop.asset"), Load<CinematicAsset>(DataRoot + "/Cinematics/Finale.asset")
            }.Where(asset => asset != null).ToArray();
            foreach (var cinematic in cinematicAssets)
            {
                var dialogue = dialogueAssets.FirstOrDefault(asset => asset.id == CinematicDialogue(cinematic.id));
                cinematic.subtitleCues = dialogue?.lines.Select((line, index) => new CinematicSubtitleCue
                {
                    startSeconds = index * 5f,
                    text = line.text
                }).ToArray() ?? Array.Empty<CinematicSubtitleCue>();
                EditorUtility.SetDirty(cinematic);
            }
            var manifest = BuildManifest(quests, dialogues, cinematicAssets);
            WriteManifest(manifest);
            CreateCatalog(questAssets, dialogueAssets, cinematicAssets, endingAssets, manifest.triggers.Select(trigger => trigger.id).ToArray());
            CreateCharacterPrefabs();
            CreateTriggerPrefabs(manifest.triggers);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static QuestAsset CreateQuest(QuestSpec spec)
        {
            var asset = LoadOrCreate<QuestAsset>(QuestAssetPath(spec.id));
            asset.id = spec.id;
            asset.title = spec.title;
            asset.hint = spec.hint;
            asset.objectives = ObjectiveStepsFor(spec).Select((id, index) => new QuestObjective { id = id, description = index == 0 ? spec.hint : $"Continue: {spec.hint}", requiredAmount = 1 }).ToList();
            asset.completionFacts = spec.facts;
            asset.nextQuestIds = spec.next;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static DialogueAsset CreateDialogue(DialogueSpec spec)
        {
            var asset = LoadOrCreate<DialogueAsset>($"{DataRoot}/Dialogue/{Pascal(spec.id)}.asset");
            asset.id = spec.id;
            if (MeaningfulStoryDialogueSeeder.TryBuild(asset, spec.id))
            {
                EditorUtility.SetDirty(asset);
                return asset;
            }

            asset.lines = new List<DialogueLine>();
            for (var index = 0; index < spec.lines.Length; index++)
            {
                asset.lines.Add(new DialogueLine { speakerId = spec.speakers[Mathf.Min(index, spec.speakers.Length - 1)], text = spec.lines[index] });
            }
            AddJamieResponseChoices(asset, spec.id);
            var target = spec.kind is "required" or "cinematic" ? 9 : spec.kind == "ending" ? 5 : 5;
            foreach (var line in AuthoredConnectiveLines(spec.id).Take(Math.Max(0, target - asset.lines.Count)))
                asset.lines.Add(new DialogueLine { speakerId = spec.speakers[Mathf.Min(spec.speakers.Length - 1, 0)], text = line });
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void AddJamieResponseChoices(DialogueAsset asset, string id)
        {
            if (!id.StartsWith("optional_") && !id.StartsWith("farewell_")) return;
            var responseStart = asset.lines.Count + 1;
            var labels = JamieToneChoiceLabels(id);
            asset.lines.Add(new DialogueLine
            {
                speakerId = "Jamie",
                text = JamieTonePrompt(id),
                textChinese = JamieTonePromptChinese(id),
                presentation = DialoguePresentation.Narration,
                choices = new List<DialogueChoice>
                {
                    new DialogueChoice { text = labels[0], textChinese = labels[1], grantedFact = $"{id}_committed", nextLineIndex = responseStart },
                    new DialogueChoice { text = labels[2], textChinese = labels[3], grantedFact = $"{id}_curious", nextLineIndex = responseStart + 1 },
                    new DialogueChoice { text = labels[4], textChinese = labels[5], grantedFact = $"{id}_uncertain", nextLineIndex = responseStart + 2 },
                    new DialogueChoice { text = labels[6], textChinese = labels[7], grantedFact = $"{id}_silent", nextLineIndex = responseStart + 3 }
                }
            });
            var speaker = asset.lines.FirstOrDefault(line => line != null && line.speakerId != "Jamie")?.speakerId ?? "Friend";
            var responseLines = new List<DialogueLine>();
            var responses = JamieToneResponses(id);
            var responsesChinese = JamieToneResponsesChinese(id);
            for (var index = 0; index < responses.Length; index++)
            {
                var line = new DialogueLine { speakerId = speaker, text = responses[index], textChinese = responsesChinese[index] };
                responseLines.Add(line);
                asset.lines.Add(line);
            }
            var branchExit = asset.lines.Count;
            foreach (var line in responseLines) line.nextLineIndex = branchExit;
        }

        private static string[] JamieToneResponses(string id) => id switch
        {
            "optional_elias_garage" => new[] { "Then help me check the bolts; I need a job I can finish.", "Ask me why I keep every receipt. I might answer.", "You do not have to promise me Friday to stand here now.", "I will give the silence a minute before I call it agreement." },
            "optional_maya_mural" or "farewell_maya" => new[] { "Then bring paint, not a speech.", "I do, but I am tired of being brave for an audience.", "Uncertainty has better colors than certainty.", "I can work beside quiet. I cannot work beside pretending." },
            "optional_noah_radio" or "farewell_noah" => new[] { "Stay for the next recording; the train comes through at six.", "I do, and that is why the answer scares me.", "I am trying to let an unfinished answer exist.", "I will leave a space on the tape for what you did not say." },
            _ => new[] { "Then help me turn the chairs over when the last customer leaves.", "I do; that is why I keep making jokes around it.", "Not knowing is still more useful than faking a map.", "Okay. I will not make the quiet carry all the weight." }
        };

        private static string JamieTonePrompt(string id) => id switch
        {
            "optional_elias_garage" => "How should Jamie answer Elias in the garage?",
            "optional_maya_mural" => "How should Jamie answer Maya at the mural?",
            "optional_noah_radio" => "How should Jamie answer Noah beside the radio?",
            "optional_leo_diner" => "How should Jamie answer Leo at the diner?",
            "farewell_maya" => "How should Jamie answer Maya before morning?",
            "farewell_noah" => "How should Jamie answer Noah before morning?",
            _ => "How should Jamie answer Leo before morning?"
        };

        private static string JamieTonePromptChinese(string id) => id switch
        {
            "optional_elias_garage" => "在车库里，杰米该怎么回答伊莱亚斯？",
            "optional_maya_mural" => "在壁画前，杰米该怎么回答玛雅？",
            "optional_noah_radio" => "在收音机旁，杰米该怎么回答诺亚？",
            "optional_leo_diner" => "在餐馆里，杰米该怎么回答利奥？",
            "farewell_maya" => "天亮以前，杰米该怎么回答玛雅？",
            "farewell_noah" => "天亮以前，杰米该怎么回答诺亚？",
            _ => "天亮以前，杰米该怎么回答利奥？"
        };

        private static string[] JamieToneChoiceLabels(string id) => id switch
        {
            "optional_elias_garage" => new[]
            {
                "I'll stay and help you finish the car.", "我会留下来，帮你把车修完。",
                "When did the plan become something you had to carry alone?", "这个计划从什么时候起，变成了只能由你一个人扛的东西？",
                "I care about you. I still don't know whether Friday is mine.", "我在乎你，但我仍不知道星期五是不是也属于我。",
                "Set the wrench beside him without answering.", "不作回答，只把扳手放到他手边。"
            },
            "optional_maya_mural" => new[]
            {
                "I'll help repaint the arrow, even if it points somewhere new.", "我会帮你重画那支箭，哪怕它最后指向别处。",
                "Do you want the studio, or permission to want it?", "你想要的是那间工作室，还是允许自己想要它？",
                "I don't know where I belong yet. I want to see what you paint.", "我还不知道自己属于哪里，但我想看看你接下来会画什么。",
                "Hold the ladder and let Maya keep working.", "扶住梯子，让玛雅继续画下去。"
            },
            "optional_noah_radio" => new[]
            {
                "I'll stay for the six o'clock recording.", "我会留下来，陪你录下六点的声音。",
                "Whose voice do you want on the next tape, yours or theirs?", "下一盘磁带里，你想留下谁的声音：你的，还是他们的？",
                "I don't know my answer either. We can record that.", "我也不知道自己的答案。我们可以把这份不知道录下来。",
                "Put on the spare headphones and listen.", "戴上那副备用耳机，安静地听。"
            },
            "optional_leo_diner" => new[]
            {
                "I'll help close the diner when the last customer leaves.", "最后一位客人走后，我会陪你一起关店。",
                "Do you want to stay, or are you protecting everyone else from goodbye?", "你是真的想留下，还是只想替大家挡住这场告别？",
                "I don't know where I'll wake up next week.", "我也不知道下周会在哪里醒来。",
                "Turn one chair over beside him.", "不说话，只在他旁边翻起一把椅子。"
            },
            "farewell_maya" => new[]
            {
                "If I leave, I'll come back to see what the mural becomes.", "如果我离开，我也会回来看看这幅壁画最后变成什么。",
                "Do you want me to stay, or just choose for myself?", "你希望我留下，还是只希望我真正替自己选择？",
                "I still don't know which road is mine.", "我仍然不知道哪一条路才属于我。",
                "Take the brush and add one quiet stroke.", "接过画笔，安静地添上一笔。"
            },
            "farewell_noah" => new[]
            {
                "I'll listen when your first broadcast goes live.", "你的第一次广播开始时，我一定会听。",
                "Do you want the station more than the road north?", "比起北上的路，你是不是更想要那间电台？",
                "I don't know where I'm going, but I want to hear your answer.", "我不知道自己会去哪里，但我想听见你的答案。",
                "Put on the spare headphones beside him.", "在他身边戴上那副备用耳机。"
            },
            _ => new[]
            {
                "Whatever I choose, I'll come back for the good mug.", "无论我怎么选，我都会回来拿那只完好的杯子。",
                "Do you want the diner, or are you afraid to leave Ruth?", "你想要的是这家餐馆，还是你害怕离开露丝？",
                "I don't know whether this is goodbye.", "我还不知道这算不算告别。",
                "Help him stack the last chair without speaking.", "不说话，帮他叠好最后一把椅子。"
            }
        };

        private static string[] JamieToneResponsesChinese(string id) => id switch
        {
            "optional_elias_garage" => new[] { "那就帮我检查螺栓吧。我需要一件能够真正做完的事。", "问问我为什么留着每张收据。也许我会回答。", "你不必向我承诺星期五，才能站在这里陪我。", "我会先给这份沉默一分钟，再决定它算不算同意。" },
            "optional_maya_mural" or "farewell_maya" => new[] { "那就带颜料来，别带一篇演讲。", "我想。但我已经厌倦了为了观众装得勇敢。", "不确定，往往比确定拥有更多颜色。", "我可以和沉默并肩工作，但不能和伪装一起。" },
            "optional_noah_radio" or "farewell_noah" => new[] { "留下来录下一段吧，火车六点会经过这里。", "我想。正因为想，所以这个答案才让我害怕。", "我正在学着接受一个还没写完的答案。", "我会在磁带上留一段空白，放你没有说出口的话。" },
            _ => new[] { "那最后一位客人走后，帮我把椅子翻到桌上。", "我想。正因为想，我才总绕着它开玩笑。", "不知道，也比假装手里有地图更有用。", "好吧。我不会让沉默替我们扛下所有重量。" }
        };

        // These are scene-specific spoken beats, reviewed as authored dialogue rather than runtime filler.
        private static string[] AuthoredConnectiveLines(string id) => id switch
        {
            "clock_in_dialogue" => new[] { "Leo slips Ruth a clean receipt and calls it a retirement plan.", "Ruth points at the travel-fund jar, then at the coffee getting cold.", "Jamie carries soup past the window where the closed depot reflects the diner light.", "Leo says every tip is one mile north, then quietly adds, 'or one more day here.'", "The bell rings again before anyone can answer." },
            "missing_socket_dialogue" => new[] { "Elias marks Friday on the garage calendar with a grease pencil.", "Maya wipes a black thumbprint from the map before he sees it.", "The battery coughs once, then the dash lights wake in a thin blue line.", "Elias smiles like the sound has kept a promise to him.", "Jamie pockets the spare socket instead of throwing it back in the drawer." },
            "parts_future_dialogue" => new[] { "Brooks saves the fan belt beneath a FINAL WEEK sign.", "Noah finds the fuses by listening for the old shop radio.", "Ruth produces the toolbox from under a stack of pie tins, exactly as predicted.", "Each errand makes Greybridge feel less like a map and more like a list of people.", "Jamie returns to the garage with their arms full." },
            "rooftop_inventory_dialogue" => new[] { "Noah counts coins twice and writes the smaller number down.", "Maya traces the painted skyline on the childhood map with one finger.", "Leo contributes a crumpled bill and refuses to say where it came from.", "The northern ridge glows beyond the factory roof, beautiful at this distance.", "Someone below turns off a storefront sign." },
            "last_sign_dialogue" => new[] { "The sign is heavier than it looks, its paint worn smooth by winter hands.", "Maya photographs the empty hooks after the wood comes down.", "Maya smooths the creased Greybridge Arts Center flyer against the wall.", "Her thumb pauses over the word INVITED. Jamie lets the silence hold the question.", "They carry the board toward the mural wall." },
            "dead_air_dialogue" => new[] { "Noah labels each sound in careful block letters.", "The recorder catches the diner bell through an open window.", "He rewinds to the rooftop wind and lets it play too long.", "Jamie notices the radio application under a coil of wire.", "Noah turns the page face down, but not away." },
            "one_more_table_dialogue" => new[] { "The table leg scrapes the pavement like it remembers every move.", "Ruth checks the underside and finds Leo's grandmother's initials.", "Leo says the suitcase is for morale, then looks at the empty zipper.", "Jamie steadies the table while Leo takes the long way back inside.", "The diner smells like onions and rain." },
            "chapter_two_rooftop" => new[] { "Elias redraws Friday in darker marker after everyone has gone quiet.", "Maya watches the ink bleed into the old calendar square.", "Noah folds his hands around the recorder instead of speaking.", "Leo starts a joke, then leaves it unfinished.", "The rooftop wind lifts the corner of the map." },
            "alternator_dialogue" => new[] { "Elias passes Jamie a wrench without looking up from the engine.", "A postcard from his brother is taped above the workbench, edges curled.", "He says North taught his brother to breathe; Jamie hears the years inside it.", "The alternator clicks into place with a sound too small for the pressure around it.", "Elias keeps working after the light fades." },
            "first_light_dialogue" => Array.Empty<string>(),
            "road_test_dialogue" => new[] { "The wagon stalls halfway down the service road.", "Jamie and Elias push until the tires find pavement again.", "Elias laughs once, then sounds surprised it came out.", "Mud gathers under Jamie's shoes as the engine catches.", "They drive back slower than they left." },
            "static_dialogue" => Array.Empty<string>(),
            "pack_trunk_dialogue" => new[] { "The childhood box will fit only if another thing stays behind.", "Maya's painting rests against the tire, still wet at one corner.", "Noah tests the recorder's red light before wrapping it in a shirt.", "Elias measures the empty space twice and calls it practical.", "Jamie closes the trunk only after choosing." },
            "last_night_open_dialogue" => Array.Empty<string>(),
            "rooftop_fracture" => new[] { "The chalk arrow has faded until it points nowhere in particular.", "Elias looks at the car keys, not at any of them.", "Maya's voice shakes only after she has finished speaking.", "Noah puts the recorder down so both hands are free.", "Elias leaves the map pinned under a loose brick." },
            "things_we_leave_dialogue" => new[] { "The photograph shows five faces leaning into a summer that has already happened.", "The notebook has one blank first page and no instructions.", "The house key is warm from Jamie's palm.", "The old map still has every childhood signature on its reverse.", "Jamie closes the drawer on the objects not chosen.", "A bus ticket from years ago falls out of the map and stays on the floor.", "The pocket is small, which makes the decision feel less like a speech." },
            "spare_key_dialogue" => new[] { "The second key is newer than the others and still smells of cut metal.", "Elias says nothing about the missing three keys.", "The engine ticks as it cools behind them.", "Jamie sets the key down between the map and the receipt jar.", "Neither of them calls it an ultimatum." },
            "before_morning_dialogue" => new[] { "Maya's studio light is on across the street.", "A radio signal leaks softly from the Vale shop.", "Leo has stacked the diner chairs but not locked the side door.", "The road north is clear, and so is the road no one named.", "Morning has not made the decision easier.", "Jamie can visit each friend without turning the visit into a vote.", "Dawn waits at the edge of every rooftop window." },
            "prologue_opening" => new[] { "Young hands sign the back of the map in pen that will later fade.", "The blackout makes the distant city look close enough to touch.", "Five friends lean together against the cold rooftop wall.", "The camera cuts to the same map pinned in the garage years later.", "A board beneath it reads FIVE DAYS." },
            "missed_first_light" => new[] { "Blue tape still marks the place where Maya's largest canvas hung.", "The coordinator has swept the gallery floor but not the paint flecks.", "Jamie reads her name on the program after the room has emptied." },
            "missed_alternator" => new[] { "The tarp rises and falls with the garage fan.", "Elias has written torque numbers on the wall in white chalk.", "A single glove lies beside the alternator bracket." },
            "missed_static" => new[] { "The radio case is back beneath the shop counter.", "Noah's application envelope has been opened and resealed.", "Static spills from a speaker with no one there to answer." },
            "missed_road_test" => new[] { "Fresh mud stops at the garage threshold.", "The passenger seat belt is twisted from someone riding alone.", "A cooling engine gives the service road no explanation." },
            "missed_last_night_open" => new[] { "The chalkboard menu still lists soup that will not be served.", "Ruth's keys are gone from their hook behind the counter.", "Leo's joke is written on a napkin and never delivered." },
            "missed_pack_trunk" => new[] { "The trunk latch is shut around a decision Jamie did not help make.", "A recorder cable trails from the garage floor to nowhere.", "The painting leans against a tire, safe but not chosen." },
            "highlight_maya" => new[] { "Maya stands beneath the gallery lights and lets the silence belong to the paintings.", "A child points at the shuttered market on one canvas.", "Maya hands Jamie the studio key without claiming she will use it.", "The coordinator leaves the room so the paintings can be looked at without an explanation.", "Maya notices the old map in Jamie's bag and smiles without forgiving it.", "Outside, the market sign becomes a dark blue shape in the window.", "She says the studio can be a place to start, not a place to hide." },
            "highlight_noah" => new[] { "Noah puts on the headphones before he enters the booth.", "The first thing on the tape is rain on the diner awning.", "He speaks his own name into the microphone and does not apologize.", "His father hears the signal through the shop radio but does not interrupt it.", "Noah lowers the volume until the station sounds like a room, not an escape hatch.", "Jamie hears the garage wrench click between the train and the wind.", "The recording ends with Noah breathing into the microphone on purpose." },
            "highlight_leo" => new[] { "Leo keeps one diner light on after the chairs are stacked.", "He rubs the CLOSED sign clean with his sleeve.", "The blank reverse side makes him laugh without hiding the sadness.", "Ruth leaves him the keys long enough to lock the door himself.", "Leo sets one mug aside for his grandmother and does not make a joke about it.", "The fryer goes quiet, making the room feel larger.", "He turns the sign over slowly, as if the word needs time to land." },
            "finale_are_you_coming" => new[] { "The station wagon idles while the five friends keep separate distances.", "The second key catches the first edge of dawn.", "No marker points toward the road nobody named.", "Maya waits near the arts center with paint on her sleeve.", "Noah stands between the shop and the radio station, listening to both.", "Leo leans against the dark diner door with the sign turned blank.", "Elias asks once and lets the engine fill the space afterward." },
            "ending_northbound_high" => new[] { "Jamie hears the engine before closing the passenger door.", "Elias waits until Jamie has taken the key before putting the car in gear.", "The ridge fills the windshield, still beautiful and still unknown.", "Neither of them calls the other one certain." },
            "ending_northbound_low" => new[] { "The car rolls north without deciding what the memory means.", "Elias grips the wheel as if recognition could make the road safer.", "Jamie sees the old map folded in the glove compartment.", "Greybridge shrinks in the rear window without becoming unreal." },
            "ending_home_high" => new[] { "The garage light reaches the old map on the wall.", "Jamie clears a space on the workbench beside the travel-fund jar.", "The wagon stays parked, repaired but not made into a verdict.", "From the open door, the neighborhood looks like work that can begin." },
            "ending_home_low" => new[] { "A bus passes without stopping, and Jamie watches it go.", "The empty bench is cold through Jamie's jacket.", "There is no speech to make the waiting sound planned.", "Jamie stays until the first shop light comes on." },
            "ending_no_map_notebook" => new[] { "The ink blots once, then the page accepts the date.", "Jamie writes nothing else, leaving room beneath it.", "The unmarked road has gravel, weeds, and no promise of an audience.", "The notebook closes only after Jamie has started walking." },
            "ending_no_map_map" => new[] { "The folded map stays in Jamie's pocket, neither obeyed nor discarded.", "Its creases keep the rooftop signatures together.", "The unmarked road bends before any city lights appear.", "Jamie walks until the chalk arrow is out of sight." },
            "ending_maya" => new[] { "Fresh paint leaves the old chalk arrow visible underneath.", "Maya hands Jamie the roller and chooses a color without asking permission.", "The mural gains a shape that does not need to point north.", "They step back together and leave an edge unfinished." },
            "ending_noah" => new[] { "Greybridge's train, bell, and wind arrive together through the headphones.", "Noah adjusts the level until the garage tools stop swallowing the birds.", "Jamie hears a town becoming material instead of a command.", "The ON AIR light turns red for the first time." },
            "ending_leo" => new[] { "Leo turns the blank side of the sign toward the street.", "Jamie sets a chair upright beneath the diner window.", "The kitchen light is small enough to feel like a question, not a rescue.", "Leo writes nothing on the sign yet." },
            "npc_ruth" => new[] { "Ruth dries the same mug twice before handing it to Jamie.", "She says the diner has survived worse than a hard week." },
            "npc_market" => new[] { "The owner straightens a clearance label that refuses to stay flat.", "A crate of bruised apples waits by the door." },
            "npc_rooftop" => new[] { "The factory windows hold a broken reflection of the ridge.", "Jamie can still see every old signature on the map.", "The folding chair has rusted through at one corner.", "Below, Greybridge wakes without asking what Jamie will call it." },
            "return_to_title" => new[] { "The street sounds fade, but the directions remain.", "Northbound returns to the title without judging the road taken.", "The map rests where the story began.", "Press any key when ready to begin again." },
            _ => new[] { "The moment ends without asking anyone to name it." }
        };

        private static EndingAsset CreateEnding(EndingSpec spec)
        {
            var asset = LoadOrCreate<EndingAsset>($"{DataRoot}/Endings/{Pascal(spec.id)}.asset");
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("id").stringValue = spec.id;
            serialized.FindProperty("endCard").stringValue = spec.card;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void CreateCatalog(QuestAsset[] quests, DialogueAsset[] dialogues, CinematicAsset[] cinematics, EndingAsset[] endings, string[] triggerIds)
        {
            var catalog = LoadOrCreate<NarrativeContentCatalog>($"{ResourceRoot}/NarrativeContentCatalog.asset");
            catalog.quests = quests;
            catalog.dialogues = dialogues;
            catalog.cinematics = cinematics;
            catalog.endings = endings;
            catalog.triggerIds = triggerIds;
            catalog.characterIds = new[] { "jamie", "elias", "maya", "noah", "leo" };
            catalog.characterPrefabs = new[] { "Jamie", "Elias", "Maya", "Noah", "Leo" }
                .Select(name => Load<GameObject>($"Assets/Northbound/Prefabs/Characters/{name}.prefab")).Where(prefab => prefab != null).ToArray();
            EditorUtility.SetDirty(catalog);
        }

        private static void CreateCharacterPrefabs()
        {
            foreach (var pair in new[] { ("Elias", "elias", "optional_elias_garage"), ("Maya", "maya", "optional_maya_mural"), ("Noah", "noah", "optional_noah_radio"), ("Leo", "leo", "optional_leo_diner") })
            {
                var character = new GameObject(pair.Item1);
                var collider = character.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                character.AddComponent<NarrativeCharacterInteractor>().Configure(pair.Item2, pair.Item3);
                PrefabUtility.SaveAsPrefabAsset(character, $"Assets/Northbound/Prefabs/Characters/{pair.Item1}.prefab");
                UnityEngine.Object.DestroyImmediate(character);
            }
        }

        private static void CreateTriggerPrefabs(IEnumerable<ContentTrigger> triggers)
        {
            foreach (var trigger in triggers)
            {
                var route = new GameObject(Pascal(trigger.id));
                var collider = route.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                route.AddComponent<NarrativeRouteTrigger>().Configure(trigger.id, PromptFor(trigger), null);
                PrefabUtility.SaveAsPrefabAsset(route, $"Assets/Northbound/Prefabs/Triggers/{Pascal(trigger.id)}.prefab");
                UnityEngine.Object.DestroyImmediate(route);
            }
        }

        private static NarrativeContentManifest BuildManifest(IReadOnlyCollection<QuestSpec> quests, IReadOnlyCollection<DialogueSpec> dialogues, IEnumerable<CinematicAsset> cinematics)
        {
            var chapterOrder = new[] { "prologue", "chapter_1", "chapter_2", "chapter_3_day_3", "chapter_3_day_2", "chapter_4", "finale" };
            var missionTriggers = quests.Select(spec => new ContentTrigger { id = spec.id + "_trigger", routeType = "quest", targetId = spec.id, chapterId = ChapterFor(spec.id), prerequisiteFacts = FactsFor(spec.id), phase = "play" });
            var conversationTriggers = dialogues.Where(spec => spec.id.StartsWith("optional_") || spec.id.StartsWith("farewell_") || spec.id.StartsWith("missed_"))
                .Select(spec => new ContentTrigger { id = spec.id + "_trigger", routeType = "dialogue", targetId = spec.id, chapterId = ChapterForDialogue(spec.id), prerequisiteFacts = FactsForDialogue(spec.id), phase = "optional" });
            var cinematicTriggers = cinematics.Select(asset => new ContentTrigger { id = asset.id + "_cinematic_trigger", routeType = "cinematic", targetId = asset.id, chapterId = ChapterForCinematic(asset.id), phase = "automatic" });
            return new NarrativeContentManifest
            {
                chapters = chapterOrder.Select((id, index) => new ContentChapter { id = id, nextId = index + 1 < chapterOrder.Length ? chapterOrder[index + 1] : "", requiredQuestIds = RequiredForChapter(id) }).ToArray(),
                quests = quests.Select(spec => new ContentQuest { id = spec.id, chapterId = ChapterFor(spec.id), pairId = spec.pair, dialogueId = spec.dialogue, triggerId = spec.id + "_trigger", prerequisiteQuestIds = PrerequisitesFor(spec.id), prerequisiteFacts = FactsFor(spec.id), completionMode = "physical", minigameId = MinigameFor(spec.id), completionFacts = spec.facts, nextQuestIds = spec.next }).ToArray(),
                dialogues = dialogues.Select(spec => new ContentDialogue { id = spec.id, kind = spec.kind }).ToArray(),
                triggers = missionTriggers.Concat(conversationTriggers).Concat(cinematicTriggers).ToArray(),
                cinematics = cinematics.Select(asset => new ContentCinematic { id = asset.id, completionFact = asset.completionFact, dialogueId = CinematicDialogue(asset.id), subtitleCues = asset.subtitleCues.Select(cue => $"{TimeSpan.FromSeconds(cue.startSeconds):mm\\:ss} {cue.text}").ToArray() }).ToArray(),
                facts = FactIds().Select(id => new ContentFact { id = id }).ToArray(),
                endings = EndingSpecs().Select(spec => new ContentEnding { id = spec.id, dialogueIds = EndingDialogueIds(spec.id) }).ToArray()
                ,characters = new[] { "jamie", "elias", "maya", "noah", "leo" }.Select(id => new ContentCharacter { id = id, prefabId = id }).ToArray()
            };
        }

        private static string ChapterFor(string id) => id switch
        {
            "clock_in" or "missing_socket" or "parts_future" or "rooftop_inventory" => "chapter_1",
            "last_sign" or "dead_air" or "one_more_table" => "chapter_2",
            "alternator" or "first_light" => "chapter_3_day_3",
            "road_test" or "static" or "pack_trunk" or "last_night_open" => "chapter_3_day_2",
            "things_we_leave" or "spare_key" or "before_morning" => "chapter_4",
            _ => "prologue"
        };
        private static string[] PrerequisitesFor(string id) => id switch
        {
            "missing_socket" => new[] { "clock_in" }, "parts_future" => new[] { "missing_socket" }, "rooftop_inventory" => new[] { "parts_future" },
            "last_sign" => new[] { "rooftop_inventory" }, "dead_air" => new[] { "last_sign" }, "one_more_table" => new[] { "dead_air" },
            "alternator" or "first_light" => new[] { "one_more_table" }, "road_test" or "static" => new[] { "alternator", "first_light" },
            "pack_trunk" or "last_night_open" => new[] { "road_test", "static" }, "things_we_leave" => new[] { "pack_trunk", "last_night_open" },
            "spare_key" => new[] { "things_we_leave" }, "before_morning" => new[] { "spare_key" }, _ => Array.Empty<string>()
        };
        private static string[] FactsFor(string id) => Array.Empty<string>();
        private static string MinigameFor(string id) => id is "clock_in" or "last_night_open" ? "diner_shift" : id == "dead_air" ? "wiring_game" : id == "pack_trunk" ? "trunk_packing" : string.Empty;
        private static string ChapterForDialogue(string id) => id.StartsWith("farewell_") ? "chapter_4" : id.StartsWith("missed_") ? "chapter_3_day_2" : id.StartsWith("optional_") ? "chapter_2" : "chapter_1";
        private static string[] FactsForDialogue(string id) => id.StartsWith("missed_") ? new[] { id } : Array.Empty<string>();
        private static string ChapterForCinematic(string id) => id == "opening" ? "prologue" : id == "maya" ? "chapter_3_day_3" : id == "finale" ? "finale" : "chapter_3_day_2";
        private static string CinematicDialogue(string id) => id == "opening" ? "prologue_opening" : id == "rooftop" ? "rooftop_fracture" : id == "finale" ? "finale_are_you_coming" : $"highlight_{id}";
        private static string[] CinematicCues(string id) => id switch
        {
            "opening" => new[] { "00:00 See those lights?", "00:04 That's a water tower.", "00:08 Tonight, it's north.", "00:12 What's there?", "00:16 Whatever isn't here.", "00:20 Do they have better fries?", "00:24 Statistically, probably.", "00:28 Then it's settled. We save up, fix a car, and we all go.", "00:38 All of us?", "00:42 All of us." },
            "rooftop" => new[] { "00:00 We made one promise.", "00:08 We were twelve.", "00:16 We were honest.", "00:24 We were scared.", "00:34 Maybe growth is admitting the plan doesn't fit.", "00:48 That's the problem, Eli. You started fixing us too." },
            "finale" => new[] { "00:00 The engine starts.", "00:12 The second key rests on the roof.", "00:28 Are you coming?" },
            "maya" => new[] { "00:00 The gallery opens its door.", "00:14 The paintings hold Greybridge in blue.", "00:34 The studio key waits on the table." },
            "noah" => new[] { "00:00 The radio tower is up the hill.", "00:16 Noah carries the recorder.", "00:36 For today, that is far enough." },
            _ => new[] { "00:00 The diner bell rings once.", "00:15 Leo turns the sign over.", "00:36 Tomorrow gets a turn." }
        };

        private static string[] EndingDialogueIds(string id) => id switch
        {
            "northbound" => new[] { "ending_northbound_high", "ending_northbound_low" },
            "home_chosen" => new[] { "ending_home_high", "ending_home_low" },
            "no_map" => new[] { "ending_no_map_photo", "ending_no_map_notebook", "ending_no_map_house_key", "ending_no_map_map" },
            "pause_journey" => new[] { "ending_pause_journey" },
            "not_alone_maya" => new[] { "ending_maya" },
            "not_alone_noah" => new[] { "ending_noah" },
            "not_alone_leo" => new[] { "ending_leo" },
            _ => Array.Empty<string>()
        };

        private static void WriteManifest(NarrativeContentManifest manifest)
        {
            var json = JsonUtility.ToJson(manifest, true);
            File.WriteAllText($"{DataRoot}/content-manifest.json", json);
            File.WriteAllText($"{ResourceRoot}/content-manifest.json", json);
        }

        private static string[] RequiredForChapter(string chapter) => chapter switch
        {
            "chapter_1" => new[] { "clock_in", "missing_socket", "parts_future", "rooftop_inventory" },
            "chapter_2" => new[] { "last_sign", "dead_air", "one_more_table" },
            "chapter_4" => new[] { "things_we_leave", "spare_key", "before_morning" },
            _ => Array.Empty<string>()
        };

        private static string PromptFor(ContentTrigger trigger) => trigger.routeType == "quest" ? "Begin mission" : trigger.routeType == "cinematic" ? "Watch memory" : "Talk";
        private static string QuestAssetPath(string id) => id == "alternator" ? $"{DataRoot}/Quests/EliasAlternator.asset" : id == "first_light" ? $"{DataRoot}/Quests/MayaFirstLight.asset" : $"{DataRoot}/Quests/{Pascal(id)}.asset";
        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject { var asset = Load<T>(path); if (asset != null) return asset; asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset; }
        private static void EnsureFolder(string path) { if (AssetDatabase.IsValidFolder(path)) return; var parent = Path.GetDirectoryName(path)?.Replace("\\", "/"); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path)); }
        private static string Pascal(string id) => string.Concat((id ?? "").Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));

        private sealed class EndingSpec { public readonly string id, card; public EndingSpec(string id, string card) { this.id = id; this.card = card; } }
        private static EndingSpec[] EndingSpecs() => new[]
        {
            new EndingSpec("northbound", "Some promises carry us forward. Some ask us how long we are willing to be carried."),
            new EndingSpec("home_chosen", "Staying is not the absence of a journey when staying is a choice."),
            new EndingSpec("no_map", "Not every road begins with a destination."),
            new EndingSpec("pause_journey", "A pause can be a direction when it is chosen with care.")
        };

        private static string[] FactIds() => new[]
        {
            "helped_elias", "helped_maya", "helped_noah", "helped_leo", "attended_maya_exhibition", "completed_road_test", "packed_trunk", "packed_noah_recorder",
            "carried_photo", "carried_notebook", "carried_house_key", "carried_old_map", "missed_alternator", "missed_first_light", "missed_road_test", "missed_static",
            "missed_pack_trunk", "missed_last_night_open", "cinematic_opening_complete", "cinematic_maya_complete", "cinematic_noah_complete", "cinematic_leo_complete",
            "cinematic_rooftop_complete", "cinematic_finale_complete", "earned_travel_fund", "battery_fitted", "parts_collected", "four_days", "maya_invited",
            "noah_recordings_heard", "leo_grandmother_table", "promise", "car_progress", "connection", "two_keys", "farewell_complete",
            "choice_ch1_clock_northbound", "choice_ch1_clock_balance", "choice_ch1_clock_home", "choice_ch1_rooftop_northbound", "choice_ch1_rooftop_balance",
            "choice_ch1_rooftop_home", "story_mark_ch1_a", "story_mark_ch1_b", "story_mark_ch1_c", "story_mark_ch2_a", "story_mark_ch2_b", "story_mark_ch2_c",
            "story_mark_ch3_a", "story_mark_ch3_b", "story_mark_ch3_c", "story_mark_ch4_a", "story_mark_ch4_b", "story_mark_ch4_c",
            "optional_elias_garage_committed", "optional_elias_garage_curious", "optional_elias_garage_uncertain", "optional_elias_garage_silent",
            "optional_maya_mural_committed", "optional_maya_mural_curious", "optional_maya_mural_uncertain", "optional_maya_mural_silent",
            "optional_noah_radio_committed", "optional_noah_radio_curious", "optional_noah_radio_uncertain", "optional_noah_radio_silent",
            "optional_leo_diner_committed", "optional_leo_diner_curious", "optional_leo_diner_uncertain", "optional_leo_diner_silent",
            "farewell_maya_committed", "farewell_maya_curious", "farewell_maya_uncertain", "farewell_maya_silent",
            "farewell_noah_committed", "farewell_noah_curious", "farewell_noah_uncertain", "farewell_noah_silent",
            "farewell_leo_committed", "farewell_leo_curious", "farewell_leo_uncertain", "farewell_leo_silent"
        };

        private static string[] ObjectiveStepsFor(QuestSpec spec) => spec.id switch
        {
            "missing_socket" => new[] { "find_socket", "fit_battery" },
            "parts_future" => new[] { "collect_belt", "collect_fuses", "collect_toolbox" },
            "first_light" => new[] { "hang_painting", "set_lights", "open_exhibition" },
            "alternator" => new[] { "lift_alternator", "connect_belt", "test_charge" },
            "road_test" => new[] { "drive_service_road", "push_wagon", "return_garage" },
            "static" => new[] { "carry_recorder", "deliver_radio_case" },
            "things_we_leave" => new[] { "choose_carried_object" },
            "before_morning" => new[] { "visit_maya", "visit_noah", "visit_leo" },
            _ => new[] { spec.objective }
        };

        private static QuestSpec[] QuestSpecs() => new[]
        {
            new QuestSpec("clock_in", "Clock In", "Help Ruth through the diner shift.", "serve_tables", "clock_in_dialogue", facts: new[] { "earned_travel_fund" }, next: new[] { "missing_socket" }),
            new QuestSpec("missing_socket", "The Missing Socket", "Find the wrench and fit the battery.", "fit_battery", "missing_socket_dialogue", facts: new[] { "battery_fitted" }, next: new[] { "parts_future" }),
            new QuestSpec("parts_future", "Parts of a Future", "Collect the belt, fuses, and toolbox.", "collect_parts", "parts_future_dialogue", facts: new[] { "parts_collected" }, next: new[] { "rooftop_inventory" }),
            new QuestSpec("rooftop_inventory", "Rooftop Inventory", "Count money and parts with the group.", "count_inventory", "rooftop_inventory_dialogue", facts: new[] { "four_days" }, next: new[] { "last_sign" }),
            new QuestSpec("last_sign", "The Last Sign", "Help Maya take down Brooks Market's sign.", "remove_sign", "last_sign_dialogue", facts: new[] { "maya_invited" }, next: new[] { "dead_air" }),
            new QuestSpec("dead_air", "Dead Air", "Repair Noah's recorder wiring.", "wire_recorder", "dead_air_dialogue", facts: new[] { "noah_recordings_heard" }, next: new[] { "one_more_table" }),
            new QuestSpec("one_more_table", "One More Table", "Return the old diner table.", "return_table", "one_more_table_dialogue", facts: new[] { "leo_grandmother_table" }, next: new[] { "alternator", "first_light" }),
            new QuestSpec("alternator", "The Alternator", "Stay at the garage and install the alternator.", "install_alternator", "alternator_dialogue", "alternator|first_light", new[] { "helped_elias", "promise", "car_progress" }, new[] { "road_test", "static" }),
            new QuestSpec("first_light", "First Light", "Attend Maya's exhibition before it closes.", "attend_exhibition", "first_light_dialogue", "alternator|first_light", new[] { "helped_maya", "attended_maya_exhibition", "connection" }, new[] { "road_test", "static" }),
            new QuestSpec("road_test", "Road Test", "Push the station wagon through its first road test.", "push_wagon", "road_test_dialogue", "road_test|static", new[] { "helped_elias", "completed_road_test", "promise" }, new[] { "pack_trunk", "last_night_open" }),
            new QuestSpec("static", "Static", "Help Noah carry his radio equipment.", "carry_equipment", "static_dialogue", "road_test|static", new[] { "helped_noah", "connection" }, new[] { "pack_trunk", "last_night_open" }),
            new QuestSpec("pack_trunk", "Pack the Trunk", "Choose what fits in the station wagon.", "pack_trunk", "pack_trunk_dialogue", "pack_trunk|last_night_open", new[] { "packed_trunk", "promise" }, new[] { "things_we_leave" }),
            new QuestSpec("last_night_open", "Last Night Open", "Help Leo close Ruth's Diner.", "close_diner", "last_night_open_dialogue", "pack_trunk|last_night_open", new[] { "helped_leo", "connection" }, new[] { "things_we_leave" }),
            new QuestSpec("things_we_leave", "Things We Leave", "Choose one thing to carry into morning.", "choose_object", "things_we_leave_dialogue", facts: Array.Empty<string>(), next: new[] { "spare_key" }),
            new QuestSpec("spare_key", "The Spare Key", "Speak with Elias at the garage.", "find_key", "spare_key_dialogue", facts: new[] { "two_keys" }, next: new[] { "before_morning" }),
            new QuestSpec("before_morning", "Before Morning", "Visit the people who are still here.", "visit_friends", "before_morning_dialogue", facts: new[] { "farewell_complete" })
        };

        private static DialogueSpec[] DialogueSpecs() => new[]
        {
            D("prologue_opening", "cinematic", "Young Elias|Young Maya|Young Elias|Young Jamie|Young Elias|Young Leo|Young Noah|Young Elias|Young Maya|Young Elias", "See those lights?|That's a water tower.|Tonight, it's north.|What's there?|Whatever isn't here.|Do they have better fries?|Statistically, probably.|Then it's settled. We save up, fix a car, and we all go.|All of us?|All of us."),
            D("clock_in_dialogue", "required", "Leo|Jamie|Leo|Ruth", "Five more shifts and I retire forever.|You've worked here for three weeks.|Exactly. I've given this place my youth.|Then give table four their fries before you leave it all behind."),
            D("missing_socket_dialogue", "required", "Elias|Maya|Elias|Elias", "Friday. Six in the morning. No speeches, no delays.|You just gave a speech.|That was a schedule.|The battery is in. Hear that? The car is learning our names."),
            D("parts_future_dialogue", "required", "Jamie|Noah|Maya", "The market still has the belt. The shop has the fuses. Ruth says the toolbox is probably under a pie tin.|That sounds plausible.|Everything in Greybridge is under something it shouldn't be."),
            D("rooftop_inventory_dialogue", "required", "Elias|Leo|Maya|Elias", "Next week, this place will be behind us.|You say that like the place can hear you.|It can. It just doesn't care.|Four days. We are closer than we've ever been."),
            D("last_sign_dialogue", "required", "Jamie|Maya|Jamie|Maya|Maya", "I found the Greybridge Arts Center's local exhibition invitation behind the sign. You didn't tell them.|There's nothing to tell.|They invited your paintings, Maya.|One local exhibition isn't a whole future.|It is a room with a light on. That might be enough for one night."),
            D("dead_air_dialogue", "required", "Jamie|Noah|Noah", "Why record things you want to leave?|Because leaving and forgetting aren't the same.|Listen: train, bell, wrenches, wind. It sounds small when you put it together."),
            D("one_more_table_dialogue", "required", "Leo|Jamie|Leo", "This table has survived three owners, two floods, and my grandmother's opinions.|Your suitcase is empty.|Packing is a state of mind. Mine is currently evasive."),
            D("chapter_two_rooftop", "required", "Elias|Maya|Elias|Maya|Elias", "If we keep moving the date, it stops being a plan.|Maybe plans are allowed to move.|People say that when they're scared.|Maybe they are scared. That doesn't make them wrong.|Three days. Friday is still Friday."),
            D("alternator_dialogue", "required", "Elias|Jamie|Elias|Jamie", "He said the first night away was the first time he could breathe.|Does he still call?|He's busy.|That wasn't my question."),
            D("first_light_dialogue", "required", "Maya|Jamie|Maya|Maya|||||Maya", "I thought painting this place meant I couldn't escape it.|And now?|Now I think leaving is easier than looking at it.|The Greybridge Arts Center coordinator said the upstairs studio is mine if tonight's exhibition opens. I hate that I want it.|Rolled canvases lean beneath a row of unlit gallery lamps.|Three empty hooks wait on the worn brick wall.|An extension cable stops just short of the final lamp.|Outside, the exhibition's first visitor checks the still-locked door.|Help me open it before I find an excuse not to."),
            D("road_test_dialogue", "required", "Elias|Jamie|Elias|Elias", "Everyone loves the dream. Nobody wants the weight.|Maybe it isn't their dream anymore.|Then they should have said that before I carried it for them.|Push on three. The car doesn't care whose side we're on."),
            D("static_dialogue", "required", "Father|Noah|Father|Noah|||Noah|Jamie|", "A hobby doesn't keep the lights on.|Neither does pretending I chose this.|You're walking away from your family.|I'm trying to find out who's walking.|The equipment case sits open between them, still missing the recorder and its cables.|Noah's father leaves the shop doorway clear, but does not step aside.|The radio booth closes in an hour.|Then let's carry what you chose.|The recorder and microphone case wait on separate repair benches."),
            D("pack_trunk_dialogue", "required", "Jamie|Elias|Elias", "There is room for tools, a box, a painting, a recorder, and a bag. There is not room for all of it.|Then pick what survives the road.|No. Pick what you can live without. That's different."),
            D("last_night_open_dialogue", "required", "Leo|Jamie|Leo|Jamie|Leo|Leo||Leo|", "I kept saying I'd leave first.|Why?|Because if I joked about leaving, nobody could accuse me of being too scared to go.|Are you scared?|Of leaving? Yeah. Of staying? Also yeah. That's the annoying part.|Do you think choosing a place means it gets to keep you?|The last customers are still eating. Every chair remains down, and the OPEN side of the sign faces the street.|If you mean it, help me give them one ordinary last hour.|Ruth leaves the closing keys beside the bell and says nothing."),
            D("rooftop_fracture", "cinematic", "Elias|Maya|Elias|Maya|Elias|Noah|Elias|Leo", "We made one promise.|We were twelve.|We were honest.|We were scared.|And now you're all pretending fear is growth.|Maybe growth is admitting the plan doesn't fit.|It fit when I was fixing everything.|That's the problem, Eli. You started fixing us too."),
            D("rooftop_decision", "required", "Jamie", "What should the old promise mean now?"),
            D("things_we_leave_dialogue", "required", "Jamie|Narrator", "A photograph, a blank notebook, a house key, and the old map wait on the bed.|One object fits in the pocket. The others remain where they have always been."),
            D("spare_key_dialogue", "required", "Jamie|Elias|Jamie|Elias", "Why only two?|Because I'm done begging people to want their own future.|Their future—or yours?|You really sound like them now."),
            D("before_morning_dialogue", "required", "Jamie|Narrator", "The street is quiet enough to hear the engine cool.|There is time to visit, but not to make anybody else's decision."),
            D("farewell_maya", "farewell", "Maya", "If you go, don't go because he remembers an older version of you.|The mural can change without pretending the old arrow was never there."),
            D("farewell_noah", "farewell", "Noah", "I kept waiting to feel certain. I don't think certainty comes first.|The station has one spare set of headphones. That feels like a fact, at least."),
            D("farewell_leo", "farewell", "Leo", "Whatever you do, don't turn it into proof that the rest of us were wrong.|Also, if you leave, take the good diner mug. The chipped one is haunted."),
            D("optional_elias_garage", "optional", "Elias", "I know how this looks. Like I made a list and called it love.|I did make a list. I just thought everyone was writing on the same page."),
            D("optional_maya_mural", "optional", "Maya", "That drawing is not evidence. It's a twelve-year-old trying to keep four loud people in one frame.|I used to think changing my mind made the first idea a lie."),
            D("optional_noah_radio", "optional", "Noah", "My dad has a plan for me. Elias has a plan for me. It's strange how everyone's plan sounds like responsibility when they say it out loud.|I am tired of answering with a nod."),
            D("optional_leo_diner", "optional", "Leo", "I hate what this place does to people.|That isn't the same as hating the people. Took me long enough."),
            D("missed_first_light", "missed", "Maya", "The exhibition door is locked now. There is a brushstroke of blue beneath the handle.|I was there, even if you weren't."),
            D("missed_alternator", "missed", "Elias", "The garage is dark. The alternator sits under a tarp beside a work light.|I can finish a car alone. That isn't the same as wanting to."),
            D("missed_static", "missed", "Noah", "The equipment has been returned to the shop. The recorder's red light is off.|I heard myself on the application. I almost didn't recognize the voice."),
            D("missed_road_test", "missed", "Elias", "The wagon has fresh mud on one tire and no new stories in it.|It ran. I guess that was supposed to settle something."),
            D("missed_last_night_open", "missed", "Leo", "The diner is dark before closing time. A chair waits upside down on the old table.|Nobody likes a quiet goodbye."),
            D("missed_pack_trunk", "missed", "Jamie", "The trunk is packed. Something important has been left on the floor.|Every container tells a story about what it cannot hold."),
            D("highlight_maya", "cinematic", "Maya", "The room is nearly empty, which is somehow worse and better.|You looked at it anyway. That counts."),
            D("highlight_noah", "cinematic", "Noah", "The radio tower is not north. It is only up the hill.|For today, that is far enough."),
            D("highlight_leo", "cinematic", "Leo", "Last customer. Last fries. Last bell. I don't know what comes after last.|Maybe tomorrow gets a turn."),
            D("finale_are_you_coming", "cinematic", "Elias", "Are you coming?"),
            D("ending_northbound_high", "ending", "Elias|Jamie|Elias", "Ready?|No.|Good. Me neither."),
            D("ending_northbound_low", "ending", "Elias", "I knew you'd remember."),
            D("ending_home_high", "ending", "Jamie", "A light comes on in the garage. It is small, but it is deliberate."),
            D("ending_home_low", "ending", "Jamie", "The bus stop has no answer posted. Jamie sits anyway."),
            D("ending_no_map_notebook", "ending", "Jamie", "Jamie writes the date in the blank notebook, then walks where no arrow points."),
            D("ending_no_map_map", "ending", "Jamie", "Jamie folds the old map and keeps it. The road does not require it to be destroyed."),
            D("ending_no_map_photo", "ending", "Jamie", "Jamie holds the photograph to sunrise until the faces lose their glare.|The people in it remain part of the road without deciding its direction.|A truck passes without asking where Jamie is headed.|The photograph returns to a pocket warmed by walking."),
            D("ending_no_map_house_key", "ending", "Jamie", "Jamie keeps the house key where it can still open a door later.|Leaving the road unmarked does not mean locking every way back.|The key is not a chain when Jamie decides what it opens.|Morning finds the key beside an unfinished route."),
            D("ending_pause_journey", "ending", "Jamie|Narrator", "Jamie does not leave tonight.|The road remains open, but the choice is finally Jamie's.|Morning can begin without pretending it has already been decided."),
            D("npc_ruth", "optional", "Ruth", "A town is not a machine. It does not stop just because somebody leaves.|Take care of your people. Then take care of yourself."),
            D("npc_market", "optional", "Market Owner", "FINAL WEEK is not the same thing as final day.|People keep confusing the two."),
            D("npc_rooftop", "optional", "Jamie", "The chalk arrow still points north. The wind does not."),
            D("return_to_title", "ending", "Narrator", "Northbound. Return to Title.")
        };

        private static DialogueSpec D(string id, string kind, string speakers, string lines) => new DialogueSpec(id, kind, speakers.Split('|'), lines.Split('|'));
    }
}
