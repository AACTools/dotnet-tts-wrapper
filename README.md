# DotNet TTS Wrapper

A .NET NuGet package that provides a unified API for working with multiple cloud-based and local Text-to-Speech (TTS) services. Ported from js-tts-wrapper.

## Supported Engines

| Engine | Word Events | Streaming | Notes |
|--------|------------|-----------|-------|
| **SAPI** | ⚠️ Estimated | ❌ No | Windows only, built-in system voices |
| **Azure** | ✅ Real | ✅ Yes | Azure Speech SDK (requires Azure key) |
| **Google** | ❌ No | ✅ Yes | Requires Google Cloud credentials |
| **Polly** | ❌ No | ✅ Yes | AWS Polly (requires AWS credentials) |
| **OpenAI** | ❌ No | ✅ Yes | OpenAI TTS API |
| **ElevenLabs** | ❌ No | ✅ Yes | ElevenLabs API |
| **Watson** | ❌ No | ✅ Yes | IBM Watson TTS |
| **PlayHT** | ❌ No | ✅ Yes | Play.ht API |
| **WitAI** | ❌ No | ✅ Yes | Wit.ai API |
| **Gemini** | ❌ No | ✅ Yes | Google Gemini TTS |
| **Cartesia** | ❌ No | ✅ Yes | Cartesia API |
| **Deepgram** | ❌ No | ✅ Yes | Deepgram TTS |
| **Hume** | ❌ No | ✅ Yes | Hume AI API |
| **xAI** | ❌ No | ✅ Yes | xAI Grok TTS |
| **FishAudio** | ❌ No | ✅ Yes | Fish Audio API |
| **Mistral** | ❌ No | ✅ Yes | Mistral AI TTS |
| **Murf** | ❌ No | ✅ Yes | Murf AI API |
| **UnrealSpeech** | ❌ No | ✅ Yes | Unreal Speech API |
| **Resemble** | ❌ No | ✅ Yes | Resemble AI API |
| **UpliftAI** | ❌ No | ✅ Yes | Uplift AI API |
| **ModelsLab** | ❌ No | ✅ Yes | Models Lab API |
| **SherpaOnnx** | ❌ No | ❌ No | Local offline TTS (Kokoro/Matcha/VITS models) |
| **eSpeak** | ❌ No | ❌ No | Coming soon (local offline TTS) |
| **CereVoice** | ❌ No | ❌ No | Coming soon (CereProc TTS) |

## Features

- **Unified API**: Single interface for 20+ TTS engines
- **True Streaming**: IAsyncEnumerable-based audio chunk streaming where supported
- **Word Timings**: Real word boundary events from Azure SDK, estimated for other engines
- **SSML Support**: Fluent SSML builder for expressive speech synthesis
- **Cross-platform**: Windows, Linux, macOS support (engine-dependent)
- **Modern .NET**: Built for .NET 8.0 with latest C# language features

## Installation

```bash
dotnet add package DotNetTtsWrapper
```

## Basic Usage

```csharp
using DotNetTtsWrapper.Models;
using DotNetTtsWrapper.Engines;

// Create a TTS client
var azureCredentials = new AzureCredentials 
{ 
    SubscriptionKey = "your-key", 
    Region = "westus" 
};
var client = TtsFactory.CreateClient("azure", azureCredentials);

// Get available voices
var voices = await client.GetVoicesAsync();
client.SetVoice(voices[0].Id);

// Synthesize speech
var result = await client.SynthToBytesAsync("Hello world!");
File.WriteAllBytes("output.wav", result.AudioData);

// With word timings
var options = new TtsOptions { EnableWordTimings = true };
var resultWithTimings = await client.SynthToBytesAsync("Hello world!", options);
foreach (var timing in resultWithTimings.WordTimings)
{
    Console.WriteLine($"{timing.Word}: {timing.StartTime}s - {timing.EndTime}s");
}

// Stream audio
var streamResult = await client.SynthToStreamAsync("Hello world!");
await foreach (var chunk in streamResult.AudioStream)
{
    // Process audio chunks
    ProcessAudioChunk(chunk.AudioData);
}
```

## Advanced Features

### SSML Builder
```csharp
var ssml = SsmlBuilder.Speak()
    .Voice("en-US-AriaNeural")
    .WithRate("fast")
    .WithPitch("high")
    .WithVolume(80)
    .AddText("Hello world!")
    .Build();
```

### Word Events
```csharp
client.WordBoundary += (sender, e) => 
{
    Console.WriteLine($"Word: {e.Word}, Time: {e.StartTime}s");
};
await client.SpeakAsync("Hello world!");
```

## Engine-Specific Setup

### Azure Speech SDK
```csharp
var credentials = new AzureCredentials 
{ 
    SubscriptionKey = "your-key", 
    Region = "your-region" 
};
var client = new AzureSdkTtsClient(credentials);
```

### SAPI (Windows Only)
```csharp
var client = new SapiTtsClient();
var voices = await client.GetVoicesAsync();
```

## Architecture

- **Abstract Factory Pattern**: `TtsFactory.CreateClient()` for engine creation
- **Event-Driven**: Word boundary, speech started/completed events
- **Async-First**: Full async/await support throughout
- **Streaming**: IAsyncEnumerable for true audio chunk streaming
- **Modular**: Optional engine-specific packages for reduced dependencies

## Requirements

- .NET 8.0 or higher
- Windows OS for SAPI engine
- Platform-specific packages for some engines

## License

Ported from js-tts-wrapper with .NET-specific enhancements.

## Roadmap

- [ ] eSpeak integration (local offline TTS)
- [ ] Speech Markdown support
- [ ] Advanced model management for SherpaOnnx
- [ ] Additional cloud engine integrations