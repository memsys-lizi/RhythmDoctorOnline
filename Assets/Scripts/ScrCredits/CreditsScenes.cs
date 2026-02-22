using System;
using System.Collections.Generic;
using UnityEngine;

namespace Credits
{
    /// <summary>
    /// Builds all credit scenes. Port of animation_scenes.py.
    /// </summary>
    public static class CreditsScenes
    {
        public static Scene[] BuildAll(CreditsCanvas canvas)
        {
            if (canvas == null) return Array.Empty<Scene>();

            return new[]
            {
                Wipe(canvas),
                ClearWipe(canvas),
                Clear(canvas),
                OceanB(canvas),
                OceanC(canvas),
                OceanD(canvas),
                Text(canvas),
                Title(canvas),
                Beats(canvas),
                BeatsLr(canvas),
                BeatsSide(canvas),
                Dates(canvas),
                RedrawUi(canvas),
                Weather(canvas),
                Funding(canvas),
                Loading(canvas),
                QuickLoading(canvas),
                ErrorScreen(canvas),
                FundingDouble(canvas),
                AccessPoints(canvas),
                FundingSingle(canvas),
                FundingDown(canvas),
                Poweroff(canvas)
            };
        }

        private static Scene Wipe(CreditsCanvas c)
        {
            return new Scene("wipe", new[]
            {
                new Generator(0, Generator.Always(), Generator.NoCreate(),
                    (g, b) =>
                    {
                        int amount = (int)Mathf.Pow(b, 1.4f);
                        var chars = new[] { "##", "@@", "  " };
                        var colours = new List<CanvasColor>();
                        for (int i = 0; i < b; i++) colours.Add(CanvasColor.BrightWhite);
                        for (int i = 0; i < 70 - b; i++) colours.Add(CanvasColor.White);
                        for (int i = 0; i < 4 * (40 - b); i++) colours.Add(CanvasColor.BrightBlack);
                        AnimationFunctions.Noise(c, 0, amount, chars, colours.ToArray());
                    }, Generator.NoRequest())
            });
        }

        private static Scene ClearWipe(CreditsCanvas c)
        {
            return new Scene("clear_wipe", new[]
            {
                new Generator(0, Generator.Always(), Generator.NoCreate(),
                    (g, b) => AnimationFunctions.Noise(c, 0, (int)Mathf.Pow(b, 2.2f), new[] { "  " }, new[] { CanvasColor.BrightWhite }),
                    Generator.NoRequest())
            });
        }

        private static Scene Clear(CreditsCanvas c)
        {
            return new Scene("clear", new[]
            {
                new Generator(0, Generator.Always(), g => AnimationFunctions.Clear(c, 0), Generator.NoRequest(), Generator.NoRequest())
            });
        }

        private static Scene OceanB(CreditsCanvas c)
        {
            return new Scene("ocean_b", new[]
            {
                new Generator(0, Generator.EveryNBeats(2),
                    g => g.SetData("ocean", OceanLogic.BeginOcean(), "ocean_glitch", 1, "ocean_col", CanvasColor.BrightBlue),
                    (g, b) => c.SetString(0, new Vector2Int(0, 14),
                        OceanLogic.UpdateOceanSlices(g.GetData<List<System.Text.StringBuilder>>("ocean"), g.GetData("ocean_glitch", 1)),
                        g.GetData<CanvasColor?>("ocean_col") ?? CanvasColor.BrightBlue),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(),
                    g => g.SetData("text", "", "offset", 0),
                    (g, b) => AnimationFunctions.TypeText(c, g, 0, 1, 1, CanvasColor.BrightWhite),
                    Generator.NoRequest())
            });
        }

