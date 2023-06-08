﻿using Speechmatics.Realtime.Client.Enumerations;
using Speechmatics.Realtime.Client.Messages;
using System.Collections.Generic;

namespace Speechmatics.Realtime.Client.Messages
{
    internal class StartRecognitionMessage : BaseMessage
    {
        public StartRecognitionMessage(SmRtApiConfigBase smConfig, AudioFormatSubMessage audioFormatSubMessage, AdditionalVocabSubMessage? additionalVocab)
        {
            audio_format = audioFormatSubMessage;
            transcription_config["language"] = smConfig.Model;
            if (additionalVocab != null)
            {
                transcription_config["additional_vocab"] = additionalVocab.Data;
            }
            if (smConfig.OutputLocale != null)
            {
                transcription_config["output_locale"] = smConfig.OutputLocale;
            }
            transcription_config["max_delay"] = smConfig.MaxDelay;
            if (smConfig.MaxDelayMode != null)
            {
                transcription_config["max_delay_mode"] = smConfig.MaxDelayMode;
            }
            transcription_config["enable_partials"] = smConfig.EnablePartials;
            transcription_config["enable_entities"] = smConfig.EnableEntities;
            if (smConfig.OperatingPoint != null)
            {
                transcription_config["operating_point"] = smConfig.OperatingPoint;
            }
            if (DiarizationType.Speaker.Equals(smConfig.Diarization)) 
            {
                transcription_config["diarization"] = "speaker";
            }
            if (smConfig.SpeakerDiarizationConfig != null)
            {
                var speakerDiarizationConfig = new Dictionary<string, object>();
                if (smConfig.SpeakerDiarizationConfig.MaxSpeakers != null)
                {
                    speakerDiarizationConfig["max_speakers"] = smConfig.SpeakerDiarizationConfig.MaxSpeakers;
                }
                transcription_config["speaker_diarization_config"] = speakerDiarizationConfig;
            }
            if (smConfig.PunctuationOverrides != null)
            {
                var punctuationOverrides = new Dictionary<string, object>();
                if (smConfig.PunctuationOverrides.PermittedMarks != null)
                {
                    punctuationOverrides["permitted_marks"] = smConfig.PunctuationOverrides.PermittedMarks;
                }
                if (smConfig.PunctuationOverrides.Sensitivity != null)
                {
                    punctuationOverrides["sensitivity"] = smConfig.PunctuationOverrides.Sensitivity;
                }
                transcription_config["punctuation_overrides"] = punctuationOverrides;
            }

            if (smConfig.Domain != null)
            {
                transcription_config["domain"] = smConfig.Domain;
            }
        }

        public string message => "StartRecognition";
        public AudioFormatSubMessage audio_format { get; }
        public Dictionary<string, object> transcription_config { get; } = new Dictionary<string, object>();
    }
}