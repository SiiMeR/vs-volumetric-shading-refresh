using System;
using System.Text;
using System.Text.RegularExpressions;

namespace volumetricshadingupdated.VolumetricShading.Patch;

public class FunctionExtractor
{
    private static readonly Regex FunctionPrototypeRegex = new Regex("^\\S+\\s+([a-z_][a-z0-9_]*)\\s*\\([^)]*\\)\\s*{$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private readonly StringBuilder _sb = new StringBuilder();

    public string ExtractedContent => _sb.ToString();

    public bool Extract(string code, string functionName)
    {
        StringBuilder stringBuilder = new StringBuilder();
        int num = 0;
        bool flag = false;
        bool flag2 = false;
        bool flag3 = false;
        bool flag4 = false;
        char c = '\0';
        bool flag5 = true;
        bool flag6 = false;
        foreach (char c2 in code)
        {
            if (flag3)
            {
                char c3 = c2;
                if (c3 != '\n')
                {
                    if (c3 != '\\')
                    {
                        goto IL_0059;
                    }

                    flag4 = true;
                }
                else
                {
                    if (flag4)
                    {
                        goto IL_0059;
                    }

                    flag3 = false;
                }
            }
            else if (flag2)
            {
                if (c2 == '/' && c == '*')
                {
                    flag2 = false;
                }
            }
            else if (flag)
            {
                if (c2 == '\n')
                {
                    flag = false;
                }
            }
            else
            {
                if (num == 0 || flag6)
                {
                    stringBuilder.Append(c2);
                }

                switch (c2)
                {
                    case '#':
                        if (flag5)
                        {
                            flag3 = true;
                            stringBuilder.Remove(stringBuilder.Length - 1, 1);
                        }

                        break;
                    case '/':
                        if (c == '/')
                        {
                            flag = true;
                            stringBuilder.Remove(stringBuilder.Length - 2, 2);
                        }

                        break;
                    case '*':
                        if (c == '/')
                        {
                            flag2 = true;
                            stringBuilder.Remove(stringBuilder.Length - 2, 2);
                        }

                        break;
                    case ';':
                        if (num == 0)
                        {
                            stringBuilder.Clear();
                        }

                        break;
                    case '{':
                    {
                        if (num > 0)
                        {
                            num++;
                            break;
                        }

                        Match match = FunctionPrototypeRegex.Match(stringBuilder.ToString().Trim());
                        if (!match.Success)
                        {
                            throw new InvalidOperationException("Parsing error - function header doesn't match.");
                        }

                        flag6 = match.Groups[1].Value == functionName;
                        num++;
                        break;
                    }
                    case '}':
                        num--;
                        if (num < 0)
                        {
                            throw new InvalidOperationException("Depth got too low - parsing error");
                        }

                        if (num == 0)
                        {
                            if (flag6)
                            {
                                _sb.Append(stringBuilder);
                                _sb.Append('\n');
                                return true;
                            }

                            stringBuilder.Clear();
                        }

                        break;
                }
            }

            goto IL_01e8;
            IL_0059:
            if (c2 != '\r')
            {
                flag4 = false;
            }

            goto IL_01e8;
            IL_01e8:
            if (c2 == '\n')
            {
                flag5 = true;
            }
            else if (!char.IsWhiteSpace(c2))
            {
                flag5 = false;
            }

            c = c2;
        }

        return false;
    }
}