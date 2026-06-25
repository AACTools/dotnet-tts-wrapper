using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

/// <summary>
/// AWS Polly TTS Client with proper AWS Signature Version 4 authentication.
/// </summary>
public class PollyTtsClient : HttpTtsClientBase
{
    private readonly PollyCredentials _credentials;

    protected override string BaseEndpoint => $"https://polly.{_credentials.Region}.amazonaws.com";
    protected override bool SupportsStreaming => true;

    public PollyTtsClient(PollyCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = true,
            SupportsSsml = true,
            SupportsSpeechMarkdown = true,
            RequiresInternet = true,
            IsWindowsSupported = true,
            IsLinuxSupported = true,
            IsMacOsSupported = true
        };

        VoiceId = "Joanna";
    }

    public override string GetSpeechMarkdownPlatform()
    {
        return global::SpeechMarkdown.Platform.AmazonAlexa;
    }

    private object BuildPayload(string text, TtsOptions? options)
    {
        return new
        {
            Text = text,
            OutputFormat = options?.Format switch
            {
                AudioFormat.Mp3 => "mp3",
                AudioFormat.Wav => "pcm",
                AudioFormat.Ogg => "ogg_vorbis",
                _ => "mp3"
            },
            VoiceId = options?.VoiceId ?? VoiceId ?? "Joanna",
            SampleRate = "16000",
            TextType = options?.RawSsml == true || text.TrimStart().StartsWith("<speak") ? "ssml" : "text"
        };
    }

    public override async Task<TtsSynthesisResult> SynthToBytesAsync(string text, TtsOptions? options = null)
    {
        var preparedText = await PrepareTextAsync(text, options);
        var payload = BuildPayload(preparedText, options);
        var body = JsonSerializer.Serialize(payload);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var bodyHash = HexEncode(SHA256.HashData(bodyBytes));

        var url = $"{BaseEndpoint}/v1/speech";
        var uri = new Uri(url);
        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");
        var dateStamp = now.ToString("yyyyMMdd");

        var signedHeaders = "content-type;host;x-amz-content-sha256;x-amz-date;x-amz-target";
        var canonicalHeaders = $"content-type:application/json\nhost:{uri.Host}\nx-amz-content-sha256:{bodyHash}\nx-amz-date:{amzDate}\nx-amz-target:Amazon.Polly_20160620\n";

        var canonicalRequest = $"POST\n{uri.AbsolutePath}\n\n{canonicalHeaders}\n{signedHeaders}\n{bodyHash}";
        var canonicalHash = HexEncode(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));

        var credentialScope = $"{dateStamp}/{_credentials.Region}/polly/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{canonicalHash}";

        var kSecret = Encoding.UTF8.GetBytes($"AWS4{_credentials.SecretAccessKey}");
        var kDate = HMACSHA256(kSecret, Encoding.UTF8.GetBytes(dateStamp));
        var kRegion = HMACSHA256(kDate, Encoding.UTF8.GetBytes(_credentials.Region));
        var kService = HMACSHA256(kRegion, Encoding.UTF8.GetBytes("polly"));
        var kSigning = HMACSHA256(kService, Encoding.UTF8.GetBytes("aws4_request"));
        var signature = HexEncode(HMACSHA256(kSigning, Encoding.UTF8.GetBytes(stringToSign)));

        var authorization = $"AWS4-HMAC-SHA256 Credential={_credentials.AccessKeyId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("X-Amz-Date", amzDate);
        request.Headers.TryAddWithoutValidation("X-Amz-Target", "Amazon.Polly_20160620");
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        // Use ByteArrayContent to avoid .NET appending charset=utf-8 to Content-Type
        // AWS Sig V4 signs the exact content-type value, charset mismatch causes 403
        var content = new ByteArrayContent(bodyBytes);
        content.Headers.TryAddWithoutValidation("Content-Type", "application/json");
        content.Headers.TryAddWithoutValidation("x-amz-content-sha256", bodyHash);
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) { var errorBody = await response.Content.ReadAsStringAsync(); throw new HttpRequestException($"Polly TTS failed: {(int)response.StatusCode}\n{errorBody}"); }

        var audioData = await response.Content.ReadAsByteArrayAsync();
        return new TtsSynthesisResult
        {
            AudioData = audioData,
            Format = AudioFormat.Mp3,
            SampleRate = 16000,
            Channels = 1
        };
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options) => BuildPayload(text, options);

    protected override string GetSynthesisEndpoint(TtsOptions options) => "v1/speech";

    private static byte[] HMACSHA256(byte[] key, byte[] data)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    private static string HexEncode(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    protected override string GetStreamingEndpoint(TtsOptions options)
    {
        return "v1/speech";
    }

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{BaseEndpoint}/v1/voices",
                JsonContent.Create(new { })
            );
            if (!response.IsSuccessStatusCode) { var errorBody = await response.Content.ReadAsStringAsync(); throw new HttpRequestException($"Polly TTS failed: {(int)response.StatusCode}\n{errorBody}"); }

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonString);

            var voices = new List<TtsVoice>();
            if (jsonDoc.RootElement.TryGetProperty("Voices", out var voicesArray))
            {
                foreach (var voice in voicesArray.EnumerateArray())
                {
                    var voiceId = voice.GetProperty("Id").GetString();
                    var gender = voice.GetProperty("Gender").GetString();

                    voices.Add(new TtsVoice
                    {
                        Id = voiceId ?? "",
                        Name = voiceId ?? "",
                        Gender = MapGender(gender),
                        Provider = "polly",
                        LanguageCodes = new List<LanguageInfo>
                        {
                            new LanguageInfo
                            {
                                Bcp47 = voice.GetProperty("LanguageCode").GetString() ?? "",
                                Iso639_3 = voice.GetProperty("LanguageCode").GetString()?.Split('-')[0] ?? "",
                                Display = voice.GetProperty("LanguageName").GetString() ?? ""
                            }
                        }
                    });
                }
            }

            return voices;
        }
        catch (Exception)
        {
            return GetFallbackVoices();
        }
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            var voices = await GetVoicesAsync();
            return new CredentialsValidationResult
            {
                IsValid = voices.Count > 0,
                AvailableVoiceCount = voices.Count,
                EngineName = "polly"
            };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                EngineName = "polly"
            };
        }
    }

    private List<TtsVoice> GetFallbackVoices()
    {
        return new List<TtsVoice>
        {
            new() { Id = "Joanna", Name = "Joanna", Gender = VoiceGender.Female, Provider = "polly", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "Matthew", Name = "Matthew", Gender = VoiceGender.Male, Provider = "polly", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "Kimberly", Name = "Kimberly", Gender = VoiceGender.Female, Provider = "polly", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        };
    }

    private static VoiceGender MapGender(string? gender)
    {
        return gender?.ToLowerInvariant() switch
        {
            "female" => VoiceGender.Female,
            "male" => VoiceGender.Male,
            _ => VoiceGender.Unknown
        };
    }
}