        private static Scene OceanC(CreditsCanvas c)
        {
            return new Scene("ocean_c", new[]
            {
                new Generator(0, Generator.EveryNBeats(2),
                    g => g.SetData("ocean", OceanLogic.BeginOcean(), "ocean_glitch", 16, "ocean_col", CanvasColor.BrightBlack),
                    (g, b) => c.SetString(0, new Vector2Int(0, 14),
                        OceanLogic.UpdateOceanSlices(g.GetData<List<System.Text.StringBuilder>>("ocean"), g.GetData("ocean_glitch", 16)),
                        g.GetData<CanvasColor?>("ocean_col") ?? CanvasColor.BrightBlack),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(), g => g.SetData("text", "", "offset", 0),
                    (g, b) => AnimationFunctions.TypeText(c, g, 0, 1, 1, CanvasColor.BrightRed, (int)Mathf.Pow(b, 1.143f) % 20 != 0 && (int)Mathf.Pow(b, 1.143f) % 20 != 8 && (int)Mathf.Pow(b, 1.143f) % 20 != 17 && (int)Mathf.Pow(b, 1.143f) % 20 != 15),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(), g => g.SetData("text", "", "offset", 0),
                    (g, b) => AnimationFunctions.TypeText(c, g, 0, 1, 1, CanvasColor.BrightRed, (int)Mathf.Pow(b, 1.2f) % 20 == 0 || (int)Mathf.Pow(b, 1.2f) % 20 == 15),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(), g => g.SetData("text", "", "offset", 0),
                    (g, b) => AnimationFunctions.TypeText(c, g, 0, 1, 1, CanvasColor.BrightRed, (int)Mathf.Pow(b, 1.2f) % 20 == 8),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(), g => g.SetData("text", "", "offset", 0),
                    (g, b) => AnimationFunctions.TypeText(c, g, 0, 1, 1, CanvasColor.BrightRed, (int)Mathf.Pow(b, 1.2f) % 20 == 17),
                    Generator.NoRequest())
            });
        }

        private static Scene OceanD(CreditsCanvas c)
        {
            return new Scene("ocean_d", new[]
            {
                new Generator(0, Generator.Always(),
                    g => g.SetData("ocean", OceanLogic.BeginOcean(), "ocean_glitch", 100, "ocean_col", CanvasColor.Magenta),
                    (g, b) => c.SetString(0, new Vector2Int(0, 14),
                        OceanLogic.UpdateOceanSlices(g.GetData<List<System.Text.StringBuilder>>("ocean"), g.GetData("ocean_glitch", 100)),
                        g.GetData<CanvasColor?>("ocean_col") ?? CanvasColor.Magenta),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(), Generator.NoCreate(),
                    (g, b) =>
                    {
                        var hexLines = new List<string>();
                        for (int i = 0; i < 4; i++)
                        {
                            var line = new List<string>();
                            for (int j = 0; j < 8; j++) line.Add(AnimationFunctions.GenerateRandomHex(4));
                            hexLines.Add(string.Join(" ", line));
                        }
                        AnimationFunctions.SetMultilineString(c, 0, 1, 1,
                            "The system has encountered a fatal error. Please wait.\n\n[ERR: 801]\n\n" + string.Join("\n", hexLines),
                            CanvasColor.BrightRed);
                    }, Generator.NoRequest())
            });
        }

        private static Scene Text(CreditsCanvas c)
        {
            return new Scene("typewrite", new[]
            {
                new Generator(0, Generator.Always(), g => g.SetData("text", "", "offset", 0),
                    (g, b) => AnimationFunctions.TypeText(c, g, 0, 1, 1, CanvasColor.BrightWhite),
                    Generator.NoRequest())
            });
        }

