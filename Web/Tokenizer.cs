using System.Collections.Concurrent;
using Microsoft.DeepDev;

namespace CheBot;

public class Tokenizer
{
    const string IM_START = "<|im_start|>";
    const string IM_END = "<|im_end|>";

    static readonly ConcurrentDictionary<string, ITokenizer> tokenizers = new();
    
    static readonly Dictionary<string, int> specialTokens = new()
    {
        { IM_START, 100264},
        { IM_END, 100265},
    };

    Tokenizer() { }

    public static Tokenizer Default { get; } = new Tokenizer();

    public async Task<int> MeasureAsync(string model, string text)
    {
        if (!tokenizers.TryGetValue(model, out var tokenizer))
        {
            tokenizer = await TokenizerBuilder.CreateByModelNameAsync(model, specialTokens);
            tokenizers.TryAdd(model, tokenizer);
        }

        return tokenizer.Encode($"{IM_START}{text}{IM_END}", specialTokens.Keys).Count;
    }
}