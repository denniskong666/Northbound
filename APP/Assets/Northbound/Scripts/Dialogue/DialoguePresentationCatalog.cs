using System;
using System.Collections.Generic;

namespace Northbound.Dialogue
{
    /// <summary>Presentation overrides for legacy lines authored before narration was a first-class type.</summary>
    public static class DialoguePresentationCatalog
    {
        private static readonly HashSet<string> NarrationLines = CreateNarrationLines();

        public static bool IsNarration(string dialogueId, int lineIndex, DialogueLine line)
        {
            if (line == null)
            {
                return false;
            }

            return line.presentation == DialoguePresentation.Narration ||
                string.IsNullOrWhiteSpace(line.speakerId) ||
                string.Equals(line.speakerId, "Narrator", StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(line.text) &&
                 line.text.StartsWith("How should Jamie", StringComparison.OrdinalIgnoreCase)) ||
                NarrationLines.Contains(Key(dialogueId, lineIndex));
        }

        private static HashSet<string> CreateNarrationLines()
        {
            var lines = new HashSet<string>(StringComparer.Ordinal);
            Add(lines, "alternator_dialogue", 4, 5, 6, 7, 8);
            Add(lines, "before_morning_dialogue", 0, 1, 2, 3, 4, 6, 8);
            Add(lines, "chapter_two_rooftop", 7, 8);
            Add(lines, "clock_in_dialogue", 3, 4);
            AddRange(lines, "dead_air_dialogue", 3, 7);
            AddRange(lines, "ending_home_high", 0, 4);
            AddRange(lines, "ending_home_low", 0, 4);
            Add(lines, "ending_leo", 2, 3, 4);
            AddRange(lines, "ending_no_map_house_key", 0, 4);
            AddRange(lines, "ending_no_map_map", 0, 4);
            AddRange(lines, "ending_no_map_notebook", 0, 4);
            AddRange(lines, "ending_no_map_photo", 0, 4);
            Add(lines, "ending_noah", 2, 3, 4);
            Add(lines, "ending_northbound_high", 3, 4);
            AddRange(lines, "ending_northbound_low", 1, 4);
            Add(lines, "ending_pause_journey", 0, 2);
            Add(lines, "farewell_leo", 2);
            Add(lines, "farewell_maya", 2);
            Add(lines, "farewell_noah", 2);
            AddRange(lines, "finale_are_you_coming", 1, 7);
            AddRange(lines, "first_light_dialogue", 4, 7);
            AddRange(lines, "highlight_leo", 2, 8);
            AddRange(lines, "highlight_maya", 2, 8);
            AddRange(lines, "highlight_noah", 2, 8);
            Add(lines, "last_night_open_dialogue", 6, 8);
            AddRange(lines, "last_sign_dialogue", 8, 11);
            Add(lines, "missed_alternator", 0, 2, 3, 4);
            Add(lines, "missed_first_light", 0, 2, 3, 4);
            Add(lines, "missed_last_night_open", 0, 2, 3, 4);
            AddRange(lines, "missed_pack_trunk", 0, 4);
            Add(lines, "missed_road_test", 0, 2, 3, 4);
            Add(lines, "missed_static", 0, 2, 3, 4);
            AddRange(lines, "missing_socket_dialogue", 4, 8);
            Add(lines, "npc_market", 2, 3);
            AddRange(lines, "npc_rooftop", 0, 4);
            Add(lines, "npc_ruth", 2, 3);
            AddRange(lines, "one_more_table_dialogue", 3, 7);
            Add(lines, "optional_elias_garage", 2);
            Add(lines, "optional_leo_diner", 2);
            Add(lines, "optional_maya_mural", 2);
            Add(lines, "optional_noah_radio", 2);
            Add(lines, "pack_trunk_dialogue", 0, 3, 4, 5, 6, 7);
            AddRange(lines, "parts_future_dialogue", 3, 7);
            AddRange(lines, "return_to_title", 0, 4);
            AddRange(lines, "road_test_dialogue", 4, 8);
            Add(lines, "rooftop_decision", 0, 1, 2, 10);
            Add(lines, "rooftop_fracture", 8);
            Add(lines, "rooftop_inventory_dialogue", 3, 4, 5);
            AddRange(lines, "spare_key_dialogue", 4, 8);
            Add(lines, "static_dialogue", 4, 5, 8);
            AddRange(lines, "things_we_leave_dialogue", 0, 8);
            return lines;
        }

        private static void Add(HashSet<string> lines, string dialogueId, params int[] indices)
        {
            foreach (var index in indices)
            {
                lines.Add(Key(dialogueId, index));
            }
        }

        private static void AddRange(HashSet<string> lines, string dialogueId, int first, int last)
        {
            for (var index = first; index <= last; index++)
            {
                lines.Add(Key(dialogueId, index));
            }
        }

        private static string Key(string dialogueId, int lineIndex) => $"{dialogueId ?? string.Empty}:{lineIndex}";
    }
}
