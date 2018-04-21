﻿using Dopamine.Core.Api.Lyrics;
using Dopamine.Core.Utils;
using Dopamine.Presentation.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dopamine.Presentation.Utils
{
    public static class LyricsUtils
    {
        public static IList<LyricsLineViewModel> ParseLyrics(Lyrics lyrics)
        {
            var linesWithTimestamps = new List<LyricsLineViewModel>();
            var linesWithoutTimestamps = new List<LyricsLineViewModel>();

            var reader = new StringReader(lyrics.Text);

            TimeSpan previousTimestamp = TimeSpan.Zero;
            bool isLyricsStart = true;
            string line;

            while (true)
            {
                // Process 1 line
                line = reader.ReadLine();

                if (line == null)
                {
                    // No line found, we reached the end. Exit while loop.
                    break;
                }

                if (line.Length == 0)
                {
                    if (!isLyricsStart)
                    {
                        // Empty lines, which are not at the start of the lyrics, need special treatment.
                        if (previousTimestamp > TimeSpan.Zero)
                        {
                            linesWithTimestamps.Add(new LyricsLineViewModel(previousTimestamp, string.Empty));
                        }
                        else
                        {
                            linesWithoutTimestamps.Add(new LyricsLineViewModel(string.Empty));
                        }
                    }

                    // Process the next line
                    continue;
                }

                // Check if the line has characters and is enclosed in brackets (starts with [ and ends with ]).
                if (!(line.StartsWith("[") && line.LastIndexOf(']') > 0))
                {
                    // This line is not enclosed in brackets, so it cannot have timestamps.
                    linesWithoutTimestamps.Add(new LyricsLineViewModel(line));
                    previousTimestamp = TimeSpan.Zero;
                    isLyricsStart = false;

                    // Process the next line
                    continue;
                }

                // Check if the line is a tag
                MatchCollection tagMatches = Regex.Matches(line, @"\[[a-z]+?:.*?\]");

                if (tagMatches.Count > 0)
                {
                    // This is a tag: ignore this line and process the next line.
                    continue;
                }

                // Get all substrings between square brackets for this line
                MatchCollection ms = Regex.Matches(line, @"\[.*?\]");
                var timestamps = new List<TimeSpan>();
                bool couldParseAllTimestamps = true;

                // Loop through all matches
                foreach (Match m in ms)
                {
                    var time = TimeSpan.Zero;
                    string subString = m.Value.Trim('[', ']');

                    if (FormatUtils.ParseLyricsTime(subString, out time))
                    {
                        timestamps.Add(time);
                    }
                    else
                    {
                        couldParseAllTimestamps = false;
                    }
                }

                // Check if all timestamps could be parsed
                if (couldParseAllTimestamps)
                {
                    int startIndex = line.LastIndexOf(']') + 1;

                    foreach (TimeSpan timestamp in timestamps)
                    {
                        linesWithTimestamps.Add(new LyricsLineViewModel(timestamp, line.Substring(startIndex)));
                        previousTimestamp = timestamp;
                        isLyricsStart = false;
                    }
                }
                else
                {
                    // The line has mistakes. Consider it as a line without timestamps.
                    linesWithoutTimestamps.Add(new LyricsLineViewModel(line));
                    previousTimestamp = TimeSpan.Zero;
                    isLyricsStart = false;
                }
            }

            // Order the time stamped lines
            linesWithTimestamps = new List<LyricsLineViewModel>(linesWithTimestamps.OrderBy(p => p.Time).ThenByDescending(p => p.Text));

            // Merge both collections, lines with timestamps first.
            linesWithTimestamps.AddRange(linesWithoutTimestamps);

            return linesWithTimestamps;
        }
    }
}