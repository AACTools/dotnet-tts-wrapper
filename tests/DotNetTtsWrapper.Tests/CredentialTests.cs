using DotNetTtsWrapper.Models;
using DotNetTtsWrapper.Engines;
using Xunit;

namespace DotNetTtsWrapper.Tests;

public class CredentialTests
{
    [Fact]
    public void OpenAI_DefaultModel_Is_Tts1()
    {
        var creds = new OpenAICredentials { ApiKey = "test" };
        Assert.Equal("tts-1", creds.Model);
    }

    [Fact]
    public void OpenAI_CanSet_HdModel()
    {
        var creds = new OpenAICredentials { ApiKey = "test", Model = "tts-1-hd" };
        Assert.Equal("tts-1-hd", creds.Model);
    }

    [Fact]
    public void OpenAI_OrganizationId_Defaults_Empty()
    {
        var creds = new OpenAICredentials { ApiKey = "test" };
        Assert.Equal(string.Empty, creds.OrganizationId);
    }

    [Fact]
    public async Task OpenAI_Validate_RejectsEmptyKey()
    {
        var creds = new OpenAICredentials { ApiKey = "" };
        var result = await creds.ValidateAsync();
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task OpenAI_Validate_AcceptsNonEmptyKey()
    {
        var creds = new OpenAICredentials { ApiKey = "sk-test" };
        var result = await creds.ValidateAsync();
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ElevenLabs_DefaultModel_Is_MultilingualV2()
    {
        var creds = new ElevenLabsCredentials { ApiKey = "test" };
        Assert.Equal("eleven_multilingual_v2", creds.ModelId);
    }

    [Fact]
    public void ElevenLabs_CanSet_MonolingualModel()
    {
        var creds = new ElevenLabsCredentials { ApiKey = "test", ModelId = "eleven_monolingual_v1" };
        Assert.Equal("eleven_monolingual_v1", creds.ModelId);
    }

    [Fact]
    public void ElevenLabs_DefaultStability_Is_Half()
    {
        var creds = new ElevenLabsCredentials { ApiKey = "test" };
        Assert.Equal(0.5f, creds.Stability);
    }

    [Fact]
    public void ElevenLabs_CanSet_VoiceSettings()
    {
        var creds = new ElevenLabsCredentials
        {
            ApiKey = "test",
            Stability = 0.3f,
            SimilarityBoost = 0.9f
        };
        Assert.Equal(0.3f, creds.Stability);
        Assert.Equal(0.9f, creds.SimilarityBoost);
    }

    [Fact]
    public void Polly_Region_Defaults_East1()
    {
        var creds = new PollyCredentials
        {
            AccessKeyId = "test",
            SecretAccessKey = "test"
        };
        Assert.Equal("us-east-1", creds.Region);
    }

    [Fact]
    public void Polly_CanSet_Region()
    {
        var creds = new PollyCredentials
        {
            AccessKeyId = "test",
            SecretAccessKey = "test",
            Region = "eu-west-1"
        };
        Assert.Equal("eu-west-1", creds.Region);
    }

    [Fact]
    public async Task Polly_Validate_RequiresBothKeys()
    {
        var creds = new PollyCredentials { AccessKeyId = "", SecretAccessKey = "" };
        var result = await creds.ValidateAsync();
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Google_KeyFilePath_Defaults_Null()
    {
        var creds = new GoogleCredentials { ApiKey = "test" };
        Assert.Null(creds.KeyFilePath);
    }

    [Fact]
    public async Task Google_Validate_RejectsBadKeyFilePath()
    {
        var creds = new GoogleCredentials { ApiKey = "test", KeyFilePath = "/nonexistent/path.json" };
        var result = await creds.ValidateAsync();
        Assert.False(result.IsValid);
    }
}

public class FactoryTests
{
    [Fact]
    public void Factory_Supports_AllMajorEngines()
    {
        var engines = TtsFactory.GetSupportedEngines();
        Assert.Contains("azure", engines);
        Assert.Contains("openai", engines);
        Assert.Contains("elevenlabs", engines);
        Assert.Contains("google", engines);
        Assert.Contains("polly", engines);
        Assert.Contains("sherpaonnx", engines);
    }

    [Fact]
    public void Factory_NormalizesEngineNames()
    {
        var engines = TtsFactory.GetSupportedEngines();
        Assert.Contains("sherpaonnx", engines);
    }
}

public class SherpaOnnxCredentialTests
{
    [Fact]
    public void SherpaOnnx_Supports_ExplicitFilePaths()
    {
        var creds = new SherpaOnnxCredentials
        {
            ModelFilePath = "/path/to/model.onnx",
            TokensFilePath = "/path/to/tokens.txt",
            DataDirPath = "/path/to/espeak-ng-data"
        };
        Assert.Equal("/path/to/model.onnx", creds.ModelFilePath);
        Assert.Equal("/path/to/tokens.txt", creds.TokensFilePath);
        Assert.Equal("/path/to/espeak-ng-data", creds.DataDirPath);
    }

    [Fact]
    public void SherpaOnnx_Supports_LexiconFilePath()
    {
        var creds = new SherpaOnnxCredentials
        {
            ModelFilePath = "/model.onnx",
            LexiconFilePath = "/lexicon.txt"
        };
        Assert.Equal("/lexicon.txt", creds.LexiconFilePath);
    }
}
