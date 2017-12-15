﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business {
    public class FfmpegBusiness {
        /// <summary>
        /// Encodes specified file into H264 format. The script file must already be written.
        /// </summary>
        /// <param name="settings">An object containing the encoding settings.</param>
        /// <returns>Whether the operation was completed.</returns>
        public static bool EncodeVideo(MediaEncoderSettings settings) {
            File.Delete(settings.OutputFile);
            string PipeArgs;
            if (settings.VideoCodec == VideoCodecs.x264) {
                PipeArgs = string.Format(@"""{0}avs2yuv.exe"" ""{1}"" -o - | ""{0}ffmpeg.exe"" -y -i - -an -c:v libx264 -psy-rd 1:0.05 -preset {2} -crf {3} ""{4}""",
                    Settings.AviSynthPluginsPath, settings.ScriptFile, settings.EncodePreset, settings.EncodeQuality, settings.OutputFile);
            } else if (settings.VideoCodec == VideoCodecs.x265) {
                PipeArgs = string.Format(@"""{0}avs2yuv.exe"" ""{1}"" -o - | ""{0}ffmpeg.exe"" -y -i - -an -c:v libx265 -preset {2} -crf {3} ""{4}""",
                    Settings.AviSynthPluginsPath, settings.ScriptFile, settings.EncodePreset, settings.EncodeQuality, settings.OutputFile);
            } else { // AVI
                PipeArgs = string.Format(@"""{0}avs2yuv.exe"" ""{1}"" -o - | ""{0}ffmpeg.exe"" -y -i - -an -c:v utvideo ""{4}""",
                    Settings.AviSynthPluginsPath, settings.ScriptFile, settings.EncodePreset, settings.EncodeQuality, settings.OutputFile);
            }
            //}
            return Run("cmd", CommandPipe(PipeArgs), false);
        }

        public static bool RunDeshakerPass(MediaEncoderSettings settings, ExecutingProcessHandler callback) {
            string TempOut = settings.DeshakerLog + ".y4m";
            string Args = string.Format(@"""{0}"" -o - ""{1}""", settings.DeshakerScript, TempOut);
            bool Result = Run(@"Encoder\avs2yuv.exe", Args, true, ProcessPriorityClass.Normal, callback);
            File.Delete(TempOut);
            return Result;
        }

        /// <summary>
        /// Saves the audio output of specified script into a WAV file.
        /// </summary>
        /// <param name="settings">An object containing the encoding settings.</param>
        public static void SaveAudioToWav(MediaEncoderSettings settings, bool silent) {
            string TempFile = settings.TempFile + ".avs";
            AviSynthScriptBuilder Script = new AviSynthScriptBuilder();
            if (settings.VideoCodec != VideoCodecs.Copy) {
                // Read source script.
                Script.Script = File.ReadAllText(settings.ScriptFile);
                // Remote MT code.
                Script.RemoveMT();
                Script.AppendLine("Trim(0,0)");
            } else {
                // Read full video file.
                Script.AddPluginPath();
                if (settings.ConvertToAvi || settings.InputFile.ToLower().EndsWith(".avi"))
                    Script.OpenAvi(settings.InputFile, !string.IsNullOrEmpty(settings.SourceAudioFormat));
                else
                    Script.OpenDirect(settings.InputFile, !string.IsNullOrEmpty(settings.SourceAudioFormat));
                Script.AppendLine("KillVideo()");
            }
            Script.AppendLine();
            // Add audio gain.
            if (settings.AudioGain.HasValue && settings.AudioGain != 0) {
                Script.AppendLine("AmplifydB({0})", settings.AudioGain.Value);
            }
            if (settings.ChangeAudioPitch) {
                // Change pitch to 432hz.
                Script.LoadPluginDll("TimeStretch.dll");
                Script.AppendLine("ResampleAudio(48000)");
                Script.AppendLine("TimeStretchPlugin(pitch = 100.0 * 0.98181819915771484)");
            }
            // Add TWriteWAV.
            Script.AppendLine();
            Script.LoadPluginDll("TWriteAVI.dll");
            Script.AppendLine(@"TWriteWAV(""{0}"", true)", Script.GetAsciiPath(settings.AudioFileWav));
            Script.AppendLine("ForceProcessWAV()");

            // Write temp script.
            Script.WriteToFile(TempFile);
            // Execute. It aways returns an error but file is generated.
            string Args = string.Format(@"""{0}"" -o -", TempFile);
            Run("Encoder\\avs2yuv.exe", Args, silent);
            File.Delete(TempFile);
        }

        public static bool EncodeAudio(MediaEncoderSettings settings, bool silent) {
            if (settings.AudioAction == AudioActions.EncodeOpus) {
                string Args = string.Format(@"--bitrate {0} ""{1}"" ""{2}""", settings.AudioQuality, settings.AudioFileWav, settings.AudioFileOpus);
                return Run("Encoder\\opusenc.exe", Args, silent);
            } else if (settings.AudioAction == AudioActions.EncodeAac || settings.AudioAction == AudioActions.EncodeFlac) {
                string Args = string.Format(@"-i {2}{0} -b:a {1}k {3}",
                    settings.AudioAction == AudioActions.EncodeFlac ? " -c:a flac" : "",
                    settings.AudioQuality,
                    settings.AudioFileWav,
                    settings.AudioAction == AudioActions.EncodeFlac ? settings.AudioFileFlac : settings.AudioFileAac);
                return Run("Encoder\\ffmpeg.exe", Args, silent);
            } else
                return true;
        }

        public static float? GetPixelAspectRatio(MediaEncoderSettings settings) {
            string ConsoleOut = RunToString("Encoder\\ffmpeg.exe", string.Format(@"-i ""{0}""", settings.FilePath));
            int PosStart = ConsoleOut.IndexOf("[SAR ") + 5;
            int PosEnd = ConsoleOut.IndexOf(" DAR ", PosStart);
            if (PosStart < 0 || PosEnd < 0)
                return null;
            string SARText = ConsoleOut.Substring(PosStart, PosEnd - PosStart);
            string[] SAR = SARText.Split(':');
            if (SAR.Length != 2)
                return null;
            try {
                float Result = (float)Math.Round((decimal)int.Parse(SAR[0]) / int.Parse(SAR[1]), 3);
                return Result;
            }
            catch {
                return null;
            }
        }


        /// <summary>
        /// Returns the audio gain that can be applied to an audio file.
        /// </summary>
        /// <param name="settings">The settings pointing to the file to analyze.</param>
        /// <returns>A float value representing the audio gain that can be applied, or null if it failed.</returns>
        public static float? GetAudioGain(MediaEncoderSettings settings) {
            string TempResult = settings.TempFile + ".txt";
            string Args = string.Format(@"""{0}ffmpeg.exe"" -i ""{1}"" -af ""volumedetect"" -f null null > ""{1}"" 2>&1",
                Settings.AviSynthPluginsPath, settings.FilePath, TempResult);
            Run("cmd", CommandPipe(Args), true);
            float? Result = null;
            if (File.Exists(TempResult)) {
                string FileString = File.ReadAllText(TempResult);
                // Find max_volume.
                string SearchVal = "max_volume: ";
                int Pos1 = FileString.IndexOf(SearchVal);
                if (Pos1 >= 0) {
                    Pos1 += SearchVal.Length;
                    // Find end of line.
                    int Pos2 = FileString.IndexOf('\r', Pos1);
                    if (Pos2 >= 0) {
                        string MaxVolString = FileString.Substring(Pos1, Pos2 - Pos1);
                        if (MaxVolString.Length > 3) {
                            // Remove ' dB'
                            MaxVolString = MaxVolString.Substring(0, MaxVolString.Length - 3);
                            float MaxVol = 0;
                            if (float.TryParse(MaxVolString, out MaxVol))
                                Result = Math.Abs(MaxVol);
                        }
                    }
                }
                File.Delete(TempResult);
            }
            return Result;
        }

        public static Rect GetAutoCropRect(MediaEncoderSettings settings, bool silent) {
            string TempScript = settings.TempFile + ".avs";
            string TempResult = settings.TempFile + ".txt";
            string TempOut = settings.TempFile + ".y4m";

            // Create script to get auto-crop coordinates
            AviSynthScriptBuilder Script = new AviSynthScriptBuilder();
            Script.AddPluginPath();
            Script.OpenDirect(settings.FilePath, false);
            Script.LoadPluginDll("RoboCrop26.dll");
            Script.AppendLine(@"RoboCrop(LogFn=""{0}"")", Script.GetAsciiPath(TempResult));
            Script.AppendLine("Trim(0,-1)");
            Script.WriteToFile(TempScript);

            // Run script.
            Run(@"Encoder\avs2yuv.exe", String.Format(@"""{0}"" -o ""{1}""", TempScript, TempOut), true);

            // Read auto crop coordinates
            Rect Result = new Rect();
            if (File.Exists(TempResult)) {
                string[] Values = File.ReadAllText(TempResult).Split(' ');
                if (Values.Length >= 13) {
                    Result.Left = int.Parse(Values[10]);
                    Result.Top = int.Parse(Values[11]);
                    Result.Right = settings.SourceWidth.Value - int.Parse(Values[12]);
                    Result.Bottom = settings.SourceHeight.Value - int.Parse(Values[13]);
                    // Make result conservative, we have to visually see a line of black border to know the right dimensions.
                    if (Result.Left > 0)
                        Result.Left--;
                    if (Result.Top > 0)
                        Result.Top--;
                    if (Result.Right > 0)
                        Result.Right--;
                    if (Result.Bottom > 0)
                        Result.Bottom--;
                }
            }

            // Delete temp files.
            Exception LastError = null;
            for (int i = 0; i < 10; i++) {
                try {
                    File.Delete(TempScript);
                    File.Delete(TempResult);
                    File.Delete(TempOut); // Dummy file that received avs2yuv output.
                    break;
                }
                catch (Exception e) {
                    LastError = e;
                    System.Threading.Thread.Sleep(500);
                }
            }
            if (LastError != null)
                throw LastError;

            return Result;
        }

        /// <summary>
        /// Gets an AviSynth clip information by running a script that outputs the frame count to a file.
        /// </summary>
        /// <param name="source">The AviSynth script to get information for.</param>
        /// <param name="silent">If true, the x264 window will be hidden.</param>
        /// <returns>The clip information.</returns>
        public static ClipInfo GetClipInfo(MediaEncoderSettings settings, string scriptFile, bool silent) {
            string TempScript = settings.TempFile + ".avs";
            string TempResult = settings.TempFile + ".txt";
            string TempOut = settings.TempFile + ".y4m";

            // Read source script and remove MT. Also remove Deshaker if present.
            string FileContent = File.ReadAllText(scriptFile);
            FileContent.Replace(Environment.NewLine + "Deshaker", Environment.NewLine + "#Deshaker");
            AviSynthScriptBuilder Script = new AviSynthScriptBuilder(FileContent);
            Script.RemoveMT();
            //Script.DitherOut(false);

            // Get frame count.
            Script.AppendLine();
            Script.AppendLine(@"WriteFileStart(""{0}"", ""FrameRate""{1}""Framecount"")", TempResult, @", """""" "","" """""", ");
            Script.AppendLine("Trim(0,-1)");
            Script.WriteToFile(TempScript);

            // Run script.
            Run(@"Encoder\avs2yuv.exe", String.Format(@"""{0}"" -o {1}", TempScript, TempOut), true);

            // Read frame count
            ClipInfo Result = null;
            if (File.Exists(TempResult)) {
                string FileString = File.ReadAllText(TempResult);
                string[] FileValues = FileString.Split(',');
                Result = new ClipInfo();
                try {
                    Result.FrameRate = float.Parse(FileValues[0], CultureInfo.InvariantCulture);
                    Result.FrameCount = int.Parse(FileValues[1]);
                }
                catch {
                    Result = null;
                }
            }

            // Delete temp files.
            File.Delete(TempScript);
            File.Delete(TempResult);
            File.Delete(TempOut); // Dummy file that received avs2yuv output.

            return Result;
        }

        /// <summary>
        /// Returns which version of OpenCL the GPU supports.
        /// </summary>
        /// <returns>The version of OpenCL supported by the GPU.</returns>
        public static SupportedOpenClVersion TestSupportedOpenClVersion() {
            if (GpuSupportsOpenCL(true))
                return SupportedOpenClVersion.v12;
            else if (GpuSupportsOpenCL(false))
                return SupportedOpenClVersion.v11;
            else
                return SupportedOpenClVersion.None;
        }

        /// <summary>
        /// Returns whether the GPU supports the latest version of KNLMeans with OpenCL 1.2
        /// </summary>
        /// <param name="supports11">If true, it will instead test whether the GPU supports OpenCL 1.1</param>
        /// <returns>True if OpenCL is supported.</returns>
        private static bool GpuSupportsOpenCL(bool version12) {
            string TempScript = Settings.TempFilesPath + "Temp.avs";
            string TempResult = Settings.TempFilesPath + "Temp.txt";
            string TempOut = Settings.TempFilesPath + "Temp.y4m";

            AviSynthScriptBuilder Script = new AviSynthScriptBuilder();
            Script.AddPluginPath();
            Script.LoadPluginDll(string.Format("KNLMeansCL{0}.dll", version12 ? "" : "-6.11"));
            Script.AppendLine(@"colorbars(pixel_type = ""yv12"").killaudio().trim(1, 1)");
            Script.AppendLine("Result = true");
            Script.AppendLine("try {");
            Script.AppendLine(@"KNLMeansCL(device_type=""GPU"")");
            Script.AppendLine("} catch(error_msg) {");
            Script.AppendLine("Result = false");
            Script.AppendLine("}");
            Script.AppendLine(@"WriteFileStart(""{0}"", string(Result))", TempResult);
            Script.WriteToFile(TempScript);

            // Run script.
            FfmpegBusiness.Run(@"Encoder\avs2yuv.exe", String.Format(@"""{0}"" -o ""{1}""", TempScript, TempOut), true);

            // Read frame count
            bool Result = false;
            if (File.Exists(TempResult)) {
                string FileString = File.ReadAllText(TempResult);
                try {
                    Result = bool.Parse(FileString.TrimEnd());
                }
                catch {
                    Result = false;
                }
            }

            // Delete temp files.
            File.Delete(TempScript);
            File.Delete(TempResult);
            File.Delete(TempOut); // Dummy file that received avs2yuv output.

            return Result;
        }


        public class ClipInfo {
            public float FrameRate;
            public int FrameCount;
        }
    }
}
