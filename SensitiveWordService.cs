﻿using Ccy.Chat.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
//using ToolGood.Words;

namespace Ccy.Chat.Common
{
    /// <summary>
    /// 敏感词组件
    /// </summary>
    public class SensitiveWordService
    {
        public static SensitiveWordService Instance { get { return new SensitiveWordService(); } }

        //private static WordsSearch wordsSearch;

        static SensitiveWordService()
        {
            //Init();
        }

        #region ToolGood.Words
        ///// <summary>
        ///// Init word
        ///// </summary>
        //private static void Init()
        //{
        //    var lst = DAL.GetWords();
        //    wordsSearch = new WordsSearch();
        //    wordsSearch.SetKeywords(lst.Select((d) => d.Word).ToArray());
        //}


        ///// <summary>
        ///// 过滤敏感词
        ///// </summary>
        ///// <param name="text"></param>
        ///// <param name="replaceChar">替换的字符</param>
        ///// <returns></returns>
        //public string Filter(string text, char replaceChar = '*')
        //{
        //    if (string.IsNullOrEmpty(text))
        //    {
        //        return text;
        //    }
        //    return wordsSearch.Replace(text, replaceChar);
        //}

        ///// <summary>
        ///// 是否包含
        ///// </summary>
        ///// <param name="text"></param>
        ///// <returns></returns>
        //public bool IsVerified(string text)
        //{
        //    return wordsSearch.ContainsAny(text);
        //}
        #endregion

        /// <summary>
        /// 替换敏感词
        /// </summary>
        /// <param name="text"></param>
        /// <param name="replaceChar"></param>
        /// <returns></returns>
        public string Replace(string text, char replaceChar = '*')
        {
            if (text == null)
            {
                return text;
            }
            int textLength = text.Length;
            if (textLength == 0)
            {
                return text;
            }

            return Filter(text, textLength, replaceChar);
        }

        /// <summary>
        /// 是否包含敏感词
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool IsVerified(string text)
        {
            if (text == null)
            {
                return false;
            }
            int textLength = text.Length;
            if (textLength == 0)
            {
                return false;
            }

            return Filter(text, textLength);
        }

        #region 增强
        private static Dictionary<int, Dictionary<string, int>> WordParticiple { get; set; }

        private static List<int> WordLenLst { get; set; }

        private static int WordLenLstCount = 0;

        private static Dictionary<int, char> Punctuation = new Dictionary<int, char>();
        public void InitWordParticiple()
        {
            var lst = DAL.GetWords().ToList();
            WordLenLst = lst.GroupBy((d) => d.Len).Select((d) => d.Key).OrderByDescending((d) => d).ToList();
            WordParticiple = new Dictionary<int, Dictionary<string, int>>();
            foreach (var len in WordLenLst)
            {
                var lenGroupLst = lst.FindAll((d) => d.Len == len);
                Dictionary<string, int> dic = null;
                if (IsExistsLetter(lenGroupLst))
                {
                    dic = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    dic = new Dictionary<string, int>();
                }

                foreach (var item in lenGroupLst)
                {
                    dic.Add(item.Word, item.Len);
                }
                WordParticiple.Add(len, dic);
            }
            WordLenLstCount = WordLenLst.Count;
        }

