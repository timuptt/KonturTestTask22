using System;
using System.Collections.Generic;
using System.Linq;

namespace Morphology
{
    public class SentenceMorpher {

        private readonly Dictionary<string, Dictionary<uint, string>> _morphMap;
        private readonly Dictionary<string, uint> _tagsMap;

        public SentenceMorpher(Dictionary<string, Dictionary<uint, string>> morphMap, Dictionary<string, uint> tagsMap)
        {
            _morphMap = morphMap;
            _tagsMap = tagsMap;
        }

        /// <summary>
        ///     Создает <see cref="SentenceMorpher"/> из переданного набора строк словаря.
        /// </summary>
        /// <remarks>
        ///     В этом методе должен быть код инициализации: 
        ///     чтение и преобразование входных данных для дальнейшего их использования
        /// </remarks>
        /// <param name="dictionaryLines">
        ///     Строки исходного словаря OpenCorpora в формате plain-text.
        ///     <code> СЛОВО(знак_табуляции)ЧАСТЬ РЕЧИ( )атрибут1[, ]атрибут2[, ]атрибутN </code>
        /// </param>
        public static SentenceMorpher Create(IEnumerable<string> dictionaryLines)
        {
            var morphMap = new Dictionary<string, Dictionary<uint, string>>();
            var tagsMap = new Dictionary<string, uint>();

            var currentWord = string.Empty;
            var isNormalForm = false;

            foreach (var dictionaryLine in dictionaryLines)
            {
                var lowerDictionaryLine = dictionaryLine.ToLower();
                if (string.IsNullOrWhiteSpace(lowerDictionaryLine))
                {
                    continue;
                }

                if (char.IsDigit(lowerDictionaryLine[0]))
                {
                    isNormalForm = true;
                    continue;
                }

                uint parsedTag;
                if (isNormalForm)
                {
                    currentWord = lowerDictionaryLine.Split('\t')[0];

                    if (!morphMap.ContainsKey(currentWord))
                    {
                        morphMap.Add(currentWord, new Dictionary<uint, string>());
                        isNormalForm = false;
                        continue;
                    }

                    parsedTag = ParseTag(lowerDictionaryLine, tagsMap);

                    if (!morphMap[currentWord].ContainsKey(parsedTag))
                    {
                        morphMap[currentWord].Add(parsedTag, lowerDictionaryLine.Split('\t')[0]);
                        isNormalForm = false;
                    }

                    continue;
                }

                var splittedWord = lowerDictionaryLine.Split('\t')[0];
                parsedTag = ParseTag(lowerDictionaryLine, tagsMap);

                if (!morphMap[currentWord].ContainsKey(parsedTag))
                {
                    morphMap[currentWord].Add(parsedTag, splittedWord);
                }
            }

            return new SentenceMorpher(morphMap, tagsMap);
        }

        /// <summary>
        ///     Выполняет склонение предложения согласно указанному формату
        /// </summary>
        /// <param name="sentence">
        ///     Входное предложение <para/>
        ///     Формат: набор слов, разделенных пробелами.
        ///     После слова может следовать спецификатор требуемой части речи (формат описан далее),
        ///     если он отсутствует - слово требуется перенести в выходное предложение без изменений.
        ///     Спецификатор имеет следующий формат: <code>{ЧАСТЬ РЕЧИ,аттрибут1,аттрибут2,..,аттрибутN}</code>
        ///     Если для спецификации найдётся несколько совпадений - используется первое из них
        /// </param>
        public virtual string Morph(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                return string.Empty;
            }

            sentence = sentence.ToLower();
            var splitedSentence = sentence.Split(' ', '\t');
            var resultSentence = new List<string>();

            foreach (var word in splitedSentence)
            {
                if (!word.Contains('{'))
                {
                    resultSentence.Add(word);
                    continue;
                }

                var splitedWord = word.Split('{', '}');
                if (string.IsNullOrWhiteSpace(splitedWord[1]))
                {
                    resultSentence.Add(splitedWord[0]);
                    continue;
                }

                resultSentence.Add(MorphWord(splitedWord[0], GenerateTagsCode(splitedWord[1], _tagsMap)));
            }

            sentence = string.Join(' ', resultSentence);
            return sentence;
        }

        private static uint ParseTag(string dictionaryLine, Dictionary<string, uint> _tagsMap)
        {
            var splitedLine = dictionaryLine.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            var tagsMultiple = (uint)1;

            if (!_tagsMap.ContainsKey(splitedLine[0]))
            {
                var splitedTags = splitedLine[1].Split(',', ' ');
                foreach (var tag in splitedTags)
                {
                    var lowerTag = tag.ToLower();
                    if (_tagsMap.ContainsKey(lowerTag))
                    {
                        tagsMultiple *= (uint)_tagsMap[lowerTag];
                        continue;
                    }

                    if (_tagsMap.Count == 0)
                    {
                        _tagsMap.Add(lowerTag, PrimeNumber.GetBasePrime());
                        tagsMultiple *= PrimeNumber.GetBasePrime();

                        continue;
                    }

                    var currentTag = PrimeNumber.GetNext(_tagsMap.Values.Max());
                    _tagsMap.Add(lowerTag, currentTag);
                    tagsMultiple *= currentTag;
                }
            }
            return tagsMultiple;
        }

        private string MorphWord(string word, uint tagCode)
        {   
            if (!_morphMap.ContainsKey(word))
            {
                return word;
            }

            if (_morphMap[word].ContainsKey(tagCode))
            {
                return _morphMap[word][tagCode];
            }

            return FindNearestWord(word, tagCode);
        }

        private string FindNearestWord(string word, uint tagCode)
        {
            foreach(var wordForm in _morphMap[word])
            {
                if (wordForm.Key % tagCode == 0 && PrimeNumber.CheckPrime(wordForm.Key / tagCode))
                {
                    return wordForm.Value;
                }
            }

            return word;
        }

        private uint GenerateTagsCode(string tags, Dictionary<string, uint> tagsMap)
        {
            var splitedTags = tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var generatedCode = 1u;

            foreach(var tag in splitedTags)
            {
                if (tagsMap.ContainsKey(tag))
                {
                    generatedCode *= tagsMap[tag];
                }
                else
                {
                    return 1;
                }
            }

            return generatedCode;
        }
    }

    public static class PrimeNumber
    {
        private static readonly uint basePrime = 2;

        public static uint GetBasePrime()
        {
            return basePrime;
        }

        public static uint GetNext(uint current)
        {
            if (current == basePrime)
            {
                return 3;
            }
            if (current == 3)
            {
                return 5;
            }

            var nextPrime = current + 2;

            while (CheckPrime(nextPrime) == false)
            {
                nextPrime += 2;
            }

            return nextPrime;
        }

        public static bool CheckPrime(uint number)
        {
            bool IsPrime = true;

            if (number is 2 or 3 or 5)
            {
                return true;
            }

            for (int i = 2; i < number / 2; i++)
            {
                if (number % i == 0)
                {
                    IsPrime = false;
                    break;
                }
            }

            return IsPrime;
        }
    }
}