        private static Scene Title(CreditsCanvas c)
        {
            var gens = new List<Generator>
            {
                GenAt(64, (g, b) => c.SetString(0, new Vector2Int(1, 1), "animation | Unity / C#", CanvasColor.BrightCyan)),
                GenAt(80, (g, b) => c.SetString(0, new Vector2Int(1, 2), "game      | RD Online", CanvasColor.BrightCyan)),
                GenAt(96, (g, b) => c.SetString(0, new Vector2Int(1, 3), "dev       | memsyslizi", CanvasColor.BrightCyan)),
                GenAt(112, (g, b) => c.SetString(0, new Vector2Int(1, 4), "assist    | StArray", CanvasColor.BrightCyan)),
                GenAt(120, (g, b) => c.SetString(0, new Vector2Int(1, 5), "open test | 20260201", CanvasColor.Cyan)),
                GenAt(124, (g, b) => c.SetString(0, new Vector2Int(1, 6), "release   | Feb 15", CanvasColor.Cyan)),
                GenAt(128, (g, b) => c.SetString(0, new Vector2Int(1, 7), "bgm       | Frums - Credits", CanvasColor.BrightCyan)),
                GenAt(192, (g, b) => c.SetString(0, new Vector2Int(1, 8), "run C#", CanvasColor.Cyan)),
                GenAt(256, (g, b) => c.SetString(0, new Vector2Int(1, 9), "author: memsyslizi & StArray", CanvasColor.Cyan)),
                GenAt(320, (g, b) => c.SetString(0, new Vector2Int(0, 21), new string('-', 80), CanvasColor.BrightWhite)),
                GenAt(384, (g, b) => c.SetString(0, new Vector2Int(1, 22), "> _ ", CanvasColor.BrightWhite)),
                GenAt(448, (g, b) => AnimationFunctions.SetMultilineString(c, 0, 26, 14, "----------------------------\n" + string.Concat(System.Linq.Enumerable.Repeat("|                          |\n", 6)), CanvasColor.BrightWhite)),
                GenAt(512, (g, b) => AnimationFunctions.SetMultilineString(c, 0, 27, 15, "22.10.2009", CanvasColor.BrightYellow)),
                GenAt(576, (g, b) => AnimationFunctions.SetMultilineString(c, 0, 26, 16, "----------------------------", CanvasColor.BrightWhite)),
                GenAt(640, (g, b) => AnimationFunctions.SetMultilineString(c, 0, 27, 17, "43°F      Wind 13 mph W \n                        ", CanvasColor.BrightCyan)),
                GenAt(704, (g, b) => AnimationFunctions.SetMultilineString(c, 0, 32, 19, "Precipitation \n    20.3%     ", CanvasColor.BrightBlue)),
                GenAt(768, (g, b) => AnimationFunctions.SetMultilineString(c, 0, 36, 15, "Cloudy", CanvasColor.Yellow))
            };
            return new Scene("title", gens.ToArray());
        }

        private static Generator GenAt(int beat, Action<Generator, int> request)
        {
            return new Generator(beat, Generator.AtBeat(beat), Generator.NoCreate(), (g, b) => request(g, b), Generator.NoRequest());
        }

        private static Scene Beats(CreditsCanvas c)
        {
            return new Scene("beats", new[]
            {
                new Generator(0, Generator.CombineConditions(Generator.EveryOnOff(48, 16), Generator.EveryNBeats(4)),
                    g => g.SetData("beat_toggle", true),
                    (g, b) => AnimationFunctions.BeatToggle(c, g, 0, 18, 22, 11, 15, "@@", CanvasColor.BrightYellow),
                    Generator.NoRequest()),
                new Generator(0, Generator.CombineConditions(Generator.EveryOffOn(48, 16), Generator.EveryNBeats(2)),
                    g => g.SetData("beat_toggle", true),
                    (g, b) => AnimationFunctions.BeatToggle(c, g, 0, 18, 22, 11, 15, "##", CanvasColor.BrightGreen),
                    Generator.NoRequest())
            });
        }

        private static Scene BeatsLr(CreditsCanvas c)
        {
            return new Scene("beats_lr", new[]
            {
                new Generator(0, Generator.CombineConditions(Generator.EveryOnOff(48, 16), Generator.EveryNBeats(4)),
                    g => g.SetData("beat_toggle", true),
                    (g, b) => AnimationFunctions.BeatToggle(c, g, 0, 18, 20, 11, 15, "@@", CanvasColor.BrightYellow),
                    Generator.NoRequest()),
                new Generator(0, Generator.CombineConditions(Generator.EveryOffOn(48, 16), Generator.EveryNBeats(2)),
                    g => g.SetData("beat_toggle", true),
                    (g, b) => AnimationFunctions.BeatToggle(c, g, 0, 20, 22, 11, 15, "##", CanvasColor.BrightGreen),
                    Generator.NoRequest())
            });
        }

        private static Scene BeatsSide(CreditsCanvas c)
        {
            return new Scene("beats_side", new[]
            {
                new Generator(0, Generator.CombineConditions(Generator.EveryOnOff(48, 16), Generator.EveryNBeats(4)),
                    g => g.SetData("beat_toggle", true),
                    (g, b) => AnimationFunctions.BeatToggle(c, g, 0, 18, 20, 11, 15, "@@", CanvasColor.BrightYellow),
                    Generator.NoRequest()),
                new Generator(0, Generator.CombineConditions(Generator.EveryOffOn(48, 16), Generator.EveryNBeats(2)),
                    g => g.SetData("beat_toggle", true),
                    (g, b) => AnimationFunctions.BeatToggle(c, g, 0, 20, 22, 11, 15, "##", CanvasColor.BrightGreen),
                    Generator.NoRequest())
            });
        }

