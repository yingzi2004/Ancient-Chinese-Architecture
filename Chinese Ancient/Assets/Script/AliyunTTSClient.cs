using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
[RequireComponent(typeof(AudioSource))]
public class AliyunTTSClient : MonoBehaviour
{
    [Header("阿里云凭证")]
    [SerializeField] private string accessKeyId = "";
    [SerializeField] private string accessKeySecret = "";
    [SerializeField] private string appKey = "";
    [Header("Token 手动模式 (可选)")]
    [SerializeField] private string manualToken = "";
    [Header("语音设置")]
    [SerializeField] private VoiceType voice = VoiceType.Xiaoyun;
    [Range(0, 100)]
    [SerializeField] private int volume = 50;
    [Range(-500, 500)]
    [SerializeField] private int speechRate = 0;
    [Range(-500, 500)]
    [SerializeField] private int pitchRate = 0;
    [SerializeField] private AudioFormat format = AudioFormat.MP3;
    [Header("播放设置")]
    [SerializeField] private AudioSource audioSource;
    // API端点
    private const string TTS_API_URL = "https://nls-gateway-cn-shanghai.aliyuncs.com/stream/v1/tts";
    public bool IsPlaying => audioSource != null && audioSource.isPlaying;
    public bool IsSynthesizing { get; private set; }
    public enum VoiceType
    {
        Xiaoyun,    // 小云 - 标准女声
        Xiaogang,   // 小刚 - 标准男声
        Ruoxi,      // 若兮 - 温柔女声
        Siqi,       // 思琪 - 温柔女声
        Sijia,      // 思佳 - 标准女声
        Sicheng,    // 思诚 - 标准男声
        Aiqi,       // 艾琪 - 温柔女声
        Aijia,      // 艾佳 - 标准女声
        Aixia,      // 艾夏 - 标准女声
        Aida,       // 艾达 - 标准男声
        Ninger,     // 宁儿 - 标准女声
        Ruilin,     // 瑞琳 - 标准女声
        Siyue,      // 思悦 - 温柔女声
        Aiya,       // 艾雅 - 严厉女声
        Aimei,      // 艾美 - 甜美女声
        Aijing,     // 艾婧 - 严厉女声
        Xiaomei,    // 小美 - 甜美女声
        Lydia,      // Lydia - 英文女声
        William,    // William - 英文男声
        Aitong,     // 艾彤 - 儿童音
        Aiwei,      // 艾薇 - 萝莉女声
        Aibao,      // 艾宝 - 萝莉女声
        Shanshan,   // 姗姗 - 粤语女声
        Xiaoyue,    // 小玥 - 四川话女声
    }
    public enum AudioFormat
    {
        MP3,
        WAV,
        PCM
    }
    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    public void Speak(string text, Action onComplete = null, Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            onError?.Invoke("文本不能为空");
            return;
        }
        if (IsSynthesizing)
        {
            onError?.Invoke("正在合成中，请稍候");
            return;
        }
        StartCoroutine(SynthesizeAndPlay(text, onComplete, onError));
    }
    public void Stop()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    public void SetVoice(VoiceType voiceType)
    {
        voice = voiceType;
    }
    public void SetVolume(int vol)
    {
        volume = Mathf.Clamp(vol, 0, 100);
    }
    public void SetSpeechRate(int rate)
    {
        speechRate = Mathf.Clamp(rate, -500, 500);
    }
    public void SetPitchRate(int pitch)
    {
        pitchRate = Mathf.Clamp(pitch, -500, 500);
    }
    public void SetCredentials(string akId, string akSecret, string appkey)
    {
        accessKeyId = akId?.Trim();
        accessKeySecret = akSecret?.Trim();
        appKey = appkey?.Trim();
    }
    private string cachedToken = "";
    private long tokenExpireTime = 0;
    private IEnumerator SynthesizeAndPlay(string text, Action onComplete, Action<string> onError)
    {
        // 自动取出首尾空格，防止复制粘贴带入空格导致 404
        accessKeyId = accessKeyId?.Trim();
        accessKeySecret = accessKeySecret?.Trim();
        appKey = appKey?.Trim();
        Debug.Log($"[AliyunTTS] 开始合成: {text}");
        IsSynthesizing = true;
        if (string.IsNullOrWhiteSpace(appKey))
        {
            IsSynthesizing = false;
            Debug.LogError("[AliyunTTS] 错误: 未配置 App Key");
            onError?.Invoke("请配置 App Key");
            yield break;
        }
        // 1. 获取 Token
        string token = manualToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            // 检查缓存
            if (!string.IsNullOrEmpty(cachedToken) && DateTimeOffset.UtcNow.ToUnixTimeSeconds() < tokenExpireTime - 60)
            {
                token = cachedToken;
                Debug.Log("[AliyunTTS] 使用缓存 Token");
            }
            else
            {
                Debug.Log("[AliyunTTS] 正在请求新 Token...");
                // 需要重新获取 Token
                if (string.IsNullOrWhiteSpace(accessKeyId) || string.IsNullOrWhiteSpace(accessKeySecret))
                {
                    IsSynthesizing = false;
                    Debug.LogError("[AliyunTTS] 错误: 未配置 AK/SK");
                    onError?.Invoke("请配置 AccessKey ID 和 Secret 以自动获取 Token");
                    yield break;
                }
                // 创建 Token 请求
                string tokenUrl = CreateTokenUrl(accessKeyId, accessKeySecret);
                using (UnityWebRequest tokenRequest = UnityWebRequest.Get(tokenUrl))
                {
                    tokenRequest.certificateHandler = new BypassCertificate();
                    yield return tokenRequest.SendWebRequest();
                    if (tokenRequest.result != UnityWebRequest.Result.Success)
                    {
                        IsSynthesizing = false;
                        string err = $"获取Token失败: {tokenRequest.error}\nURL: {tokenUrl}";
                        Debug.LogError($"[AliyunTTS] {err}");
                        onError?.Invoke(err);
                        yield break;
                    }
                    // 解析 Token
                    try
                    {
                        string json = tokenRequest.downloadHandler.text;
                        Debug.Log($"[AliyunTTS] Token响应: {json}");
                        var tokenData = JsonUtility.FromJson<TokenResponse>(json);
                        if (tokenData != null && tokenData.Token != null && !string.IsNullOrEmpty(tokenData.Token.Id))
                        {
                            cachedToken = tokenData.Token.Id;
                            tokenExpireTime = tokenData.Token.ExpireTime;
                            token = cachedToken;
                            Debug.Log($"[AliyunTTS] Token获取成功: {token}");
                        }
                        else
                        {
                            IsSynthesizing = false;
                            Debug.LogError($"[AliyunTTS] Token响应解析不足: {json}");
                            onError?.Invoke($"Token响应解析失败: {json}");
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        IsSynthesizing = false;
                        Debug.LogError($"[AliyunTTS] Token解析异常: {ex.Message}");
                        onError?.Invoke($"Token解析异常: {ex.Message}");
                        yield break;
                    }
                }
            }
        }
        // 2. 构建 TTS 请求
        string voiceName = GetVoiceName(voice);
        string formatStr = GetFormatString(format);
        if (format == AudioFormat.MP3) formatStr = "wav";
        string url = $"{TTS_API_URL}?appkey={appKey}&token={token}&text={UnityWebRequest.EscapeURL(text)}&format={formatStr}&voice={voiceName}&volume={volume}&speech_rate={speechRate}&pitch_rate={pitchRate}";
        Debug.Log($"[AliyunTTS] 请求音频URL: {url}");
        // 3. 请求音频
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            request.certificateHandler = new BypassCertificate();
            request.timeout = 30;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                IsSynthesizing = false;
                string errorMsg = request.error;
                Debug.LogError($"[AliyunTTS] 音频下载失败: {errorMsg}");
                onError?.Invoke($"语音合成失败: {errorMsg}");
                yield break;
            }
            // 获取AudioClip
            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            IsSynthesizing = false;
            if (clip == null)
            {
                Debug.LogError("[AliyunTTS] AudioClip 为空");
                onError?.Invoke("音频解码失败");
                yield break;
            }
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.LogWarning($"[AliyunTTS] AudioClip LoadState: {clip.loadState}");
            }
            // check AudioSource
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            // 强制设置为2D声音，确保能听到
            audioSource.spatialBlend = 0f;
            audioSource.mute = false;
