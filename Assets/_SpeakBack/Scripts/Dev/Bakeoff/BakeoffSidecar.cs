using System;
using System.Globalization;
using System.Text;
using Uprise.SpeakBack.Audio;
using Uprise.SpeakBack.Core.Audio;
using Uprise.SpeakBack.Core.Content;

namespace Uprise.SpeakBack.Dev.Bakeoff
{
    /// <summary>Device and build facts recorded with each take; no personal device names.</summary>
    public sealed class BakeoffEnvironment
    {
        /// <summary>Creates the environment record.</summary>
        public BakeoffEnvironment(string deviceModel, string operatingSystem, string appVersion, string unityVersion)
        {
            DeviceModel = deviceModel;
            OperatingSystem = operatingSystem;
            AppVersion = appVersion;
            UnityVersion = unityVersion;
        }

        /// <summary>Hardware model (SystemInfo.deviceModel), not the user-set device name.</summary>
        public string DeviceModel { get; }

        /// <summary>OS description.</summary>
        public string OperatingSystem { get; }

        /// <summary>App version.</summary>
        public string AppVersion { get; }

        /// <summary>Unity version.</summary>
        public string UnityVersion { get; }
    }

    /// <summary>
    /// Builds the JSON sidecar for one kept take (schema "bakeoff-take-v1"), as read by P05's ingest step.
    /// Contains only pseudonymous labels, IDs and numbers: no names, contact details, transcripts or free text.
    /// </summary>
    public static class BakeoffSidecar
    {
        /// <summary>Schema identifier written into every sidecar.</summary>
        public const string Schema = "bakeoff-take-v1";

        /// <summary>Serialises the sidecar.</summary>
        public static string Build(
            string takeId, string sessionId, PhraseInfo phrase, BakeoffParticipant participant, BakeoffEnvironment environment,
            CaptureDiagnostics capture, AudioProcessResult processed, ProcessorConfig config, DateTime utcNow)
        {
            var d = processed.Diagnostics;
            var json = new StringBuilder();
            json.Append("{\n");
            Field(json, 1, "schema", Schema);
            Field(json, 1, "takeId", takeId);
            Field(json, 1, "sessionId", sessionId);
            Field(json, 1, "phraseId", phrase.Id);
            Field(json, 1, "phraseContentVersion", phrase.ContentVersion);
            Field(json, 1, "speakerCode", participant.SpeakerCode);
            Open(json, 1, "groupLabels");
            Field(json, 2, "accent", participant.Accent);
            Field(json, 2, "voice", participant.Voice, last: true);
            Close(json, 1);
            Field(json, 1, "consentRef", participant.ConsentRef);
            Field(json, 1, "deviceModel", environment.DeviceModel);
            Field(json, 1, "operatingSystem", environment.OperatingSystem);
            Field(json, 1, "appVersion", environment.AppVersion);
            Field(json, 1, "unityVersion", environment.UnityVersion);
            Field(json, 1, "utc", utcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
            Open(json, 1, "files");
            Field(json, 2, "flipped16k", takeId + ".flipped16k.wav");
            Field(json, 2, "raw", takeId + ".raw.wav", last: true);
            Close(json, 1);
            Open(json, 1, "capture");
            Field(json, 2, "micDevice", capture?.DeviceName ?? string.Empty);
            Field(json, 2, "requestedRate", capture?.RequestedRate ?? 0);
            Field(json, 2, "grantedRate", capture?.GrantedRate ?? 0);
            Field(json, 2, "channels", capture?.Channels ?? 0);
            Field(json, 2, "startLatencyMs", capture?.StartLatencyMilliseconds ?? -1.0);
            Field(json, 2, "framesCaptured", capture?.FramesCaptured ?? 0);
            Field(json, 2, "autoStopped", capture != null && capture.AutoStopped);
            Field(json, 2, "captureSeconds", d.CaptureSeconds, last: true);
            Close(json, 1);
            Open(json, 1, "durations");
            Field(json, 2, "rawSeconds", processed.RawAttempt.DurationSeconds);
            Field(json, 2, "flippedSeconds", processed.FlippedAttempt.DurationSeconds);
            Field(json, 2, "scorerInputSeconds", processed.ScorerInput.DurationSeconds);
            Field(json, 2, "scorerInputRate", processed.ScorerInput.SampleRate, last: true);
            Close(json, 1);
            Open(json, 1, "processing");
            Field(json, 2, "targetRate", config.TargetRate);
            Field(json, 2, "gainEnabled", config.GainEnabled);
            Field(json, 2, "capturePeak", d.CapturePeak);
            Field(json, 2, "captureRms", d.CaptureRms);
            Field(json, 2, "clippingRatio", d.ClippingRatio);
            Field(json, 2, "noiseFloorRms", d.NoiseFloorRms);
            Field(json, 2, "thresholdRms", d.ThresholdRms);
            Field(json, 2, "voicedWindows", d.VoicedWindowCount);
            Field(json, 2, "voicedSeconds", d.VoicedSeconds);
            Field(json, 2, "retainedSeconds", d.RetainedSeconds);
            Field(json, 2, "appliedGain", d.AppliedGain, last: true);
            Close(json, 1, last: true);
            json.Append("}\n");
            return json.ToString();
        }

        private static void Open(StringBuilder json, int depth, string key) =>
            json.Append(' ', depth * 2).Append(Quote(key)).Append(": {\n");

        private static void Close(StringBuilder json, int depth, bool last = false) =>
            json.Append(' ', depth * 2).Append('}').Append(last ? "\n" : ",\n");

        private static void Field(StringBuilder json, int depth, string key, string value, bool last = false) =>
            Raw(json, depth, key, Quote(value), last);

        private static void Field(StringBuilder json, int depth, string key, int value, bool last = false) =>
            Raw(json, depth, key, value.ToString(CultureInfo.InvariantCulture), last);

        private static void Field(StringBuilder json, int depth, string key, double value, bool last = false) =>
            Raw(json, depth, key, value.ToString("0.######", CultureInfo.InvariantCulture), last);

        private static void Field(StringBuilder json, int depth, string key, bool value, bool last = false) =>
            Raw(json, depth, key, value ? "true" : "false", last);

        private static void Raw(StringBuilder json, int depth, string key, string value, bool last) =>
            json.Append(' ', depth * 2).Append(Quote(key)).Append(": ").Append(value).Append(last ? "\n" : ",\n");

        private static string Quote(string value)
        {
            var builder = new StringBuilder("\"");
            foreach (var c in value ?? string.Empty)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 0x20) builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else builder.Append(c);
                        break;
                }
            }

            return builder.Append('"').ToString();
        }
    }
}