        private static Scene Dates(CreditsCanvas c)
        {
            return new Scene("dates", new[]
            {
                new Generator(0, Generator.BeforeN(64 * 8), Generator.NoCreate(),
                    (g, b) => c.SetString(0, new Vector2Int(27, 15), AnimationFunctions.WorkOutDate(b), CanvasColor.BrightYellow),
                    Generator.NoRequest()),
                new Generator(64 * 8, Generator.BeforeN(64 * 12 - 4), Generator.NoCreate(),
                    (g, b) => c.SetString(0, new Vector2Int(27, 15), AnimationFunctions.WorkOutDate(64 * 8 + (b - 64 * 8) * 2), CanvasColor.BrightYellow),
                    Generator.NoRequest()),
                new Generator(64 * 12 - 4, Generator.AtBeat(64 * 12 - 4), Generator.NoCreate(),
                    (g, b) => c.SetString(0, new Vector2Int(27, 15), "??.??.????", CanvasColor.BrightYellow),
                    Generator.NoRequest()),
                new Generator(64 * 12 + 4, Generator.CombineConditions(Generator.EveryNBeats(4), Generator.BeforeN(64 * 16)), Generator.NoCreate(),
                    (g, b) => c.SetString(0, new Vector2Int(27, 15), AnimationFunctions.WorkOutDate(UnityEngine.Random.Range(64 * 12 + b * 8, 64 * 12 + b * 24 + 1)), CanvasColor.BrightYellow),
                    Generator.NoRequest()),
                new Generator(64 * 16, Generator.Always(), Generator.NoCreate(),
                    (g, b) => c.SetString(0, new Vector2Int(27, 15), AnimationFunctions.WorkOutDate(64 * 16 + b * 64 - 64 * 16 * 32), CanvasColor.BrightYellow),
                    Generator.NoRequest())
            });
        }

        private static Scene RedrawUi(CreditsCanvas c)
        {
            return new Scene("redraw_ui", new[]
            {
                GenAt(0, (g, b) => c.SetString(0, new Vector2Int(0, 21), new string('-', 80), CanvasColor.BrightWhite)),
                GenAt(0, (g, b) => c.SetString(0, new Vector2Int(1, 22), "> _ ", CanvasColor.BrightWhite)),
                GenAt(2, (g, b) => AnimationFunctions.SetMultilineString(c, 0, 26, 14, "----------------------------\n" + string.Concat(System.Linq.Enumerable.Repeat("|                          |\n", 6)), CanvasColor.BrightWhite)),
                GenAt(3, (g, b) => AnimationFunctions.SetMultilineString(c, 0, 27, 15, "22.10.2009", CanvasColor.BrightYellow)),
                GenAt(4, (g, b) => AnimationFunctions.SetMultilineString(c, 0, 26, 16, "----------------------------", CanvasColor.BrightWhite)),
                GenAt(5, (g, b) => AnimationFunctions.SetMultilineString(c, 0, 27, 17, "43°F      Wind 13 mph W ", CanvasColor.BrightCyan)),
                GenAt(6, (g, b) => AnimationFunctions.SetMultilineString(c, 0, 32, 19, "Precipitation \n    20.3%     ", CanvasColor.BrightBlue)),
                GenAt(7, (g, b) => AnimationFunctions.SetMultilineString(c, 0, 36, 15, "Cloudy", CanvasColor.Yellow))
            });
        }