<<<<<<< Updated upstream
            audioSource.volume = volume / 100f; // AI辅助生成：DeepSeek-R1-0528, 2026年3月9日 - 修复2：修正音量计算，volume是0-100范围
=======
            audioSource.volume = volume / 100f; 


>>>>>>> Stashed changes
            Debug.Log($"[AliyunTTS] 播放音频: 长度={clip.length}s, 通道={clip.channels}, 频率={clip.frequency}");
            // 播放音频
            audioSource.clip = clip;
            audioSource.Play();
            // 等待播放完成
            while (audioSource.isPlaying)
            {
                yield return null;
            }
            Debug.Log("[AliyunTTS] 播放完成");
            onComplete?.Invoke();
        }
    }
    private string GetToken()
    {
        return cachedToken;
    }
    [Serializable]
    private class TokenResponse
    {
        public string ErrMsg;
        public TokenData Token;
    }
    [Serializable]
    private class TokenData
    {
        public string Id;
        public long ExpireTime;
    }
    // 生成获取 NLS Token 的请求 URL
    private string CreateTokenUrl(string akId, string akSecret)
    {
        // 1. 参数准备
        var parameters = new Dictionary<string, string>()
        {
            {"AccessKeyId", akId},
            {"Action", "CreateToken"},
            {"Format", "JSON"},
            {"RegionId", "cn-shanghai"},
            {"SignatureMethod", "HMAC-SHA1"},
            {"SignatureNonce", Guid.NewGuid().ToString()},
            {"SignatureVersion", "1.0"},
            {"Timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")},
            {"Version", "2019-02-28"}
        };
        // 2. 构造规范化请求串 CanonicalizedQueryString
        // 阿里云 POP 签名必须按参数名严格 ASCII 升序排序
        var sortedKeys = parameters.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        StringBuilder cqs = new StringBuilder();
        foreach (var key in sortedKeys)
        {
            if (cqs.Length > 0) cqs.Append("&");
            cqs.Append(PercentageEncode(key)).Append("=").Append(PercentageEncode(parameters[key]));
        }
        // 3. 构造待签名字符串 StringToSign
        // 必须严格遵守 POP 签名规则：HTTPMethod + "&" + percentEncode("/") + "&" + percentEncode(CanonicalizedQueryString)
        // 注意：StringToSign 里的 percentEncode 是对 "整个 cqs 字符串" 进行第二次 URL 编码
        string stringToSign = "GET&" + PercentageEncode("/") + "&" + PercentageEncode(cqs.ToString());
        // 调试用日志，帮助排查签名错误
        Debug.Log($"[AliyunTTS] StringToSign: {stringToSign}");
        // 4. 计算签名 (Key必须加上&)
        string signature = ComputeSignature(stringToSign, akSecret + "&");
        // 5. 将 Signature 参数添加到请求串
        // Signature 必须也要编码
        return $"https://nls-meta.cn-shanghai.aliyuncs.com/?Signature={PercentageEncode(signature)}&{cqs.ToString()}";
    }
    private string PercentageEncode(string value)
    {
        if (value == null) return null;
        // 阿里云 POP 签名要求字符编码为大写形式 (例如 %3A 而不是 %3a)
        // UnityWebRequest.EscapeURL 有时返回小写，需要手动修正
        string encoded = UnityWebRequest.EscapeURL(value);
        encoded = encoded.Replace("+", "%20")
        .Replace("*", "%2A")
        .Replace("%7E", "~");
        // 确保所有 %xx 格式的 hex 都是大写
        char[] chars = encoded.ToCharArray();
        for (int i = 0; i < chars.Length - 2; i++)
        {
            if (chars[i] == '%')
            {
                chars[i + 1] = char.ToUpper(chars[i + 1]);
                chars[i + 2] = char.ToUpper(chars[i + 2]);
            }
        }
        return new string(chars);
    }
    private string ComputeSignature(string data, string key)
    {
        using (var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key)))
        {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }
    }
    private string GetVoiceName(VoiceType voiceType)
    {
        return voiceType.ToString().ToLower();
    }
    private string GetFormatString(AudioFormat fmt)
    {
        switch (fmt)
        {
            case AudioFormat.WAV: return "wav";
            case AudioFormat.PCM: return "pcm";
            default: return "mp3";
        }
    }
    private AudioClip CreateAudioClipFromWav(byte[] data)
    {
        try
        {
            // 解析WAV头
            if (data.Length < 44) return null;
            int channels = BitConverter.ToInt16(data, 22);
            int sampleRate = BitConverter.ToInt32(data, 24);
            int bitsPerSample = BitConverter.ToInt16(data, 34);
            // 找到data块
            int dataOffset = 44;
            int dataSize = data.Length - dataOffset;
            float[] samples = new float[dataSize / (bitsPerSample / 8)];
            if (bitsPerSample == 16)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    short sample = BitConverter.ToInt16(data, dataOffset + i * 2);
                    samples[i] = sample / 32768f;
                }
            }
            else if (bitsPerSample == 8)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = (data[dataOffset + i] - 128) / 128f;
                }
            }
            AudioClip clip = AudioClip.Create("TTS", samples.Length / channels, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        catch (Exception e)
        {
            Debug.LogError($"WAV解码错误: {e.Message}");
            return null;
        }
    }
    private AudioClip CreateAudioClipFromPcm(byte[] data)
    {
        try
        {
            // 假设16kHz, 16bit, 单声道
            int sampleRate = 16000;
            int channels = 1;
            float[] samples = new float[data.Length / 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short sample = BitConverter.ToInt16(data, i * 2);
                samples[i] = sample / 32768f;
            }
            AudioClip clip = AudioClip.Create("TTS", samples.Length, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        catch (Exception e)
        {
            Debug.LogError($"PCM解码错误: {e.Message}");
            return null;
        }
    }
    private AudioClip CreateAudioClipFromMp3(byte[] data)
    {
        // Unity原生不支持MP3解码
        // 建议使用WAV格式，或导入第三方MP3解码库
        Debug.LogWarning("Unity不支持直接解码MP3，建议在Inspector中选择WAV格式");
        return null;
    }
    public static string[] GetAllVoiceNames()
    {
        return Enum.GetNames(typeof(VoiceType));
    }
    public void SetVoiceByName(string voiceName)
    {
        if (Enum.TryParse<VoiceType>(voiceName, true, out VoiceType vt))
        {
            voice = vt;
        }
    }
    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}
