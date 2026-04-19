using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ScreenWordOverlay.Services;

/// <summary>
/// 逐词翻译服务
/// 优先级：用户术语表 > 内置术语表 > 本地词典 > 词形还原后重查 > 在线翻译 > 保留原词
/// </summary>
public class TranslationService
{
    private readonly SettingsService _settingsService;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(3) };

    /// <summary>
    /// 在线翻译缓存（避免重复请求）
    /// </summary>
    private static readonly Dictionary<string, string> _onlineCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 正在进行在线翻译的单词集合（防止重复请求）
    /// </summary>
    private static readonly HashSet<string> _pendingOnline = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 简单词形还原后缀规则
    /// </summary>
    private static readonly (string suffix, string[] replacements)[] StemmingRules =
    {
        ("ying", new[] { "y", "ie" }),      // copying -> copy, copie
        ("ing", new[] { "", "e" }),          // editing -> edit, edite; running -> run, runne
        ("ied", new[] { "y" }),              // carried -> carry
        ("ies", new[] { "y" }),              // carries -> carry
        ("ed", new[] { "", "e" }),           // built -> built, builte; worked -> work, worke
        ("es", new[] { "", "e" }),           // boxes -> box, boxe; goes -> go, goe
        ("ly", new[] { "" }),                // quickly -> quick
        ("tion", new[] { "te" }),            // generation -> generate
        ("sion", new[] { "se" }),            // extension -> extense
        ("ment", new[] { "" }),              // development -> develop
        ("ness", new[] { "" }),              // happiness -> happi
        ("ful", new[] { "" }),               // helpful -> help
        ("less", new[] { "" }),              // helpless -> help
        ("able", new[] { "" }),              // readable -> read
        ("ible", new[] { "" }),              // accessible -> access
        ("ous", new[] { "" }),               // famous -> fam
        ("ive", new[] { "e", "" }),          // active -> acte, activ
        ("al", new[] { "" }),                // natural -> natur
        ("er", new[] { "" }),                // builder -> build
        ("est", new[] { "" }),               // fastest -> fast
        ("s", new[] { "" }),                 // words -> word
    };

    public TranslationService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// 翻译单个单词
    /// </summary>
    public string TranslateWord(string word)
    {
        var localResult = TranslateWordLocal(word);
        return localResult;
    }

    /// <summary>
    /// 批量翻译单词列表（异步版本，避免在线翻译阻塞UI）
    /// </summary>
    public async Task TranslateWordsAsync(List<Models.OcrWord> words)
    {
        // 第一轮：本地翻译
        var needOnlineWords = new List<Models.OcrWord>();
        foreach (var w in words)
        {
            var translation = TranslateWordLocal(w.Text);
            w.Translation = translation;
            w.IsTranslated = translation != w.Text;
            if (!w.IsTranslated && _settingsService.Settings.UseOnlineTranslation)
                needOnlineWords.Add(w);
        }

        // 第二轮：在线翻译未命中的单词
        if (needOnlineWords.Count > 0)
        {
            var tasks = needOnlineWords.Select(async w =>
            {
                var result = await TranslateWordOnlineAsync(w.Text.ToLowerInvariant().Trim());
                if (result != null && result != w.Text)
                {
                    w.Translation = result;
                    w.IsTranslated = true;
                }
            });
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// 批量翻译单词列表（同步版本，仅本地翻译，不阻塞UI）
    /// </summary>
    public void TranslateWords(List<Models.OcrWord> words)
    {
        foreach (var w in words)
        {
            var translation = TranslateWordLocal(w.Text);
            w.Translation = translation;
            w.IsTranslated = translation != w.Text;
        }
    }

    /// <summary>
    /// 对本地未翻译的单词进行在线翻译补全（异步，不阻塞UI，翻译完成后回调刷新）
    /// </summary>
    public async Task TranslateWordsOnlineAsync(List<Models.OcrWord> words, Action? onCompleted = null)
    {
        if (!_settingsService.Settings.UseOnlineTranslation) return;

        var needOnlineWords = words.Where(w => !w.IsTranslated).ToList();
        if (needOnlineWords.Count == 0) return;

        var tasks = needOnlineWords.Select(async w =>
        {
            var result = await TranslateWordOnlineAsync(w.Text.ToLowerInvariant().Trim());
            if (result != null && result != w.Text)
            {
                w.Translation = result;
                w.IsTranslated = true;
            }
        });

        await Task.WhenAll(tasks);
        onCompleted?.Invoke();
    }

    /// <summary>
    /// 纯本地翻译（不调用在线API）
    /// </summary>
    private string TranslateWordLocal(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return word;
        if (IsNumericOrVersion(word)) return word;

        var lowerWord = word.ToLowerInvariant().Trim();

        // 1. 用户术语表
        if (_settingsService.UserTerminology.TryGetValue(lowerWord, out var userTerm))
            return string.IsNullOrEmpty(userTerm) ? word : userTerm;

        // 2. 内置术语表
        if (_settingsService.BuiltInTerminology.TryGetValue(lowerWord, out var builtInTerm))
            return string.IsNullOrEmpty(builtInTerm) ? word : builtInTerm;

        // 3. 本地词典
        if (_settingsService.Dictionary.TryGetValue(lowerWord, out var dictTrans))
            return string.IsNullOrEmpty(dictTrans) ? word : dictTrans;

        // 4. 词形还原后重查词典和术语表
        var stems = GetStems(lowerWord);
        foreach (var stem in stems)
        {
            if (_settingsService.UserTerminology.TryGetValue(stem, out var ut))
                return string.IsNullOrEmpty(ut) ? word : ut;
            if (_settingsService.BuiltInTerminology.TryGetValue(stem, out var bt))
                return string.IsNullOrEmpty(bt) ? word : bt;
            if (_settingsService.Dictionary.TryGetValue(stem, out var dt))
                return string.IsNullOrEmpty(dt) ? word : dt;
        }

        return word;
    }

    /// <summary>
    /// 获取词形还原的可能词干
    /// </summary>
    private static List<string> GetStems(string word)
    {
        var stems = new List<string>();

        foreach (var (suffix, replacements) in StemmingRules)
        {
            if (word.Length > suffix.Length + 1 && word.EndsWith(suffix))
            {
                var basePart = word[..^suffix.Length];
                foreach (var rep in replacements)
                {
                    var candidate = basePart + rep;
                    if (candidate.Length >= 2)
                        stems.Add(candidate);
                }
            }
        }

        return stems;
    }

    /// <summary>
    /// 判断是否为数字或版本号
    /// </summary>
    private static bool IsNumericOrVersion(string word)
    {
        // 纯数字
        if (double.TryParse(word, out _)) return true;

        // 版本号模式 如 2.5, v1.0, 3rd
        if (Regex.IsMatch(word, @"^v?\d+([.\-]\d+)*$")) return true;

        return false;
    }

    /// <summary>
    /// 在线翻译单个单词（异步，使用有道词典建议API，无需Key，返回精确词性+释义）
    /// </summary>
    private async Task<string?> TranslateWordOnlineAsync(string word)
    {
        // 检查缓存
        if (_onlineCache.TryGetValue(word, out var cached))
            return cached == word ? null : cached;

        // 防止重复请求
        lock (_pendingOnline)
        {
            if (_pendingOnline.Contains(word)) return null;
            _pendingOnline.Add(word);
        }

        try
        {
            var url = $"https://dict.youdao.com/suggest?num=1&q={Uri.EscapeDataString(word)}&doctype=json";
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);

            var root = doc.RootElement;

            // 检查返回状态
            if (root.TryGetProperty("result", out var resultObj) &&
                resultObj.TryGetProperty("code", out var codeEl) &&
                codeEl.GetInt32() == 200)
            {
                if (root.TryGetProperty("data", out var dataObj) &&
                    dataObj.TryGetProperty("entries", out var entriesArr) &&
                    entriesArr.GetArrayLength() > 0)
                {
                    var firstEntry = entriesArr[0];
                    if (firstEntry.TryGetProperty("explain", out var explainEl))
                    {
                        var explain = explainEl.GetString()?.Trim();

                        if (!string.IsNullOrEmpty(explain))
                        {
                            // 提取第一个释义（去掉词性前缀后的第一个中文释义）
                            var cleaned = ExtractFirstMeaning(explain);
                            if (!string.IsNullOrEmpty(cleaned))
                            {
                                _onlineCache[word] = cleaned;
                                return cleaned;
                            }
                        }
                    }
                }
            }

            _onlineCache[word] = word;
            return null;
        }
        catch
        {
            // 网络错误等，不缓存，下次可重试
            return null;
        }
        finally
        {
            lock (_pendingOnline)
            {
                _pendingOnline.Remove(word);
            }
        }
    }

    /// <summary>
    /// 从有道词典释义中提取第一个核心中文释义
    /// 如 "n. 实施，执行；安装" → "实施"
    /// 如 "adj. 美丽的，漂亮的" → "美丽的"
    /// </summary>
    private static string ExtractFirstMeaning(string explain)
    {
        // 去掉词性前缀 (n. adj. v. adv. prep. conj. pron. interj. art. 等)
        // 找第一个 "." 后面的内容
        var dotIndex = explain.IndexOf('.');
        var text = dotIndex >= 0 && dotIndex < explain.Length - 1
            ? explain[(dotIndex + 1)..].Trim()
            : explain;

        // 按中文逗号、顿号、分号、英文逗号分割，取第一个释义
        var separators = new[] { '\uFF0C', '\u3001', '\uFF1B', ',' }; // ，、；,
        var firstSepIndex = int.MaxValue;
        foreach (var sep in separators)
        {
            var idx = text.IndexOf(sep);
            if (idx > 0 && idx < firstSepIndex)
                firstSepIndex = idx;
        }

        var result = firstSepIndex < int.MaxValue
            ? text[..firstSepIndex].Trim()
            : text.Trim();

        // 限制长度，避免太长的释义显示不下
        if (result.Length > 6)
            result = result[..6] + "\u2026"; // …

        return result;
    }

}