        private static Scene Weather(CreditsCanvas c)
        {
            var known = KnownWeathers.Get();
            return new Scene("weather", new[]
            {
                new Generator(0, Generator.CombineConditions(Generator.EveryNBeats(64), Generator.BeforeN(64 * 8)), Generator.NoCreate(),
                    (g, b) => AnimationFunctions.RenderWeather(c, 0, 27, 17, known[Mathf.Min(2, b / 64)], b < 128 ? 0 : 1),
                    Generator.NoRequest()),
                new Generator(64 * 8, Generator.CombineConditions(Generator.EveryNBeats(32), Generator.BeforeN(64 * 12)), Generator.NoCreate(),
                    (g, b) => AnimationFunctions.RenderWeather(c, 0, 27, 17, known[2], 1),
                    Generator.NoRequest()),
                new Generator(64 * 12 - 4, Generator.AtBeat(64 * 12 - 4), Generator.NoCreate(),
                    (g, b) => AnimationFunctions.RenderWeather(c, 0, 27, 17, known[4], 1),
                    Generator.NoRequest()),
                new Generator(64 * 12 + 4, Generator.CombineConditions(Generator.EveryNBeats(4), Generator.BeforeN(64 * 16)), Generator.NoCreate(),
                    (g, b) => AnimationFunctions.RenderWeather(c, 0, 27, 17, known[2], 14),
                    Generator.NoRequest()),
                new Generator(64 * 16, Generator.Always(), Generator.NoCreate(),
                    (g, b) => AnimationFunctions.RenderWeather(c, 0, 27, 17, known[3], 1, (int)Mathf.Max(0, (b - 1080) * 2.2f)),
                    Generator.NoRequest())
            });
        }

        private static Scene Funding(CreditsCanvas c)
        {
            return new Scene("funding", new[]
            {
                new Generator(0, Generator.Always(),
                    g => g.SetData("text", (object)new string[0][], "offset", 0, "lineno", 0, "type_col", CanvasColor.BrightWhite),
                    (g, b) => AnimationFunctions.TypewriteByWord(c, g, 0, 2, 22, g.GetData<CanvasColor?>("type_col") ?? CanvasColor.BrightWhite),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(), Generator.NoCreate(),
                    (g, b) => AnimationFunctions.WriteHistory(c, g, 0, 1, 19, CanvasColor.BrightBlack, 1),
                    Generator.NoRequest())
            });
        }

        private static Scene Loading(CreditsCanvas c)
        {
            return new Scene("loadingbar", new[]
            {
                new Generator(0, Generator.AtBeat(0), g => g.Scene.SetData("progress", 0),
                    (g, b) => AnimationFunctions.SetMultilineString(c, 0, 2, 9, "----------------------------------\n|                                |\n----------------------------------", CanvasColor.BrightWhite),
                    Generator.NoRequest()),
                new Generator(1, Generator.AtBeat(1), g => g.Scene.SetData("progress", 0),
                    (g, b) => AnimationFunctions.SetMultilineString(c, 0, 3, 10, "          Loading...          \n", CanvasColor.BrightBlack),
                    Generator.NoRequest()),
                new Generator(0, Generator.CombineConditions(Generator.BeforeN(240), Generator.EveryNBeats(8)), Generator.NoCreate(),
                    (g, b) => g.Scene.OperData("progress", o => (int)o + 1),
                    Generator.NoRequest()),
                new Generator(240, Generator.Always(), Generator.NoCreate(),
                    (g, b) => g.Scene.OperData("progress", o => (int)o + UnityEngine.Random.Range(40, 71)),
                    Generator.NoRequest()),
                new Generator(0, Generator.BeforeN(240), Generator.NoCreate(),
                    (g, b) => c.SetString(0, new Vector2Int(3, 10), new string('#', g.Scene.GetData("progress", 0)), CanvasColor.BrightYellow),
                    Generator.NoRequest()),
                new Generator(240, Generator.Always(), Generator.NoCreate(),
                    (g, b) => c.SetString(0, new Vector2Int(3, 10), AnimationFunctions.FuckUpText(new string('#', g.Scene.GetData("progress", 0)), 400), CanvasColor.BrightRed),
                    Generator.NoRequest()),
                new Generator(0, Generator.BeforeN(240),
                    g => g.SetData("text", (object)new string[0][], "offset", 0, "lineno", 0, "type_col", CanvasColor.BrightBlack),
                    (g, b) => AnimationFunctions.TypewriteByWord(c, g, 0, 3, 12, g.GetData<CanvasColor?>("type_col") ?? CanvasColor.BrightBlack),
                    Generator.NoRequest())
            });
        }