        private bool IsExistsLetter(List<WordInfo> lst)
        {
            foreach (var wordInfo in lst)
            {
                if (Regex.Matches(wordInfo.Word, "[a-zA-Z]").Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 过滤
        /// </summary>
        /// <param name="text"></param>
        /// <param name="replaceChar"></param>
        /// <returns></returns>
        private string Filter(string text, int textLength, char replaceChar)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<int, char> punctuation = null;
            int newTextLen = 0;
            int firstIdx = 0;
            string str = null;

            if (!FilterWordMaxLen(text, str, textLength, GetWordFirstLen(textLength, out firstIdx), sb, ref punctuation, out newTextLen, replaceChar))
            {
                if (newTextLen == 0)
                {
                    return text;
                }
                text = sb.ToString();

                for (int m = firstIdx + 1; m < WordLenLstCount; m++)
                {
                    int len = WordLenLst[m];
                    var dic = WordParticiple[len];
                    for (int i = 0; i < newTextLen; i++)
                    {
                        if ((i + len) > newTextLen)
                        {
                            break;
                        }

                        str = text.Substring(i, len);

                        if (dic.ContainsKey(str))
                        {
                            //替换
                            for (int k = i; k < i + len; k++)
                            {
                                sb[k] = replaceChar;
                            }
                            //位移到下一块索引开始
                            i += len - 1;
                        }
                    }
                }

                if (punctuation != null)
                {
                    foreach (var item in punctuation)
                    {
                        sb.Insert(item.Key, item.Value);
                    }
                }
            }

            return sb.ToString();
        }

        private bool Filter(string text, int textLength)
        {
            StringBuilder sb = new StringBuilder();
            int newTextLen = 0;
            int firstIdx = 0;
            string str = null;

            if (FilterWordMaxLen(text, str, textLength, GetWordFirstLen(textLength, out firstIdx), sb, out newTextLen))
            {
                return true;
            }

            if (newTextLen == 0)
            {
                return false;
            }
            text = sb.ToString();

            for (int m = firstIdx + 1; m < WordLenLstCount; m++)
            {
                int len = WordLenLst[m];
                var dic = WordParticiple[len];
                for (int i = 0; i < newTextLen; i++)
                {
                    if ((i + len) > newTextLen)
                    {
                        break;
                    }

                    str = text.Substring(i, len);

                    if (dic.ContainsKey(str))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private int GetWordFirstLen(int textLength, out int idx)
        {
            idx = 0;
            int firstLen = 0;
            for (int i = 0; i < WordLenLstCount; i++)
            {
                firstLen = WordLenLst[i];
                if (firstLen <= textLength)
                {
                    idx = i;
                    break;
                }
            }
            return firstLen;
        }

        private bool FilterWordMaxLen(string text, string str, int textLength, int wordMaxLen, StringBuilder sb, ref Dictionary<int, char> punctuation, out int newTextLen, char replaceChar)
        {
            newTextLen = 0;
            int subMum = 0;
            var dic = WordParticiple[wordMaxLen];

            for (int i = 0; i < textLength; i++)
            {
                if ((i + wordMaxLen) > textLength)
                {
                    subMum = i;
                    break;
                }
                str = text.Substring(i, wordMaxLen);

                if (dic.ContainsKey(str))
                {
                    sb.Append(replaceChar, wordMaxLen);
                    return true;
                    //newTextLen += wordMaxLen;
                    ////位移到下一块索引开始
                    //i += wordMaxLen - 1;
                    //subMum = i + 1;
                }
                else
                {
                    char c = text[i];
                    //收集符号
                    if (IsPunctuation(c))
                    {
                        if (punctuation == null)
                        {
                            punctuation = new Dictionary<int, char>();
                        }
                        punctuation.Add(i, c);
                    }
                    else
                    {
                        sb.Append(c);
                        newTextLen += 1;
                    }
                }
            }

            if (subMum > 0 && subMum < textLength)
            {
                for (int i = subMum; i < textLength; i++)
                {
                    char c = text[i];
                    if (IsPunctuation(c))
                    {
                        if (punctuation == null)
                        {
                            punctuation = new Dictionary<int, char>();
                        }
                        punctuation.Add(i, c);
                    }
                    else
                    {
                        sb.Append(c);
                        newTextLen += 1;
                    }
                }
            }

            return false;
        }

        private bool FilterWordMaxLen(string text, string str, int textLength, int wordMaxLen, StringBuilder sb, out int newTextLen)
        {
            newTextLen = 0;
            int subMum = 0;
            var dic = WordParticiple[wordMaxLen];

            for (int i = 0; i < textLength; i++)
            {
                if ((i + wordMaxLen) > textLength)
                {
                    subMum = i;
                    break;
                }
                str = text.Substring(i, wordMaxLen);

                if (dic.ContainsKey(str))
                {
                    return true;
                }
                else
                {
                    char c = text[i];
                    //收集符号
                    if (!IsPunctuation(c))
                    {
                        sb.Append(c);
                        newTextLen += 1;
                    }
                }
            }

            if (subMum > 0 && subMum < textLength)
            {
                for (int i = subMum; i < textLength; i++)
                {
                    char c = text[i];
                    if (!IsPunctuation(c))
                    {
                        sb.Append(c);
                        newTextLen += 1;
                    }
                }
            }

            return false;
        }

        private bool IsPunctuation(char c)
        {
            return Char.IsPunctuation(c) ||
                Char.IsWhiteSpace(c) ||
                Char.IsDigit(c) ||
                Char.IsSeparator(c) ||
                Char.IsSymbol(c);

        }
        #endregion
    }
}