        private static Scene QuickLoading(CreditsCanvas c)
        {
            return new Scene("fastload", new[]
            {
                new Generator(0, Generator.AtBeat(0), g => g.Scene.SetData("progress", 0),
                    (g, b) => AnimationFunctions.SetMultilineString(c, 0, 2, 9, "----------------------------------\n|                                |\n----------------------------------", CanvasColor.BrightWhite),
                    Generator.NoRequest()),
                new Generator(1, Generator.AtBeat(1), g => g.Scene.SetData("progress", 0),
                    (g, b) => AnimationFunctions.SetMultilineString(c, 0, 3, 10, "        Please wait...        \n", CanvasColor.BrightBlack),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(), Generator.NoCreate(),
                    (g, b) => g.Scene.OperData("progress", o => (int)o + UnityEngine.Random.Range(4, 7)),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(), Generator.NoCreate(),
                    (g, b) => c.SetString(0, new Vector2Int(3, 10), new string('#', g.Scene.GetData("progress", 0)), CanvasColor.BrightGreen),
                    Generator.NoRequest())
            });
        }

        private static Scene ErrorScreen(CreditsCanvas c)
        {
            return new Scene("error", new[]
            {
                new Generator(0, Generator.AtBeat(0), g => g.Scene.SetData("progress", 0),
                    (g, b) => AnimationFunctions.SetMultilineString(c, 0, 0, 14, new string('-', 80) + "\n\n\n\n\n\n\n\n  $ ", CanvasColor.BrightWhite),
                    Generator.NoRequest()),
                new Generator(0, Generator.AtBeat(0), g => g.Scene.SetData("progress", 0),
                    (g, b) => AnimationFunctions.SetMultilineString(c, 0, 1, 1, "Automatic diagnosis unsuccessful. Please wait.\n> ", CanvasColor.BrightBlack),
                    Generator.NoRequest())
            });
        }

        private static Scene FundingDouble(CreditsCanvas c)
        {
            return new Scene("fundingx2", new[]
            {
                new Generator(0, Generator.Always(),
                    g => g.SetData("text", (object)new string[0][], "offset", 0, "lineno", 0, "type_col", CanvasColor.Red),
                    (g, b) => AnimationFunctions.TypewriteByWord(c, g, 0, 2, 2, g.GetData<CanvasColor?>("type_col") ?? CanvasColor.Red),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(),
                    g => g.SetData("text", (object)new string[0][], "offset", 0, "lineno", 0, "type_col", CanvasColor.BrightCyan),
                    (g, b) => AnimationFunctions.TypewriteByWord(c, g, 0, 2, 22, g.GetData<CanvasColor?>("type_col") ?? CanvasColor.BrightCyan, true, "history2"),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(), Generator.NoCreate(),
                    (g, b) => AnimationFunctions.WriteHistory(c, g, 0, 1, 21, CanvasColor.BrightBlack, 15, "history2"),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(),
                    g => g.SetData("text", (object)new string[0][], "offset", 0, "lineno", 0, "type_col", CanvasColor.BrightYellow),
                    (g, b) => AnimationFunctions.TypewriteByWord(c, g, 0, 4, 5, g.GetData<CanvasColor?>("type_col") ?? CanvasColor.BrightYellow, true, "history3"),
                    Generator.NoRequest())
            });
        }

        private static Scene AccessPoints(CreditsCanvas c)
        {
            return new Scene("accesspoints", new[]
            {
                new Generator(0, Generator.CombineConditions(Generator.BeforeN(20), Generator.EveryNBeats(2)), Generator.NoCreate(),
                    (g, b) =>
                    {
                        var sb = new System.Text.StringBuilder();
                        for (int yblock = 0; yblock < 4; yblock++)
                        {
                            for (int yline = 0; yline < 3; yline++)
                            {
                                for (int x = 0; x < 6; x++)
                                {
                                    string part = yline == 1 ? $"PBS {(yblock * 6 + x + 1):D2}" : (yline == 0 ? "  ###  " : "Unknown");
                                    if (UnityEngine.Random.Range(0, Mathf.Max(1, 32 - (int)Mathf.Pow(b, 1.2f))) < 4)
                                        sb.Append(part + "   ");
                                    else
                                        sb.Append("       ");
                                }
                                sb.Append("\n");
                            }
                            sb.Append("\n\n");
                        }
                        AnimationFunctions.SetMultilineString(c, 0, 1, 1, sb.ToString().TrimEnd(), CanvasColor.BrightBlack);
                    }, Generator.NoRequest()),
                new Generator(20, Generator.AtBeat(20), Generator.NoCreate(),
                    (g, b) =>
                    {
                        var sb = new System.Text.StringBuilder();
                        for (int yblock = 0; yblock < 4; yblock++)
                        {
                            for (int yline = 0; yline < 3; yline++)
                            {
                                for (int x = 0; x < 6; x++)
                                    sb.Append((yline == 1 ? $"PBS {(yblock * 6 + x + 1):D2}" : (yline == 0 ? "  ###  " : "Unknown")) + "   ");
                                sb.Append("\n");
                            }
                            sb.Append("\n\n");
                        }
                        AnimationFunctions.SetMultilineString(c, 0, 1, 1, sb.ToString().TrimEnd(), CanvasColor.Red);
                    }, Generator.NoRequest()),
                new Generator(0, Generator.AtBeat(0), Generator.NoCreate(),
                    (g, b) => AnimationFunctions.SetMultilineString(c, 0, 1, 17,
                        "No access points are broadcasting.\nManual search in progress.\nLast search 27.02.2019 (532 days ago)",
                        CanvasColor.BrightBlack),
                    Generator.NoRequest()),
                new Generator(80, Generator.CombineConditions(Generator.EveryNBeats(8), Generator.BeforeN(976)),
                    g => g.SetData("counter", 0, "block", 0),
                    (g, b) => AnimationFunctions.ShowAccessPointVisual(c, g, 0, 1, 1), // x=1,y=1
                    Generator.NoRequest()),
                new Generator(968, Generator.AtBeat(968), Generator.NoCreate(),
                    (g, b) => AnimationFunctions.SetMultilineString(c, 0, 6, 9, "  @@@  \nPBS #14\n Active", CanvasColor.BrightGreen),
                    Generator.NoRequest())
            });
        }

        private static Scene FundingSingle(CreditsCanvas c)
        {
            return new Scene("fdg_single", new[]
            {
                new Generator(0, Generator.AtBeat(0), g => g.Scene.SetData("progress", 0),
                    (g, b) => AnimationFunctions.SetMultilineString(c, 0, 0, 20, new string('-', 80) + "\n  Sending > ", CanvasColor.BrightWhite),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(),
                    g => g.SetData("text", (object)new string[0][], "offset", 0, "lineno", 0, "type_col", CanvasColor.Yellow),
                    (g, b) => AnimationFunctions.TypewriteByWord(c, g, 0, 6, 21, g.GetData<CanvasColor?>("type_col") ?? CanvasColor.Yellow),
                    Generator.NoRequest())
            });
        }

        private static Scene FundingDown(CreditsCanvas c)
        {
            return new Scene("fdg_down", new[]
            {
                new Generator(0, Generator.Always(),
                    g => g.SetData("text", (object)new string[0][], "offset", 0, "lineno", 0, "type_col", CanvasColor.Yellow),
                    (g, b) => AnimationFunctions.TypewriteByWord(c, g, 0, 1, 1 + g.GetData("lineno", 0), g.GetData<CanvasColor?>("type_col") ?? CanvasColor.Yellow),
                    Generator.NoRequest()),
                new Generator(0, Generator.Always(),
                    g => g.SetData("text", (object)new string[0][], "offset", 0, "lineno", 0, "type_col", CanvasColor.Yellow),
                    (g, b) => AnimationFunctions.TypewriteByWord(c, g, 0, 1, 20 + g.GetData("lineno", 0), g.GetData<CanvasColor?>("type_col") ?? CanvasColor.Yellow),
                    Generator.NoRequest())
            });
        }

        private static Scene Poweroff(CreditsCanvas c)
        {
            return new Scene("poweroff", new[]
            {
                new Generator(3, Generator.Always(), Generator.NoCreate(), (g, b) => AnimationFunctions.MakePoweroffBars(c, b - 2, 0, CanvasColor.Black), Generator.NoRequest()),
                new Generator(2, Generator.Always(), Generator.NoCreate(), (g, b) => AnimationFunctions.MakePoweroffBars(c, b - 1, 0, CanvasColor.BrightBlack), Generator.NoRequest()),
                new Generator(1, Generator.Always(), Generator.NoCreate(), (g, b) => AnimationFunctions.MakePoweroffBars(c, b, 0, CanvasColor.White), Generator.NoRequest()),
                new Generator(0, Generator.Always(), Generator.NoCreate(), (g, b) => AnimationFunctions.MakePoweroffBars(c, b + 1, 0, CanvasColor.BrightWhite), Generator.NoRequest())
            });
        }
    }
}
